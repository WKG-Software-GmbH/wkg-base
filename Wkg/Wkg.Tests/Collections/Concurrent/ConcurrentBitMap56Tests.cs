using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Wkg.Collections.Concurrent.Tests;

[TestClass]
public class ConcurrentBitMap56Tests
{
    [TestMethod]
    public void Full_ReturnsBitMapWithAllBitsSet1()
    {
        ConcurrentBitMap56 bitMap = ConcurrentBitMap56.Full(56);
        Assert.IsTrue(bitMap.IsFull(56));
        for (int i = 0; i < 56; i++)
        {
            Assert.IsTrue(bitMap.IsBitSet(i));
        }
    }

    [TestMethod]
    public void Full_ReturnsBitMapWithAllBitsSet2()
    {
        ConcurrentBitMap56 bitMap = ConcurrentBitMap56.Full(48);
        Assert.IsTrue(bitMap.IsFull(48));
        for (int i = 0; i < 48; i++)
        {
            Assert.IsTrue(bitMap.IsBitSet(i));
        }
    }

    [TestMethod]
    public void Empty_ReturnsBitMapWithAllBitsClear()
    {
        ConcurrentBitMap56 bitMap = ConcurrentBitMap56.Empty;
        Assert.IsTrue(bitMap.IsEmpty);
        for (int i = 0; i < 56; i++)
        {
            Assert.IsFalse(bitMap.IsBitSet(i));
        }
    }

    [TestMethod]
    public void UpdateBit_UpdatesBitAtSpecifiedIndex()
    {
        ConcurrentBitMap56 bitMap = ConcurrentBitMap56.Empty;
        ConcurrentBitMap56.UpdateBit(ref bitMap, 0, true);
        Assert.IsTrue(bitMap.IsBitSet(0));
        ConcurrentBitMap56.UpdateBit(ref bitMap, 0, false);
        Assert.IsFalse(bitMap.IsBitSet(0));

        ConcurrentBitMap56.UpdateBit(ref bitMap, 49, true);
        Assert.IsTrue(bitMap.IsBitSet(49));
        ConcurrentBitMap56.UpdateBit(ref bitMap, 49, false);
        Assert.IsFalse(bitMap.IsBitSet(49));
        ConcurrentBitMap56.UpdateBit(ref bitMap, 49, false);
        Assert.IsFalse(bitMap.IsBitSet(49));

        ConcurrentBitMap56.UpdateBit(ref bitMap, 55, true);
        Assert.IsTrue(bitMap.IsBitSet(55));
        ConcurrentBitMap56.ClearAll(ref bitMap);
        Assert.IsFalse(bitMap.IsBitSet(55));
        Assert.IsTrue(bitMap.IsEmpty);
    }

    [TestMethod]
    public void InsertBitAt_InsertsBitAtSpecifiedIndex()
    {
        ConcurrentBitMap56 bitMap = ConcurrentBitMap56.Empty;
        for (int i = 0; i < 8; i++)
        {
            ConcurrentBitMap56.UpdateBit(ref bitMap, i, true);
        }
        Assert.AreEqual(0xFFuL, bitMap.AsUInt64());
        ConcurrentBitMap56.InsertBitAt(ref bitMap, 4, false);
        Assert.AreEqual(0b1_1110_1111uL, bitMap.AsUInt64());
        Assert.IsFalse(bitMap.IsBitSet(4));
        Assert.IsTrue(bitMap.IsBitSet(8));
        ConcurrentBitMap56.InsertBitAt(ref bitMap, 15, true);
        Assert.AreEqual(0b1000_0001_1110_1111uL, bitMap.AsUInt64());
        Assert.IsTrue(bitMap.IsBitSet(15));
        Assert.IsTrue(bitMap.IsBitSet(8));
    }

    [TestMethod]
    public void RemoveBitAt_RemovesBitAtSpecifiedIndex()
    {
        ConcurrentBitMap56 bitMap = ConcurrentBitMap56.Empty;
        for (int i = 0; i < 8; i++)
        {
            ConcurrentBitMap56.UpdateBit(ref bitMap, i, true);
        }
        Assert.AreEqual(0xFFuL, bitMap.AsUInt64());
        ConcurrentBitMap56.UpdateBit(ref bitMap, 15, true);
        Assert.IsTrue(bitMap.IsBitSet(15));
        Assert.IsFalse(bitMap.IsBitSet(14));
        ConcurrentBitMap56.RemoveBitAt(ref bitMap, 4);
        Assert.AreEqual(0b0100_0000_0111_1111uL, bitMap.AsUInt64());
        Assert.IsTrue(bitMap.IsBitSet(4));
        Assert.IsFalse(bitMap.IsBitSet(15));
        Assert.IsTrue(bitMap.IsBitSet(14));
    }

    [TestMethod]
    public void TokenizedUpdateTest()
    {
        ConcurrentBitMap56 bitMap = ConcurrentBitMap56.Empty;
        for (int i = 0; i < 8; i++)
        {
            ConcurrentBitMap56.UpdateBit(ref bitMap, i, true);
        }
        Assert.AreEqual(0xFFuL, bitMap.AsUInt64());
        byte token = bitMap.GetToken();
        Assert.IsTrue(ConcurrentBitMap56.TryUpdateBit(ref bitMap, token, 4, false));
        Assert.AreEqual(0b1110_1111uL, bitMap.AsUInt64());
        Assert.IsFalse(bitMap.IsBitSet(4));
        // The token is no longer valid.
        Assert.IsFalse(ConcurrentBitMap56.TryUpdateBit(ref bitMap, token, 0, false));
        Assert.AreEqual(0b1110_1111uL, bitMap.AsUInt64());
        Assert.IsTrue(bitMap.IsBitSet(0));
        token = bitMap.GetToken();
        Assert.IsTrue(ConcurrentBitMap56.TryUpdateBit(ref bitMap, token, 0, false));
        Assert.AreEqual(0b1110_1110uL, bitMap.AsUInt64());
        Assert.IsFalse(bitMap.IsBitSet(0));
    }
}
