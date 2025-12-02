using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Common.Extensions;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Tests.Threading.Workloads;

[TestClass]
public sealed class WorkloadSynchronizationContextTests
{
    public TestContext TestContext { get; set; }

    private static ClasslessWorkloadFactory<int> CreateDefaultFactory(bool continueOnCapturedContext, bool flowExecutionContext) => WorkloadFactoryBuilder.Create<int>()
        .UseAnonymousWorkloadPooling(4)
        .UseMaximumConcurrency(1)
        .FlowExecutionContextToContinuations(flowExecutionContext)
        .RunContinuationsOnCapturedContext(continueOnCapturedContext)
        .UseClasslessRoot<Fifo>(1);

    [TestMethod]
    public async Task TestContinueOnCapturedContextFalseAsync()
    {
        using ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: false, flowExecutionContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        await factory.ScheduleAsync(_ => Thread.Sleep(50), cancellationToken: TestContext.CancellationToken);
        Assert.AreNotEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
    }

    [TestMethod]
    public async Task TestContinueOnCapturedContextTrueAsync()
    {
        using ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: true, flowExecutionContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        await factory.ScheduleAsync(_ => Thread.Sleep(50), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
    }

    [TestMethod]
    public void TestContinueOnCapturedContextFalseFlowExecutionContextFalse()
    {
        using ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: false, flowExecutionContext: false);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        AsyncLocal<int> asyncLocal = new()
        {
            Value = 1
        };
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        Assert.AreEqual(1, asyncLocal.Value);
        using ManualResetEventSlim mres = new(false);
        factory.ScheduleAsync(_ => Thread.Sleep(50), cancellationToken: TestContext.CancellationToken).ContinueWith(() =>
        {
            Assert.AreNotEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
            Assert.AreNotEqual(1, asyncLocal.Value);
            mres.Set();
        });
        mres.Wait(TestContext.CancellationToken);
    }

    [TestMethod]
    public void TestContinueOnCapturedContextFalseFlowExecutionContextTrue()
    {
        using ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: false, flowExecutionContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        AsyncLocal<int> asyncLocal = new()
        {
            Value = 1
        };
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        Assert.AreEqual(1, asyncLocal.Value);
        using ManualResetEventSlim mres = new(false);
        factory.ScheduleAsync(_ => Thread.Sleep(50), cancellationToken: TestContext.CancellationToken).ContinueWith(() =>
        {
            Assert.AreNotEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
            Assert.AreEqual(1, asyncLocal.Value);
            mres.Set();
        });
        mres.Wait(TestContext.CancellationToken);
    }

    [TestMethod]
    public void TestContinueOnCapturedContextTrueFlowExecutionContextFalse()
    {
        using ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: true, flowExecutionContext: false);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        AsyncLocal<int> asyncLocal = new()
        {
            Value = 1
        };
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        Assert.AreEqual(1, asyncLocal.Value);
        using ManualResetEventSlim mres = new(false);
        factory.ScheduleAsync(_ => Thread.Sleep(50), cancellationToken: TestContext.CancellationToken).ContinueWith(() =>
        {
            Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
            Assert.AreNotEqual(1, asyncLocal.Value);
            mres.Set();
        });
        mres.Wait(TestContext.CancellationToken);
    }

    [TestMethod]
    public void TestContinueOnCapturedContextTrueFlowExecutionContextTrue()
    {
        using ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: true, flowExecutionContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        AsyncLocal<int> asyncLocal = new()
        {
            Value = 1
        };
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        Assert.AreEqual(1, asyncLocal.Value);
        using ManualResetEventSlim mres = new(false);
        factory.ScheduleAsync(_ => Thread.Sleep(50), cancellationToken: TestContext.CancellationToken).ContinueWith(() =>
        {
            Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
            Assert.AreEqual(1, asyncLocal.Value);
            mres.Set();
        });
        mres.Wait(TestContext.CancellationToken);
    }

    private class MySynchronizationContext : SynchronizationContext
    {
        public int State { get; set; }

        public override void Post(SendOrPostCallback d, object? state) => base.Post(state =>
        {
            SetSynchronizationContext(this);
            d.Invoke(state);
        }, state);
    }
}