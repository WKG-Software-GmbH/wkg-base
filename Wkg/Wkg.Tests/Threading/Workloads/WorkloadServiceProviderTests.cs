using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.DependencyInjection.Implementations;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Tests;

[TestClass]
public class WorkloadServiceProviderTests
{
    private static ClasslessWorkloadFactoryWithDI<int> CreateDefaultFactory<TServiceProviderFactory>() 
        where TServiceProviderFactory : class, IWorkloadServiceProviderFactory, new() => WorkloadFactoryBuilder.Create<int>()
        .UseAnonymousWorkloadPooling(4)
        .UseMaximumConcurrency(1)
        .UseDependencyInjection<TServiceProviderFactory>(di => di
            .AddSingleton(new MySingletonService(42))
            .AddService<IMyService, MyService>(() => new MyService()))
        .UseClasslessRoot<Fifo>(1);

    [TestMethod]
    public async Task TestSimpleServiceProvider1()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<SimpleWorkloadServiceProviderFactory>();
        WorkloadResult<int> result = await factory.ScheduleAsync((services, flag) => 
        {
            IMyService service = services.GetRequiredService<IMyService>();
            return service.GetNext();
        });
        Assert.AreEqual(1, result.Result);
        // wait for the worker thread to terminate
        Thread.Sleep(1000);
        result = await factory.ScheduleAsync((services, flag) =>
        {
            IMyService service = services.GetRequiredService<IMyService>();
            return service.GetNext();
        });
        Assert.AreEqual(1, result.Result);
    }

    [TestMethod]
    public async Task TestSimpleServiceProvider2()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<SimpleWorkloadServiceProviderFactory>();
        ManualResetEventSlim mres = new(false);
        Workload<int> workload1 = factory.ScheduleAsync((services, flag) =>
        {
            mres.Wait();
            IMyService service = services.GetRequiredService<IMyService>();
            return service.GetNext();
        });
        Workload<int> workload2 = factory.ScheduleAsync((services, flag) =>
        {
            IMyService service = services.GetRequiredService<IMyService>();
            return service.GetNext();
        });
        mres.Set();
        WorkloadResult<int> result1 = await workload1;
        WorkloadResult<int> result2 = await workload2;
        Assert.AreEqual(1, result1.Result);
        Assert.AreEqual(2, result2.Result);
    }

    [TestMethod]
    public async Task TestSimpleServiceProvider3()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<SimpleWorkloadServiceProviderFactory>();
        Workload<int> workload1 = factory.ScheduleAsync((services, flag) =>
        {
            MySingletonService service = services.GetRequiredService<MySingletonService>();
            return service.GetNext();
        });
        Thread.Sleep(1000);
        Workload<int> workload2 = factory.ScheduleAsync((services, flag) =>
        {
            MySingletonService service = services.GetRequiredService<MySingletonService>();
            return service.GetNext();
        });
        WorkloadResult<int> result1 = await workload1;
        WorkloadResult<int> result2 = await workload2;
        Assert.AreEqual(42, result1.Result);
        Assert.AreEqual(42, result2.Result);
    }

    [TestMethod]
    public async Task TestSimpleServiceForNonExistingService()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<SimpleWorkloadServiceProviderFactory>();
        WorkloadResult<int> result1 = await factory.ScheduleAsync((services, flag) =>
        {
            string service = services.GetRequiredService<string>();
            return 42;
        });
        Assert.AreEqual(WorkloadStatus.Faulted, result1.CompletionStatus);
        Assert.AreEqual(typeof(InvalidOperationException), result1.Exception?.GetType());
    }

    [TestMethod]
    public async Task TestSimpleServiceForNonExistingService2()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<SimpleWorkloadServiceProviderFactory>();
        WorkloadResult<int> result1 = await factory.ScheduleAsync((services, flag) =>
        {
            bool b = services.TryGetService(out string? service);
            if (b)
            {
                return 42;
            }
            throw new InvalidOperationException("nope");
        });
        Assert.AreEqual(WorkloadStatus.Faulted, result1.CompletionStatus);
        Assert.AreEqual(typeof(InvalidOperationException), result1.Exception?.GetType());
        Assert.AreEqual("nope", result1.Exception?.Message);
    }

    [TestMethod]
    public async Task TestPooledServiceProvider1()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<PooledWorkloadServiceProviderFactory>();
        WorkloadResult<int> result = await factory.ScheduleAsync((services, flag) =>
        {
            IMyService service = services.GetRequiredService<IMyService>();
            return service.GetNext();
        });
        Assert.AreEqual(1, result.Result);
        // wait for the worker thread to terminate
        Thread.Sleep(1000);
        result = await factory.ScheduleAsync((services, flag) =>
        {
            IMyService service = services.GetRequiredService<IMyService>();
            return service.GetNext();
        });
        // the service should be pooled, so the counter should not be reset
        Assert.AreEqual(2, result.Result);
    }

    [TestMethod]
    public async Task TestPooledServiceProvider2()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<PooledWorkloadServiceProviderFactory>();
        ManualResetEventSlim mres = new(false);
        Workload<int> workload1 = factory.ScheduleAsync((services, flag) =>
        {
            mres.Wait();
            IMyService service = services.GetRequiredService<IMyService>();
            return service.GetNext();
        });
        Workload<int> workload2 = factory.ScheduleAsync((services, flag) =>
        {
            IMyService service = services.GetRequiredService<IMyService>();
            return service.GetNext();
        });
        mres.Set();
        WorkloadResult<int> result1 = await workload1;
        WorkloadResult<int> result2 = await workload2;
        Assert.AreEqual(1, result1.Result);
        Assert.AreEqual(2, result2.Result);
    }

    [TestMethod]
    public async Task TestPooledServiceProvider3()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<PooledWorkloadServiceProviderFactory>();
        Workload<int> workload1 = factory.ScheduleAsync((services, flag) =>
        {
            MySingletonService service = services.GetRequiredService<MySingletonService>();
            return service.GetNext();
        });
        Thread.Sleep(1000);
        Workload<int> workload2 = factory.ScheduleAsync((services, flag) =>
        {
            MySingletonService service = services.GetRequiredService<MySingletonService>();
            return service.GetNext();
        });
        WorkloadResult<int> result1 = await workload1;
        WorkloadResult<int> result2 = await workload2;
        Assert.AreEqual(42, result1.Result);
        Assert.AreEqual(42, result2.Result);
    }

    [TestMethod]
    public async Task TestPooledServiceForNonExistingService()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<PooledWorkloadServiceProviderFactory>();
        WorkloadResult<int> result1 = await factory.ScheduleAsync((services, flag) =>
        {
            string service = services.GetRequiredService<string>();
            return 42;
        });
        Assert.AreEqual(WorkloadStatus.Faulted, result1.CompletionStatus);
        Assert.AreEqual(typeof(InvalidOperationException), result1.Exception?.GetType());
    }

    [TestMethod]
    public async Task TestPooledServiceForNonExistingService2()
    {
        ClasslessWorkloadFactoryWithDI<int> factory = CreateDefaultFactory<PooledWorkloadServiceProviderFactory>();
        WorkloadResult<int> result1 = await factory.ScheduleAsync((services, flag) =>
        {
            bool b = services.TryGetService(out string? service);
            if (b)
            {
                return 42;
            }
            throw new InvalidOperationException("nope");
        });
        Assert.AreEqual(WorkloadStatus.Faulted, result1.CompletionStatus);
        Assert.AreEqual(typeof(InvalidOperationException), result1.Exception?.GetType());
        Assert.AreEqual("nope", result1.Exception?.Message);
    }

    private interface IMyService
    {
        int GetNext();
    }

    private class MyService : IMyService
    {
        private int _counter;

        public MyService()
        {
            _counter = 0;
        }

        public int GetNext() => Interlocked.Increment(ref _counter);
    }

    private class MySingletonService
    {
        private readonly int _counter;

        public MySingletonService(int counter) => _counter = counter;

        public int GetNext() => _counter;
    }
}
