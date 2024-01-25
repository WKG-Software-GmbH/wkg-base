using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Csv;
using System.Threading.Channels;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful.PrioFast;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace ConsoleApp1;

[RPlotExporter]
[MaxRelativeError(0.01)]
[CsvMeasurementsExporter]
public class CashVsTpl
{
    public int WorkloadCount => 1000;

    [Params(2, 4, 8, 16)]
    public int Concurrency;
    private const int MAX_CONCURRENCY = 16;

    public int Spins => 768;

    public int Depth => 3;

    public int BranchingFactor => 4;

    private ClasslessWorkloadFactory<int>[] _cash = null!;
    private ClassfulWorkloadFactory<int>[] _cashClassful = null!;
    private ClassfulWorkloadFactory<int>[] _cashClassfulOptimized = null!;

    private readonly ManualResetEventSlim _lockingMres = new(false);
    private readonly ManualResetEventSlim _lockingMresOptimized = new(false);
    private readonly ManualResetEventSlim _classlessMres = new(false);

    private AwaitableWorkload[] _lockingWorkloads = null!;
    private AwaitableWorkload[] _lockingWorkloadsOptimized = null!;
    private AwaitableWorkload[] _classlessWorkloads = null!;

    private Task[][] _channelTasks = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _lockingWorkloads = new AwaitableWorkload[WorkloadCount];
        _classlessWorkloads = new AwaitableWorkload[WorkloadCount];
        _lockingWorkloadsOptimized = new AwaitableWorkload[WorkloadCount];
        _cash = new ClasslessWorkloadFactory<int>[MAX_CONCURRENCY];
        _cashClassful = new ClassfulWorkloadFactory<int>[MAX_CONCURRENCY];
        _cashClassfulOptimized = new ClassfulWorkloadFactory<int>[MAX_CONCURRENCY];
        _channelTasks = new Task[MAX_CONCURRENCY][];
        for (int concurrency = 0; concurrency < MAX_CONCURRENCY; concurrency++)
        {
            _channelTasks[concurrency] = new Task[concurrency + 1];
            _cash[concurrency] = WorkloadFactoryBuilder.Create<int>()
                .UseMaximumConcurrency(concurrency + 1)
                .UseClasslessRoot<Fifo>(1);
            _cashClassful[concurrency] = CreateLockingFactory(concurrency + 1, Depth + 1, BranchingFactor);
            _cashClassfulOptimized[concurrency] = CreateOptimizedFactory(concurrency + 1, Depth + 1, BranchingFactor);
        }
    }

    public static ClassfulWorkloadFactory<int> CreateLockingFactory(int concurrency, int depth, int branchingFactor)
    {
        HandleCounter handleCounter = new(2);
        return WorkloadFactoryBuilder.Create<int>()
            .UseMaximumConcurrency(concurrency)
            .UseClassfulRoot<PrioFastLocking<int>>(1, root => ConfigureLockingLevel(root, depth - 1, branchingFactor, handleCounter));
    }

    private static void ConfigureLockingLevel(PrioFastLocking<int> builder, int remainingDepth, int branchingFactor, HandleCounter nextHandle)
    {
        if (remainingDepth == 0)
        {
            for (int i = 0; i < branchingFactor; i++, nextHandle.Handle++)
            {
                builder.AddClasslessChild<Fifo>(nextHandle.Handle, i);
            }
        }
        else
        {
            for (int i = 0; i < branchingFactor; i++)
            {
                int handle = nextHandle.Handle;
                nextHandle.Handle++;
                builder.AddClassfulChild<PrioFastLocking<int>>(handle, i, child => ConfigureLockingLevel(child, remainingDepth - 1, branchingFactor, nextHandle));
            }
        }
    }

    public static ClassfulWorkloadFactory<int> CreateOptimizedFactory(int concurrency, int depth, int branchingFactor)
    {
        HandleCounter handleCounter = new(2);
        return WorkloadFactoryBuilder.Create<int>()
            .UseMaximumConcurrency(concurrency)
            .UseClassfulRoot<PrioFastLockingBitmap<int>>(1, root => ConfigureOptimizedLevel(root, depth - 1, branchingFactor, handleCounter));
    }

    private static void ConfigureOptimizedLevel(PrioFastLockingBitmap<int> builder, int remainingDepth, int branchingFactor, HandleCounter nextHandle)
    {
        if (remainingDepth == 0)
        {
            for (int i = 0; i < branchingFactor; i++, nextHandle.Handle++)
            {
                builder.AddClasslessChild<Fifo>(nextHandle.Handle, i);
            }
        }
        else
        {
            for (int i = 0; i < branchingFactor; i++)
            {
                int handle = nextHandle.Handle;
                nextHandle.Handle++;
                builder.AddClassfulChild<PrioFastLockingBitmap<int>>(handle, i, child => ConfigureOptimizedLevel(child, remainingDepth - 1, branchingFactor, nextHandle));
            }
        }
    }

    [Benchmark(Baseline = true)]
    public int Perfect()
    {
        int result = 0;
        for (int i = 0; i < WorkloadCount / Concurrency; i++)
        {
            result ^= ReliableSpinner.Spin(Spins);
        }
        return result;
    }

    [Benchmark]
    public async Task ParallelFor() => await Parallel.ForAsync(0, WorkloadCount, new ParallelOptions() { MaxDegreeOfParallelism = Concurrency }, ParallelForWork);

    private ValueTask ParallelForWork(int _, CancellationToken _1)
    {
        int r = Work(CancellationFlag.None);
        return ValueTask.CompletedTask;
    }

    [Benchmark]
    public async Task TplChannels()
    {
        Task[] tasks = _channelTasks[Concurrency - 1];
        Channel<Func<CancellationFlag, int>> channel = Channel.CreateUnbounded<Func<CancellationFlag, int>>();
        for (int i = 0; i < Concurrency; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await foreach (Func<CancellationFlag, int> action in channel.Reader.ReadAllAsync())
                {
                    action.Invoke(CancellationFlag.None);
                }
            });
        }
        for (int i = 0; i < WorkloadCount; i++)
        {
            await channel.Writer.WriteAsync(Work);
        }
        channel.Writer.Complete();
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task CashClassless()
    {
        AwaitableWorkload[] workloads = _classlessWorkloads;
        ClasslessWorkloadFactory<int> fifo = _cash[Concurrency - 1];
        _classlessMres.Reset();
        for (int i = 0; i < Concurrency; i++)
        {
            fifo.Schedule(_classlessMres.Wait);
        }
        for (int i = 0; i < workloads.Length; i++)
        {
            workloads[i] = fifo.ScheduleAsync(1, Work);
        }
        _classlessMres.Set();
        await Workload.WhenAll(workloads);
    }

    [Benchmark]
    public async Task CashClassful()
    {
        AwaitableWorkload[] workloads = _lockingWorkloads;
        ClassfulWorkloadFactory<int> lockingBitmap = _cashClassful[Concurrency - 1];
        int totalNodes = NodeCount(Depth, BranchingFactor);
        _lockingMres.Reset();
        for (int i = 0; i < Concurrency; i++)
        {
            lockingBitmap.Schedule(_lockingMres.Wait);
        }
        for (int i = 0; i < workloads.Length; i++)
        {
            int handle = i % totalNodes + 1;
            workloads[i] = lockingBitmap.ScheduleAsync(handle, Work);
        }
        _lockingMres.Set();
        await Workload.WhenAll(workloads);
    }

    [Benchmark]
    public async Task CashClassfulOptimized()
    {
        AwaitableWorkload[] workloads = _lockingWorkloadsOptimized;
        ClassfulWorkloadFactory<int> lockingBitmap = _cashClassfulOptimized[Concurrency - 1];
        int totalNodes = NodeCount(Depth, BranchingFactor);
        _lockingMresOptimized.Reset();
        for (int i = 0; i < Concurrency; i++)
        {
            lockingBitmap.Schedule(_lockingMresOptimized.Wait);
        }
        for (int i = 0; i < workloads.Length; i++)
        {
            int handle = i % totalNodes + 1;
            workloads[i] = lockingBitmap.ScheduleAsync(handle, Work);
        }
        _lockingMresOptimized.Set();
        await Workload.WhenAll(workloads);
    }

    private int Work(CancellationFlag flag) => ReliableSpinner.Spin(Spins);

    public static int NodeCount(int depth, int branchingFactor) => (int)(Math.Pow(branchingFactor, depth + 1) - 1) / (branchingFactor - 1);
}
