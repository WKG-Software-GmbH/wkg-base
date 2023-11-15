using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Common.Extensions;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Tests;

[TestClass]
public class WorkloadSynchronizationContextTests
{
    private static ClasslessWorkloadFactory<int> CreateDefaultFactory(bool continueOnCapturedContext, bool flowExecutionContext) => WorkloadFactoryBuilder.Create<int>()
        .UseAnonymousWorkloadPooling(4)
        .UseMaximumConcurrency(1)
        .FlowExecutionContextToContinuations(flowExecutionContext)
        .RunContinuationsOnCapturedContext(continueOnCapturedContext)
        .UseClasslessRoot<Fifo>(1);

    [TestMethod]
    public async Task TestContinueOnCapturedContextFalseAsync()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: false, flowExecutionContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        await factory.ScheduleAsync(_ => Thread.Sleep(50));
        Assert.AreNotEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
    }

    [TestMethod]
    public async Task TestContinueOnCapturedContextTrueAsync()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: true, flowExecutionContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        await factory.ScheduleAsync(_ => Thread.Sleep(50));
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
    }

    [TestMethod]
    public void TestContinueOnCapturedContextFalseFlowExecutionContextFalse()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: false, flowExecutionContext: false);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        AsyncLocal<int> asyncLocal = new()
        {
            Value = 1
        };
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        Assert.AreEqual(1, asyncLocal.Value);
        ManualResetEventSlim mres = new(false);
        factory.ScheduleAsync(_ => Thread.Sleep(50)).ContinueWith(() =>
        {
            Assert.AreNotEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
            Assert.AreNotEqual(1, asyncLocal.Value);
            mres.Set();
        }); 
        mres.Wait();
    }

    [TestMethod]
    public void TestContinueOnCapturedContextFalseFlowExecutionContextTrue()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: false, flowExecutionContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        AsyncLocal<int> asyncLocal = new()
        {
            Value = 1
        };
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        Assert.AreEqual(1, asyncLocal.Value);
        ManualResetEventSlim mres = new(false);
        factory.ScheduleAsync(_ => Thread.Sleep(50)).ContinueWith(() =>
        {
            Assert.AreNotEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
            Assert.AreEqual(1, asyncLocal.Value);
            mres.Set();
        });
        mres.Wait();
    }

    [TestMethod]
    public void TestContinueOnCapturedContextTrueFlowExecutionContextFalse()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: true, flowExecutionContext: false);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        AsyncLocal<int> asyncLocal = new()
        {
            Value = 1
        };
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        Assert.AreEqual(1, asyncLocal.Value);
        ManualResetEventSlim mres = new(false);
        factory.ScheduleAsync(_ => Thread.Sleep(50)).ContinueWith(() =>
        {
            Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
            Assert.AreNotEqual(1, asyncLocal.Value);
            mres.Set();
        });
        mres.Wait();
    }

    [TestMethod]
    public void TestContinueOnCapturedContextTrueFlowExecutionContextTrue()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: true, flowExecutionContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        AsyncLocal<int> asyncLocal = new()
        {
            Value = 1
        };
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        Assert.AreEqual(1, asyncLocal.Value);
        ManualResetEventSlim mres = new(false);
        factory.ScheduleAsync(_ => Thread.Sleep(50)).ContinueWith(() =>
        {
            Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
            Assert.AreEqual(1, asyncLocal.Value);
            mres.Set();
        });
        mres.Wait();
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