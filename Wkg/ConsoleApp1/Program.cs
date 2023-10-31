using System.Diagnostics;
using Wkg.Logging;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Loggers;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classifiers.Qdiscs;
using Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

Log.UseLogger(Logger.Create(LoggerConfiguration.Create()
    //.AddSink<ColoredThreadBasedConsoleSink>()
    .AddSink<ColoredConsoleSink>()
    .UseEntryGenerator<TracingLogEntryGenerator>()
    .RegisterMainThread(Thread.CurrentThread)
    .UseDefaultLogWriter(LogWriter.Blocking)));

ClassifyingWorkloadFactory<int> factory = new QdiscBuilder<int>()
    .UseMaximumConcurrency(4)
    .FlowExecutionContextToContinuations()
    .RunContinuationsOnCapturedContext()
    .UseAnonymousWorkloadPooling(poolSize: 64)
    .UseClassifyingRoot<RoundRobinQdisc<int, State>, State>(1, state => state.QdiscType == QdiscType.RoundRobin)
        .AddClasslessChild<FifoQdisc<int>>(2, state => state.QdiscType == QdiscType.Fifo).Build()
        .AddClassifyingChild<RoundRobinQdisc<int, SomeOtherState>, SomeOtherState>(3, otherState => true)
            .AddClassifyingChild<RoundRobinQdisc<int, SomeOtherState>, SomeOtherState>(10, otherState => true)
                .AddClassifyingChild<RoundRobinQdisc<int, SomeOtherState>, SomeOtherState>(11, otherState => true)
                    .AddClassifyingChild<RoundRobinQdisc<int, SomeOtherState>, SomeOtherState>(12, otherState => true)
                        .AddClassifyingChild<RoundRobinQdisc<int, SomeOtherState>, SomeOtherState>(13, otherState => true)
                            .AddClasslessChild<LifoQdisc<int>>(14).Build()
                            .Build()
                        .Build()
                    .Build()
                .Build()
            .AddClasslessChild<LifoQdisc<int>>(4).Build()
            .AddClasslessChild<FifoQdisc<int>>(5).Build()
            .AddClasslessChild<LifoQdisc<int>>(6).Build()
            .Build()
        .AddClasslessChild<LifoQdisc<int>>(7, state => state.QdiscType == QdiscType.Lifo).Build()
        .AddClasslessChild<FifoQdisc<int>>(8).Build()
        .Build();

AsyncLocal<int> asyncLocal = new()
{
    Value = 1337
};
Log.WriteInfo($"ThreadLocal: {asyncLocal.Value}");

SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext());

Log.WriteInfo(SynchronizationContext.Current?.ToString() ?? "null");

WorkloadResult<int> result = await factory.ScheduleAsync(flag =>
{
    Log.WriteInfo($"Hello from the root scheduler. The async local value is {asyncLocal.Value}.");
    Thread.Sleep(1000);
    Log.WriteInfo("Goodbye from the root scheduler!");
    return 42;
});
Log.WriteInfo($"Result: {result}");

Log.WriteInfo($"ThreadLocal: {asyncLocal.Value}");
Log.WriteInfo(SynchronizationContext.Current?.ToString() ?? "null");

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
        workloads2[i] = factory.ClassifyAsync(lifoState, DoStuff);
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
    wls[i] = factory.ScheduleAsync(flag =>
    {
        Thread.Sleep(100);
    });
}
Log.WriteInfo("Waiting for all workloads to complete...");
await Workload.WhenAll(wls);
Log.WriteFatal("DONE WITH TESTS");

ClasslessWorkloadFactory<int> simpleFactory = new QdiscBuilder<int>()
    .UseMaximumConcurrency(16)
    .FlowExecutionContextToContinuations()
    .RunContinuationsOnCapturedContext()
    .UseAnonymousWorkloadPooling(poolSize: 64)
    .UseClasslessRoot<FifoQdisc<int>>(1)
    .Build();

Log.WriteInfo("Starting simple tests...");
AwaitableWorkload[] wls2 = new AwaitableWorkload[80];
for (int i = 0; i < 80; i++)
{
    wls2[i] = simpleFactory.ScheduleAsync(flag =>
    {
        Thread.Sleep(100);
    });
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

enum QdiscType : int
{
    Unspecified,
    Fifo,
    Lifo,
    RoundRobin
}

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