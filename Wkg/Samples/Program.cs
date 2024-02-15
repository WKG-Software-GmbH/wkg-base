/* ==================================================================================================
 * 
 * My beautiful test-playground-thingy for API design and testing. 
 * This is probably not what you're looking for :)
 * 
 * ================================================================================================== */

using System.Diagnostics;
using Wkg.Logging;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Loggers;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.DependencyInjection.Implementations;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;
using Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;
using Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;
using Wkg.Threading.Workloads.Queuing.Classless.ConstrainedLifo;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;
using Wkg.Threading.Workloads.Queuing.Classless.Lifo;
using Wkg.Threading.Workloads.Queuing.Classless.PriorityFifoFast;

//Stopwatch sw = Stopwatch.StartNew();
//for (int i = 0; i < 1000; i++)
//{
//    ReliableSpinner.Spin(1 << 20);
//}
//sw.Stop();
//Log.WriteInfo($"SpinWait: {TimeSpan.FromTicks(sw.ElapsedTicks / 1000)}");

//Environment.SetEnvironmentVariable("R_HOME", @"E:\software\R-4.3.2");
//BenchmarkRunner.Run<CashVsTpl>();
//Console.ReadLine();
//return;
Log.UseLogger(Logger.Create(LoggerConfiguration.Create()
    //.AddSink<ColoredThreadBasedConsoleSink>()
    .AddSink<ColoredConsoleSink>()
    .SetMinimumLogLevel(LogLevel.Diagnostic)
    .UseEntryGenerator<TracingLogEntryGenerator>()
    .RegisterMainThread(Thread.CurrentThread)
    .UseDefaultLogWriter(LogWriter.Blocking)));

//Tests tests = new()
//{
//    Concurrency = 2,
//    Depth = 6,
//};
//tests.GlobalSetup();
//Random random = new(42);

//for (int i = 0; i < 250; i++)
//{
//    for (int j = 0; j < 2048; j++)
//    {
//        await tests.Bitmap();
//        Thread.SpinWait(random.Next(0, 1000));
//        Log.WriteInfo($"Iteration {i} - {j}");
//    }
//    Log.WriteDebug($"{i}");
//}

//Log.WriteInfo("AAAAAAAAAAAAAAAA");
//return;

using (ClassfulWorkloadFactory<QdiscType> clubmappFactory = WorkloadFactoryBuilder.Create<QdiscType>()
    // the root scheduler is allowed to run up to 4 workers at the same time
    .UseMaximumConcurrency(8)
    // async/await continuations will run in the same async context as the scheduling thread
    .FlowExecutionContextToContinuations()
    // async/await continuations will run with the same synchronization context (e.g, UI thread)
    .RunContinuationsOnCapturedContext()
    // anonymous workloads will be pooled and reused.
    // no allocations will be made up until more than 64 workloads are scheduled at the same time
    // note that this does not apply to awaitable workloads (e.g, workloads that return a result to the caller)
    // or to stateful workloads (e.g, workloads that capture state)
    .UseAnonymousWorkloadPooling(poolSize: 64)
    // the root scheduler will fairly dequeue workloads alternating between the two child schedulers (Round Robin)
    // a classifying root scheduler can have children and also allows dynamic assignment of workloads to child schedulers
    // based on some state object
    .UseClassfulRoot<RoundRobinBitmap>(QdiscType.RoundRobin, roundRobinClassBuilder => roundRobinClassBuilder
        .ConfigureClassificationPredicates(classificationBuilder => classificationBuilder
            .AddPredicate<State>(state => state.QdiscType == QdiscType.RoundRobin))
        // one child scheduler will dequeue workloads in a First In First Out manner
        .AddClasslessChild<Fifo>(QdiscType.Fifo)
        // the other child scheduler will dequeue workloads in a Last In First Out manner
        .AddClasslessChild<ConstrainedLifo>(QdiscType.Lifo, qdisc => qdisc
            .WithConstrainedPrioritizationOptions(ConstrainedPrioritizationOptions.MinimizeWorkloadCancellation)
            .WithCapacity(16))))
{
    const int loops = 10_000_000;
    SharedInt sharedInt = new()
    {
        Value = loops
    };
    ManualResetEventSlim mres = new(false);
    for (int i = 0; i < loops; i++)
    {
        clubmappFactory.Schedule(() =>
        {
            int result = Interlocked.Decrement(ref sharedInt.Value);
            if (result % 100000 == 0)
            {
                Log.WriteInfo($"Remaining: {result}");
            }
            if (result == 0)
            {
                Log.WriteInfo("All workloads have completed.");
                mres.Set();
            }
        });
    }
    mres.Wait();
    await clubmappFactory.ScheduleAsync(QdiscType.Fifo, flag =>
    {
        Log.WriteInfo("Starting background work...");
        for (int i = 0; i < 10; i++)
        {
            flag.ThrowIfCancellationRequested();
            Log.WriteDiagnostic($"doing work ...");
            Thread.Sleep(100);
        }
        Log.WriteInfo("Done with background work.");
    });
}

using ClassfulWorkloadFactoryWithDI<int> factory = WorkloadFactoryBuilder.Create<int>()
    .UseMaximumConcurrency(2)
    .FlowExecutionContextToContinuations()
    .RunContinuationsOnCapturedContext()
    .UseDependencyInjection<PooledWorkloadServiceProviderFactory>(services => services
        .AddService<IMyService, MyService>(() => new MyService())
        .AddService(() => new MyService()))
    .UseAnonymousWorkloadPooling(poolSize: 64)
    .UseClassfulRoot<GeneralizedFairQueuing<int>>(1, root => root
        .WithClassificationPredicate(o => o is State state && state.QdiscType == QdiscType.RoundRobin)
        .WithLocalQueue<Fifo>()
        .AssumeMaximimNumberOfDistinctPayloads(16)
        .PreferFairness(PreferredFairness.LongTerm)
        .SetMeasurementSampleLimit(1000)
        .UsePreciseMeasurements(true)
        .UseSchedulerTimeModel(VirtualTimeModel.Average)
        .UseExecutionTimeModel(VirtualTimeModel.Average)
        .AddClasslessChild<PriorityFifoFast>(1000, 1d, 1d, classifier =>
            classifier.AddPredicate<long>(l => true),
            child => child
                .WithBandCount(4)
                .WithBandHandles(1000, 1001, 1002, 1003)
                .WithDefaultBand(3)
                .WithBandSelector(state => state switch
                {
                    long and < 0 => 0,
                    long and < 100 => 1,
                    long and < 1000 => 2,
                    _ => -1
                }))
        .AddClasslessChild<Fifo>(2, workloadSchedulingWeight: 2d, executionPunishmentFactor: 2d, classifier => classifier
            .AddPredicate<State>(state => state.QdiscType == QdiscType.Fifo)
            .AddPredicate<int>(i => (i & 1) == 0))
        .AddClasslessChild<Lifo>(14)
        .AddClasslessChild<Lifo>(7, workloadSchedulingWeight: 0.5d, executionPunishmentFactor: 0.5d, classifier => classifier
            .AddPredicate<State>(state => state.QdiscType == QdiscType.Lifo)
            .AddPredicate<int>(i => (i & 1) == 1))
        .AddClasslessChild<ConstrainedFifo>(8, qdisc => qdisc
            .WithCapacity(8)));

factory.ScheduleAsync(1, flag => Log.WriteInfo("Hello from the root scheduler!")).ContinueWith(_ =>
{
    Log.WriteInfo("Hello from the root scheduler again!");
});

List<int> myData = Enumerable.Range(0, 10000).ToList();
int sum = myData.Sum();
Log.WriteInfo($"Sum: {sum}");

int[] result = await factory.TransformAllAsync(myData, (data, cancellationFlag) => data * 10);

Log.WriteInfo($"Result Sum 1: {result.Select(i => (long)i).Sum()}");
await Task.Delay(2500);
Log.WriteInfo($"Sum: {sum}");

int[] resultClassified = await factory.ClassifyAndTransformAllAsync(myData, (data, cancellationFlag) => data * 10);

Log.WriteInfo($"Result Sum 2: {resultClassified.Select(i => (long)i).Sum()}");
await Task.Delay(2500);

CancellationTokenSource cts = new();
Workload workload = factory.ScheduleAsync(flag =>
{
    Log.WriteInfo($"Hello from the root scheduler again");
    Thread.Sleep(1000);
    flag.ThrowIfCancellationRequested();
    Log.WriteFatal("I should not have gotten here.");
}, cts.Token);

cts.Cancel();

Workload wl2 = (Workload)await Workload.WhenAny(workload);

Debug.Assert(wl2.IsCompleted);

WorkloadResult result2 = wl2.GetAwaiter().GetResult();

Log.WriteInfo($"Result: {result2}");

Workload<string> workload3 = factory.ScheduleAsync(flag =>
{
    Log.WriteInfo($"Hello from the root scheduler again again");
    Thread.Sleep(1000);
    return "Wow. The blocking wait actually worked.";
});

WorkloadResult<string> result3 = workload3.GetAwaiter().GetResult();

Log.WriteInfo($"Result: {result3}");
if (result3.TryGetResult(out string? value))
{
    Log.WriteInfo($"Result: {value}");
}

Workload wl4 = factory.ScheduleAsync(flag =>
{
    Log.WriteInfo($"Hello from the root scheduler again again again");
    Thread.Sleep(1000);
    throw new Exception("This is an exception.");
});

WorkloadResult result4 = await wl4;

Log.WriteInfo($"Result: {result4}");

TaskWorkload taskwl = factory.ScheduleTaskAsync(async flag =>
{
    Log.WriteInfo($"Hello from the root scheduler from a Task!");
    await Task.Delay(1000);
    Log.WriteInfo($"Hello from the root scheduler from a Task! (after delay)");
    throw new Exception("This is an exception.");
});

WorkloadResult taskResult = await taskwl;

Log.WriteInfo($"Result: {taskResult}");

AwaitableWorkload[] taskWorkloads = new AwaitableWorkload[10];

for (int i = 0; i < taskWorkloads.Length; i++)
{
    Wrapper w = new(i);
    taskWorkloads[i] = factory.ScheduleTaskAsync(async flag =>
    {
        Log.WriteInfo($"Hello from Task {w.Value}");
        await Task.Delay(1000);
        Log.WriteInfo($"Bye from Task {w.Value}");
    });
}

await Workload.WhenAll(taskWorkloads);

const int WORKLOAD_COUNT = 10;
AwaitableWorkload[] workloads1 = new AwaitableWorkload[WORKLOAD_COUNT];

State fifoState = new(QdiscType.Fifo);
State lifoState = new(QdiscType.Lifo);
State state = new(QdiscType.RoundRobin);

for (int times = 0; times < 2; times++)
{
    for (int i = 0; i < WORKLOAD_COUNT; i++)
    {
        if (i == 3)
        {
            // self-canceling workload (gotta test cancelling a running workload)
            workloads1[i] = factory.ClassifyAsync(fifoState, cancellationFlag =>
            {
                Log.WriteInfo("#1 Cancelling myself :P");
                for (int i = 0; i < 10; i++)
                {
                    cancellationFlag.ThrowIfCancellationRequested();
                    if (i == 5)
                    {
                        workloads1[3].TryCancel();
                    }
                }
                Log.WriteWarning("I should not have gotten here.");
            });
            continue;
        }
        if (i == 5)
        {
            workloads1[i] = factory.ClassifyAsync(fifoState, _ => workloads1[4].TryCancel());
            continue;
        }
        workloads1[i] = factory.ClassifyAsync(fifoState, DoStuff);
    }
    AwaitableWorkload[] workloads2 = new AwaitableWorkload[WORKLOAD_COUNT];
    for (int i = 0; i < WORKLOAD_COUNT; i++)
    {
        if (i == 3)
        {
            // self-canceling workload (gotta test cancelling a running workload)
            workloads2[i] = factory.ClassifyAsync(lifoState, cancellationFlag =>
            {
                Log.WriteInfo("#2 Cancelling myself :P");
                for (int i = 0; i < 10; i++)
                {
                    cancellationFlag.ThrowIfCancellationRequested();
                    if (i == 5)
                    {
                        workloads2[3].TryCancel();
                    }
                }
                Log.WriteWarning("I should not have gotten here.");
            });
            continue;
        }
        if (i == 5)
        {
            workloads2[i] = factory.ClassifyAsync(lifoState, _ => workloads2[4].TryCancel());
            continue;
        }
        workloads2[i] = factory.ClassifyAsync(lifoState, DoStuffShort);
    }

    factory.Classify(fifoState, () =>
    {
        Log.WriteDebug("Attempting to cancel completed workload.");
        bool result = workloads1[0].TryCancel();
        Log.WriteDebug($"Result: {result}");
    });
    factory.Schedule(14, () => Log.WriteEvent("Hello from Nested RR scheduler? I guess? WTF :)"));

    await Workload.WhenAll(workloads1);
    await Workload.WhenAll(workloads2);
}
Log.WriteFatal("STARTING TESTS");
AwaitableWorkload[] wls = new AwaitableWorkload[80];
for (int i = 0; i < 80; i++)
{
    wls[i] = factory.ScheduleAsync(_ => Thread.Sleep(100));
}
Log.WriteInfo("Waiting for all workloads to complete...");
await Workload.WhenAll(wls);
Log.WriteFatal("DONE WITH TESTS");

using ClasslessWorkloadFactory<int> simpleFactory = WorkloadFactoryBuilder.Create<int>()
    .UseMaximumConcurrency(16)
    .FlowExecutionContextToContinuations()
    .RunContinuationsOnCapturedContext()
    .UseAnonymousWorkloadPooling(poolSize: 64)
    .UseClasslessRoot<Fifo>(1);

Log.WriteInfo("Starting simple tests...");
AwaitableWorkload[] wls2 = new AwaitableWorkload[80];
for (int i = 0; i < 80; i++)
{
    wls2[i] = simpleFactory.ScheduleAsync(flag => Thread.Sleep(100));
}
Log.WriteInfo("Waiting for all workloads to complete...");
await Workload.WhenAll(wls2);
Log.WriteInfo("Done with simple tests.");

static void DoStuff(CancellationFlag cancellationFlag)
{
    Log.WriteInfo("Doing stuff...");
    for (int i = 0; i < 10; i++)
    {
        cancellationFlag.ThrowIfCancellationRequested();
        Thread.Sleep(100);
    }
    Log.WriteInfo("Done doing stuff.");
}

static void DoStuffShort(CancellationFlag cancellationFlag)
{
    Log.WriteInfo("Doing stuff...");
    for (int i = 0; i < 5; i++)
    {
        cancellationFlag.ThrowIfCancellationRequested();
        Thread.Sleep(100);
    }
    Log.WriteInfo("Done doing stuff.");
}

enum QdiscType : int
{
    Unspecified,
    Fifo,
    Lifo,
    RoundRobin
}

class SharedInt
{
    public int Value;
}

record Wrapper(int Value);

record State(QdiscType QdiscType);
record SomeOtherState();

class MySynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state) => base.Post(state =>
    {
        SetSynchronizationContext(this);
        d.Invoke(state);
    }, state);
}

interface IMyService
{
    int GetNext();
}

class MyService : IMyService
{
    private int _counter;

    public MyService()
    {
        _counter = 0;
    }

    public int GetNext() => Interlocked.Increment(ref _counter);
}

enum SensorToS
{
    Root,
    Critical,
    Normal,
    OperatorInput,
    Telemetry,
    SensorGroups,
    SensorGroup1,
    SensorGroup2,
    SensorGroup3,
    Volatile,
    ProcessOnIdle
}

record EmergencyStopRequest();
record HeartbeatRequest();
record MotorRpmReading(int MotorId, int Rpm)
{
    public static int CRITICAL_RPM = 1000;
}
record GyroReading(float X, float Y, float Z);