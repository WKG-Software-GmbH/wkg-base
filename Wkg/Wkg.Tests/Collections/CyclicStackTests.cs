using Wkg.Collections;

namespace Wkg.Tests.Collections;

[TestClass]
public sealed class CyclicStackTests : BaseTest
{
    [TestMethod]
    public void CyclicStackTest()
    {
        CyclicStack<int> cyclicStack = new(5);

        cyclicStack.Push(0);
        cyclicStack.Push(1);
        cyclicStack.Push(2);
        cyclicStack.Push(3);

        Assert.AreEqual(3, cyclicStack.Pop());
        Assert.AreEqual(2, cyclicStack.Pop());
        Assert.AreEqual(1, cyclicStack.Pop());
        Assert.AreEqual(0, cyclicStack.Pop());

        Assert.AreEqual(0, cyclicStack.Count);

        cyclicStack.Push(0);
        cyclicStack.Push(1);
        cyclicStack.Push(2);
        cyclicStack.Push(3);

        Assert.AreEqual(4, cyclicStack.Count);
        cyclicStack.Clear();
        Assert.AreEqual(0, cyclicStack.Count);

        cyclicStack.Push(0);
        cyclicStack.Push(-1);
        cyclicStack.Push(-2);
        cyclicStack.Push(-3);
        cyclicStack.Push(-4);
        cyclicStack.Push(-5);
        cyclicStack.Push(-6);

        Assert.AreEqual(5, cyclicStack.Count);

        Assert.AreEqual(-6, cyclicStack.Peek());

        Assert.AreEqual(-6, cyclicStack.Pop());
        Assert.AreEqual(-5, cyclicStack.Pop());
        Assert.AreEqual(-4, cyclicStack.Pop());
        Assert.AreEqual(-3, cyclicStack.Pop());
        Assert.AreEqual(-2, cyclicStack.Pop());

        Assert.AreEqual(0, cyclicStack.Count);
        Assert.ThrowsExactly<InvalidOperationException>(() => cyclicStack.Pop());
        Assert.ThrowsExactly<InvalidOperationException>(() => cyclicStack.Peek());

        cyclicStack.Push(0);
        Assert.AreEqual(1, cyclicStack.Count);
        Assert.AreEqual(0, cyclicStack.Pop());

        Assert.AreEqual(0, cyclicStack.Count);
    }
}
