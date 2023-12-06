﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Csv;
using Wkg.Data.Pooling;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classless.PriorityFifoFast;

namespace ConsoleApp1;

[RPlotExporter]
[CsvMeasurementsExporter]
public class Tests
{
    [Params(1000)]
    public int WorkloadCount;

    [Params(2, 4, 8)]
    public int Concurrency;

    [Params(0, 10, 20)]
    public int SleepTime;

    private static readonly Random _bitmapRandom = new(42);
    private static readonly Random _lockingRandom = new(42);

    private ClasslessWorkloadFactory<int>[] _bitmaps = null!;

    private ClasslessWorkloadFactory<int>[] _locking = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _bitmaps = new ClasslessWorkloadFactory<int>[17];
        _locking = new ClasslessWorkloadFactory<int>[17];
        for (int i = 1; i < 17; i++)
        {
            _bitmaps[i] = WorkloadFactoryBuilder.Create<int>()
                .UseMaximumConcurrency(i)
                .UseClasslessRoot<PriorityFifoFast>(1, root => root
                    .WithBandHandles(1, 2, 3, 4, 5));

            _locking[i] = WorkloadFactoryBuilder.Create<int>()
                .UseMaximumConcurrency(i)
                .UseClasslessRoot<PriorityFifoFastLocking>(1, root => root
                    .WithBandHandles(1, 2, 3, 4, 5));
        }
    }

    [Benchmark]
    public async Task Bitmap()
    {
        PooledArray<AwaitableWorkload> workloads = ArrayPool.Rent<AwaitableWorkload>(WorkloadCount);
        ClasslessWorkloadFactory<int> bitmap = _bitmaps[Concurrency];
        for (int i = 0; i < workloads.Length; i++)
        {
            int handle = _bitmapRandom.Next(1, 6);
            workloads.Array[i] = bitmap.ScheduleAsync(handle, Work);
        }
        await Workload.WhenAll(workloads.Array[..workloads.Length]);
        ArrayPool.Return(workloads);
    }

    [Benchmark]
    public async Task Locking()
    {
        PooledArray<AwaitableWorkload> workloads = ArrayPool.Rent<AwaitableWorkload>(WorkloadCount);
        ClasslessWorkloadFactory<int> counter = _locking[Concurrency];
        for (int i = 0; i < workloads.Length; i++)
        {
            int handle = _lockingRandom.Next(1, 6);
            workloads.Array[i] = counter.ScheduleAsync(handle, Work);
        }
        await Workload.WhenAll(workloads.Array[..workloads.Length]);
        ArrayPool.Return(workloads);
    }

    private void Work(CancellationFlag flag)
    {
        flag.ThrowIfCancellationRequested();
        Thread.Sleep(SleepTime);
    }
}

file class Container
{
    public volatile int Count;
    public long Result;
}