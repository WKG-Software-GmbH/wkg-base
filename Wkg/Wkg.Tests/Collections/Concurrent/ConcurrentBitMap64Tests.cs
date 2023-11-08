using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Wkg.Collections.Concurrent.Tests;

[TestClass]
public class ConcurrentBitMap64Tests
{
    [TestMethod]
    public void Full_ReturnsBitMapWithAllBitsSet1()
    {
        ConcurrentBitMap64 bitMap = ConcurrentBitMap64.Full(64);
        Assert.IsTrue(bitMap.IsFull(64));
        for (int i = 0; i < 64; i++)
        {
            Assert.IsTrue(bitMap.IsBitSet(i));
        }
    }

    [TestMethod]
    public void Full_ReturnsBitMapWithAllBitsSet2()
    {
        ConcurrentBitMap64 bitMap = ConcurrentBitMap64.Full(48);
        Assert.IsTrue(bitMap.IsFull(48));
        for (int i = 0; i < 48; i++)
        {
            Assert.IsTrue(bitMap.IsBitSet(i));
        }
    }

    [TestMethod]
    public void Empty_ReturnsBitMapWithAllBitsClear()
    {
        ConcurrentBitMap64 bitMap = ConcurrentBitMap64.Empty;
        Assert.IsTrue(bitMap.IsEmpty);
        for (int i = 0; i < 64; i++)
        {
            Assert.IsFalse(bitMap.IsBitSet(i));
        }
    }

    [TestMethod]
    public void UpdateBit_UpdatesBitAtSpecifiedIndex()
    {
        ConcurrentBitMap64 bitMap = ConcurrentBitMap64.Empty;
        ConcurrentBitMap64.UpdateBit(ref bitMap, 0, true);
        Assert.IsTrue(bitMap.IsBitSet(0));
        ConcurrentBitMap64.UpdateBit(ref bitMap, 0, false);
        Assert.IsFalse(bitMap.IsBitSet(0));

        ConcurrentBitMap64.UpdateBit(ref bitMap, 49, true);
        Assert.IsTrue(bitMap.IsBitSet(49));
        ConcurrentBitMap64.UpdateBit(ref bitMap, 49, false);
        Assert.IsFalse(bitMap.IsBitSet(49));
        ConcurrentBitMap64.UpdateBit(ref bitMap, 49, false);
        Assert.IsFalse(bitMap.IsBitSet(49));

        ConcurrentBitMap64.UpdateBit(ref bitMap, 63, true);
        Assert.IsTrue(bitMap.IsBitSet(63));
        ConcurrentBitMap64.ClearAll(ref bitMap);
        Assert.IsFalse(bitMap.IsBitSet(63));
        Assert.IsTrue(bitMap.IsEmpty);
    }

    [TestMethod]
    public void InsertBitAt_InsertsBitAtSpecifiedIndex()
    {
        ConcurrentBitMap64 bitMap = ConcurrentBitMap64.Empty;
        for (int i = 0; i < 8; i++)
        {
            ConcurrentBitMap64.UpdateBit(ref bitMap, i, true);
        }
        Assert.AreEqual(0xFFuL, bitMap.AsUInt64());
        ConcurrentBitMap64.InsertBitAt(ref bitMap, 4, false);
        Assert.AreEqual(0b1_1110_1111uL, bitMap.AsUInt64());
        Assert.IsFalse(bitMap.IsBitSet(4));
        Assert.IsTrue(bitMap.IsBitSet(8));
        ConcurrentBitMap64.InsertBitAt(ref bitMap, 15, true);
        Assert.AreEqual(0b1000_0001_1110_1111uL, bitMap.AsUInt64());
        Assert.IsTrue(bitMap.IsBitSet(15));
        Assert.IsTrue(bitMap.IsBitSet(8));
    }

    [TestMethod]
    public void RemoveBitAt_RemovesBitAtSpecifiedIndex()
    {
        ConcurrentBitMap64 bitMap = ConcurrentBitMap64.Empty;
        for (int i = 0; i < 8; i++)
        {
            ConcurrentBitMap64.UpdateBit(ref bitMap, i, true);
        }
        Assert.AreEqual(0xFFuL, bitMap.AsUInt64());
        ConcurrentBitMap64.UpdateBit(ref bitMap, 15, true);
        Assert.IsTrue(bitMap.IsBitSet(15));
        Assert.IsFalse(bitMap.IsBitSet(14));
        ConcurrentBitMap64.RemoveBitAt(ref bitMap, 4);
        Assert.AreEqual(0b0100_0000_0111_1111uL, bitMap.AsUInt64());
        Assert.IsTrue(bitMap.IsBitSet(4));
        Assert.IsFalse(bitMap.IsBitSet(15));
        Assert.IsTrue(bitMap.IsBitSet(14));
    }
}
