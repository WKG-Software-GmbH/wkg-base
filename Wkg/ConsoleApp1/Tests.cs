using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Csv;
using Wkg.Data.Pooling;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classful;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace ConsoleApp1;

[RPlotExporter]
[CsvMeasurementsExporter]
public class Tests
{
    //[Params(1000)]
    public int WorkloadCount => 1000;

    [Params(2, 4, 8, 16)]
    public int Concurrency;
    private const int MAX_CONCURRENCY = 16;

    [Params(1, 2, 3, 4, 5, 6)]
    public int Depth;
    private const int MAX_DEPTH = 6;

    //[Params(4)]
    public int BranchingFactor => 4;

    //[Params(100000)]
    public int Spins => 100000;

    private static readonly Random _bitmapRandom = new(42);
    private static readonly Random _lockingRandom = new(42);

    public ClassfulWorkloadFactory<int>[,] _bitmaps = null!;

    private ClassfulWorkloadFactory<int>[,] _locking = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _bitmaps = new ClassfulWorkloadFactory<int>[MAX_CONCURRENCY, MAX_DEPTH];
        _locking = new ClassfulWorkloadFactory<int>[MAX_CONCURRENCY, MAX_DEPTH];
        for (int concurrency = 0; concurrency < MAX_CONCURRENCY; concurrency++)
        {
            for (int depth = 0; depth < MAX_DEPTH; depth++)
            {
                // concurrency and depth are 1-based (obviously), so adjust array indices accordingly
                _bitmaps[concurrency, depth] = CreateFactory<RoundRobinBitmap56>(concurrency + 1, depth + 1, BranchingFactor);
                _locking[concurrency, depth] = CreateFactory<RoundRobinLocking>(concurrency + 1, depth + 1, BranchingFactor);
            }
        }
    }

    public static ClassfulWorkloadFactory<int> CreateFactory<TQdisc>(int concurrency, int depth, int branchingFactor)
        where TQdisc : ClassfulQdiscBuilder<TQdisc>, IClassfulQdiscBuilder<TQdisc>
    {
        HandleCounter handleCounter = new(2);
        return WorkloadFactoryBuilder.Create<int>()
            .UseMaximumConcurrency(concurrency)
            .UseClassfulRoot<TQdisc>(1, root => ConfigureLevel(root, depth - 1, branchingFactor, handleCounter));
    }

    private static void ConfigureLevel<TQdisc>(ClassfulBuilder<int, SimplePredicateBuilder, TQdisc> builder, int remainingDepth, int branchingFactor, HandleCounter nextHandle)
        where TQdisc : ClassfulQdiscBuilder<TQdisc>, IClassfulQdiscBuilder<TQdisc>
    {
        if (remainingDepth == 0)
        {
            for (int i = 0; i < branchingFactor; i++, nextHandle.Handle++)
            {
                builder.AddClasslessChild<Fifo>(nextHandle.Handle);
            }
        }
        else
        {
            for (int i = 0; i < branchingFactor; i++)
            {
                int handle = nextHandle.Handle;
                nextHandle.Handle++;
                builder.AddClassfulChild<TQdisc>(handle, child => ConfigureLevel(child, remainingDepth - 1, branchingFactor, nextHandle));
            }
        }
    }

    public static int NodeCount(int depth, int branchingFactor) => (int)(Math.Pow(branchingFactor, depth + 1) - 1) / (branchingFactor - 1);

    [Benchmark]
    public async Task Bitmap()
    {
        PooledArray<AwaitableWorkload> workloads = ArrayPool.Rent<AwaitableWorkload>(WorkloadCount);
        ClassfulWorkloadFactory<int> bitmap = _bitmaps[Concurrency - 1, Depth - 1];
        int totalNodes = NodeCount(Depth, BranchingFactor);
        await Parallel.ForAsync(0, workloads.Length, (i, _) =>
        {
            int handle = Random.Shared.Next(1, totalNodes + 1);
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
        ClassfulWorkloadFactory<int> locking = _locking[Concurrency - 1, Depth - 1];
        int totalNodes = NodeCount(Depth, BranchingFactor);
        await Parallel.ForAsync(0, workloads.Length, (i, _) =>
        {
            int handle = Random.Shared.Next(1, totalNodes + 1);
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

    private class HandleCounter(int handle)
    {
        public int Handle = handle;
    }
}
