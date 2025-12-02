using System.Diagnostics;

namespace Wkg.Threading;

/// <summary>
/// A simple lock that can be used across asynchronous boundaries.
/// </summary>
/// <remarks>
/// This lock is reentrant for the same async-flow, as long as Exit is called the same number of times as Enter.
/// </remarks>
[DebuggerDisplay("IsHeld = {IsHeld}")]
// nobody wants to step through the internals of a lock when debugging business logic,
// so we extensively use DebuggerStepThrough to allow the debugger to skip over the internals of the lock
// and quickly get to the delegate execution of the user code.
[method: DebuggerStepThrough]
public sealed class AsyncLock() : IDisposable
{
    // Task.CompletedTask, just with a dummy bool result to wrap Action delegates into Task<TResult>
    private static readonly Task<bool> s_completedDummyTask = Task.FromResult(true);

    // the semaphore is used as a gatekeeper to ensure that only one async flow can enter the lock at a time
    // it implements the waiting mechanism for the lock
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);
    // the CTS is used to cancel all waiting async flows when the lock is disposed
    private readonly CancellationTokenSource _cts = new();
    // the async local is used to keep track of the number of locks held by the current async flow
    // this allows the lock to be reentrant for the same async flow
    private readonly AsyncLocal<int> _al_locksHeld = new();
    // an "interlocked" boolean to keep track of the disposed state
    private ConcurrentBoolean _disposedValue;
    // an interlocked counter to keep track of the number of waiting async flows
    // this is important to ensure that we can dispose the semaphore safely,
    // releasing its resources only after all waiting async flows have been fully cancelled
    private int _waitingCount;

    /// <summary>
    /// Whether the current async flow has exclusive ownership of the lock.
    /// </summary>
    public bool IsHeld => _al_locksHeld.Value > 0;

    /// <summary>
    /// The number of times the current async flow has entered the lock recursively.
    /// </summary>
    internal int LocksHeld => _al_locksHeld.Value;

    /// <summary>
    /// Asynchronously enters the lock, running the specified synchronized action within the lock.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the synchronized action.</typeparam>
    /// <param name="synchronizedAction">The action to run within the lock.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the synchronized action.</returns>
    /// <exception cref="LockDisposedException">If the lock is disposed while waiting to enter.</exception>
    /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> is canceled before or while waiting to enter the lock.</exception>
    [DebuggerStepThrough]
    public Task<TResult> RunActionAsync<TResult>(Func<TResult> synchronizedAction, CancellationToken cancellationToken = default)
    {
        return LockAsync(Wrapper, cancellationToken);

        [DebuggerStepThrough]
        Task<TResult> Wrapper(CancellationToken _) => Task.FromResult(synchronizedAction());
    }

    /// <summary>
    /// Asynchronously enters the lock, running the specified synchronized action within the lock.
    /// </summary>
    /// <param name="synchronizedAction">The action to run within the lock.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="LockDisposedException">If the lock is disposed while waiting to enter.</exception>
    /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> is canceled before or while waiting to enter the lock.</exception>
    [DebuggerStepThrough]
    public Task RunActionAsync(Action synchronizedAction, CancellationToken cancellationToken = default)
    {
        return LockAsync(Wrapper, cancellationToken);

        [DebuggerStepThrough]
        Task<bool> Wrapper(CancellationToken _)
        {
            synchronizedAction();
            return s_completedDummyTask;
        }
    }

    /// <summary>
    /// Attempts to asynchronously enter the lock, running the specified synchronized action within the lock.
    /// If the lock is disposed while waiting, a corresponding <see cref="AsyncLockResult{TResult}"/> is returned indicating that the provided <paramref name="synchronizedAction"/> was not executed.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the synchronized action.</typeparam>
    /// <param name="synchronizedAction">The action to run within the lock.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AsyncLockResult{TResult}"/> indicating whether the action was executed and its result if applicable.</returns>
    /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> is canceled before or while waiting to enter the lock.</exception>
    [DebuggerStepThrough]
    public async Task<AsyncLockResult<TResult>> TryRunActionAsync<TResult>(Func<TResult> synchronizedAction, CancellationToken cancellationToken = default)
    {
        try
        {
            TResult result = await RunActionAsync(synchronizedAction, cancellationToken);
            return new AsyncLockResult<TResult>(result, TaskExecuted: true);
        }
        catch (LockDisposedException)
        {
            return AsyncLockResult.Skipped<TResult>();
        }
    }

    /// <summary>
    /// Attempts to asynchronously enter the lock, running the specified synchronized action within the lock.
    /// If the lock is disposed while waiting, a corresponding <see cref="AsyncLockResult"/> is returned indicating that the provided <paramref name="synchronizedAction"/> was not executed.
    /// </summary>
    /// <param name="synchronizedAction">The action to run within the lock.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AsyncLockResult"/> indicating whether the action was executed.</returns>
    /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> is canceled before or while waiting to enter the lock.</exception>
    [DebuggerStepThrough]
    public async Task<AsyncLockResult> TryRunActionAsync(Action synchronizedAction, CancellationToken cancellationToken = default)
    {
        try
        {
            await RunActionAsync(synchronizedAction, cancellationToken);
            return new AsyncLockResult(TaskExecuted: true);
        }
        catch (LockDisposedException)
        {
            return AsyncLockResult.Skipped();
        }
    }

    /// <summary>
    /// Asynchronously enters the lock, running the specified synchronized task within the lock.
    /// </summary>
    /// <param name="synchronizedTask">The task to run within the lock.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="LockDisposedException">If the lock is disposed while waiting to enter.</exception>
    /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> is canceled before or while waiting to enter the lock.</exception>
    [DebuggerStepThrough]
    public Task RunTaskAsync(Func<CancellationToken, Task> synchronizedTask, CancellationToken cancellationToken = default)
    {
        return LockAsync(Wrapper, cancellationToken);

        [DebuggerStepThrough]
        async Task<bool> Wrapper(CancellationToken ct)
        {
            await synchronizedTask(ct);
            return true;
        }
    }

    /// <summary>
    /// Asynchronously enters the lock, running the specified synchronized task within the lock.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the synchronized task.</typeparam>
    /// <param name="synchronizedTask">The task to run within the lock.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the synchronized task.</returns>
    /// <exception cref="LockDisposedException">If the lock is disposed while waiting to enter.</exception>
    /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> is canceled before or while waiting to enter the lock.</exception>
    [DebuggerStepThrough]
    public Task<TResult> RunTaskAsync<TResult>(Func<CancellationToken, Task<TResult>> synchronizedTask, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(synchronizedTask);
        return LockAsync(synchronizedTask, cancellationToken);
    }

    /// <summary>
    /// Attempts to asynchronously enter the lock, running the specified synchronized task within the lock.
    /// If the lock is disposed while waiting, a corresponding <see cref="AsyncLockResult"/> is returned indicating that the provided <paramref name="synchronizedTask"/> was not executed.
    /// </summary>
    /// <param name="synchronizedTask">The task to run within the lock.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AsyncLockResult"/> indicating whether the task was executed.</returns>
    /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> is canceled before or while waiting to enter the lock.</exception>
    [DebuggerStepThrough]
    public async Task<AsyncLockResult> TryRunTaskAsync(Func<CancellationToken, Task> synchronizedTask, CancellationToken cancellationToken = default)
    {
        try
        {
            await RunTaskAsync(synchronizedTask, cancellationToken);
            return new AsyncLockResult(TaskExecuted: true);
        }
        catch (LockDisposedException)
        {
            return AsyncLockResult.Skipped();
        }
    }

    /// <summary>
    /// Attempts to asynchronously enter the lock, running the specified synchronized task within the lock.
    /// If the lock is disposed while waiting, a corresponding <see cref="AsyncLockResult{TResult}"/> is returned indicating that the provided <paramref name="synchronizedTask"/> was not executed.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the synchronized task.</typeparam>
    /// <param name="synchronizedTask">The task to run within the lock.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AsyncLockResult{TResult}"/> indicating whether the task was executed and its result if applicable.</returns>
    /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> is canceled before or while waiting to enter the lock.</exception>
    [DebuggerStepThrough]
    public async Task<AsyncLockResult<TResult>> TryRunTaskAsync<TResult>(Func<CancellationToken, Task<TResult>> synchronizedTask, CancellationToken cancellationToken = default)
    {
        try
        {
            TResult result = await RunTaskAsync(synchronizedTask, cancellationToken);
            return new AsyncLockResult<TResult>(result, TaskExecuted: true);
        }
        catch (LockDisposedException)
        {
            return AsyncLockResult.Skipped<TResult>();
        }
    }

    // allow the debugger to step into this method to quickly get to the user code
    // since DebuggerStepThrough doesn't respect async boundaries we can't use it here
    private async Task<TResult> LockAsync<TResult>(Func<CancellationToken, Task<TResult>> synchronizedTask, CancellationToken cancellationToken)
    {
        // validate arguments and enter the lock, allowing the debugger to skip over the internals
        await EnterLockAsync(synchronizedTask, cancellationToken);
        // at this point, we are holding the lock
        // increment the number of locks held by this async flow to allow reentrancy
        _al_locksHeld.Value++;
        try
        {
            // pass the original cancellation token to the synchronized task
            // the TCS is only used to be able to break out of the semaphore wait
            return await synchronizedTask(cancellationToken);
        }
        finally
        {
            ExitLock();
        }
    }

    // enter the lock and do all the things we want to hide from the debugger
    [DebuggerStepThrough]
    private async Task EnterLockAsync<TResult>(Func<CancellationToken, Task<TResult>> synchronizedTask, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(synchronizedTask);
        CheckDisposed();
        // only enter the lock if we are not already holding it (reentrancy)
        // remember that we cannot modify the async local here, since changes to it
        // are not propagated up "against the flow" of async continuations
        // the value will be incremented by the caller after this method returns
        if (_al_locksHeld.Value == 0)
        {
            // this is the first time we are entering the lock
            CancellationTokenSource? linkedCts = null;
            CancellationToken ct;
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // we will need to allocate a linked token source to ensure that we can cancel the wait from external sources as well as on disposal
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
                ct = linkedCts.Token;
            }
            else
            {
                // we can use the internal CTS directly, since we don't need to worry about external cancellation
                ct = _cts.Token;
            }
            try
            {
                // keep track of waiting async flows
                Interlocked.Increment(ref _waitingCount);
                // acquire the semaphore
                await _semaphore.WaitAsync(ct);
            }
            catch (ObjectDisposedException inner)
            {
                // we failed to acquire the semaphore because it was disposed
                // this can happen if we were just about to acquire the semaphore when the lock was disposed
                // after we checked for disposal (TOC/TOU)
                // since this is an exogenous exception, we need to convert it to a LockDisposedException
                // to be able to detect and handle it properly if we are running in one of the TryRun*Async methods
                LockDisposedException.Rethrow(inner);
            }
            catch (OperationCanceledException)
            {
                // convert the cancellation to a LockDisposedException if cancellation was due to disposal
                CheckDisposed();
                // otherwise, re-throw the cancellation
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _waitingCount);
                // sadly, we can't really TryReset and pool the CTS, like we usually do
                // since the CTS is specifically linked to whatever CancellationToken was passed in
                linkedCts?.Dispose();
            }
        }
    }

    // run cleanup internals hidden from the user code
    [DebuggerStepThrough]
    private void ExitLock()
    {
        // decrement the number of locks held by this async flow
        // this honestly is pretty much a no-op, since changes to the async local
        // are not propagated up "against the flow" of async continuations.
        // we are decrementing it anyway, to maintain sanity
        _al_locksHeld.Value--;
        Debug.Assert(_al_locksHeld.Value >= 0);
        // if we are the outermost lock, we can release the semaphore
        if (_al_locksHeld.Value == 0)
        {
            // but first, we need to re-sample for disposal
            bool disposedValue = Atomic.VolatileRead(ref _disposedValue);
            if (!disposedValue)
            {
                // catch exogenous cancellation due to concurrent disposal
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // we are disposed, but since we are releasing the lock anyways
                    // it's okay to ignore this issue. We could also re-throw the exception
                    // but since the inner task already completed without error, it would be a bit silly to do so.
                    // so just swallow the exception and move on with life
                }
            }
        }
    }

    [DebuggerStepThrough]
    private void CheckDisposed()
    {
        bool disposedValue = Atomic.VolatileRead(ref _disposedValue);
        LockDisposedException.ThrowIf(disposedValue, this);
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="AsyncLock"/> class.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe, but will cause all pending and future interactions with the lock to throw an <see cref="LockDisposedException"/>.
    /// </remarks>
    public void Dispose()
    {
        // ensure that we only dispose once
        // also, causes all future interactions to throw an LockDisposedException
        if (Atomic.CompareExchange(ref _disposedValue, value: true, comparand: false) == ConcurrentBoolean.FALSE)
        {
            // we are the first to dispose, so we need to cancel all waiting async flows
            // this will cause them to throw an OperationCanceledException when they try to acquire the lock
            _cts.Cancel();
            _cts.Dispose();
            // we are marked for disposal, so any waiting threads are scheduled to throw an exception
            // since it may take some time for the task continuations to be scheduled,
            // we wait until the cancellation has been observed by all waiting threads
            // if we don't do this, the cancellation may never be propagated to all waiters,
            // causing them to hang indefinitely (since the will not, and cannot, ever be released).
            // we don't expect this to take long, so we can just spin a few times until it's done
            SpinWait.SpinUntil(() => Volatile.Read(in _waitingCount) == 0);
            // now it should be safe to dispose the semaphore, because:
            // - any new attempts to acquire the lock will throw a LockDisposedException
            // - any waiting async flows have already received the notification to cancel. They are in the process of throwing an LockDisposedException
            // - the async flow that is currently holding the lock, if any, will be able to release it without issue
            _semaphore.Dispose();
        }
    }
}