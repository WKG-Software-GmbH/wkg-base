using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Collections;

namespace Wkg.Tests.Collections;

[TestClass]
public class CyclicQueueTests : BaseTest
{
    [TestMethod]
    public void CyclicQueueTest()
    {
        CyclicQueue<int> cyclicQueue = new(5);

        cyclicQueue.Enqueue(0);
        cyclicQueue.Enqueue(1);
        cyclicQueue.Enqueue(2);
        cyclicQueue.Enqueue(3);

        Assert.AreEqual(0, cyclicQueue.Dequeue());
        Assert.AreEqual(1, cyclicQueue.Dequeue());
        Assert.AreEqual(2, cyclicQueue.Dequeue());
        Assert.AreEqual(3, cyclicQueue.Dequeue());

        Assert.AreEqual(0, cyclicQueue.Count);

        cyclicQueue.Enqueue(0);
        cyclicQueue.Enqueue(1);
        cyclicQueue.Enqueue(2);
        cyclicQueue.Enqueue(3);

        Assert.AreEqual(4, cyclicQueue.Count);
        cyclicQueue.Clear();
        Assert.AreEqual(0, cyclicQueue.Count);

        cyclicQueue.Enqueue(0);
        cyclicQueue.Enqueue(-1);
        cyclicQueue.Enqueue(-2);
        cyclicQueue.Enqueue(-3);
        cyclicQueue.Enqueue(-4);
        cyclicQueue.Enqueue(-5);
        cyclicQueue.Enqueue(-6);

        Assert.AreEqual(5, cyclicQueue.Count);

        Assert.AreEqual(-2, cyclicQueue.Peek());
        Assert.AreEqual(-2, cyclicQueue.Dequeue());
        Assert.AreEqual(-3, cyclicQueue.Dequeue());
        Assert.AreEqual(-4, cyclicQueue.Dequeue());
        Assert.AreEqual(-5, cyclicQueue.Dequeue());
        Assert.AreEqual(-6, cyclicQueue.Dequeue());

        Assert.AreEqual(0, cyclicQueue.Count);
        Assert.ThrowsException<InvalidOperationException>(() => cyclicQueue.Dequeue());
        Assert.ThrowsException<InvalidOperationException>(() => cyclicQueue.Peek());

        cyclicQueue.Enqueue(0);
        Assert.AreEqual(1, cyclicQueue.Count);
        Assert.AreEqual(0, cyclicQueue.Dequeue());

        Assert.AreEqual(0, cyclicQueue.Count);
    }
}
