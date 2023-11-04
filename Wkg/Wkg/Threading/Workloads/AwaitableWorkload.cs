using System.Diagnostics;
using Wkg.Common.ThrowHelpers;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Continuations;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

/// <summary>
/// Represents a workload that can be awaited and supports cancellation.
/// </summary>
public abstract class AwaitableWorkload : AbstractWorkloadBase
{
    private protected static readonly IQdisc _qdiscCompletionSentinel = new QdiscCompletionSentinel();
    private protected IQdisc? _qdisc;

    // result fields
    private protected Exception? _exception;
    private protected CancellationTokenRegistration? _cancellationTokenRegistration;

    // continuations
    private static readonly object _workloadCompletionSentinel = new();
    private protected object? _continuation;
    private readonly WorkloadContextOptions _continuationOptions;

    private protected AwaitableWorkload(WorkloadStatus status, WorkloadContextOptions continuationOptions, CancellationToken cancellationToken) : base(status)
    {
        if (cancellationToken.CanBeCanceled)
        {
            _cancellationTokenRegistration = cancellationToken.Register(OnCancellationTokenFired);
        }
        _continuationOptions = continuationOptions;
    }

    private protected void OnCancellationTokenFired()
    {
        DebugLog.WriteDiagnostic($"{this}: cancellation token registration indicates cancellation request.", LogWriter.Blocking);
        TryCancel();
    }

    /// <summary>
    /// Indicates whether the result has been set and that the workload is in any of the terminal states: <see cref="WorkloadStatus.RanToCompletion"/>, <see cref="WorkloadStatus.Faulted"/>, or <see cref="WorkloadStatus.Canceled"/>.
    /// </summary>
    public override bool IsCompleted => base.IsCompleted && ContinuationsInvoked;

    internal bool ContinuationsInvoked => ReferenceEquals(Volatile.Read(ref _continuation), _workloadCompletionSentinel);

    /// <summary>
    /// Attempts to transition the workload to the <see cref="WorkloadStatus.Canceled"/> state.
    /// </summary>
    /// <returns><see langword="true"/> if the workload was successfully canceled; <see langword="false"/> if the workload has already completed or is in an uncancelable state.</returns>
    public bool TryCancel()
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to cancel workload.", LogWriter.Blocking);
        // fast and easy path. we can simply check against the base implementation as we don't care about the result
        if (base.IsCompleted)
        {
            // already completed, nothing to do
            DebugLog.WriteDiagnostic($"{this}: Workload is already completed, nothing to do.", LogWriter.Blocking);
            return false;
        }
        // try to abort from pre-execution state
        if (Atomic.TryTestAnyFlagsExchange(ref _status, WorkloadStatus.Canceled, CommonFlags.PreExecution))
        {
            DebugLog.WriteDiagnostic($"{this}: Successfully canceled workload from pre-execution state.", LogWriter.Blocking);
            // attempt to remove the workload from the qdisc
            IQdisc? qdisc, resampledQdisc;
            do
            {
                qdisc = Volatile.Read(ref _qdisc);
                if (qdisc is null)
                {
                    DebugLog.WriteDiagnostic($"{this}: Workload is not bound to a qdisc, nothing to do.", LogWriter.Blocking);
                    // no qdisc, we're done
                    return true;
                }
                // attempt to remove the workload from the qdisc
                if (qdisc.TryRemoveInternal(this))
                {
                    // we successfully removed the workload from the qdisc, we're done
                    // detach from the qdisc (only after we successfully removed the workload from it)
                    UnbindQdiscUnsafe();
                    DebugLog.WriteDiagnostic($"{this}: Successfully removed workload from qdisc.", LogWriter.Blocking);
                    return true;
                }
                // we failed to remove the workload from the qdisc.
                // either the qdisc doesn't support removal of workloads, or the workload was already dequeued into another qdisc
                // we'll try to resample the qdisc and try again
                resampledQdisc = Volatile.Read(ref _qdisc);
            } while (!ReferenceEquals(qdisc, resampledQdisc));
            // we failed to remove the workload from the qdisc, so the qdisc must not support removal of workloads
            // this is valid. the workload will just be ignored when it is dequeued
            DebugLog.WriteDiagnostic($"{this}: Failed to remove workload from qdisc. The qdisc does not support removal of workloads.", LogWriter.Blocking);
            return true;
        }
        // didn't work. We're either running or invalid, request cancellation
        // we can only request cancellation but can't guarantee it will be honored
        if (Interlocked.CompareExchange(ref _status, WorkloadStatus.CancellationRequested, WorkloadStatus.Running) == WorkloadStatus.Running)
        {
            // it is the responsibility of the workload to check for cancellation
            DebugLog.WriteDiagnostic($"{this}: Successfully requested cancellation of workload.", LogWriter.Blocking);
            return true;
        }
        DebugLog.WriteDiagnostic($"{this}: Failed to request cancellation of workload.", LogWriter.Blocking);
        // invalid state or workload just completed
        return false;
    }

    internal bool InternalTryMarkAborted() =>
        Interlocked.CompareExchange(ref _status, WorkloadStatus.Canceled, WorkloadStatus.CancellationRequested) == WorkloadStatus.CancellationRequested;

    internal override void InternalAbort()
    {
        DebugLog.WriteDiagnostic($"{this}: Forcing internal cancellation.", LogWriter.Blocking);
        // we're forcing cancellation, so we can just set the status to canceled
        if (Atomic.TryTestAnyFlagsExchange(ref _status, WorkloadStatus.Canceled, ~CommonFlags.Completed))
        {
            DebugLog.WriteDiagnostic($"{this}: Successfully forced internal cancellation.", LogWriter.Blocking);
            SetCanceledResultUnsafe();
            InternalRunContinuations();
        }
        else
        {
            DebugLog.WriteDiagnostic($"{this}: Failed to force internal cancellation. Status is '{Status}'.", LogWriter.Blocking);
        }
    }

    internal override bool TryInternalBindQdisc(IQdisc qdisc)
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to bind workload to qdisc.", LogWriter.Blocking);
        // sample the current qdisc
        IQdisc? current = Volatile.Read(ref _qdisc);
        // if the sentine is set, we cannot bind
        if (ReferenceEquals(current, _qdiscCompletionSentinel))
        {
            DebugLog.WriteDiagnostic($"{this}: Failed to bind workload to qdisc. The qdisc is in a completion state and the sentinel is set.", LogWriter.Blocking);
            return false;
        }
        // attempt to transition to the scheduled state
        if (Atomic.TryTestAnyFlagsExchange(ref _status, WorkloadStatus.Scheduled, WorkloadStatus.Created | WorkloadStatus.Scheduled))
        {
            // successfully transitioned to the scheduled state
            // now just try to CAS the qdisc in there
            // if we fail, then another thread was faster and we should just return
            // it is also possible that the workload is already running, or has just been canceled
            // in any way, we try our best, and if it doesn't work, we just give up
            bool result = ReferenceEquals(Interlocked.CompareExchange(ref _qdisc, qdisc, current), current);
            if (result)
            {
                DebugLog.WriteDiagnostic($"{this}: Successfully bound workload to qdisc.", LogWriter.Blocking);
            }
            else
            {
                DebugLog.WriteWarning($"{this}: Failed to bind workload to qdisc. The workload is already running, has been canceled, or another thread was faster.", LogWriter.Blocking);
            }
            return result;
        }
        // the workload could have been initialized with a canceled cancellation token
        // in that case, we can give up and must set the result
        WorkloadStatus status = Status;
        if (status == WorkloadStatus.Canceled)
        {
            DebugLog.WriteDiagnostic($"{this}: Failed to bind workload to qdisc. The workload was canceled before it could be bound.", LogWriter.Blocking);
            SetCanceledResultUnsafe();
        }
        else
        {
            // this is weird.
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Workload is in an invalid state. This is a bug. Status was '{status}' during binding attempt.");
            // mark the workload as faulted
            Volatile.Write(ref _status, WorkloadStatus.Faulted);
            SetFaultedResultUnsafe(exception);
        }
        // run continuations
        InternalRunContinuations();
        return false;
    }

    private protected abstract bool TryExecuteUnsafeCore(out WorkloadStatus preTerminationStatus);

    private protected abstract void SetFaultedResultUnsafe(Exception ex);

    private protected abstract void SetCanceledResultUnsafe();

    /// <inheritdoc/>
    internal override bool TryRunSynchronously()
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to execute workload.", LogWriter.Blocking);
        WorkloadStatus preTerminationStatus;
        if (Interlocked.CompareExchange(ref _status, WorkloadStatus.Running, WorkloadStatus.Scheduled) == WorkloadStatus.Scheduled)
        {
            // we successfully transitioned to the running state
            DebugLog.WriteDiagnostic($"{this}: Successfully transitioned to running state.", LogWriter.Blocking);
            UnbindQdiscUnsafe();
            try
            {
                if (!TryExecuteUnsafeCore(out preTerminationStatus))
                {
                    // this should never happen
                    DebugLog.WriteWarning($"{this}: Workload is in an invalid state. This is a bug. Status was '{preTerminationStatus}' after execution.", LogWriter.Blocking);
                    goto FAILURE;
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Canceled);
                SetCanceledResultUnsafe();
                DebugLog.WriteDiagnostic($"{this}: Execution was canceled with an OperationCanceledException.", LogWriter.Blocking);
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                SetFaultedResultUnsafe(ex);
                DebugLog.WriteDiagnostic($"{this}: Execution finished faulted with '{ex.GetType().Name}: {ex.Message}'", LogWriter.Blocking);
            }
            return true;
        }
    FAILURE:
        // not all qdiscs may support removal of canceled workloads)
        if ((preTerminationStatus = Volatile.Read(ref _status)) != WorkloadStatus.Canceled)
        {
            // log this occurrence as it should never happen
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Workload is in an invalid state. This is a bug. Encountered Status '{preTerminationStatus}' during execution attempt.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            // notify the caller that the workload could not be executed
            // they might be awaiting the workload, so we need to set the result if it's not already set
            if (!IsCompleted)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                SetFaultedResultUnsafe(exception);
            }
        }
        // in any case, we couldn't execute the workload due to some scheduling issue
        // returning false will allow the scheduler to back-track and try again from the previous state if possible
        return false;
    }

    /// <summary>
    /// Attempts to unbind the workload from the specified qdisc. 
    /// This method requires that this workload has already been dequeued from the qdisc.
    /// </summary>
    private void UnbindQdiscUnsafe() => Volatile.Write(ref _qdisc, _qdiscCompletionSentinel);

    internal void AddOrRunInlineContinuationAction(object continuation, bool scheduleBeforeOthers = false)
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to add or run inline continuation.", LogWriter.Blocking);
        if (!TryAddContinuation(continuation, scheduleBeforeOthers))
        {
            DebugLog.WriteDiagnostic($"{this}: Failed to add continuation. Executing inline.", LogWriter.Blocking);
            if (continuation is Action action)
            {
                action.Invoke();
            }
            else if (continuation is IWorkloadContinuation wc)
            {
                wc.Invoke(this);
            }
            else
            {
                DebugLog.WriteException(WorkloadSchedulingException.CreateVirtual($"Invalid continuation type '{continuation.GetType().Name}'. This is a bug. Please report this issue."), LogWriter.Blocking);
            }
        }
    }

    internal void AddOrRunContinuation(IWorkloadContinuation continuation, bool scheduleBeforeOthers = false)
    {
        if (!TryAddContinuation(continuation, scheduleBeforeOthers))
        {
            DebugLog.WriteDiagnostic($"{this}: Failed to add continuation. Invoking on context.", LogWriter.Blocking);
            continuation.Invoke(this);
        }
    }

    internal void SetContinuationForAwait(Action continuationAction)
    {
        Debug.Assert(continuationAction != null);
        DebugLog.WriteDiagnostic($"{this}: Setting continuation for await.", LogWriter.Blocking);

        IWorkloadContinuation wc;

        // check if we need to capture the current synchronization context
        // we only capture it if it's not the default one and if the caller requested it
        if (_continuationOptions.ContinueOnCapturedContext 
            && SynchronizationContext.Current is SynchronizationContext syncContext 
            && syncContext.GetType() != typeof(SynchronizationContext))
        {
            DebugLog.WriteDiagnostic($"{this}: Capturing synchronization context for continuation.", LogWriter.Blocking);
            wc = new SynchronizationContextAwareWorkloadAwaiterContinuation(continuationAction, syncContext, _continuationOptions.FlowExecutionContext);
        }
        else
        {
            // otherwise, capture the execution context if requested or just post the continuation to the thread pool
            DebugLog.WriteDiagnostic(_continuationOptions.FlowExecutionContext
                ? $"{this}: Flowing execution context for continuation."
                : $"{this}: Not flowing execution context for continuation.", LogWriter.Blocking);
            wc = new WorkloadAwaiterContinuation(continuationAction, _continuationOptions.FlowExecutionContext);
        }
        // try to add the continuation or run it inline if we failed (inlining will work because we are on the caller's thread)
        AddOrRunContinuation(wc, scheduleBeforeOthers: false);
    }

    internal bool TryAddContinuation(object continuation, bool scheduleBeforeOthers)
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to add continuation.", LogWriter.Blocking);
        // fast and easy path
        if (IsCompleted)
        {
            DebugLog.WriteDiagnostic($"{this}: Workload is already completed, not scheduling continuation.", LogWriter.Blocking);
            // already completed, nothing to do
            return false;
        }
        // attempt to simply CAS the continuation in
        object? currentContinuation = Volatile.Read(ref _continuation);
        if (currentContinuation == null && Interlocked.CompareExchange(ref _continuation, continuation, null) == null)
        {
            DebugLog.WriteDiagnostic($"{this}: Successfully set continuation.", LogWriter.Blocking);
            // great, we were the first to set the continuation
            return true;
        }
        DebugLog.WriteDiagnostic($"{this}: Continuation already set, attempting to append to list.", LogWriter.Blocking);
        // we failed to set the continuation.
        // we'll have to do it the hard way
        return TryAddContinuationComplex(continuation, scheduleBeforeOthers);
    }

    internal bool TryAddContinuationComplex(object continuation, bool scheduleBeforeOthers)
    {
        // take a snapshot of the current continuation
        object? currentContinuation = Volatile.Read(ref _continuation);
        Debug.Assert(currentContinuation != null);

        // we know that the current continuation is either an simple continuation action, a list of continuation actions, or the sentinel
        // if it's a simple continuation action, we'll have to upgrade it to a list of continuation actions
        if (!ReferenceEquals(currentContinuation, _workloadCompletionSentinel) && currentContinuation is not List<object?>)
        {
            DebugLog.WriteDiagnostic($"{this}: Upgrading continuation to list.", LogWriter.Blocking);
            // create a new list of continuation actions and try to CAS it in
            Interlocked.CompareExchange(ref _continuation, new List<object?> { currentContinuation }, currentContinuation);

            // we either successfully upgraded the continuation to a list, or someone else did it before us
            // it is also possible that the sentinel was set in the meantime
        }
        // current continuation is now either a list of continuation actions, or the sentinel
        // resample the current continuation
        currentContinuation = Volatile.Read(ref _continuation);

        Debug.Assert(ReferenceEquals(currentContinuation, _workloadCompletionSentinel) || currentContinuation is List<object?>);

        // if it's a list, we'll try to add the continuation to it
        if (currentContinuation is List<object?> list)
        {
            lock (list)
            {
                // it could be that the sentinel was set in the meantime while we were busy acquiring the lock
                if (!ReferenceEquals(Volatile.Read(ref _continuation), _workloadCompletionSentinel))
                {
                    if (list.Count == list.Capacity)
                    {
                        DebugLog.WriteDiagnostic($"{this}: List is about to grow, running cleanup.", LogWriter.Blocking);
                        list.RemoveAll(o => o == null);
                    }

                    DebugLog.WriteDiagnostic($"{this}: Adding continuation to list.", LogWriter.Blocking);
                    // great, we can add the continuation to the list
                    if (scheduleBeforeOthers)
                    {
                        list.Insert(0, continuation);
                    }
                    else
                    {
                        list.Add(continuation);
                    }
                    // we successfully added the continuation to the list, return true
                    return true;
                }
            }
        }
        DebugLog.WriteDiagnostic($"{this}: Failed to add continuation (sentinel was set).", LogWriter.Blocking);
        // we failed to add the continuation to the list (the sentinel was set in the meantime)
        // return false to indicate that the caller should execute the continuation directly
        return false;
    }

    internal void RemoveContinuation(object continuation)
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to remove continuation.", LogWriter.Blocking);
        object? currentContinuation = Volatile.Read(ref _continuation);
        if (ReferenceEquals(currentContinuation, _workloadCompletionSentinel))
        {
            DebugLog.WriteDiagnostic($"{this}: Continuation sentinel was set, nothing to do.", LogWriter.Blocking);
            return;
        }
        List<object?>? list = currentContinuation as List<object?>;
        if (list is null)
        {
            if (!ReferenceEquals(Interlocked.CompareExchange(ref _continuation, new List<object>(), continuation), continuation))
            {
                // it is not the continuation we were looking for or someone else replace the original continuation with a list
                // or the sentinel was set in the meantime
                // so either it's the list now, or the sentinel
                list = Volatile.Read(ref _continuation) as List<object?>;
                DebugLog.WriteDiagnostic($"{this}: Lost race to replace continuation with list while removing continuation.", LogWriter.Blocking);
            }
            else
            {
                DebugLog.WriteDiagnostic($"{this}: Successfully removed single continuation.", LogWriter.Blocking);
                // we successfully replaced the continuation with a list (cannot go back to null)
                return;
            }
        }
        Debug.Assert(list is not null || ReferenceEquals(Volatile.Read(ref _continuation), _workloadCompletionSentinel));
        if (list is not null)
        {
            lock (list)
            {
                if (!ReferenceEquals(Volatile.Read(ref _continuation), _workloadCompletionSentinel))
                {
                    DebugLog.WriteDiagnostic($"{this}: Removing continuation from list.", LogWriter.Blocking);
                    int index = list.IndexOf(continuation);
                    if (index >= 0)
                    {
                        list[index] = null;
                    }
                }
            }
        }
        DebugLog.WriteDiagnostic($"{this}: Continuation was removed, not found, or sentinel was set.", LogWriter.Blocking);
    }

    internal override void InternalRunContinuations()
    {
        DebugLog.WriteDiagnostic($"{this}: Running async continuations for workload.", LogWriter.Blocking);

        // unregister the cancellation token registration if necessary
        if (_cancellationTokenRegistration.HasValue)
        {
            _cancellationTokenRegistration.Value.Unregister();
            DebugLog.WriteDiagnostic($"{this}: Unregistered cancellation token registration.", LogWriter.Blocking);
        }

        // CAS the sentinel in to prevent further continuations from being added
        object? continuations = Interlocked.Exchange(ref _continuation, _workloadCompletionSentinel);

        Debug.Assert(!ReferenceEquals(continuations, _workloadCompletionSentinel), "Continuation sentinel was already set. This should never happen.");

        // decide what to do based on the current continuation
        switch (continuations)
        {
            case null:
                DebugLog.WriteDiagnostic($"{this}: No continuations to run.", LogWriter.Blocking);
                return;
            case Action singleContinuation:
                DebugLog.WriteDiagnostic($"{this}: Running inline continuation.", LogWriter.Blocking);
                singleContinuation.Invoke();
                return;
            case IWorkloadContinuation workloadContinuation:
                DebugLog.WriteDiagnostic($"{this}: Running workload continuation.", LogWriter.Blocking);
                workloadContinuation.Invoke(this);
                return;
            case List<object?> list:
            {
                // acquire the lock to prevent further continuations from being added
                // after we've acquired the lock, we can just drop it immediately, as we are sure that no one else will be adding continuations
                lock (list)
                { }

                DebugLog.WriteDiagnostic($"{this}: Running {list.Count(c => c is not null)} continuations.", LogWriter.Blocking);
                foreach (object? continuation in list)
                {
                    switch (continuation)
                    {
                        case Action action:
                            action.Invoke();
                            break;
                        case IWorkloadContinuation wc:
                            wc.Invoke(this);
                            break;
                        case null:
                            break;
                        default:
                            DebugLog.WriteError($"{this}: Invalid continuation type '{continuation.GetType().Name}'. This is a bug. Please report this issue.", LogWriter.Blocking);
                            break;
                    }
                }
                return;
            }
        }
        DebugLog.WriteError($"{this}: Invalid continuation type '{continuations.GetType().Name}'. This is a bug. Please report this issue.", LogWriter.Blocking);
    }

    /// <summary>
    /// Blocks the current thread until the workload completes execution.
    /// </summary>
    public void Wait() => InternalWait(Timeout.Infinite, CancellationToken.None);

    /// <summary>
    /// Blocks the current thread until the workload completes execution, or until the specified timeout elapses.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for the workload to complete execution.</param>
    /// <returns><see langword="true"/> if the workload completed execution within the specified timeout; <see langword="false"/> if the workload has not completed execution within the specified timeout.</returns>
    public bool Wait(TimeSpan timeout) =>
        InternalWait(timeout.Milliseconds, CancellationToken.None);

    /// <summary>
    /// Blocks the current thread until the workload completes execution, or until the specified <see cref="CancellationToken"/> is canceled.
    /// </summary>
    /// <param name="token">The <see cref="CancellationToken"/> to observe.</param>
    /// <returns><see langword="true"/> if the workload completed execution; <see langword="false"/> if the workload has not completed execution and the <see cref="CancellationToken"/> was canceled.</returns>
    public bool Wait(CancellationToken token) =>
        InternalWait(Timeout.Infinite, token);

    /// <summary>
    /// Blocks the current thread until the workload completes execution, or until the specified timeout elapses, or until the specified <see cref="CancellationToken"/> is canceled.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for the workload to complete execution.</param>
    /// <param name="token">The <see cref="CancellationToken"/> to observe.</param>
    /// <returns><see langword="true"/> if the workload completed execution within the specified timeout; <see langword="false"/> if the workload has not completed execution within the specified timeout or the <see cref="CancellationToken"/> was canceled.</returns>
    public bool Wait(TimeSpan timeout, CancellationToken token) => 
        InternalWait(timeout.Milliseconds, token);

    internal bool InternalWait(int millisecondsTimeout, CancellationToken token)
    {
        Throw.ArgumentOutOfRangeException.IfLessThan(millisecondsTimeout, -1, nameof(millisecondsTimeout));

        if (IsCompleted)
        {
            return true;
        }

        return SpinThenBlockingWait(millisecondsTimeout, token);
    }

    private bool SpinThenBlockingWait(int millisecondsTimeout, CancellationToken token)
    {
        uint startTimeTicks = (uint)Environment.TickCount;
        if (IsCompleted)
        {
            return true;
        }

        if (millisecondsTimeout == 0)
        {
            return false;
        }

        SpinWait spinner = default;
        // these spin counts are based on the values used by the TPL in Task.Wait
        int spinCount = Environment.ProcessorCount == 1 ? 1 : 35;
        for (int i = 0; i < spinCount; i++)
        {
            spinner.SpinOnce();
            if (IsCompleted)
            {
                DebugLog.WriteDiagnostic($"{this}: workload completed during spin wait.", LogWriter.Blocking);
                return true;
            }
        }
        DebugLog.WriteDiagnostic($"{this}: workload did not complete during spin wait. entering blocking wait.", LogWriter.Blocking);
        ManualResetEventSlim mres = new(false);
        // box the continuation first to capture the object reference
        object continuationBox = new Action(mres.Set);
        bool waitSuccessful = false;
        try
        {
            // try to add the continuation or immediately run it inline if we failed
            // otherwise, we could end up deadlocking the caller
            AddOrRunInlineContinuationAction(continuationBox, scheduleBeforeOthers: true);

            if (millisecondsTimeout == Timeout.Infinite)
            {
                waitSuccessful = mres.Wait(Timeout.Infinite, token);
            }
            else
            {
                uint remainingTicks = (uint)Environment.TickCount - startTimeTicks;
                if (remainingTicks < millisecondsTimeout)
                {
                    waitSuccessful = mres.Wait((int)(millisecondsTimeout - remainingTicks), token);
                }
            }
        }
        finally
        {
            if (!IsCompleted)
            {
                RemoveContinuation(continuationBox);
            }
        }
        return waitSuccessful;
    }
}
