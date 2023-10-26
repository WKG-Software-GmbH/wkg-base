// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using Wkg.Extensions.Common;
using Wkg.Logging;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Loggers;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Internals;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Queuing.Classifiers.Qdiscs;
using Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

Log.UseLogger(Logger.Create(LoggerConfiguration.Create()
    //.AddSink<ColoredThreadBasedConsoleSink>()
    .AddSink<ColoredConsoleSink>()
    .UseEntryGenerator<TracingLogEntryGenerator>()
    .RegisterMainThread(Thread.CurrentThread)
    .UseDefaultLogWriter(LogWriter.Blocking)));

RoundRobinQdisc<QdiscType, State> root = new(QdiscType.RoundRobin, state => state.QdiscType == QdiscType.RoundRobin);
WorkloadScheduler baseScheduler = new(root, maximumConcurrencyLevel: 2);
root.To<IQdisc>().InternalInitialize(baseScheduler);
FifoQdisc<QdiscType> fifo = new(QdiscType.Fifo);
LifoQdisc<QdiscType> lifo = new(QdiscType.Lifo);
root.TryAddChild(fifo, state => state.QdiscType == QdiscType.Fifo);
root.TryAddChild(lifo, state => state.QdiscType == QdiscType.Lifo);
WorkloadFactory<QdiscType> factory = new(root);

const int WORKLOAD_COUNT = 10;
Workload[] workloads1 = new Workload[WORKLOAD_COUNT];

State fifoState = new(QdiscType.Fifo);
State lifoState = new(QdiscType.Lifo);
State state = new(QdiscType.RoundRobin);

for (int i = 0; i < WORKLOAD_COUNT; i++)
{
    if (i == 3)
    {
        // self-canceling workload (gotta test cancelling a running workload)
        workloads1[i] = factory.Schedule(fifoState, cancellationFlag =>
        {
            Log.WriteInfo("Cancelling myself :P");
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
        workloads1[i] = factory.Schedule(fifoState, _ => workloads1[6].TryCancel());
        continue;
    }
    workloads1[i] = factory.Schedule(fifoState, DoStuff);
}
Workload[] workloads2 = new Workload[WORKLOAD_COUNT];
for (int i = 0; i < WORKLOAD_COUNT; i++)
{
    if (i == 3)
    {
        // self-canceling workload (gotta test cancelling a running workload)
        workloads2[i] = factory.Schedule(lifoState, cancellationFlag =>
        {
            Log.WriteInfo("Cancelling myself :P");
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
        workloads2[i] = factory.Schedule(lifoState, _ => workloads2[6].TryCancel());
        continue;
    }
    workloads2[i] = factory.Schedule(lifoState, DoStuff);
}

factory.Schedule(fifoState, _ =>
{
    Log.WriteDebug("Attempting to cancel completed workload.");
    bool result = workloads1[0].TryCancel();
    Log.WriteDebug($"Result: {result}");
});

TaskCompletionSource tcs1 = new();
TaskCompletionSource tcs2 = new();
TaskCompletionSource tcs3 = new();

factory.Schedule(fifoState, _ =>
{
    Log.WriteEvent("Signaling completion for tcs1.");
    tcs1.SetResult();
});
factory.Schedule(lifoState, _ =>
{
    Log.WriteEvent("Signaling completion for tcs2.");
    tcs2.SetResult();
});
factory.Schedule(state, _ =>
{
    Log.WriteEvent("Signaling completion for tcs3.");
    tcs3.SetResult();
});

await Task.WhenAll(tcs1.Task, tcs2.Task, tcs3.Task);
Log.WriteInfo("main thread exiting...");
await Task.Delay(10000);

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

enum QdiscType
{
    Unspecified,
    Fifo,
    Lifo,
    RoundRobin
}

record State(QdiscType QdiscType);