using BenchmarkDotNet.Running;
using ConsoleApp1;
using System.Collections.Concurrent;
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
using Wkg.Threading.Workloads.Queuing.Classful.Qdiscs;
using Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

internal class Program
{
    int a = 0;

    private void Asdf() => a++;

    private static unsafe void Test()
    {
        Program p = new();
        Action action1 = p.Asdf;
        Stopwatch sw = new();
        nint handle = default;
        for (int i = 0; i < 100; i++)
        {
            sw.Start();
            handle = action1.Method.MethodHandle.GetFunctionPointer();
            sw.Stop();
            Console.WriteLine(sw.Elapsed.ToString());
            sw.Reset();
        }
        Log.WriteInfo($"Static instance handle: {handle}");
        for (int i = 0; i < 100; i++)
        {
            if (i == 50)
            {
                Console.WriteLine("new test");
                sw.Start();
                Action a = Test;
                handle = a.Method.MethodHandle.GetFunctionPointer();
                sw.Stop();
                Console.WriteLine(sw.Elapsed.ToString());
                sw.Reset();
            }
            else
            {
                sw.Start();
                Action a = p.Asdf;
                handle = a.Method.MethodHandle.GetFunctionPointer();
                sw.Stop();
                Console.WriteLine(sw.Elapsed.ToString());
                sw.Reset();
            }
        }
        // "this" pointer (arg0 for all instance methods in .NET's calling convention)
        // we can also just pass any other arbitrary object reference here for extra fun :)
        delegate*<Program, void> ptr1 = (delegate*<Program, void>)handle;
        ptr1(p);
    }

    private static async Task Main(string[] args)
    {
        BenchmarkRunner.Run<Tests>();
        Console.ReadLine();
        return;

        Log.UseLogger(Logger.Create(LoggerConfiguration.Create()
            //.AddSink<ColoredThreadBasedConsoleSink>()
            .AddSink<ColoredConsoleSink>()
            .UseEntryGenerator<TracingLogEntryGenerator>()
            .RegisterMainThread(Thread.CurrentThread)
            .UseDefaultLogWriter(LogWriter.Blocking)));

        Test();
        return;

        ClassfulWorkloadFactory<QdiscType> clubmappFactory = WorkloadFactoryBuilder.Create<QdiscType>()
            // the root scheduler is allowed to run up to 4 workers at the same time
            .UseMaximumConcurrency(4)
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
            .UseClassfulRoot<RoundRobinQdisc<QdiscType>>(QdiscType.RoundRobin)
                .AddClassificationPredicate<State>(state => state.QdiscType == QdiscType.RoundRobin)
                // one child scheduler will dequeue workloads in a First In First Out manner
                .AddClasslessChild<FifoQdisc<QdiscType>>(QdiscType.Fifo)
                // the other child scheduler will dequeue workloads in a Last In First Out manner
                .AddClasslessChild<LifoQdisc<QdiscType>>(QdiscType.Lifo)
                .Build();

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

        ClassfulWorkloadFactoryWithDI<int> factory = WorkloadFactoryBuilder.Create<int>()
            .UseMaximumConcurrency(4)
            .FlowExecutionContextToContinuations()
            .RunContinuationsOnCapturedContext()
            .UseDependencyInjection<PooledWorkloadServiceProviderFactory>(services => services
                .AddService<IMyService, MyService>(() => new MyService())
                .AddService(() => new MyService()))
            .UseAnonymousWorkloadPooling(poolSize: 64)
            .UseClassfulRoot<RoundRobinQdisc<int>>(1)
                .AddClassificationPredicate<State>(state => state.QdiscType == QdiscType.RoundRobin)
                .AddClasslessChild<FifoQdisc<int>>(2, child => child
                    .WithClassificationPredicate<State>(state => state.QdiscType == QdiscType.Fifo)
                    .WithClassificationPredicate<int>(i => (i & 1) == 0))
                .AddClassfulChild<RoundRobinQdisc<int>>(3, child => child
                    .AddClassfulChild<RoundRobinQdisc<int>>(10, child => child
                        .AddClassfulChild<RoundRobinQdisc<int>>(11, child => child
                            .AddClassfulChild<RoundRobinQdisc<int>>(12, child => child
                                .AddClassfulChild<RoundRobinQdisc<int>>(13, child => child
                                    .AddClasslessChild<LifoQdisc<int>>(14))))))
                    .AddClasslessChild<LifoQdisc<int>>(4)
                    .AddClasslessChild<FifoQdisc<int>>(5)
                    .AddClasslessChild<LifoQdisc<int>>(6)
                .AddClasslessChild<LifoQdisc<int>>(7, child => child
                    .WithClassificationPredicate<State>(state => state.QdiscType == QdiscType.Lifo)
                    .WithClassificationPredicate<int>(i => (i & 1) == 1))
                .AddClasslessChild<FifoQdisc<int>>(8)
                .Build();

        List<int> myData = Enumerable.Range(0, 1000).ToList();
        int sum = myData.Sum();
        Log.WriteInfo($"Sum: {sum}");

        List<int> result = await factory.TransformAllAsync(myData, (data, cancellationFlag) => data * 100);

        Log.WriteInfo($"Result Sum 1: {result.Select(i => (long)i).Sum()}");
        await Task.Delay(2500);
        Log.WriteInfo($"Sum: {sum}");

        List<int> resultClassified = await factory.ClassifyAndTransformAllAsync(myData, (data, cancellationFlag) => data * 100);

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

        ClasslessWorkloadFactory<int> simpleFactory = WorkloadFactoryBuilder.Create<int>()
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

    }

    private static void DoStuff(CancellationFlag cancellationFlag)
    {
        Log.WriteInfo("Doing stuff...");
        for (int i = 0; i < 10; i++)
        {
            cancellationFlag.ThrowIfCancellationRequested();
            Thread.Sleep(100);
        }
        Log.WriteInfo("Done doing stuff.");
    }
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

