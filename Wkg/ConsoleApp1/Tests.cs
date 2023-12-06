using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Csv;
using Wkg.Data.Pooling;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace ConsoleApp1;

[RPlotExporter]
[CsvMeasurementsExporter]
public class Tests
{
    [Params(1000)]
    public int WorkloadCount;

    [Params(2, 4, 8, 16)]
    public int Concurrency;

    [Params(10000, 250000)]
    public int Spins;

    private static readonly Random _bitmapRandom = new(42);
    private static readonly Random _lockingRandom = new(42);

    public ClassfulWorkloadFactory<int>[] _bitmaps = null!;

    private ClassfulWorkloadFactory<int>[] _locking = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _bitmaps = new ClassfulWorkloadFactory<int>[17];
        _locking = new ClassfulWorkloadFactory<int>[17];
        for (int i = 1; i < 17; i++)
        {
            _bitmaps[i] = WorkloadFactoryBuilder.Create<int>()
                .UseMaximumConcurrency(i)
                .UseClassfulRoot<RoundRobinBitmap>(1, root => root
                    .AddClassfulChild<RoundRobinBitmap>(2, child => child
                        .AddClassfulChild<RoundRobinBitmap>(3, child => child
                            .AddClasslessChild<Fifo>(4)
                            .AddClasslessChild<Fifo>(5)
                            .AddClasslessChild<Fifo>(6))
                        .AddClassfulChild<RoundRobinBitmap>(7, child => child
                            .AddClasslessChild<Fifo>(8)
                            .AddClasslessChild<Fifo>(9)
                            .AddClasslessChild<Fifo>(10))
                        .AddClassfulChild<RoundRobinBitmap>(11, child => child
                            .AddClasslessChild<Fifo>(12)
                            .AddClasslessChild<Fifo>(13)
                            .AddClasslessChild<Fifo>(14)))
                    .AddClassfulChild<RoundRobinBitmap>(15, child => child
                        .AddClassfulChild<RoundRobinBitmap>(16, child => child
                            .AddClasslessChild<Fifo>(17)
                            .AddClasslessChild<Fifo>(18)
                            .AddClasslessChild<Fifo>(19))
                        .AddClassfulChild<RoundRobinBitmap>(20, child => child
                            .AddClasslessChild<Fifo>(21)
                            .AddClasslessChild<Fifo>(22)
                            .AddClasslessChild<Fifo>(23))
                        .AddClassfulChild<RoundRobinBitmap>(24, child => child
                            .AddClasslessChild<Fifo>(25)
                            .AddClasslessChild<Fifo>(26)
                            .AddClasslessChild<Fifo>(27)))
                    .AddClassfulChild<RoundRobinBitmap>(28, child => child
                        .AddClassfulChild<RoundRobinBitmap>(29, child => child
                            .AddClasslessChild<Fifo>(30)
                            .AddClasslessChild<Fifo>(31)
                            .AddClasslessChild<Fifo>(32))
                        .AddClassfulChild<RoundRobinBitmap>(33, child => child
                            .AddClasslessChild<Fifo>(34)
                            .AddClasslessChild<Fifo>(35)
                            .AddClasslessChild<Fifo>(36))
                        .AddClassfulChild<RoundRobinBitmap>(37, child => child
                            .AddClasslessChild<Fifo>(38)
                            .AddClasslessChild<Fifo>(39)
                            .AddClasslessChild<Fifo>(40))));

            _locking[i] = WorkloadFactoryBuilder.Create<int>()
                .UseMaximumConcurrency(i)
                .UseClassfulRoot<RoundRobinLocking>(1, root => root
                    .AddClassfulChild<RoundRobinLocking>(2, child => child
                        .AddClassfulChild<RoundRobinLocking>(3, child => child
                            .AddClasslessChild<Fifo>(4)
                            .AddClasslessChild<Fifo>(5)
                            .AddClasslessChild<Fifo>(6))
                        .AddClassfulChild<RoundRobinLocking>(7, child => child
                            .AddClasslessChild<Fifo>(8)
                            .AddClasslessChild<Fifo>(9)
                            .AddClasslessChild<Fifo>(10))
                        .AddClassfulChild<RoundRobinLocking>(11, child => child
                            .AddClasslessChild<Fifo>(12)
                            .AddClasslessChild<Fifo>(13)
                            .AddClasslessChild<Fifo>(14)))
                    .AddClassfulChild<RoundRobinLocking>(15, child => child
                        .AddClassfulChild<RoundRobinLocking>(16, child => child
                            .AddClasslessChild<Fifo>(17)
                            .AddClasslessChild<Fifo>(18)
                            .AddClasslessChild<Fifo>(19))
                        .AddClassfulChild<RoundRobinLocking>(20, child => child
                            .AddClasslessChild<Fifo>(21)
                            .AddClasslessChild<Fifo>(22)
                            .AddClasslessChild<Fifo>(23))
                        .AddClassfulChild<RoundRobinLocking>(24, child => child
                            .AddClasslessChild<Fifo>(25)
                            .AddClasslessChild<Fifo>(26)
                            .AddClasslessChild<Fifo>(27)))
                    .AddClassfulChild<RoundRobinLocking>(28, child => child
                        .AddClassfulChild<RoundRobinLocking>(29, child => child
                            .AddClasslessChild<Fifo>(30)
                            .AddClasslessChild<Fifo>(31)
                            .AddClasslessChild<Fifo>(32))
                        .AddClassfulChild<RoundRobinLocking>(33, child => child
                            .AddClasslessChild<Fifo>(34)
                            .AddClasslessChild<Fifo>(35)
                            .AddClasslessChild<Fifo>(36))
                        .AddClassfulChild<RoundRobinLocking>(37, child => child
                            .AddClasslessChild<Fifo>(38)
                            .AddClasslessChild<Fifo>(39)
                            .AddClasslessChild<Fifo>(40))));
        }
    }

    [Benchmark]
    public async Task Bitmap()
    {
        PooledArray<AwaitableWorkload> workloads = ArrayPool.Rent<AwaitableWorkload>(WorkloadCount);
        ClassfulWorkloadFactory<int> bitmap = _bitmaps[Concurrency];
        int handle = _bitmapRandom.Next(1, 41);
        await Parallel.ForAsync(0, workloads.Length, (i, _) =>
        {
            workloads.Array[i] = bitmap.ScheduleAsync(handle, Work);
            return ValueTask.CompletedTask;
        });
        await Workload.WhenAll(workloads.Array[..workloads.Length]);
        ArrayPool.Return(workloads);
    }

    [Benchmark]
    public async Task Locking()
    {
        PooledArray<AwaitableWorkload> workloads = ArrayPool.Rent<AwaitableWorkload>(WorkloadCount);
        ClassfulWorkloadFactory<int> locking = _locking[Concurrency];
        int handle = _lockingRandom.Next(1, 41);
        await Parallel.ForAsync(0, workloads.Length, (i, _) =>
        {
            workloads.Array[i] = locking.ScheduleAsync(handle, Work);
            return ValueTask.CompletedTask;
        });
        await Workload.WhenAll(workloads.Array[..workloads.Length]);
        ArrayPool.Return(workloads);
    }

    private void Work(CancellationFlag flag)
    {
        flag.ThrowIfCancellationRequested();
        Thread.SpinWait(Spins);
    }
}

file class Container
{
    public volatile int Count;
    public long Result;
}