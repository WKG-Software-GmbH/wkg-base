﻿using Wkg.Threading.Workloads.Configuration;

namespace Wkg.Threading.Workloads.Queuing.Classless.Fifo;

/// <summary>
/// A qdisc that implements the First-In-First-Out (FIFO) scheduling algorithm.
/// </summary>
public sealed class Fifo : ClasslessQdiscBuilder<Fifo>, IClasslessQdiscBuilder<Fifo>
{
    public static Fifo CreateBuilder() => new();

    protected override IClasslessQdisc<THandle> BuildInternal<THandle>(THandle handle) => 
        new FifoQdisc<THandle>(handle);
}
