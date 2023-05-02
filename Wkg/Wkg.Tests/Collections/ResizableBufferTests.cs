using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Collections;
using Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;
using Wkg.Unmanaged.MemoryManagement;

namespace Wkg.Tests.Collections;

[TestClass]
public class ResizableBufferTests : BaseTest
{
    [TestMethod]
    public void ResizableBufferTest()
    {
        ResizableBuffer<byte> buffer;

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => buffer = new ResizableBuffer<byte>(-1));

        buffer = new(4096);
        Assert.AreEqual(0, buffer.Length);
        Assert.ThrowsException<IndexOutOfRangeException>(() => buffer[0]);

        byte[] myBytes = new byte[] { 0, 1, 2, 3, 4, 5, 6 };
        buffer.Add(myBytes.AsSpan());
        Assert.AreEqual(myBytes.Length, buffer.Length);
        Assert.AreEqual(0, myBytes[0]);
        Assert.AreEqual(1, myBytes[1]);
        Assert.AreEqual(2, myBytes[2]);
        Assert.AreEqual(3, myBytes[3]);
        Assert.AreEqual(4, myBytes[4]);
        Assert.AreEqual(5, myBytes[5]);
        Assert.AreEqual(6, myBytes[6]);

        Assert.ThrowsException<IndexOutOfRangeException>(() => buffer[-1]);
        Assert.ThrowsException<IndexOutOfRangeException>(() => buffer[7]);

        buffer[0] = 42;
        Assert.AreEqual(42, buffer[0]);

        buffer.Dispose();

        AllocationSnapshot? allocationSnapshot = MemoryManager.GetAllocationSnapshot(reset: true);

        Assert.AreEqual(0ul, allocationSnapshot?.TotalByteCount);

        Assert.ThrowsException<ObjectDisposedException>(() => buffer[0]);
        buffer.Dispose();
    }

    [TestMethod]
    public void AddTest()
    {
        using (ResizableBuffer<byte> buffer = new(4))
        {
            Assert.AreEqual(0, buffer.Length);

            byte[] myBytes = new byte[] { 1, 2, 3 };
            buffer.Add(myBytes.AsSpan());
            Assert.AreEqual(3, buffer.Length);

            buffer.Add(myBytes);
            Assert.AreEqual(6, buffer.Length);

            for (int i = 0; i < buffer.Length; i++)
            {
                if (i < myBytes.Length)
                {
                    Assert.AreEqual(myBytes[i], buffer[i]);
                }
                else
                {
                    Assert.AreEqual(myBytes[i - myBytes.Length], buffer[i]);
                }
            }

            Assert.ThrowsException<IndexOutOfRangeException>(() => buffer[6]);

            buffer.Add(Span<byte>.Empty);
            Assert.AreEqual(6, buffer.Length);

            buffer.Add(myBytes.AsSpan(), 1, 1);
            Assert.AreEqual(7, buffer.Length);
            Assert.AreEqual(2, buffer[6]);

            Span<byte> mySpan = buffer.AsSpan();
            Assert.AreEqual(buffer.Length, mySpan.Length);

            mySpan[0] = 5;
            Assert.AreEqual(5, buffer[0]);

            byte[] myArray = buffer.ToArray();
            Assert.AreEqual(buffer.Length, myArray.Length);

            myArray[0] = 10;
            Assert.AreEqual(5, buffer[0]);
        }
        AllocationSnapshot? allocationSnapshot = MemoryManager.GetAllocationSnapshot(reset: true);

        Assert.AreEqual(0ul, allocationSnapshot?.TotalByteCount);
    }
}
