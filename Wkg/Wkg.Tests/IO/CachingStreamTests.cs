using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.IO;

namespace Wkg.Tests.IO;

[TestClass]
public class CachingStreamTests
{
    private MemoryStream _sourceStream = null!;
    private CachingStream _cachingStream = null!;

    [TestInitialize]
    public void Setup()
    {
        byte[] data =
        [
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
            31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
            41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
            51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
            61, 62, 63, 64, 65, 66, 67, 68, 69, 70,
            71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
            81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
            91, 92, 93, 94, 95, 96, 97, 98, 99, 100
        ];
        _sourceStream = new MemoryStream(data);
        _cachingStream = new CachingStream(_sourceStream, leaveOpen: true);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _cachingStream.Dispose();
        _sourceStream.Dispose();
    }

    [TestMethod]
    public void TestCanRead() => Assert.IsTrue(_cachingStream.CanRead);

    [TestMethod]
    public void TestCanSeek() => Assert.IsTrue(_cachingStream.CanSeek);

    [TestMethod]
    public void TestCanWrite() => Assert.IsFalse(_cachingStream.CanWrite);

    [TestMethod]
    public void TestLength() => Assert.AreEqual(_sourceStream.Length, _cachingStream.Length);

    [TestMethod]
    public void TestPosition()
    {
        _cachingStream.Position = 10;
        Assert.AreEqual(10, _cachingStream.Position);
        int byteRead = _cachingStream.ReadByte();
        Assert.AreEqual(11, byteRead);
    }

    [TestMethod]
    public void TestRead()
    {
        byte[] buffer = new byte[10];
        int bytesRead = _cachingStream.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(10, bytesRead);
        Assert.IsTrue(buffer.AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
        bytesRead = _cachingStream.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(10, bytesRead);
        Assert.IsTrue(buffer.AsSpan().SequenceEqual(new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));
    }

    [TestMethod]
    public async Task TestReadAsync()
    {
        byte[] buffer = new byte[10];
        int bytesRead = await _cachingStream.ReadAsync(buffer);
        Assert.AreEqual(10, bytesRead);
        Assert.IsTrue(buffer.AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
        bytesRead = await _cachingStream.ReadAsync(buffer);
        Assert.AreEqual(10, bytesRead);
        Assert.IsTrue(buffer.AsSpan().SequenceEqual(new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));
    }

    [TestMethod]
    public void TestReadSpan()
    {
        Span<byte> buffer = new byte[10];
        int bytesRead = _cachingStream.Read(buffer);
        Assert.AreEqual(10, bytesRead);
        Assert.IsTrue(buffer.SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
        bytesRead = _cachingStream.Read(buffer);
        Assert.AreEqual(10, bytesRead);
        Assert.IsTrue(buffer.SequenceEqual(new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));
    }

    [TestMethod]
    public void TestReadByte()
    {
        Assert.AreEqual(1, _cachingStream.ReadByte());
        Assert.AreEqual(2, _cachingStream.ReadByte());
        Assert.AreEqual(3, _cachingStream.ReadByte());
    }

    [TestMethod]
    public async Task TestReadByteAsync()
    {
        int byteRead = await _cachingStream.ReadByteAsync();
        Assert.AreEqual(1, byteRead);
        byteRead = await _cachingStream.ReadByteAsync();
        Assert.AreEqual(2, byteRead);
        byteRead = await _cachingStream.ReadByteAsync();
        Assert.AreEqual(3, byteRead);
    }

    [TestMethod]
    public void TestSeek()
    {
        long newPosition = _cachingStream.Seek(10, SeekOrigin.Begin);
        Assert.AreEqual(10, newPosition);
        Assert.AreEqual(10, _cachingStream.Position);
    }

    [TestMethod]
    public async Task TestSeekAsync()
    {
        long newPosition = await _cachingStream.SeekAsync(10, SeekOrigin.Begin);
        Assert.AreEqual(10, newPosition);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void TestSetLength() => _cachingStream.SetLength(100);

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void TestWrite() => _cachingStream.Write(new byte[10], 0, 10);

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void TestFlush() => _cachingStream.Flush();

    [TestMethod]
    public void TestDispose()
    {
        _cachingStream.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => _cachingStream.ReadByte());
    }

    [TestMethod]
    public async Task TestDisposeAsync()
    {
        await _cachingStream.DisposeAsync();
        Assert.ThrowsException<ObjectDisposedException>(() => _cachingStream.ReadByte());
    }
}