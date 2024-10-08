﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Common.Extensions;
using Wkg.Threading.Workloads;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.ConstrainedLifo;
using Wkg.Threading.Workloads.WorkloadTypes;
using CL = Wkg.Threading.Workloads.Queuing.Classless.ConstrainedLifo.ConstrainedLifo;

namespace Wkg.Tests.Threading.Workloads.Queuing.Classless.ConstrainedLifo;

[TestClass]
public class ConstrainedLifoQdiscTests
{
    private static readonly IQdiscBuilderContext s_context = new QdiscBuilderContext();

    private static IClassifyingQdisc<int> CreateDefaultQdisc(int capacity)
    {
        IClassifyingQdisc<int> qdisc = CL.CreateBuilder(s_context)
            .WithCapacity(capacity)
            .To<IClasslessQdiscBuilder>()
            .BuildUnsafe<int>();
        qdisc.InternalInitialize(default(DummyScheduler));
        return qdisc;
    }

    [TestMethod]
    public void TestBuilder()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            CL.CreateBuilder(s_context).To<IClasslessQdiscBuilder>().Build(1, null));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            CL.CreateBuilder(s_context).WithCapacity(0).To<IClasslessQdiscBuilder>().Build(1, null));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            CL.CreateBuilder(s_context).WithCapacity(1).WithCapacity(0).To<IClasslessQdiscBuilder>().Build(1, null));

        Assert.ThrowsException<InvalidOperationException>(() =>
            CL.CreateBuilder(s_context).WithCapacity(1).WithCapacity(3).To<IClasslessQdiscBuilder>().Build(1, null));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            CL.CreateBuilder(s_context).WithCapacity(-1).To<IClasslessQdiscBuilder>().Build(1, null));

        IClassifyingQdisc<int> qdisc = CL.CreateBuilder(s_context).WithCapacity(8).To<IClasslessQdiscBuilder>().Build(1, null);
        Assert.IsNotNull(qdisc);
        Assert.IsInstanceOfType<ConstrainedLifoQdisc<int>>(qdisc);
    }

    [TestMethod]
    public void TestEnqueueDequeue1()
    {
        IClassifyingQdisc<int> qdisc = CreateDefaultQdisc(8);
        Assert.AreEqual(0, qdisc.BestEffortCount);
        Assert.IsTrue(qdisc.IsEmpty);

        AbstractWorkloadBase workload1 = NewDummyWorkload();
        ulong id1 = workload1.Id;
        qdisc.Enqueue(workload1);
        Assert.AreEqual(1, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        AbstractWorkloadBase workload2 = NewDummyWorkload();
        ulong id2 = workload2.Id;
        qdisc.Enqueue(workload2);
        Assert.AreEqual(2, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        AbstractWorkloadBase workload3 = NewDummyWorkload();
        ulong id3 = workload3.Id;
        qdisc.Enqueue(workload3);
        Assert.AreEqual(3, qdisc.BestEffortCount);

        Assert.IsTrue(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result3));
        Assert.AreEqual(id3, result3!.Id);
        Assert.AreEqual(2, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        Assert.IsTrue(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result2));
        Assert.AreEqual(id2, result2!.Id);
        Assert.AreEqual(1, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        Assert.IsTrue(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result1));
        Assert.AreEqual(id1, result1!.Id);
        Assert.AreEqual(0, qdisc.BestEffortCount);
        Assert.IsTrue(qdisc.IsEmpty);

        Assert.IsFalse(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result0));
        Assert.IsNull(result0);
        Assert.AreEqual(0, qdisc.BestEffortCount);
        Assert.IsTrue(qdisc.IsEmpty);

        AbstractWorkloadBase workload4 = NewDummyWorkload();
        ulong id4 = workload4.Id;
        qdisc.Enqueue(workload4);
        Assert.AreEqual(1, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        Assert.IsTrue(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result4));
        Assert.AreEqual(id4, result4!.Id);
        Assert.AreEqual(0, qdisc.BestEffortCount);
        Assert.IsTrue(qdisc.IsEmpty);
    }

    [TestMethod]
    public void TestEnqueueDequeue2()
    {
        IClassifyingQdisc<int> qdisc = CreateDefaultQdisc(4);
        Assert.AreEqual(0, qdisc.BestEffortCount);
        Assert.IsTrue(qdisc.IsEmpty);

        AbstractWorkloadBase workload1 = NewDummyWorkload();
        qdisc.Enqueue(workload1);
        Assert.AreEqual(1, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        AbstractWorkloadBase workload2 = NewDummyWorkload();
        ulong id2 = workload2.Id;
        qdisc.Enqueue(workload2);
        Assert.AreEqual(2, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        AbstractWorkloadBase workload3 = NewDummyWorkload();
        ulong id3 = workload3.Id;
        qdisc.Enqueue(workload3);
        Assert.AreEqual(3, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        AbstractWorkloadBase workload4 = NewDummyWorkload();
        ulong id4 = workload4.Id;
        qdisc.Enqueue(workload4);
        int count = qdisc.BestEffortCount;
        Assert.AreEqual(4, count);
        Assert.IsFalse(qdisc.IsEmpty);

        bool success = qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result4);
        Assert.IsTrue(success);
        Assert.AreEqual(id4, result4!.Id);
        Assert.AreEqual(3, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        AbstractWorkloadBase workload5 = NewDummyWorkload();
        ulong id5 = workload5.Id;
        qdisc.Enqueue(workload5);
        Assert.AreEqual(4, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        AbstractWorkloadBase workload6 = NewDummyWorkload();
        ulong id6 = workload6.Id;
        qdisc.Enqueue(workload6);
        Assert.AreEqual(4, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        Assert.IsTrue(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result6));
        Assert.AreEqual(id6, result6!.Id);
        Assert.AreEqual(3, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        Assert.IsTrue(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result5));
        Assert.AreEqual(id5, result5!.Id);
        Assert.AreEqual(2, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        Assert.IsTrue(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result3));
        Assert.AreEqual(id3, result3!.Id);
        Assert.AreEqual(1, qdisc.BestEffortCount);
        Assert.IsFalse(qdisc.IsEmpty);

        Assert.IsTrue(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result2));
        Assert.AreEqual(id2, result2!.Id);
        Assert.AreEqual(0, qdisc.BestEffortCount);
        Assert.IsTrue(qdisc.IsEmpty);

        Assert.IsFalse(qdisc.TryDequeueInternal(0, false, out AbstractWorkloadBase? result7));
        Assert.IsNull(result7);
        Assert.AreEqual(0, qdisc.BestEffortCount);
        Assert.IsTrue(qdisc.IsEmpty);
    }

    private static AnonymousWorkloadImpl NewDummyWorkload()
    {
        AnonymousWorkloadImpl workload = new(Pass);
        return workload;
    }
}

file readonly struct DummyScheduler : INotifyWorkScheduled
{
    void INotifyWorkScheduled.DisposeRoot() => Pass();
    void INotifyWorkScheduled.OnWorkScheduled() => Pass();
}