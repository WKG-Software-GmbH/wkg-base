using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Collections.Concurrent;
using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless.PriorityFifoFast;

internal class PriorityFifoFastQdisc<THandle> : ClasslessQdisc<THandle>
    where THandle : unmanaged
{
    private readonly ConcurrentBitmap _dataMap;
    private readonly IClassifyingQdisc<THandle>[] _bands;
    private readonly int _defaultBand;
    private readonly bool _bandHandlesConfigured;
    private readonly Func<object?, int> _bandSelector;
    private volatile int _fuzzyCount;

    public PriorityFifoFastQdisc(THandle handle, THandle[] bandHandles, int bands, int defaultBand, Func<object?, int> bandSelector, Predicate<object?>? predicate) : base(handle, predicate)
    {
        Debug.Assert(bands > 1);
        Debug.Assert(defaultBand >= 0 && defaultBand < bands);
        Debug.Assert(bandSelector is not null);
        Debug.Assert(bandHandles.Length == 0 || bandHandles.Length == bands);
        _bandHandlesConfigured = bandHandles.Length == bands;
        _dataMap = new ConcurrentBitmap(bands);
        _bands = new FifoQdisc<THandle>[bands];
        for (int i = 0; i < bands; i++)
        {
            THandle bandHandle = _bandHandlesConfigured ? bandHandles[i] : default;
            _bands[i] = new FifoQdisc<THandle>(bandHandle, null);
        }
        _defaultBand = defaultBand;
        _bandSelector = bandSelector;
    }

    public override bool IsEmpty => _dataMap.IsEmpty;

    public override int Count => _fuzzyCount;

    protected override bool CanClassify(object? state) => Predicate.Invoke(state);

    protected override bool ContainsChild(THandle handle) => false;

    protected override void EnqueueDirect(AbstractWorkloadBase workload) => EnqueueDirectCore(workload, _defaultBand);

    private void EnqueueDirectCore(AbstractWorkloadBase workload, int band)
    {
        // fuzzy count must be greater than or equal to the actual count
        // therefore pre-increment
        Interlocked.Increment(ref _fuzzyCount);
        // update the data map. This must be done before notifying the scheduler
        // it doesn't matter if it happens before or after the enqueue
        _dataMap.UpdateBit(band, true);
        _bands[band].Enqueue(workload);
    }

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        // we loop until we find something to dequeue
        // a simple for loop isn't enough because new workloads may be inserted at a lower band after we've checked it
        while (!IsEmpty)
        {
            for (int i = 0; i < _bands.Length; i++)
            {
                if (!_dataMap.IsBitSet(i))
                {
                    // just skip this band if it's empty
                    // a lookup in the data map is faster than an attempted dequeue
                    continue;
                }
                byte token;
                do
                {
                    token = _dataMap.GetToken(i);
                    if (_bands[i].TryDequeueInternal(workerId, backTrack, out workload))
                    {
                        Interlocked.Decrement(ref _fuzzyCount);
                        return true;
                    }
                    // the queue was empty, but the last state we knew about was that there should be something in the queue
                    // so we need to update the data map to reflect the new state
                    // something may have been enqueued in the meantime, so we use a token to ensure that we don't overwrite
                    // a newer state with an older one
                } while (!_dataMap.TryUpdateBit(i, token, false));
            }
        }
        // the queue was empty, and we didn't find anything to dequeue
        workload = null;
        return false;
    }

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        if (Predicate.Invoke(state))
        {
            int band = _bandSelector.Invoke(state);
            if (band == -1)
            {
                band = _defaultBand;
            }
            Throw.ArgumentOutOfRangeException.IfNotInRange(band, 0, _bands.Length - 1, nameof(band));
            EnqueueDirectCore(workload, band);
            return true;
        }
        return false;
    }

    protected override bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload)
    {
        if (_bandHandlesConfigured)
        {
            for (int i = 0; i < _bands.Length; i++)
            {
                if (_bands[i].Handle.Equals(handle))
                {
                    EnqueueDirectCore(workload, i);
                    return true;
                }
            }
        }
        return false;
    }

    protected override bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload) => TryEnqueue(state, workload);

    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path)
    {
        if (_bandHandlesConfigured)
        {
            for (int i = 0; i < _bands.Length; i++)
            {
                if (_bands[i].Handle.Equals(handle))
                {
                    path.Add(new RoutingPathNode<THandle>(this, handle, i));
                    path.Complete(_bands[i]);
                    return true;
                }
            }
        }
        return false;
    }

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        // we loop until we find something to peek or the queue is empty
        // a simple for loop isn't enough because new workloads may be inserted at a lower band after we've checked it
        while (!IsEmpty)
        {
            for (int i = 0; i < _bands.Length; i++)
            {
                if (!_dataMap.IsBitSet(i))
                {
                    // just skip this band if it's empty
                    // a lookup in the data map is faster than an attempted peek
                    continue;
                }
                byte token;
                do
                {
                    token = _dataMap.GetToken(i);
                    if (_bands[i].TryPeekUnsafe(workerId, out workload))
                    {
                        return true;
                    }
                    // the queue was empty, but the last state we knew about was that there should be something in the queue
                    // so we need to update the data map to reflect the new state
                    // something may have been enqueued in the meantime, so we use a token to ensure that we don't overwrite
                    // a newer state with an older one
                } while (!_dataMap.TryUpdateBit(i, token, false));
            }
        }
        // the queue was empty, and we didn't find anything to peek
        workload = null;
        return false;
    }

    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    protected override void WillEnqueueFromRoutingPath(ref readonly RoutingPathNode<THandle> routingPathNode, AbstractWorkloadBase workload)
    {
        Interlocked.Increment(ref _fuzzyCount);
        _dataMap.UpdateBit(routingPathNode.Offset, true);
    }
}
