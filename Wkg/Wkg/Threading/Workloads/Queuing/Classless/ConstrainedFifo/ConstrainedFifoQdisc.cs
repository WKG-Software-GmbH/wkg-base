using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

internal class ConstrainedFifoQdisc<THandle> : ClasslessQdisc<THandle>, IClassifyingQdisc<THandle> where THandle : unmanaged
{
    private protected readonly AbstractWorkloadBase?[] _workloads;

    // enqueuing and dequeuing are thread-safe by themselves, but we need to ensure that they are not interleaved
    // this is especially important for the constrained stack, because we need to ensure that the tail isn't incremented and decremented at the same time
    private protected readonly AlphaBetaLockSlim _abls;
    private protected readonly ConstrainedPrioritizationOptions _constrainedOptions;

    private protected ulong _state;

    public ConstrainedFifoQdisc(THandle handle, Predicate<object?>? predicate, int maxCount, ConstrainedPrioritizationOptions options) : base(handle, predicate)
    {
        Debug.Assert(maxCount > 0);
        Debug.Assert(maxCount <= ushort.MaxValue);

        AtomicRingBufferStateUnion initialState = new()
        {
            Head = 0,
            Tail = 0,
            IsEmpty = true
        };
        Volatile.Write(ref _state, initialState.UnderlyingStorage);
        _workloads = new AbstractWorkloadBase[maxCount];
        _abls = new AlphaBetaLockSlim();
        _constrainedOptions = options;
    }

    public override bool IsEmpty
    {
        get
        {
            ulong state = Volatile.Read(ref _state);
            return new AtomicRingBufferStateUnion(state).IsEmpty;
        }
    }

    public override int BestEffortCount
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

    protected override void EnqueueDirectLocal(AbstractWorkloadBase workload) => EnqueueInternal(workload);

    private protected virtual void EnqueueInternal(AbstractWorkloadBase workload)
    {
        // prioritize enqueuing or dequeuing based on the specified constrained options
        // use the alpha lock if we are minimizing scheduling delay (drop outdated workloads as soon as possible and increase responsiveness)
        using ILockOwnership groupLock = _constrainedOptions == ConstrainedPrioritizationOptions.MinimizeSchedulingDelay
            ? _abls.AcquireAlphaLock()
            : _abls.AcquireBetaLock();

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
            else
            {
                overriddenWorkload = null;
            }
            state.IsEmpty = false;
            newState = state.UnderlyingStorage;
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
        // prioritize enqueuing or dequeuing based on the specified constrained options
        // use the alpha lock if we are minimizing workload cancellation (execute as much as possible)
        using ILockOwnership groupLock = _constrainedOptions == ConstrainedPrioritizationOptions.MinimizeWorkloadCancellation
            ? _abls.AcquireAlphaLock()
            : _abls.AcquireBetaLock();
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
            newState = state.UnderlyingStorage;
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
