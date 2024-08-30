using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Collections.Concurrent;

namespace Wkg.Tests.Collections.Concurrent;

#pragma warning disable CS0618 // Type or member is obsolete

[TestClass]
public class ConcurrentBitmap56Tests
{
    [TestMethod]
    public void Full_ReturnsBitMapWithAllBitsSet1()
    {
        ConcurrentBitmap56State state = ConcurrentBitmap56.FullState(56);
        Assert.IsTrue(((ConcurrentBitmap56)state).IsFull(56));
        for (int i = 0; i < 56; i++)
        {
            Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(i));
        }
    }

    [TestMethod]
    public void Full_ReturnsBitMapWithAllBitsSet2()
    {
        ConcurrentBitmap56State state = ConcurrentBitmap56.FullState(48);
        Assert.IsTrue(((ConcurrentBitmap56)state).IsFull(48));
        for (int i = 0; i < 48; i++)
        {
            Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(i));
        }
    }

    [TestMethod]
    public void Empty_ReturnsBitMapWithAllBitsClear()
    {
        ConcurrentBitmap56State state = ConcurrentBitmap56.EmptyState;
        Assert.IsTrue(((ConcurrentBitmap56)state).IsEmpty());
        for (int i = 0; i < 56; i++)
        {
            Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(i));
        }
    }

    [TestMethod]
    public void UpdateBit_UpdatesBitAtSpecifiedIndex()
    {
        ConcurrentBitmap56State state = ConcurrentBitmap56.EmptyState;
        ConcurrentBitmap56.UpdateBit(ref state, 0, true);

        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(0));
        ConcurrentBitmap56.UpdateBit(ref state, 0, false);
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(0));

        ConcurrentBitmap56.UpdateBit(ref state, 49, true);
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(49));
        ConcurrentBitmap56.UpdateBit(ref state, 49, false);
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(49));
        ConcurrentBitmap56.UpdateBit(ref state, 49, false);
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(49));

        ConcurrentBitmap56.UpdateBit(ref state, 55, true);
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(55));
        ConcurrentBitmap56.ClearAll(ref state);
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(55));
        Assert.IsTrue(((ConcurrentBitmap56)state).IsEmpty());
    }

    [TestMethod]
    public void InsertBitAt_InsertsBitAtSpecifiedIndex()
    {
        ConcurrentBitmap56State state = ConcurrentBitmap56.EmptyState;
        for (int i = 0; i < 8; i++)
        {
            ConcurrentBitmap56.UpdateBit(ref state, i, true);
        }
        Assert.AreEqual(0xFFuL, ((ConcurrentBitmap56)state).GetRawData());
        ConcurrentBitmap56.InsertBitAt(ref state, 4, false);
        Assert.AreEqual(0b1_1110_1111uL, ((ConcurrentBitmap56)state).GetRawData());
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(4));
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(8));
        ConcurrentBitmap56.InsertBitAt(ref state, 15, true);
        Assert.AreEqual(0b1000_0001_1110_1111uL, ((ConcurrentBitmap56)state).GetRawData());
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(15));
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(8));
    }

    [TestMethod]
    public void RemoveBitAt_RemovesBitAtSpecifiedIndex()
    {
        ConcurrentBitmap56State state = ConcurrentBitmap56.EmptyState;
        for (int i = 0; i < 8; i++)
        {
            ConcurrentBitmap56.UpdateBit(ref state, i, true);
        }
        Assert.AreEqual(0xFFuL, ((ConcurrentBitmap56)state).GetRawData());
        ConcurrentBitmap56.UpdateBit(ref state, 15, true);
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(15));
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(14));
        ConcurrentBitmap56.RemoveBitAt(ref state, 4);
        Assert.AreEqual(0b0100_0000_0111_1111uL, ((ConcurrentBitmap56)state).GetRawData());
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(4));
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(15));
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(14));
    }

    [TestMethod]
    public void TokenizedUpdateTest()
    {
        ConcurrentBitmap56State state = ConcurrentBitmap56.EmptyState;
        for (int i = 0; i < 8; i++)
        {
            ConcurrentBitmap56.UpdateBit(ref state, i, true);
        }
        Assert.AreEqual(0xFFuL, ((ConcurrentBitmap56)state).GetRawData());
        byte token = ((ConcurrentBitmap56)state).GetToken();
        Assert.IsTrue(ConcurrentBitmap56.TryUpdateBit(ref state, token, 4, false));
        Assert.AreEqual(0b1110_1111uL, ((ConcurrentBitmap56)state).GetRawData());
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(4));
        // The token is no longer valid.
        Assert.IsFalse(ConcurrentBitmap56.TryUpdateBit(ref state, token, 0, false));
        Assert.AreEqual(0b1110_1111uL, ((ConcurrentBitmap56)state).GetRawData());
        Assert.IsTrue(((ConcurrentBitmap56)state).IsBitSet(0));
        token = ((ConcurrentBitmap56)state).GetToken();
        Assert.IsTrue(ConcurrentBitmap56.TryUpdateBit(ref state, token, 0, false));
        Assert.AreEqual(0b1110_1110uL, ((ConcurrentBitmap56)state).GetRawData());
        Assert.IsFalse(((ConcurrentBitmap56)state).IsBitSet(0));
    }
}

#pragma warning restore CS0618 // Type or member is obsolete