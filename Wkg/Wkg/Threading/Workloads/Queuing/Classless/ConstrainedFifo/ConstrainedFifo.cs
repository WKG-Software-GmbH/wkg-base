﻿using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

/// <summary>
/// Represents a constrained FIFO queueing discipline.
/// </summary>
public sealed class ConstrainedFifo : ClasslessQdiscBuilder<ConstrainedFifo>, IClasslessQdiscBuilder<ConstrainedFifo>
{
    private int _capacity = -1;
    private ConstrainedPrioritizationOptions _constrainedOptions = ConstrainedPrioritizationOptions.MinimizeWorkloadCancellation;

    public static ConstrainedFifo CreateBuilder(IQdiscBuilderContext context) => new();

    public ConstrainedFifo WithCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, ushort.MaxValue, nameof(capacity));
        if (_capacity != -1)
        {
            throw new InvalidOperationException("Capacity was already specified.");
        }

        _capacity = capacity;
        return this;
    }

    public ConstrainedFifo WithConstrainedPrioritizationOptions(ConstrainedPrioritizationOptions options)
    {
        _constrainedOptions = options;
        return this;
    }

    protected override IClassifyingQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate)
    {
        if (_capacity == -1)
        {
            throw new InvalidOperationException("No capacity was specified.");
        }

        return new ConstrainedFifoQdisc<THandle>(handle, predicate, _capacity, _constrainedOptions);
    }
}
