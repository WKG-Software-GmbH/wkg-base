// See https://aka.ms/new-console-template for more information
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
    .UseMaximumConcurrency(8)
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

const int WORKLOAD_COUNT = 10;
CancelableWorkload[] workloads1 = new CancelableWorkload[WORKLOAD_COUNT];

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
            workloads1[i] = factory.Classify(fifoState, cancellationFlag =>
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
            workloads1[i] = factory.Classify(fifoState, _ => workloads1[6].TryCancel());
            continue;
        }
        workloads1[i] = factory.Classify(fifoState, DoStuff);
    }
    CancelableWorkload[] workloads2 = new CancelableWorkload[WORKLOAD_COUNT];
    for (int i = 0; i < WORKLOAD_COUNT; i++)
    {
        if (i == 3)
        {
            // self-canceling workload (gotta test cancelling a running workload)
            workloads2[i] = factory.Classify(lifoState, cancellationFlag =>
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
            workloads2[i] = factory.Classify(lifoState, _ => workloads2[4].TryCancel());
            continue;
        }
        workloads2[i] = factory.Classify(lifoState, DoStuff);
    }

    factory.Classify(fifoState, () =>
    {
        Log.WriteDebug("Attempting to cancel completed workload.");
        bool result = workloads1[0].TryCancel();
        Log.WriteDebug($"Result: {result}");
    });

    TaskCompletionSource tcs1 = new();
    TaskCompletionSource tcs2 = new();
    TaskCompletionSource tcs3 = new();
    TaskCompletionSource tcs4 = new();

    factory.Schedule(6, () => Log.WriteEvent("Hello from Nested RR scheduler? I guess? WTF :)"));

    factory.Schedule(2, () =>
    {
        Log.WriteEvent("Signaling completion for tcs1.");
        tcs1.SetResult();
    });
    factory.Schedule(1, () =>
    {
        Log.WriteEvent("Signaling completion for tcs2.");
        tcs2.SetResult();
    });
    factory.Schedule(8, () =>
    {
        Log.WriteEvent("Signaling completion for tcs3.");
        tcs3.SetResult();
    });
    factory.Schedule(14, () =>
    {
        Log.WriteEvent("Signaling completion for tcs4.");
        tcs4.SetResult();
    });

    await Task.WhenAll(tcs1.Task, tcs2.Task, tcs3.Task, tcs4.Task);
    await Task.Delay(10000);
}
Log.WriteInfo("main thread exiting...");

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