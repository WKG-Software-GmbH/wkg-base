using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;
using System.Diagnostics;
using Wkg.Threading.Workloads.Scheduling;
using Wkg.Threading.Workloads.Exceptions;

namespace Wkg.Threading.Workloads.Tests;

[TestClass]
public class AwaitableWorkloadTests
{
    private static ClasslessWorkloadFactory<int> CreateDefaultFactory() => WorkloadFactoryBuilder.Create<int>()
        .UseAnonymousWorkloadPooling(4)
        .UseMaximumConcurrency(1)
        .UseClasslessRoot<Fifo>(1);

    [TestMethod]
    public async Task TestAwait1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync(_ =>
        {
            Thread.Sleep(100);
            return 1;
        });

        WorkloadResult<int> result = await workload;
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public async Task TestAwait2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload workload = factory.ScheduleAsync(_ => Thread.Sleep(100));

        WorkloadResult result = await workload;
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void TestBlockingWaitImplicit1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync(_ =>
        {
            Thread.Sleep(100);
            return 1;
        });
        WorkloadResult<int> result = workload.GetAwaiter().GetResult();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public void TestBlockingWaitImplicit2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload workload = factory.ScheduleAsync(_ => Thread.Sleep(100));

        WorkloadResult result = workload.GetAwaiter().GetResult();
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void TestBlockingWaitImplicit3()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync(_ =>
        {
            Thread.Sleep(100);
            return 1;
        });
        WorkloadResult<int> result = workload.Result;
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public void TestBlockingWaitImplicit4()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload workload = factory.ScheduleAsync(_ => Thread.Sleep(100));

        WorkloadResult result = workload.Result;
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void TestContinueWith1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync(_ =>
        {
            Thread.Sleep(100);
            return 1;
        });
        ManualResetEventSlim mres = new(false);
        workload.ContinueWith(result =>
        {
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, result.Result);
            Assert.IsTrue(new StackTrace().GetFrames().All(frame => frame.GetMethod()?.Name != nameof(WorkloadScheduler.WorkerLoop)));
            // ensure that the continuation is invoked inline
            Assert.IsTrue(new StackTrace().GetFrames().All(frame => frame.GetMethod()?.Name != nameof(TestContinueWith1)));
            mres.Set();
        });
        mres.Wait();
    }

    [TestMethod]
    public void TestContinueWith2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload workload = factory.ScheduleAsync(_ => Thread.Sleep(100));
        ManualResetEventSlim mres = new(false);
        workload.ContinueWith(result =>
        {
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(new StackTrace().GetFrames().All(frame => frame.GetMethod()?.Name != nameof(WorkloadScheduler.WorkerLoop)));
            // ensure that the continuation is invoked inline
            Assert.IsTrue(new StackTrace().GetFrames().All(frame => frame.GetMethod()?.Name != nameof(TestContinueWith2)));
            mres.Set();
        });
        mres.Wait();
    }

    [TestMethod]
    public void TestContinueWithInline1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync(_ => 1);
        Thread.Sleep(100);
        ManualResetEventSlim mres = new(false);
        workload.ContinueWith(result =>
        {
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, result.Result);
            Assert.IsTrue(new StackTrace().GetFrames().All(frame => frame.GetMethod()?.Name != nameof(WorkloadScheduler.WorkerLoop)));
            // ensure that the continuation is invoked inline
            Assert.IsTrue(new StackTrace().GetFrames().Any(frame => frame.GetMethod()?.Name == nameof(TestContinueWithInline1)));
            mres.Set();
        });
        mres.Wait();
    }

    [TestMethod]
    public void TestContinueWithInline2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload workload = factory.ScheduleAsync(Pass);
        Thread.Sleep(100);
        ManualResetEventSlim mres = new(false);
        workload.ContinueWith(result =>
        {
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(new StackTrace().GetFrames().All(frame => frame.GetMethod()?.Name != nameof(WorkloadScheduler.WorkerLoop)));
            // ensure that the continuation is invoked inline
            Assert.IsTrue(new StackTrace().GetFrames().Any(frame => frame.GetMethod()?.Name == nameof(TestContinueWithInline2)));
            mres.Set();
        });
        mres.Wait();
    }

    [TestMethod]
    public void TestBlockingWaitExplicit1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync(_ =>
        {
            Thread.Sleep(100);
            return 1;
        });
        workload.Wait();
        WorkloadResult<int> result = workload.Result;
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public void TestBlockingWaitExplicit2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload workload = factory.ScheduleAsync(_ => Thread.Sleep(100));
        workload.Wait();
        WorkloadResult result = workload.Result;
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void TestBlockingWaitExplicitWithTimeout1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync(_ =>
        {
            Thread.Sleep(1000);
            return 1;
        });
        bool waitResult = workload.Wait(TimeSpan.FromMilliseconds(100));
        Assert.IsFalse(waitResult);
        WorkloadResult<int> result = workload.GetAwaiter().GetResult();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public void TestBlockingWaitExplicitWithTimeout2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload workload = factory.ScheduleAsync(_ => Thread.Sleep(1000));
        bool waitResult = workload.Wait(TimeSpan.FromMilliseconds(100));
        Assert.IsFalse(waitResult);
        WorkloadResult result = workload.GetAwaiter().GetResult();
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void TestBlockingWaitExplicitWithTimeout3()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync(_ =>
        {
            Thread.Sleep(100);
            return 1;
        });
        bool waitResult = workload.Wait(TimeSpan.FromMilliseconds(250));
        Assert.IsTrue(waitResult);
        WorkloadResult<int> result = workload.Result;
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public void TestBlockingWaitExplicitWithTimeout4()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload workload = factory.ScheduleAsync(_ => Thread.Sleep(100));
        bool waitResult = workload.Wait(TimeSpan.FromMilliseconds(250));
        Assert.IsTrue(waitResult);
        WorkloadResult result = workload.GetAwaiter().GetResult();
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task TestCancellationDuringScheduling1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        ManualResetEventSlim mres = new(false);
        factory.Schedule(mres.Wait);
        Workload<int> workload = factory.ScheduleAsync(_ =>
        {
            Thread.Sleep(100);
            return 1;
        });
        bool cancellationResult = workload.TryCancel();
        Assert.IsTrue(cancellationResult);
        mres.Set();
        WorkloadResult<int> result = await workload;
        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(result.TryGetResult(out _));
    }

    [TestMethod]
    public async Task TestCancellationDuringScheduling2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        ManualResetEventSlim mres = new(false);
        factory.Schedule(mres.Wait);
        Workload workload = factory.ScheduleAsync(_ => Thread.Sleep(100));
        bool cancellationResult = workload.TryCancel();
        Assert.IsTrue(cancellationResult);
        mres.Set();
        WorkloadResult result = await workload;
        Assert.IsTrue(result.IsCanceled);
    }

    [TestMethod]
    public async Task TestCancellationDuringExecution1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        ManualResetEventSlim mres = new(false);
        Workload<int> workload = factory.ScheduleAsync(token =>
        {
            mres.Wait();
            token.ThrowIfCancellationRequested();
            return 1;
        });
        bool cancellationResult = workload.TryCancel();
        Assert.IsTrue(cancellationResult);
        mres.Set();
        WorkloadResult<int> result = await workload;
        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(result.TryGetResult(out _));
    }

    [TestMethod]
    public async Task TestCancellationDuringExecution2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        ManualResetEventSlim mres = new(false);
        Workload workload = factory.ScheduleAsync(token =>
        {
            mres.Wait();
            token.ThrowIfCancellationRequested();
        });
        bool cancellationResult = workload.TryCancel();
        Assert.IsTrue(cancellationResult);
        mres.Set();
        WorkloadResult result = await workload;
        Assert.IsTrue(result.IsCanceled);
    }

    [TestMethod]
    public async Task TestCancellationDuringExecution3()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        ManualResetEventSlim mres = new(false);
        ManualResetEventSlim isRunningMres = new(false);
        Workload<int> workload = factory.ScheduleAsync(token =>
        {
            isRunningMres.Set();
            mres.Wait();
            return token.IsCancellationRequested ? 1 : 2;
        });
        isRunningMres.Wait();
        bool cancellationResult = workload.TryCancel();
        Assert.IsTrue(cancellationResult);
        mres.Set();
        WorkloadResult<int> result = await workload;
        // workload did not honor cancellation and returned a result
        Assert.AreEqual<WorkloadStatus>(WorkloadStatus.RanToCompletion | WorkloadStatus.ContinuationsInvoked, workload.Status);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public async Task TestCancellationDuringExecution4()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        ManualResetEventSlim mres = new(false);
        Workload<int> workload = factory.ScheduleAsync(token =>
        {
            mres.Wait();
            if (token.IsCancellationRequested)
            {
                token.MarkCanceled();
                return default;
            }
            return 1;
        });
        bool cancellationResult = workload.TryCancel();
        Assert.IsTrue(cancellationResult);
        mres.Set();
        WorkloadResult<int> result = await workload;
        // workload honored cancellation and returned a result
        Assert.IsTrue(result.IsCanceled);
    }

    [TestMethod]
    public async Task TestCancellationUsingCancellationTokenDuringScheduling1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        CancellationTokenSource cts = new();
        ManualResetEventSlim mres = new(false);
        factory.Schedule(mres.Wait);
        Workload<int> workload = factory.ScheduleAsync(_ => 1, cts.Token);
        cts.Cancel();
        mres.Set();
        WorkloadResult<int> result = await workload;
        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(result.TryGetResult(out _));
    }

    [TestMethod]
    public async Task TestCancellationUsingCancellationTokenDuringScheduling2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        CancellationTokenSource cts = new();
        ManualResetEventSlim mres = new(false);
        cts.Cancel();
        Workload workload = factory.ScheduleAsync(_ => { }, cts.Token);
        Assert.IsTrue(workload.IsCompleted);
        Assert.AreEqual<WorkloadStatus>(WorkloadStatus.Canceled | WorkloadStatus.ContinuationsInvoked, workload.Status);
        WorkloadResult result = await workload;
        Assert.IsTrue(result.IsCanceled);
    }

    [TestMethod]
    public async Task TestCancellationUsingCancellationTokenDuringExecution1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        CancellationTokenSource cts = new();
        ManualResetEventSlim mres = new(false);
        Workload<int> workload = factory.ScheduleAsync(token =>
        {
            mres.Wait();
            token.ThrowIfCancellationRequested();
            return 1;
        }, cts.Token);
        cts.Cancel();
        mres.Set();
        WorkloadResult<int> result = await workload;
        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(result.TryGetResult(out _));
    }

    [TestMethod]
    public async Task TestCancellationUsingCancellationTokenDuringExecution2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        CancellationTokenSource cts = new();
        ManualResetEventSlim mres = new(false);
        Workload workload = factory.ScheduleAsync(token =>
        {
            mres.Wait();
            token.ThrowIfCancellationRequested();
        }, cts.Token);
        cts.Cancel();
        mres.Set();
        WorkloadResult result = await workload;
        Assert.IsTrue(result.IsCanceled);
    }

    [TestMethod]
    public async Task TestCancellationUsingCancellationTokenDuringExecution3()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        CancellationTokenSource cts = new();
        ManualResetEventSlim mres = new(false);
        ManualResetEventSlim isRunningMres = new(false);
        Workload<int> workload = factory.ScheduleAsync(token =>
        {
            isRunningMres.Set();
            mres.Wait();
            return token.IsCancellationRequested ? 1 : 2;
        }, cts.Token);
        isRunningMres.Wait();
        cts.Cancel();
        mres.Set();
        WorkloadResult<int> result = await workload;
        // workload did not honor cancellation and returned a result
        Assert.AreEqual<WorkloadStatus>(WorkloadStatus.RanToCompletion | WorkloadStatus.ContinuationsInvoked, workload.Status);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public async Task TestCancellationUsingCancellationTokenDuringExecution4()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        CancellationTokenSource cts = new();
        ManualResetEventSlim mres = new(false);
        Workload<int> workload = factory.ScheduleAsync(token =>
        {
            mres.Wait();
            if (token.IsCancellationRequested)
            {
                token.MarkCanceled();
                return default;
            }
            return 1;
        }, cts.Token);
        cts.Cancel();
        mres.Set();
        WorkloadResult<int> result = await workload;
        // workload honored cancellation and returned a result
        Assert.IsTrue(result.IsCanceled);
    }

    [TestMethod]
    public async Task TestCancellationUsingCancellationTokenDuringExecution5()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        CancellationTokenSource cts = new();
        ManualResetEventSlim mres = new(false);
        ManualResetEventSlim isRunningMres = new(false);
        Workload<int> workload = factory.ScheduleAsync(token =>
        {
            isRunningMres.Set();
            mres.Wait();
            if (token.IsCancellationRequested)
            {
                token.MarkCanceled();
                return default;
            }
            return 1;
        }, cts.Token);
        isRunningMres.Wait();
        cts.Cancel();
        Assert.AreEqual(WorkloadStatus.CancellationRequested, workload.Status);
        mres.Set();
        WorkloadResult<int> result = await workload;
        // workload honored cancellation and returned a result
        Assert.AreEqual<WorkloadStatus>(WorkloadStatus.Canceled | WorkloadStatus.ContinuationsInvoked, workload.Status);
        Assert.IsTrue(result.IsCanceled);
    }

    [TestMethod]
    public async Task TestSelfCancellationWithException()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        Workload<int> workload = factory.ScheduleAsync<int>(_ => throw new WorkloadCanceledException());
        WorkloadResult<int> result = await workload;
        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(result.TryGetResult(out _));
    }

    [TestMethod]
    public async Task TestFaultedWorkload1()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        const string message = "Test exception.";
        Workload<int> workload = factory.ScheduleAsync<int>(_ => throw new Exception(message));
        WorkloadResult<int> result = await workload;
        Assert.AreEqual<WorkloadStatus>(WorkloadStatus.Faulted | WorkloadStatus.ContinuationsInvoked, workload.Status);
        Assert.IsTrue(result.IsFaulted);
        Assert.IsFalse(result.TryGetResult(out _));
        Assert.IsNotNull(result.Exception);
        Assert.AreEqual(message, result.Exception!.Message);
    }

    [TestMethod]
    public async Task TestFaultedWorkload2()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory();
        const string message = "Test exception.";
        Workload workload = factory.ScheduleAsync(_ => throw new Exception(message));
        WorkloadResult result = await workload;
        Assert.AreEqual<WorkloadStatus>(WorkloadStatus.Faulted | WorkloadStatus.ContinuationsInvoked, workload.Status);
        Assert.IsTrue(result.IsFaulted);
        Assert.IsNotNull(result.Exception);
        Assert.AreEqual(message, result.Exception!.Message);
    }
}