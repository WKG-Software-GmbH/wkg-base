using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Runtime;
using System.Runtime.InteropServices;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace ConsoleApp1;

public class Tests
{
    private readonly ClasslessWorkloadFactory<int> _factory = WorkloadFactoryBuilder.Create<int>()
        .UseMaximumConcurrency(8)
        .FlowExecutionContextToContinuations(false)
        .RunContinuationsOnCapturedContext(false)
        .UseAnonymousWorkloadPooling(256)
        .UseClasslessRoot<Fifo>(1);

    private readonly int[] _data = Enumerable.Range(0, 1 << 20).Select(i => i).ToArray();

    [Benchmark]
    public long Cash()
    {
        Container container = new()
        {
            Count = _data.Length
        };

        ManualResetEventSlim mres = new(false);

        for (int i = 0; i < 1 << 20; i++)
        {
            _factory.Schedule(() =>
            {
                if (Interlocked.Decrement(ref container.Count) == 0)
                {
                    mres.Set();
                }
            });
        }

        mres.Wait();
        return container.Result;
    }

    [Benchmark]
    public long Tpl()
    {
        Container container = new()
        {
            Count = _data.Length
        };

        ManualResetEventSlim mres = new(false);

        for (int i = 0; i < 1 << 20; i++)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (Interlocked.Decrement(ref container.Count) == 0)
                {
                    mres.Set();
                }
            });
        }

        mres.Wait();
        return container.Result;
    }
}

file class Container
{
    public volatile int Count;
    public long Result;
}