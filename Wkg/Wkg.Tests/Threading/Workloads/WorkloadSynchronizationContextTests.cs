using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Common.Extensions;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Tests;

[TestClass]
public class WorkloadSynchronizationContextTests
{
    private static ClasslessWorkloadFactory<int> CreateDefaultFactory(bool continueOnCapturedContext) => WorkloadFactoryBuilder.Create<int>()
        .UseAnonymousWorkloadPooling(4)
        .UseMaximumConcurrency(1)
        .RunContinuationsOnCapturedContext(continueOnCapturedContext)
        .UseClasslessRoot<Fifo>(1);

    [TestMethod]
    public async Task TestContinueOnCapturedContextFalse()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: false);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        await factory.ScheduleAsync(_ => Thread.Sleep(50));
        Assert.AreNotEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
    }

    [TestMethod]
    public async Task TestContinueOnCapturedContextTrue()
    {
        ClasslessWorkloadFactory<int> factory = CreateDefaultFactory(continueOnCapturedContext: true);
        SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext() { State = 1 });
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
        await factory.ScheduleAsync(_ => Thread.Sleep(50));
        Assert.AreEqual(1, SynchronizationContext.Current.As<MySynchronizationContext>()?.State);
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