﻿using Wkg.Threading.Workloads.Configuration;

namespace Wkg.Threading.Workloads.Queuing.Classless.Lifo;

/// <summary>
/// A qdisc that implements the Last-In-First-Out (LIFO) scheduling algorithm.
/// </summary>
public sealed class Lifo : ClasslessQdiscBuilder<Lifo>, IClasslessQdiscBuilder<Lifo>
{
    public static Lifo CreateBuilder() => new();

    protected override IClasslessQdisc<THandle> BuildInternal<THandle>(THandle handle) => new LifoQdisc<THandle>(handle);
}
