using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Common;
using Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedLifo;

internal sealed class ConstrainedLifoQdisc<THandle> : ConstrainedFifoQdisc<THandle>, IClasslessQdisc<THandle> where THandle : unmanaged
{
    public ConstrainedLifoQdisc(THandle handle, int maxCount) : base(handle, maxCount)
    {
    }

    protected override bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
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
}
