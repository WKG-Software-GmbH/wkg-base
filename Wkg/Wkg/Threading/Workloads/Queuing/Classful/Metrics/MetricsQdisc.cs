using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.VirtualTime;

namespace Wkg.Threading.Workloads.Queuing.Classful.Metrics;

internal class MetricsQdisc<THandle> : ClassfulQdisc<THandle>, IClassfulQdisc<THandle>
    where THandle : unmanaged
{
    private IClassfulQdisc<THandle> _innerQdisc = null!;
    private readonly IVirtualTimeTable _timeTable;
    private bool _initialized;

    public MetricsQdisc(THandle handle, int maximumConcurrency, int maxSampleCount, bool usePrecise) : base(handle)
    {
        _timeTable = usePrecise
            ? VirtualTimeTable.CreatePrecise(maximumConcurrency, 32, maxSampleCount)
            : VirtualTimeTable.CreateFast(maximumConcurrency, 32, maxSampleCount);
    }

    protected override void OnInternalInitialize(INotifyWorkScheduled parentScheduler)
    {
        if (_innerQdisc is null)
        {
            throw new InvalidOperationException($"The {nameof(MetricsQdisc<THandle>)} must have exactly one child.");
        }
        BindChildQdisc(_innerQdisc);

        _initialized = true;
    }

    public override bool IsEmpty => _innerQdisc.IsEmpty;

    public override int Count => _innerQdisc.Count;

    private void AssertInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("The qdisc has not been initialized yet.");
        }
    }

    public override bool RemoveChild(IClasslessQdisc<THandle> child)
    {
        AssertInitialized();
        return _innerQdisc.RemoveChild(child);
    }

    public override bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<object?> predicate)
    {
        AssertInitialized();
        return _innerQdisc.TryAddChild(child, predicate);
    }

    public override bool TryAddChild(IClassfulQdisc<THandle> child)
    {
        if (!_initialized)
        {
            if (_innerQdisc is not null)
            {
                throw new InvalidOperationException($"A {nameof(MetricsQdisc<THandle>)} can only have one child.");
            }
            _innerQdisc = child;
            return true;
        }
        return _innerQdisc.TryAddChild(child);
    }

    public override bool TryAddChild(IClasslessQdisc<THandle> child)
    {
        AssertInitialized();
        return _innerQdisc.TryAddChild(child);
    }

    public override bool TryRemoveChild(IClasslessQdisc<THandle> child)
    {
        AssertInitialized();
        return _innerQdisc.TryRemoveChild(child);
    }

    protected override bool ContainsChild(THandle handle)
    {
        AssertInitialized();
        return _innerQdisc.ContainsChild(handle);
    }

    protected override void EnqueueDirect(AbstractWorkloadBase workload) => _innerQdisc.Enqueue(workload);

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        if (_innerQdisc.TryDequeueInternal(workerId, backTrack, out workload))
        {
            _timeTable.StartMeasurement(workload);
            return true;
        }
        else
        {
            workload = null;
            return false;
        }
    }
    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload) => _innerQdisc.TryEnqueue(state, workload);
    protected override bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload) => _innerQdisc.TryEnqueueDirect(state, workload);
    protected override bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child) => _innerQdisc.TryFindChild(handle, out child);
    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _innerQdisc.TryPeekUnsafe(workerId, out workload);
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => _innerQdisc.TryRemoveInternal(workload);
    protected override bool CanClassify(object? state) => _innerQdisc.CanClassify(state);
}
