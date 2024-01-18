using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Common;
using Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedLifo;

internal sealed class ConstrainedLifoQdisc<THandle>(THandle handle, Predicate<object?>? predicate, int maxCount) 
    : ConstrainedFifoQdisc<THandle>(handle, predicate, maxCount), IClassifyingQdisc<THandle> where THandle : unmanaged
{
    // TODO: verify that this is correct! This might be running into the same issue as the WeakPool implementation
    // where concurrent peek and pop operations can cause problems due to the delay between stack pointer updates and the underlying writes
    // Specifically push-pop-push flickering can cause the pop to access an element that has not yet been written to
    // --> confirm that this is not an issue here
    // --> if it is, we need to reevaluate the implementation and come up with a better solution (that may also apply to the WeakPool implementation)
    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        // the only between this stack and the queue is that we dequeue from the end of the array
        ulong currentState, newState;
        do
        {
            currentState = Volatile.Read(ref _state);
            AtomicRingBufferStateUnion state = new(currentState);
            if (state.Head == state.Tail && state.IsEmpty)
            {
                // stack is empty
                workload = null;
                return false;
            }
            // we need a real modulo here, not the C# remainder operator
            // -1 mod 4 = 3, not -1
            state.Tail = (ushort)MathExtensions.Modulo(state.Tail - 1, _workloads.Length);
            workload = Volatile.Read(ref _workloads[state.Tail]);
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
            int index = MathExtensions.Modulo(state.Tail - 1, _workloads.Length);
            workload = Volatile.Read(ref _workloads[index]);
        } while (Volatile.Read(ref _state) != currentState);
        Debug.Assert(workload is not null);
        return true;
    }
}
