using Wkg.Common.ThrowHelpers;
using Wkg.Data.Pooling;

namespace Wkg.IO;

/// <summary>
/// A read-only stream that caches the data read from the source stream, allowing random access to non-seekable source streams.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CachingStream"/> class.
/// </remarks>
/// <param name="source">The source stream to read from.</param>
/// <param name="leaveOpen">Whether to leave the source stream open when disposing this stream.</param>
public class CachingStream(Stream source, bool leaveOpen) : Stream
{
    private readonly bool _keepSourceOpen = leaveOpen;
    private readonly Stream _source = source;
    private readonly MemoryStream _cache = new();
    private bool _disposedValue;

    /// <summary>
    /// Gets a value indicating whether the current stream supports reading. For <see cref="CachingStream"/>, this property is always <see langword="true"/>.
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    /// Gets a value indicating whether the current stream supports seeking. For <see cref="CachingStream"/>, this property is always <see langword="true"/>.
    /// </summary>
    public override bool CanSeek => true;

    /// <summary>
    /// Gets a value indicating whether the current stream supports writing. For <see cref="CachingStream"/>, this property is always <see langword="false"/>.
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// Gets the number of bytes cached in the stream.
    /// </summary>
    public long BytesCached => _cache.Length;

    /// <summary>
    /// Returns the length of the stream in bytes. For <see cref="CachingStream"/>, this property is the length of the source stream.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _source.Length;
        }
    }

    /// <summary>
    /// Gets or sets the position within the current stream, caching data for random access as necessary.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _cache.Position;
        }

        set => Seek(value, SeekOrigin.Begin);
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBuffer(buffer, offset, count);
        if (count == 0 || Position >= _source.Length)
        {
            return 0;
        }
        // grow the cache if necessary, then read from the cache
        EnsureCached(Position + count);
        return _cache.Read(buffer, offset, count);
    }

    private static void ValidateBuffer(byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset, nameof(offset));
        ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
        Throw.ArgumentOutOfRangeException.IfNotInRange(count, min: 0, max: buffer.Length - offset, nameof(count));
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public async override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (Position >= _source.Length)
        {
            return 0;
        }
        // grow the cache if necessary, then read from the cache
        await EnsureCachedAsync(Position + buffer.Length, cancellationToken).ConfigureAwait(false);
        return _cache.Read(buffer.Span);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public override int Read(Span<byte> buffer)
    {
        if (Position >= _source.Length)
        {
            return 0;
        }
        // grow the cache if necessary, then read from the cache
        EnsureCached(Position + buffer.Length);
        return _cache.Read(buffer);
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        if (Position >= _source.Length)
        {
            return -1;
        }
        EnsureCached(Position + 1);
        return _cache.ReadByte();
    }

    /// <summary>
    /// Reads the byte at the current position in the stream, asynchronously caching data for random access as necessary.
    /// </summary>
    /// <returns>The unsigned byte cast to an <see cref="int"/>, or -1 if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public async ValueTask<int> ReadByteAsync()
    {
        if (Position >= _source.Length)
        {
            return -1;
        }
        await EnsureCachedAsync(Position + 1).ConfigureAwait(false);
        return _cache.ReadByte();
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="origin"/> parameter is not a valid value of <see cref="SeekOrigin"/>.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        long position = SeekCore(offset, origin);
        EnsureCached(position);
        _cache.Seek(position, SeekOrigin.Begin);
        return position;
    }

    /// <summary>
    /// Sets the position within the current stream, asynchronously caching data for random access as necessary.
    /// </summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
    /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The new position within the current stream.</returns>
    /// <exception cref="IOException">Thrown when an attempt is made to move the position before the beginning or beyond the end of the stream.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="origin"/> parameter is not a valid value of <see cref="SeekOrigin"/>.</exception>
    public async ValueTask<long> SeekAsync(long offset, SeekOrigin origin, CancellationToken cancellationToken = default)
    {
        long position = SeekCore(offset, origin);
        await EnsureCachedAsync(position, cancellationToken).ConfigureAwait(false);
        _cache.Seek(position, SeekOrigin.Begin);
        return position;
    }

    private long SeekCore(long offset, SeekOrigin origin)
    {
        long position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        if (position < 0)
        {
            throw new IOException("An attempt was made to move the position before the beginning of the stream.");
        }
        if (position > Length)
        {
            throw new IOException("An attempt was made to move the position beyond the end of the stream.");
        }
        return position;
    }

    /// <summary>
    /// Ensures that the first <paramref name="size"/> bytes of the stream are cached for random access.
    /// </summary>
    /// <param name="size">The number of bytes to cache.</param>
    /// <exception cref="NotSupportedException">Thrown when the stream is too large to cache.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public void EnsureCached(long size)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (size > int.MaxValue)
        {
            throw new NotSupportedException("The stream is too large to cache.");
        }
        if (size <= _cache.Length || Position >= _source.Length)
        {
            return;
        }
        PooledArray<byte> cacheBuffer = ArrayPool.Rent<byte>((int)(size - _cache.Length));
        try
        {
            // remember the current position
            long position = _cache.Position;
            _cache.Seek(0, SeekOrigin.End);
            // read from the source
            int read = _source.Read(cacheBuffer.Array, 0, cacheBuffer.Length);
            // write to the cache
            _cache.Write(cacheBuffer.Array, 0, read);
            // restore the position
            _cache.Seek(position, SeekOrigin.Begin);
        }
        finally
        {
            ArrayPool.Return(cacheBuffer);
        }
    }

    /// <summary>
    /// Ensures that the first <paramref name="size"/> bytes of the stream are cached for random access, asynchronously reading from the source stream if necessary.
    /// </summary>
    /// <param name="size">The number of bytes to cache.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <exception cref="NotSupportedException">When the stream is too large to cache.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public async ValueTask EnsureCachedAsync(long size, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (size > int.MaxValue)
        {
            throw new NotSupportedException("The stream is too large to cache.");
        }
        if (size <= _cache.Length || Position >= _source.Length)
        {
            return;
        }
        PooledArray<byte> cacheBuffer = ArrayPool.Rent<byte>((int)(size - _cache.Length));
        try
        {
            // remember the current position
            long position = _cache.Position;
            _cache.Seek(0, SeekOrigin.End);
            // read from the source
            int read = await _source.ReadAsync(cacheBuffer.Array.AsMemory(0, cacheBuffer.Length), cancellationToken).ConfigureAwait(false);
            if (read > 0)
            {
                // write to the cache
                _cache.Write(cacheBuffer.Array, 0, read);
                // restore the position
                _cache.Seek(position, SeekOrigin.Begin);
            }
        }
        finally
        {
            ArrayPool.Return(cacheBuffer);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method is not supported by <see cref="CachingStream"/> and will throw a <see cref="NotSupportedException"/>.
    /// </remarks>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    /// <remarks>
    /// This method is not supported by <see cref="CachingStream"/> and will throw a <see cref="NotSupportedException"/>.
    /// </remarks>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    /// <remarks>
    /// This method is not supported by <see cref="CachingStream"/> and will throw a <see cref="NotSupportedException"/>.
    /// </remarks>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (!_keepSourceOpen)
                {
                    _source.Dispose();
                }
                _cache.Dispose();
            }
            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public async override ValueTask DisposeAsync()
    {
        if (!_disposedValue)
        {
            if (!_keepSourceOpen)
            {
                await _source.DisposeAsync().ConfigureAwait(false);
            }
            _cache.Dispose();
            _disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }
}
