﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Common;
using Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedLifo;

internal sealed class ConstrainedLifoQdisc<THandle>(THandle handle, Predicate<object?>? predicate, int maxCount, ConstrainedPrioritizationOptions options) 
    : ConstrainedFifoQdisc<THandle>(handle, predicate, maxCount, options), IClassifyingQdisc<THandle> where THandle : unmanaged
{
    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        // prioritize enqueuing or dequeuing based on the specified constrained options
        // use the alpha lock if we are minimizing workload cancellation (execute as much as possible)
        using ILockOwnership groupLock = _constrainedOptions == ConstrainedPrioritizationOptions.MinimizeWorkloadCancellation
            ? _abls.AcquireAlphaLock()
            : _abls.AcquireBetaLock();
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
            int index = MathExtensions.Modulo(state.Tail - 1, _workloads.Length);
            workload = Volatile.Read(ref _workloads[index]);
        } while (Volatile.Read(ref _state) != currentState);
        Debug.Assert(workload is not null);
        return true;
    }
}
