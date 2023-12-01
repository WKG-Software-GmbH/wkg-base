using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

using static ConcurrentBoolean;

internal class ConstrainedFifoQdisc<THandle> : ClasslessQdisc<THandle>, IClassifyingQdisc<THandle> where THandle : unmanaged
{
    private protected readonly AbstractWorkloadBase?[] _workloads;

    private protected ulong _state;

    public ConstrainedFifoQdisc(THandle handle, Predicate<object?>? predicate, int maxCount) : base(handle, predicate)
    {
        Debug.Assert(maxCount > 0);
        Debug.Assert(maxCount <= ushort.MaxValue);

        AtomicRingBufferStateUnion initialState = new()
        {
            Head = 0,
            Tail = 0,
            IsEmpty = TRUE
        };
        Volatile.Write(ref _state, initialState.__State);
        _workloads = new AbstractWorkloadBase[maxCount];
    }

    public override bool IsEmpty
    {
        get
        {
            ulong state = Volatile.Read(ref _state);
            return new AtomicRingBufferStateUnion(state).IsEmpty;
        }
    }

    public override int Count
    {
        get
        {
            ulong currentState = Volatile.Read(ref _state);
            AtomicRingBufferStateUnion state = new(currentState);
            return state.Head < state.Tail || state.IsEmpty
                ? state.Tail - state.Head
                : _workloads.Length - state.Head + state.Tail;
        }
    }

    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    protected override void EnqueueDirect(AbstractWorkloadBase workload)
    {
        if (TryBindWorkload(workload))
        {
            EnqueueInternal(workload);
            NotifyWorkScheduled();
        }
        else
        {
            DebugLog.WriteWarning("A workload was scheduled, but could not be bound to the qdisc. This is a likely a bug in the qdisc scheduler implementation.", LogWriter.Blocking);
        }
    }

    private protected virtual void EnqueueInternal(AbstractWorkloadBase workload)
    {
        ulong currentState, newState;
        AbstractWorkloadBase? overriddenWorkload = null;
        ushort tail;
        do
        {
            currentState = Volatile.Read(ref _state);
            AtomicRingBufferStateUnion state = new(currentState);
            tail = state.Tail;
            state.Tail = (ushort)((tail + 1) % _workloads.Length);
            // if the old tail is the same as the head, and the stack is not empty, we need to override the oldest workload
            // this is because we are at the maximum capacity of the queue
            if (tail == state.Head && !state.IsEmpty)
            {
                overriddenWorkload = Volatile.Read(ref _workloads[tail]);
                // the new head is the same as the tail, because we overrode the oldest workload
                state.Head = state.Tail;
            }
            state.IsEmpty = false;
            newState = state.__State;
        } while (Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);
        // we have a new tail, so we can safely write to it
        Volatile.Write(ref _workloads[tail], workload);
        // if we overrode a workload, we need to abort it
        if (overriddenWorkload is not null)
        {
            DebugLog.WriteWarning($"A workload was scheduled, but the queue was full. The oldest workload was overridden.", LogWriter.Blocking);
            overriddenWorkload.InternalAbort();
        }
    }

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        ulong currentState, newState;
        do
        {
            currentState = Volatile.Read(ref _state);
            AtomicRingBufferStateUnion state = new(currentState);
            if (state.Head == state.Tail && state.IsEmpty)
            {
                // Queue is empty
                workload = null;
                return false;
            }
            workload = Volatile.Read(ref _workloads[state.Head]);
            state.Head = (ushort)((state.Head + 1) % _workloads.Length);
            state.IsEmpty = state.Head == state.Tail;
            newState = state.__State;
        } while (Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);
        Debug.Assert(workload is not null);
        return true;
    }

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        ulong currentState;
        do
        {
            currentState = Volatile.Read(ref _state);
            AtomicRingBufferStateUnion state = new(currentState);
            if (state.Head == state.Tail && state.IsEmpty)
            {
                // Queue is empty
                workload = null;
                return false;
            }
            workload = Volatile.Read(ref _workloads[state.Head]);
        } while (Volatile.Read(ref _state) != currentState);
        Debug.Assert(workload is not null);
        return true;
    }

    protected override bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload) => false;

    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path) => false;

    protected override bool ContainsChild(THandle handle) => false;

    protected override bool CanClassify(object? state) => Predicate.Invoke(state);

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload) => TryEnqueueDirect(state, workload);

    protected override bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload)
    {
        if (Predicate.Invoke(state))
        {
            EnqueueDirect(workload);
            return true;
        }
        return false;
    }
}
