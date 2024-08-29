using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Collections.Concurrent;
using Wkg.Tests;

namespace Wkg.Tests.Collections.Concurrent;

using static ConcurrentBitmap;

[TestClass]
public class ConcurrentBitmapTests : BaseTest
{
    [TestMethod]
    public void ConcurrentBitmapTestSingleSegment()
    {
        using ConcurrentBitmap bitmap = new(SEGMENT_BIT_SIZE);
        Assert.AreEqual(SEGMENT_BIT_SIZE, bitmap.Length);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }

        bitmap.UpdateBit(0, true);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsTrue(bitmap.IsBitSet(0));
        for (int i = 1; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        bitmap.UpdateBit(0, false);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            bitmap.UpdateBit(i, true);
            Assert.IsFalse(bitmap.IsEmpty);
            Assert.IsFalse(bitmap.IsFull);
            for (int j = 0; j < bitmap.Length; j++)
            {
                Assert.AreEqual(j <= i, bitmap.IsBitSet(j));
            }
        }
        bitmap.UpdateBit(bitmap.Length - 1, true);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.IsTrue(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            bitmap.UpdateBit(i, false);
            Assert.IsFalse(bitmap.IsEmpty);
            Assert.IsFalse(bitmap.IsFull);
            for (int j = 0; j < bitmap.Length; j++)
            {
                Assert.AreEqual(j >= i + 1, bitmap.IsBitSet(j));
            }
        }
        bitmap.UpdateBit(bitmap.Length - 1, false);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
    }

    [TestMethod]
    public void ConcurrentBitmapTestSingleCluster()
    {
        using ConcurrentBitmap bitmap = new(CLUSTER_BIT_SIZE);
        Assert.AreEqual(CLUSTER_BIT_SIZE, bitmap.Length);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }

        bitmap.UpdateBit(0, true);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsTrue(bitmap.IsBitSet(0));
        for (int i = 1; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        bitmap.UpdateBit(0, false);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            bitmap.UpdateBit(i, true);
            Assert.IsFalse(bitmap.IsEmpty);
            Assert.IsFalse(bitmap.IsFull);
            for (int j = 0; j < bitmap.Length; j++)
            {
                Assert.AreEqual(j <= i, bitmap.IsBitSet(j));
            }
        }
        bitmap.UpdateBit(bitmap.Length - 1, true);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.IsTrue(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            bitmap.UpdateBit(i, false);
            Assert.IsFalse(bitmap.IsEmpty);
            Assert.IsFalse(bitmap.IsFull);
            for (int j = 0; j < bitmap.Length; j++)
            {
                Assert.AreEqual(j >= i + 1, bitmap.IsBitSet(j));
            }
        }
        bitmap.UpdateBit(bitmap.Length - 1, false);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
    }

    [TestMethod]
    public void ConcurrentBitmapTestSuperCluster()
    {
        using ConcurrentBitmap bitmap = new(INTERNAL_NODE_BIT_LIMIT);
        Assert.AreEqual(INTERNAL_NODE_BIT_LIMIT, bitmap.Length);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }

        bitmap.UpdateBit(0, true);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsTrue(bitmap.IsBitSet(0));
        for (int i = 1; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        bitmap.UpdateBit(0, false);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            bitmap.UpdateBit(i, true);
            Assert.IsFalse(bitmap.IsEmpty);
            Assert.IsFalse(bitmap.IsFull);
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        bitmap.UpdateBit(bitmap.Length - 1, true);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.IsTrue(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            bitmap.UpdateBit(i, false);
            Assert.IsFalse(bitmap.IsEmpty);
            Assert.IsFalse(bitmap.IsFull);
        }
        bitmap.UpdateBit(bitmap.Length - 1, false);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
    }

    [TestMethod]
    public void ConcurrentBitmapTestMultiCluster()
    {
        using ConcurrentBitmap bitmap = new(INTERNAL_NODE_BIT_LIMIT + 1);
        Assert.AreEqual(INTERNAL_NODE_BIT_LIMIT + 1, bitmap.Length);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }

        bitmap.UpdateBit(0, true);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsTrue(bitmap.IsBitSet(0));
        for (int i = 1; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        bitmap.UpdateBit(0, false);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            bitmap.UpdateBit(i, true);
            Assert.IsFalse(bitmap.IsEmpty);
            Assert.IsFalse(bitmap.IsFull);
        }
        bitmap.UpdateBit(bitmap.Length - 1, true);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.IsTrue(bitmap.IsFull);
        for (int i = 0; i < bitmap.Length; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            bitmap.UpdateBit(i, false);
            Assert.IsFalse(bitmap.IsEmpty);
            Assert.IsFalse(bitmap.IsFull);
        }
        for (int i = 0; i < bitmap.Length - 1; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        bitmap.UpdateBit(bitmap.Length - 1, false);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.IsFalse(bitmap.IsFull);
    }

    [TestMethod]
    public void ConcurrentBitmapInsertRemoveTestSingleSegment()
    {
        using ConcurrentBitmap bitmap = new(SEGMENT_BIT_SIZE);
        for (int i = 0; i < 8; i++)
        {
            bitmap.UpdateBit(i, true);
        }
        bitmap.InsertBitAt(4, false);
        for (int i = 0; i < 4; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        Assert.IsFalse(bitmap.IsBitSet(4));
        for (int i = 5; i < 9; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = 9; i < 56; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        bitmap.RemoveBitAt(4);
        for (int i = 0; i < 8; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = 8; i < 56; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
            bitmap.UpdateBit(i, true);
        }
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        bitmap.InsertBitAt(4, false);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        bitmap.RemoveBitAt(4);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
    }

    [TestMethod]
    public void ConcurrentBitmapInsertRemoveTestSingleCluster()
    {
        using ConcurrentBitmap bitmap = new(CLUSTER_BIT_SIZE);
        for (int i = 0; i < SEGMENT_BIT_SIZE; i++)
        {
            bitmap.UpdateBit(i, true);
        }
        bitmap.InsertBitAt(4, false);
        for (int i = 0; i < 4; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        Assert.IsFalse(bitmap.IsBitSet(4));
        for (int i = 5; i < SEGMENT_BIT_SIZE + 1; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = SEGMENT_BIT_SIZE + 1; i < CLUSTER_BIT_SIZE; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        bitmap.RemoveBitAt(4);
        for (int i = 0; i < SEGMENT_BIT_SIZE; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = SEGMENT_BIT_SIZE; i < CLUSTER_BIT_SIZE; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
            bitmap.UpdateBit(i, true);
        }
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        bitmap.InsertBitAt(SEGMENT_BIT_SIZE + 4, false);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        bitmap.RemoveBitAt(SEGMENT_BIT_SIZE + 4);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
    }

    [TestMethod]
    public void ConcurrentBitmapInsertRemoveTestSuperCluster()
    {
        using ConcurrentBitmap bitmap = new(INTERNAL_NODE_BIT_LIMIT);
        for (int i = 0; i < CLUSTER_BIT_SIZE; i++)
        {
            bitmap.UpdateBit(i, true);
        }
        bitmap.InsertBitAt(4, false);
        for (int i = 0; i < 4; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        Assert.IsFalse(bitmap.IsBitSet(4));
        for (int i = 5; i < CLUSTER_BIT_SIZE + 1; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = CLUSTER_BIT_SIZE + 1; i < INTERNAL_NODE_BIT_LIMIT; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
        }
        bitmap.RemoveBitAt(4);
        for (int i = 0; i < CLUSTER_BIT_SIZE; i++)
        {
            Assert.IsTrue(bitmap.IsBitSet(i));
        }
        for (int i = CLUSTER_BIT_SIZE; i < INTERNAL_NODE_BIT_LIMIT; i++)
        {
            Assert.IsFalse(bitmap.IsBitSet(i));
            bitmap.UpdateBit(i, true);
        }
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        bitmap.InsertBitAt(CLUSTER_BIT_SIZE + 4, false);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        bitmap.RemoveBitAt(CLUSTER_BIT_SIZE + 4);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
    }

    [TestMethod]
    public void ConcurrentBitmapInsertRemoveTestMultiCluster()
    {
        using ConcurrentBitmap bitmap = new(INTERNAL_NODE_BIT_LIMIT + 1);
        bitmap.UpdateBit(INTERNAL_NODE_BIT_LIMIT - 1, true);
        bitmap.InsertBitAt(4, false);
        Assert.IsFalse(bitmap.IsBitSet(4));
        Assert.IsTrue(bitmap.IsBitSet(INTERNAL_NODE_BIT_LIMIT));
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        bitmap.RemoveBitAt(4);
        Assert.IsFalse(bitmap.IsBitSet(4));
        Assert.IsTrue(bitmap.IsBitSet(INTERNAL_NODE_BIT_LIMIT - 1));
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        bitmap.InsertBitAt(4, false);
        bitmap.InsertBitAt(4, false);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsTrue(bitmap.IsEmpty);
    }

    [TestMethod]
    public void TestGrowing1()
    {
        using ConcurrentBitmap bitmap = new(SEGMENT_BIT_SIZE);
        for (int i = 0; i < SEGMENT_BIT_SIZE; i++)
        {
            bitmap.UpdateBit(i, true);
        }
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(SEGMENT_BIT_SIZE, bitmap.Length);
        bitmap.InsertBitAt(0, value: false, grow: true);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(SEGMENT_BIT_SIZE + 1, bitmap.Length);
        Assert.IsTrue(bitmap.IsBitSet(SEGMENT_BIT_SIZE));
        bitmap.RemoveBitAt(0, shrink: true);
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(SEGMENT_BIT_SIZE, bitmap.Length);
    }

    [TestMethod]
    public void TestGrowing2()
    {
        using ConcurrentBitmap bitmap = new(CLUSTER_BIT_SIZE);
        for (int i = 0; i < CLUSTER_BIT_SIZE; i++)
        {
            bitmap.UpdateBit(i, true);
        }
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(CLUSTER_BIT_SIZE, bitmap.Length);
        bitmap.InsertBitAt(CLUSTER_BIT_SIZE - 1, value: false, grow: true);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(CLUSTER_BIT_SIZE + 1, bitmap.Length);
        Assert.IsTrue(bitmap.IsBitSet(CLUSTER_BIT_SIZE));
        bitmap.RemoveBitAt(CLUSTER_BIT_SIZE - 1, shrink: true);
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(CLUSTER_BIT_SIZE, bitmap.Length);
    }

    [TestMethod]
    public void TestGrowing3()
    {
        using ConcurrentBitmap bitmap = new(1);
        bitmap.UpdateBit(0, true);
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(1, bitmap.Length);
        bitmap.Grow(INTERNAL_NODE_BIT_LIMIT);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(INTERNAL_NODE_BIT_LIMIT + 1, bitmap.Length);
        Assert.IsTrue(bitmap.IsBitSet(0));
        bitmap.RemoveBitAt(0, shrink: true);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.AreEqual(INTERNAL_NODE_BIT_LIMIT, bitmap.Length);
    }

    [TestMethod]
    public void TestShrinking1()
    {
        using ConcurrentBitmap bitmap = new(INTERNAL_NODE_BIT_LIMIT + 1);
        bitmap.UpdateBit(0, true);
        Assert.IsFalse(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(INTERNAL_NODE_BIT_LIMIT + 1, bitmap.Length);
        bitmap.Shrink(INTERNAL_NODE_BIT_LIMIT);
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(1, bitmap.Length);
        Assert.IsTrue(bitmap.IsBitSet(0));
        bitmap.RemoveBitAt(0, shrink: true);
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsTrue(bitmap.IsEmpty);
        Assert.AreEqual(0, bitmap.Length);
        bitmap.InsertBitAt(0, value: true, grow: true);
        Assert.IsTrue(bitmap.IsFull);
        Assert.IsFalse(bitmap.IsEmpty);
        Assert.AreEqual(1, bitmap.Length);
        Assert.IsTrue(bitmap.IsBitSet(0));
    }
}