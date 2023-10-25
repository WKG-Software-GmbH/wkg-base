// See https://aka.ms/new-console-template for more information
using Wkg.Logging;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Loggers;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Internals;
using Wkg.Threading.Workloads.Queuing;

Log.UseLogger(Logger.Create(LoggerConfiguration.Create()
    //.AddSink<ColoredThreadBasedConsoleSink>()
    .AddSink<ColoredConsoleSink>()
    .UseEntryGenerator<TracingLogEntryGenerator>()
    .RegisterMainThread(Thread.CurrentThread)
    .UseDefaultLogWriter(LogWriter.Blocking)));

FifoQdisc root = new();
WorkloadScheduler baseScheduler = new(root, maximumConcurrencyLevel: 1);
root.InternalInitialize(baseScheduler);
WorkloadFactory factory = new(root);

const int WORKLOAD_COUNT = 10;
Workload[] workloads = new Workload[WORKLOAD_COUNT];

TaskCompletionSource tcs = new();

for (int i = 0; i < WORKLOAD_COUNT; i++)
{
    if (i == 3)
    {
        // self-canceling workload (gotta test cancelling a running workload)
        workloads[i] = factory.Schedule(cancellationFlag =>
        {
            Log.WriteInfo("Cancelling myself :P");
            for (int i = 0; i < 10; i++)
            {
                cancellationFlag.ThrowIfCancellationRequested();
                if (i == 5)
                {
                    workloads[3].TryCancel();
                }
            }
            Log.WriteWarning("I should not have gotten here.");
        });
        continue;
    }
    if (i == 5)
    {
        workloads[i] = factory.Schedule(_ => workloads[6].TryCancel());
        continue;
    }
    workloads[i] = factory.Schedule(DoStuff);
}

factory.Schedule(_ =>
{
    Log.WriteDebug("Attempting to cancel completed workload.");
    bool result = workloads[0].TryCancel();
    Log.WriteDebug($"Result: {result}");
});

factory.Schedule(_ =>
{
    Log.WriteInfo("Signaling completion.");
    tcs.SetResult();
});

await tcs.Task;
Log.WriteInfo("main thread exiting.");
await Task.Delay(1000);

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