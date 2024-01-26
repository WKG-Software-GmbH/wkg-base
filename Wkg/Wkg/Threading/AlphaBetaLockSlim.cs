using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using Wkg.Threading.Workloads.Queuing.Classful.PrioFast;

namespace Wkg.Threading;

/// <summary>
/// Remeniscent of <see cref="ReaderWriterLockSlim"/> that supports multiple readers and multiple writers concurrently,
/// but does not allow simultaneous access by members of different groups, and does not allow upgrading from one group
/// to another. The semantics of the groups are application-defined, but typically a group represents a common type of
/// operation that is compatible with other operations in the same group but not with operations in other groups.
/// </summary>
internal class AlphaBetaLockSlim : IDisposable
{
    // Lock specification for _spinLock:  This lock protects exactly the local fields associated with this
    // instance of GroupLockSlim.  It does NOT protect the memory associated with
    // the events that are raised by this lock (eg writeEvent, readEvent upgradeEvent).
    private SpinLock _spinLock;

    // These variables allow use to avoid Setting events (which is expensive) if we don't have to.
    private uint _numAlphaWaiters;      // maximum number of threads that can be doing a WaitOne on the alphaEvent
    private uint _numBetaWaiters;       // maximum number of threads that can be doing a WaitOne on the betaEvent

    // conditions we wait on.
    private EventWaitHandle? _alphaEvent;       // threads waiting to acquire an alpha lock go here (will be released in bulk)
    private EventWaitHandle? _betaEvent;        // threads waiting to acquire a beta lock go here (will be released in bulk)

    // Every lock instance has a unique ID, which is used by AlphaBetaCount to associate itself with the lock
    // without holding a reference to it.
    private static long _globalNextLockId;
    private readonly long _lockId;

    [ThreadStatic]
    private static AlphaBetaCount? __counts;

    private const int MAX_SPIN_COUNT = 20;

    private AlphaBetaOwner _ownerGroup;

    // The ulong, that contains info about the lock, is divided as follows:
    //
    // Waiting-Alphas Num-Alphas  Waiting-Betas  Num-Betas
    //    63            62.....32      31        30.......0
    //
    // Dividing the ulong, allows to vastly simplify logic for checking if an
    // alpha/beta should go in etc. Setting any alpha bit will automatically
    // make the value of the ulong much larger than the max num of betas
    // allowed, thus causing the check for max_betas to fail, and causing
    // the beta to wait.
    private ulong _owners;

    private const ulong WAITING_ALPHAS =    0x8000000000000000;
    private const ulong ALPHA_COUNT_MASK =  0x7FFFFFFF00000000;

    private const ulong WAITING_BETAS =     0x0000000080000000;
    private const ulong BETA_COUNT_MASK =   0x000000007FFFFFFF;

    // The max numbers in each group are actually one less then their theoretical max.
    // This is done in order to prevent count overflows. If the count reaches max,
    // other members will wait.
    private const ulong MAX_BETAS = BETA_COUNT_MASK - 1;
    private const ulong MAX_ALPHAS = MAX_BETAS;

    private bool _disposedValue;

    public AlphaBetaLockSlim()
    {
        _ownerGroup = AlphaBetaOwner.None;
        _lockId = Interlocked.Increment(ref _globalNextLockId);
    }

    private bool HasNoWaiters
    {
        get
        {
            Debug.Assert(_spinLock.IsHeld);
            return _numAlphaWaiters == 0 && _numBetaWaiters == 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsABCEmpty(AlphaBetaCount abc) => 
        abc.lockId == 0 || abc.ownership == AlphaBetaOwner.None;

    private bool IsABCHashEntryChanged(AlphaBetaCount abc) => abc.lockId != _lockId;

    /// <summary>
    /// This routine retrieves/sets the per-thread counts needed to enforce the
    /// various rules related to acquiring the lock.
    ///
    /// DontAllocate is set to true if the caller just wants to get an existing
    /// entry for this thread, but doesn't want to add one if an existing one
    /// could not be found.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AlphaBetaCount? GetThreadABCount(bool dontAllocate)
    {
        AlphaBetaCount? empty = null;
        for (AlphaBetaCount? abc = __counts; abc != null; abc = abc.next)
        {
            // If we find an entry for this thread, return it.
            if (abc.lockId == _lockId)
            {
                return abc;
            }
            // re-use an empty ABC if we find one.
            if (!dontAllocate && empty == null && IsABCEmpty(abc))
            {
                empty = abc;
            }
        }

        if (dontAllocate)
        {
            return null;
        }

        if (empty == null)
        {
            empty = new AlphaBetaCount
            {
                next = __counts
            };
            __counts = empty;
        }

        empty.lockId = _lockId;
        return empty;
    }

    public void EnterBetaLock() => TryEnterBetaLock(Timeout.Infinite);

    public bool TryEnterBetaLock(TimeSpan timeout) => TryEnterBetaLockCore(new TimeoutTracker(timeout));

    public bool TryEnterBetaLock(int millisecondsTimeout) => TryEnterBetaLockCore(new TimeoutTracker(millisecondsTimeout));

    private bool TryEnterBetaLockCore(TimeoutTracker timeout)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        
        AlphaBetaCount abc = GetThreadABCount(dontAllocate: false)!;
        if (abc.ownership == AlphaBetaOwner.Alpha)
        {
            throw new InvalidOperationException("Beta lock cannot be acquired while holding an alpha lock.");
        }

        // Check if the beta lock is already acquired. Note, we could
        // check the presence of a reader by not allocating abc (But that
        // would lead to two lookups in the common case. It's better to keep
        // a count in the structure).
        if (abc.ownership == AlphaBetaOwner.Beta)
        {
            throw new LockRecursionException($"Lock recursion detected while trying to enter the beta lock. {nameof(AlphaBetaLockSlim)} does not support lock recursion.");
        }
        bool retVal = true;
        int spinCount = 0;
        _spinLock.Enter(EnterSpinLockReason.EnterBeta);
        while (true)
        {
            // We can enter a beta lock if there have only beta-locks been given out
            // and an alpha is not trying to get in.
            if (_owners < MAX_BETAS)
            {
                // Good case, there is no contention, we are basically done
                _owners++;       // Indicate we have another reader
                abc.ownership = AlphaBetaOwner.Beta;
                _ownerGroup = AlphaBetaOwner.Beta;
                break;
            }
            if (timeout.IsExpired)
            {
                // We timed out.  Fail.
                _spinLock.Exit();
                return false;
            }

            if (spinCount < MAX_SPIN_COUNT && ShouldSpinForEnterBeta())
            {
                _spinLock.Exit();
                spinCount++;
                SpinWait(spinCount);
                _spinLock.Enter(EnterSpinLockReason.EnterBeta);
                // The per-thread structure may have been recycled as the lock is acquired (due to message pumping), load again.
                if (IsABCHashEntryChanged(abc))
                {
                    abc = GetThreadABCount(dontAllocate: false)!;
                }
                continue;
            }
            // Drat, we need to wait. Mark that we have waiters and wait.
            if (_betaEvent == null)
            {
                // Create the needed event (temporarily leaving the lock during wait handle creation)
                LazyCreateEvent(ref _betaEvent, isAcquiringBetaLock: true);
                if (IsABCHashEntryChanged(abc))
                {
                    abc = GetThreadABCount(dontAllocate: false)!;
                }
                // since we left the lock, start over.
                continue;
            }

            retVal = WaitOnEvent(_betaEvent, ref _numBetaWaiters, timeout, isAcquiringBetaLock: true);
            if (!retVal)
            {
                return false;
            }
            if (IsABCHashEntryChanged(abc))
            {
                abc = GetThreadABCount(dontAllocate: false)!;
            }
        }

        _spinLock.Exit();
        return retVal;
    }

    public void EnterAlphaLock() => TryEnterAlphaLock(Timeout.Infinite);

    public bool TryEnterAlphaLock(TimeSpan timeout) => TryEnterAlphaLockCore(new TimeoutTracker(timeout));

    public bool TryEnterAlphaLock(int millisecondsTimeout) => TryEnterAlphaLockCore(new TimeoutTracker(millisecondsTimeout));

    private bool TryEnterAlphaLockCore(TimeoutTracker timeout)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        AlphaBetaCount? abc = GetThreadABCount(dontAllocate: true);
        // Can't acquire alpha lock with beta lock held.
        if (abc != null)
        {
            _ = __ switch
            {
                AlphaBetaOwner.Beta => throw new InvalidOperationException("Alpha lock cannot be acquired while holding a beta lock."),
                AlphaBetaOwner.Alpha => throw new LockRecursionException($"Lock recursion detected while trying to enter the alpha lock. {nameof(AlphaBetaLockSlim)} does not support lock recursion."),
                _ => __
            };
        }

        _spinLock.Enter(EnterSpinLockReason.EnterAlpha);
        bool retVal;
        int spinCount = 0;

        while (true)
        {
            if (GetNumBetas() == 0)
            {
                // Good case, there is no contention, we are basically done
                break;
            }
            // there are beta locks out there.  We need to contend with them.
            if (timeout.IsExpired)
            {
                // well, unless we timed out.
                _spinLock.Exit();
                return false;
            }
            if (spinCount < MAX_SPIN_COUNT)
            {
                _spinLock.Exit();
                spinCount++;
                SpinWait(spinCount);
                _spinLock.Enter(EnterSpinLockReason.EnterAlpha);
                continue;
            }
            // Drat, we need to wait. Mark that we have waiters and wait.
            if (_alphaEvent == null)
            {
                // Create the needed event (temporarily leaving the lock during wait handle creation)
                LazyCreateEvent(ref _alphaEvent, isAcquiringBetaLock: false);
                continue;   // since we left the lock, start over.
            }
            // wait for the event to be set.
            retVal = WaitOnEvent(_alphaEvent, ref _numAlphaWaiters, timeout, isAcquiringBetaLock: false);
            // The lock is not held in case of failure.
            if (!retVal)
            {
                return false;
            }
        }
        // need to increment the number of alphas out there. (before releasing the lock)
        SignalAlphaLockAcquisition();
        _ownerGroup = AlphaBetaOwner.Alpha;
        _spinLock.Exit();

        return true;
    }

    public void ExitBetaLock()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        AlphaBetaCount? abc = GetThreadABCount(dontAllocate: true);
        if (abc == null || abc.ownership != AlphaBetaOwner.Beta || _ownerGroup != AlphaBetaOwner.Beta)
        {
            Debug.Assert(_ownerGroup == AlphaBetaOwner.Beta, "Internal inconsistency, exiting beta lock when not holding it");
            // You have to be holding the beta lock to make this call.
            throw new SynchronizationLockException("Beta lock cannot be released while not holding it.");
        }
        Debug.Assert(GetNumBetas() > 0, "ReleasingBetaLock: releasing lock and no beta lock taken");

        _spinLock.Enter(EnterSpinLockReason.ExitBeta);
        SignalBetaLockRelease();
        abc.ownership = AlphaBetaOwner.None;
        ExitAndWakeUpAppropriateWaiters();
    }

    public void ExitAlphaLock()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        AlphaBetaCount? abc = GetThreadABCount(dontAllocate: true);
        if (abc == null || abc.ownership != AlphaBetaOwner.Alpha || _ownerGroup != AlphaBetaOwner.Alpha)
        {
            Debug.Assert(_ownerGroup == AlphaBetaOwner.Alpha, "Internal inconsistency, exiting alpha lock when not holding it");
            // You have to be holding the alpha lock to make this call.
            throw new SynchronizationLockException("Alpha lock cannot be released while not holding it.");
        }
        Debug.Assert(GetNumAlphas() > 0, "Calling ReleaseAlphaLock when no alpha lock is held");

        _spinLock.Enter(EnterSpinLockReason.ExitAlpha);
        SignalAlphaLockRelease();
        abc.ownership = AlphaBetaOwner.None;
        ExitAndWakeUpAppropriateWaiters();
    }

    /// <summary>
    /// A routine for lazily creating a event outside the lock (so if errors
    /// happen they are outside the lock and that we don't do much work
    /// while holding a spin lock).  If all goes well, reenter the lock and
    /// set 'waitEvent'
    /// </summary>
    private void LazyCreateEvent([NotNull] ref EventWaitHandle? waitEvent, bool isAcquiringBetaLock)
    {
#if DEBUG
        Debug.Assert(_spinLock.IsHeld);
        Debug.Assert(waitEvent == null);
#endif
        _spinLock.Exit();

        EventWaitHandle newEvent = new(initialState: false, EventResetMode.ManualReset);

        EnterSpinLockReason enterMyLockReason = isAcquiringBetaLock
            ? EnterSpinLockReason.EnterBeta | EnterSpinLockReason.Wait
            : EnterSpinLockReason.EnterAlpha | EnterSpinLockReason.Wait;

        _spinLock.Enter(enterMyLockReason);
        // maybe someone snuck in.
        if (waitEvent == null)          
        {
            waitEvent = newEvent;
        }
        else
        {
            newEvent.Dispose();
        }
    }

    /// <summary>
    /// Waits on 'waitEvent' with a timeout
    /// Before the wait 'numWaiters' is incremented and is restored before leaving this routine.
    /// </summary>
    private bool WaitOnEvent(EventWaitHandle waitEvent, ref uint numWaiters, TimeoutTracker timeout, bool isAcquiringBetaLock)
    {
#if DEBUG
        Debug.Assert(_spinLock.IsHeld);
#endif
        EnterSpinLockReason enterMyLockReason = isAcquiringBetaLock
            ? EnterSpinLockReason.EnterBeta
            : EnterSpinLockReason.EnterAlpha;

        waitEvent.Reset();
        numWaiters++;

        // Setting this bit will prevent new betas from getting in.
        if (_numAlphaWaiters == 1)
        {
            SetAlphasWaiting();
        }
        // TODO: maybe we can remove this
        if (_numBetaWaiters == 1)
        {
            SetBetasWaiting();
        }
        bool waitSuccessful = false;
        // Do the wait outside of any lock
        _spinLock.Exit();

        try
        {
            waitSuccessful = waitEvent.WaitOne(timeout.RemainingMilliseconds);
        }
        finally
        {
            _spinLock.Enter(enterMyLockReason);
            numWaiters--;

            if (_numAlphaWaiters == 0)
            {
                ClearAlphasWaiting();
            }
            // TODO: maybe we can remove this
            if (_numBetaWaiters == 0)
            {
                ClearBetasWaiting();
            }
            // We may also be about to throw for some reason.  Exit myLock.
            if (!waitSuccessful)
            {
                if (!isAcquiringBetaLock)
                {
                    // Alpha waiters block beta waiters from acquiring the lock. Since this was the last alpha waiter, try
                    // to wake up the appropriate beta waiters.
                    ExitAndWakeUpAppropriateBetaWaiters();
                }
                else
                {
                    _spinLock.Exit();
                }
            }
        }
        return waitSuccessful;
    }

    private void ExitAndWakeUpAppropriateBetaWaiters()
    {
#if DEBUG
        Debug.Assert(_spinLock.IsHeld);
#endif
        // if there are other alphas waiting or there are no betas waiting, we are done.
        if (_numAlphaWaiters != 0 || _numBetaWaiters == 0)
        {
            _spinLock.Exit();
            return;
        }
        Debug.Assert(_numBetaWaiters != 0);

        bool setBetaEvent = _numBetaWaiters != 0;
        // Exit before signaling to improve efficiency (wakee will need the lock)
        _spinLock.Exit();

        if (setBetaEvent)
        {
            // release all betas. Known non-null because _numBetaWaiters != 0.
            _betaEvent!.Set();  
        }
    }

    private void SetAlphasWaiting() => _owners |= WAITING_ALPHAS;

    private void ClearAlphasWaiting() => _owners &= ~WAITING_ALPHAS;

    private void SetBetasWaiting() => _owners |= WAITING_BETAS;

    private void ClearBetasWaiting() => _owners &= ~WAITING_BETAS;

    private uint GetNumBetas() => (uint)(_owners & BETA_COUNT_MASK);

    private void SignalBetaLockAcquisition() => _owners++;

    private void SignalBetaLockRelease() => _owners--;

    private uint GetNumAlphas() => (uint)((_owners & ALPHA_COUNT_MASK) >>> 32);

    private void SignalAlphaLockAcquisition() => _owners += 0x1_0000_0000;

    private void SignalAlphaLockRelease() => _owners -= 0x1_0000_0000;

    private bool ShouldSpinForEnterBeta() =>
        // If there are more than 2 alphas, the beta is not likely to make progress by spinning.
        // Although other threads holding an alpha lock would prevent this thread from acquiring a beta lock, it is by
        // itself not a good enough reason to skip spinning.
        GetNumAlphas() <= 2;

    private static void SpinWait(int spinCount)
    {
        const int LOCK_SPIN_CYCLES = 20;

        // Exponential back-off
        if ((spinCount < 5) && (Environment.ProcessorCount > 1))
        {
            Thread.SpinWait(LOCK_SPIN_CYCLES * spinCount);
        }
        else
        {
            Thread.Sleep(0);
        }

        // Don't want to Sleep(1) in this spin wait:
        //   - Don't want to spin for that long, since a proper wait will follow when the spin wait fails. The artificial
        //     delay introduced by Sleep(1) will in some cases be much longer than desired.
        //   - Sleep(1) would put the thread into a wait state, and a proper wait will follow when the spin wait fails
        //     anyway, so it's preferable to put the thread into the proper wait state
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private struct SpinLock
    {
        private const int LOCKED = 1;
        private const int UNLOCKED = 0;

        private int _lockState;

        /// <summary>
        /// Used to deprioritize threads attempting to enter the lock when they would not make progress after doing so.
        /// <see cref="EnterSpin(EnterSpinLockReason)"/> avoids acquiring the lock as long as the operation for which it
        /// was called is deprioritized.
        ///
        /// Layout:
        /// - Low 16 bits: Number of threads that have deprioritized an enter-any-alpha operation
        /// - High 16 bits: Number of threads that have deprioritized an enter-any-beta operation
        /// </summary>
        private int _enterDeprioritizationState;

        // Layout-specific constants for _enterDeprioritizationState
        private const int DEPRIORITIZE_ENTER_BETA_INCREMENT = 1 << 16;
        private const int DEPRIORITIZE_ENTER_ALPHA_INCREMENT = 1;

        // The variables controlling spinning behavior of this spin lock
        private const int LOCK_SPIN_CYCLES = 20;
        private const int LOCK_SLEEP0_SPIN_THRESHOLD = 10;
        private const int REPRIORITIZE_LOCK_SPIN_THRESHOLD = LOCK_SLEEP0_SPIN_THRESHOLD + 60;

        private static int GetEnterDeprioritizationStateChange(EnterSpinLockReason reason)
        {
            EnterSpinLockReason operation = reason & EnterSpinLockReason.OperationMask;
            switch (operation)
            {
                case EnterSpinLockReason.EnterBeta:
                    return 0;

                case EnterSpinLockReason.ExitBeta:
                    // A beta lock is held until this thread is able to exit it, so deprioritize enter-alpha threads as they
                    // will not be able to make progress
                    return DEPRIORITIZE_ENTER_ALPHA_INCREMENT;

                case EnterSpinLockReason.EnterAlpha:
                    // Waiting alphas take precedence over new beta access attempts in order to let current betas release
                    // their lock and allow the alphas to obtain the lock. Before an alpha can register as a waiter though,
                    // the presence of just relatively few enter-beta spins can easily starve the enter-alpha from even
                    // entering this lock, delaying its spin loop for an unreasonable duration.
                    //
                    // Deprioritize enter-beta to preference enter-alpha. This makes it easier for enter-alpha threads to
                    // starve enter-beta threads. However, writers can already by design starve readers. A waiting writer
                    // blocks enter-beta threads and a new enter-alpha that needs to wait will be given precedence over
                    // previously waiting enter-beta threads.
                    return DEPRIORITIZE_ENTER_BETA_INCREMENT;

                default:
                    Debug.Assert(operation == EnterSpinLockReason.ExitAlpha);

                    // ExitAlpha:
                    // - an alpha lock is held until this thread is able to exit it, so deprioritize
                    //   enter-beta threads as they will not be able to make progress
                    return DEPRIORITIZE_ENTER_BETA_INCREMENT;
            }
        }

        private readonly ushort EnterForEnterBetaDeprioritizedCount
        {
            get
            {
                Debug.Assert(DEPRIORITIZE_ENTER_BETA_INCREMENT == (1 << 16));
                return (ushort)(_enterDeprioritizationState >>> 16);
            }
        }

        private readonly ushort EnterForEnterAlphaDeprioritizedCount
        {
            get
            {
                Debug.Assert(DEPRIORITIZE_ENTER_ALPHA_INCREMENT == 1);
                return (ushort)_enterDeprioritizationState;
            }
        }

        private readonly bool IsEnterDeprioritized(EnterSpinLockReason reason)
        {
            Debug.Assert((reason & EnterSpinLockReason.Wait) != 0 || reason == (reason & EnterSpinLockReason.OperationMask));
            Debug.Assert(
                (reason & EnterSpinLockReason.Wait) == 0 ||
                (reason & EnterSpinLockReason.OperationMask) == EnterSpinLockReason.EnterBeta ||
                (reason & EnterSpinLockReason.OperationMask) == EnterSpinLockReason.EnterAlpha);

            switch (reason)
            {
                default:
                    Debug.Assert(
                        (reason & EnterSpinLockReason.Wait) != 0 ||
                        reason == EnterSpinLockReason.ExitBeta ||
                        reason == EnterSpinLockReason.ExitAlpha);
                    return false;

                case EnterSpinLockReason.EnterBeta:
                    return EnterForEnterBetaDeprioritizedCount != 0;

                case EnterSpinLockReason.EnterAlpha:
                    Debug.Assert((GetEnterDeprioritizationStateChange(reason) & DEPRIORITIZE_ENTER_ALPHA_INCREMENT) == 0);
                    return EnterForEnterAlphaDeprioritizedCount != 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryEnter() => Interlocked.CompareExchange(ref _lockState, LOCKED, UNLOCKED) == UNLOCKED;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter(EnterSpinLockReason reason)
        {
            if (!TryEnter())
            {
                EnterSpin(reason);
            }
        }

        private void EnterSpin(EnterSpinLockReason reason)
        {
            int deprioritizationStateChange = GetEnterDeprioritizationStateChange(reason);
            if (deprioritizationStateChange != 0)
            {
                Interlocked.Add(ref _enterDeprioritizationState, deprioritizationStateChange);
            }

            for (uint spinIndex = 0; ; spinIndex++)
            {
                // - Spin-wait until the Sleep(0) threshold
                // - Beyond the Sleep(0) threshold, alternate between sleeping and spinning. Avoid using only Sleep(0), as
                //   it may be ineffective when there are no other threads waiting to run. Thread.SpinWait() is not used
                //   where there is a single processor since it would be unlikely for a meaningful change in state to occur
                //   during that.
                // - Don't Sleep(1) here, as it can lead to long latencies in the alpha/beta lock operations
                if ((spinIndex < LOCK_SLEEP0_SPIN_THRESHOLD || (spinIndex - LOCK_SLEEP0_SPIN_THRESHOLD) % 2 != 0) &&
                    Environment.ProcessorCount != 1)
                {
                    Thread.SpinWait(
                        LOCK_SPIN_CYCLES *
                        (spinIndex < LOCK_SLEEP0_SPIN_THRESHOLD ? (int)spinIndex + 1 : LOCK_SLEEP0_SPIN_THRESHOLD));
                }
                else
                {
                    Thread.Sleep(0);
                }

                if (!IsEnterDeprioritized(reason))
                {
                    if (_lockState == UNLOCKED && TryEnter())
                    {
                        if (deprioritizationStateChange != 0)
                        {
                            Interlocked.Add(ref _enterDeprioritizationState, -deprioritizationStateChange);
                        }
                        return;
                    }
                    continue;
                }

                // It's possible for an Enter thread to be deprioritized for an extended duration. It's undesirable for a
                // deprioritized thread to keep spin-waiting for the lock when a large number of such threads are involved.
                // After a threshold, ignore the deprioritization and enter this lock to allow this thread to stop spinning
                // and hopefully enter a proper wait state.
                Debug.Assert(reason is EnterSpinLockReason.EnterBeta or EnterSpinLockReason.EnterAlpha);
                if (spinIndex >= REPRIORITIZE_LOCK_SPIN_THRESHOLD)
                {
                    reason |= EnterSpinLockReason.Wait;
                    spinIndex = uint.MaxValue;
                }
            }
        }

        public void Exit()
        {
            Debug.Assert(_lockState != UNLOCKED, "Exiting spin lock that is not held");
            Volatile.Write(ref _lockState, UNLOCKED);
        }

#if DEBUG
        public readonly bool IsHeld => _lockState != UNLOCKED;
#endif
    }

    private enum EnterLockType
    {
        Beta,
        Alpha,
    }

    private enum EnterSpinLockReason
    {
        EnterBeta = 0,
        ExitBeta = 1,
        EnterAlpha = 2,
        ExitAlpha = 5,

        OperationMask = 0x7,

        Wait = 0x8
    }

    [Flags]
    private enum WaiterStates : byte
    {
        None = 0x0,
        NoWaiters = 0x1
    }
}

internal enum AlphaBetaOwner
{
    None,
    Alpha,
    Beta
}

//
// AlphaBetaCount tracks how many of each kind of lock is held by each thread.
// We keep a linked list for each thread, attached to a ThreadStatic field.
// These are reused wherever possible, so that a given thread will only
// allocate N of these, where N is the maximum number of locks held simultaneously
// by that thread.
//
internal sealed class AlphaBetaCount
{
    // Which lock does this object belong to?  This is a numeric ID for two reasons:
    // 1) We don't want this field to keep the lock object alive, and a WeakReference would
    //    be too expensive.
    // 2) Setting the value of a long is faster than setting the value of a reference.
    //    The "hot" paths in GroupLockSlim are short enough that this actually
    //    matters.
    public long lockId;

    // Does this thread own any locks?
    public AlphaBetaOwner ownership;

    // Next ABC in this thread's list.
    public AlphaBetaCount? next;
}