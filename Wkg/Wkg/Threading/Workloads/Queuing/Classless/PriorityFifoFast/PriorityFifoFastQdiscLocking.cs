using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Wkg.Collections.Concurrent;
using Wkg.Common.Extensions;
using Wkg.Common.ThrowHelpers;
using Wkg.Text;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless.PriorityFifoFast;

internal class PriorityFifoFastQdiscLocking<THandle> : ClassifyingQdisc<THandle>, INotifyWorkScheduled
    where THandle : unmanaged
{
    private readonly object _syncRoot = new();

    private readonly IClassifyingQdisc<THandle>[] _bands;
    private readonly int _defaultBand;
    private readonly bool _bandHandlesConfigured;
    private readonly Func<object?, int> _bandSelector;

    public PriorityFifoFastQdiscLocking(THandle handle, THandle[] bandHandles, int bands, int defaultBand, Func<object?, int> bandSelector, Predicate<object?>? predicate) : base(handle, predicate)
    {
        Debug.Assert(bands > 1);
        Debug.Assert(defaultBand >= 0 && defaultBand < bands);
        Debug.Assert(bandSelector is not null);
        Debug.Assert(bandHandles.Length == 0 || bandHandles.Length == bands);
        _bandHandlesConfigured = bandHandles.Length == bands;
        _bands = new FifoQdisc<THandle>[bands];
        for (int i = 0; i < bands; i++)
        {
            THandle bandHandle = _bandHandlesConfigured ? bandHandles[i] : default;
            FifoQdisc<THandle> band = new(bandHandle, null);
            band.To<IQdisc>().InternalInitialize(this);
            _bands[i] = band;
        }
        _defaultBand = defaultBand;
        _bandSelector = bandSelector;
    }

    public override bool IsEmpty => BestEffortCount == 0;

    public override int BestEffortCount
    {
        get
        {
            lock (_syncRoot)
            {
                int count = 0;
                foreach (IClassifyingQdisc<THandle> band in _bands)
                {
                    count += band.BestEffortCount;
                }
                return count;
            }
        }
    }

    protected override bool CanClassify(object? state) => Predicate.Invoke(state);

    protected override bool ContainsChild(THandle handle) => false;

    protected override void EnqueueDirect(AbstractWorkloadBase workload) => EnqueueDirectCore(workload, _defaultBand);

    private void EnqueueDirectCore(AbstractWorkloadBase workload, int band)
    {
        lock (_syncRoot)
        {
            _bands[band].Enqueue(workload);
        }
    }

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        lock (_syncRoot)
        {
            for (int i = 0; i < _bands.Length; i++)
            {
                if (_bands[i].TryDequeueInternal(workerId, backTrack, out workload))
                {
                    return true;
                }
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
        lock (_syncRoot)
        {
            // we loop until we find something to peek or the queue is empty
            for (int i = 0; i < _bands.Length; i++)
            {
                if (_bands[i].TryPeekUnsafe(workerId, out workload))
                {
                    return true;
                }
            }
        }

        workload = null;
        return false;
    }

    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    void INotifyWorkScheduled.OnWorkScheduled() => ParentScheduler.OnWorkScheduled();

    protected override void DisposeManaged()
    {
        for (int i = 0; i < _bands.Length; i++)
        {
            _bands[i].Dispose();
        }
    }

    void INotifyWorkScheduled.DisposeRoot() => ParentScheduler.DisposeRoot();

    protected override void ChildrenToTreeString(StringBuilder builder, int indent)
    {
        for (int i = 0; i < _bands.Length; i++)
        {
            builder.AppendIndent(indent).Append($"Band {i}: ");
            ChildToTreeString(_bands[i], builder, indent);
        }
    }
}
