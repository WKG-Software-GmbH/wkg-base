// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using Wkg.Logging;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Loggers;
using Wkg.Logging.Writers;
using Wkg.Threading;

Log.UseLogger(Logger.Create(LoggerConfiguration.Create()
    .AddSink<ColoredThreadBasedConsoleSink>()
    .UseEntryGenerator<SimpleLogEntryGenerator>()
    .RegisterMainThread(Thread.CurrentThread)
    .UseDefaultLogWriter(LogWriter.Blocking)));

TaskScheduler scheduler = new ConstrainedTaskScheduler(2);

TaskFactory factory = new(scheduler);

const int TASK_COUNT = 10;
Task[] tasks = new Task[TASK_COUNT];

for (int i = 0; i < TASK_COUNT; i++)
{
    tasks[i] = factory.StartNew(DoStuffAsync, TaskCreationOptions.AttachedToParent);
}

Log.WriteInfo("Waiting for tasks to complete...");
await Task.WhenAll(tasks);
Log.WriteInfo("All tasks completed.");
await Task.Delay(60 * 60 * 1000);

static async Task DoStuffAsync()
{
    await Task.Delay(250);
    Log.WriteDiagnostic($"Task {Task.CurrentId} started on thread {Environment.CurrentManagedThreadId}.");
    await Task.Delay(500);
    await Task.Delay(250);
    Log.WriteDiagnostic($"Task {Task.CurrentId} completed on thread {Environment.CurrentManagedThreadId}.");
}