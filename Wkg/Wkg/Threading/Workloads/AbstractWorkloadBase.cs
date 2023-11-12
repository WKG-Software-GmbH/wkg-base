using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Scheduling;
using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Continuations;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

/// <summary>
/// Represents an asynchronous workload that can be scheduled for execution.
/// </summary>
[DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
public abstract class AbstractWorkloadBase
{
    private protected uint _status;
    // this state may be used by qdiscs to store additional scheduling information
    internal volatile QueuingStateNode? _state;

    // continuations
    private protected static readonly object _workloadCompletionSentinel = new();
    private protected object? _continuation;

    private protected AbstractWorkloadBase(WorkloadStatus status)
    {
        _status = status;
    }

    /// <summary>
    /// Gets the unique identifier of this workload.
    /// </summary>
    public ulong Id { get; } = WorkloadIdGenerator.Generate();

    /// <summary>
    /// Retrives the current <see cref="WorkloadStatus"/> of this workload.
    /// </summary>
    public WorkloadStatus Status => Volatile.Read(ref _status);

    /// <summary>
    /// Indicates whether the workload is in any of the terminal states: <see cref="WorkloadStatus.RanToCompletion"/>, <see cref="WorkloadStatus.Faulted"/>, or <see cref="WorkloadStatus.Canceled"/>.
    /// </summary>
    public virtual bool IsCompleted => Status.IsOneOf(CommonFlags.Completed);

    internal virtual bool ContinuationsInvoked => ReferenceEquals(Volatile.Read(ref _continuation), _workloadCompletionSentinel);

    internal virtual void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) => Pass();

    /// <summary>
    /// Attempts to execute the action associated with this workload.
    /// </summary>
    /// <returns><see langword="true"/> if the workload was executed and is in any of the terminal states; <see langword="false"/> if the workload could not be executed.</returns>
    internal abstract bool TryRunSynchronously();

    internal abstract nint GetPayloadFunctionPointer();

    internal abstract void InternalAbort(Exception? exception = null);

    /// <summary>
    /// Attempts to bind the workload to the specified qdisc.
    /// </summary>
    /// <remarks>
    /// The workload should be bound first, before being enqueued into the qdisc.
    /// </remarks>
    /// <param name="qdisc">The qdisc to bind to.</param>
    /// <returns><see langword="true"/> if the workload was successfully bound to the qdisc; <see langword="false"/> if the workload has already completed, is in an unbindable state, or another binding operation was faster.</returns>
    internal abstract bool TryInternalBindQdisc(IQdisc qdisc);

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} {Id} ({Status})";

    internal virtual void AddOrRunInlineContinuationAction(object continuation, bool scheduleBeforeOthers = false)
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

    internal virtual void AddOrRunContinuation(IWorkloadContinuation continuation, bool scheduleBeforeOthers = false)
    {
        if (!TryAddContinuation(continuation, scheduleBeforeOthers))
        {
            DebugLog.WriteDiagnostic($"{this}: Failed to add continuation. Invoking on context.", LogWriter.Blocking);
            continuation.Invoke(this);
        }
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

    private protected virtual bool TryAddContinuationComplex(object continuation, bool scheduleBeforeOthers)
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

    /// <summary>
    /// Marks the workload as finalized before it falls out of scope and executes any Task continuations that were scheduled for it.
    /// Also allows the workload to be returned to a pool, if applicable.
    /// </summary>
    internal virtual void InternalRunContinuations(int workerId)
    {
        DebugLog.WriteDiagnostic($"{this}: Running async continuations for workload.", LogWriter.Blocking);

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
            case IWorkerLocalWorkloadContinuation workerLocalContinuation:
                DebugLog.WriteDiagnostic($"{this}: Running worker-local workload continuation.", LogWriter.Blocking);
                workerLocalContinuation.Invoke(this, workerId);
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
                        case IWorkerLocalWorkloadContinuation wlc:
                            wlc.Invoke(this, workerId);
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

    internal virtual void RemoveContinuation(object continuation)
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
}

file static class WorkloadIdGenerator
{
    private static ulong _nextId;

    // this is a simple, fast, and thread-safe way to generate unique IDs.
    // it is possible for the IDs to be reused, but only after 2^64 IDs have been generated
    // and it is highly unlikely that someone keeps a workload alive for so long
    // that there are any collisions
    public static ulong Generate() => Interlocked.Increment(ref _nextId);
}