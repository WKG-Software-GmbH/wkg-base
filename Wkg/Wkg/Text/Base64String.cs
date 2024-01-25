using System.Buffers.Text;
using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace Wkg.Text;

/// <summary>
/// Provides helper methods for working with base64 encoded strings.
/// </summary>
public static class Base64String
{
    /// <summary>
    /// Decodes a base64 encoded string to a UTF-8 string, without allocating temporary objects.
    /// </summary>
    /// <param name="base64EncodedString">The base64 encoded string to decode.</param>
    /// <returns>The decoded UTF-8 string.</returns>
    public static string DecodeToUtf8(string base64EncodedString)
    {
        // base64 is always ASCII, so string length == byte length
        byte[] base64Buffer = ArrayPool<byte>.Shared.Rent(base64EncodedString.Length);
        int bytesWritten = Encoding.ASCII.GetBytes(base64EncodedString, base64Buffer);
        Debug.Assert(bytesWritten == base64EncodedString.Length);
        // the rented buffer is always the next larger power of 2, so crop it to the actual length
        Span<byte> base64Span = base64Buffer.AsSpan(0, bytesWritten);
        // UTF-8 == ASCII for the first 128 characters, so we can decode in-place
        OperationStatus status = Base64.DecodeFromUtf8InPlace(base64Span, out int base64decodedLength);
        Debug.Assert(status == OperationStatus.Done);
        // the decoded length is always smaller than the encoded length, so crop it to the actual length
        Span<byte> decodedSpan = base64Span[..base64decodedLength];
        // the decoded data is UTF-8, so we can just convert it to a string
        string base64DecodedString = Encoding.UTF8.GetString(decodedSpan);
        // return the rented buffer to be reused
        ArrayPool<byte>.Shared.Return(base64Buffer);
        return base64DecodedString;
    }

    /// <summary>
    /// Encodes a UTF-8 string to a base64 encoded string, without allocating temporary objects.
    /// </summary>
    /// <param name="utf8String">The UTF-8 string to encode.</param>
    /// <returns>The base64 encoded string.</returns>
    public static string EncodeFromUtf8(string utf8String)
    {
        // encoding is an inflating operation, so determine the maximum possible size
        int utf8ByteCount = Encoding.UTF8.GetByteCount(utf8String);
        int base64CharCount = Base64.GetMaxEncodedToUtf8Length(utf8ByteCount);
        // rent a large enough buffer to hold the encoded data
        byte[] buffer = ArrayPool<byte>.Shared.Rent(base64CharCount);
        // write the UTF-8 string to the buffer
        int utf8BytesWritten = Encoding.UTF8.GetBytes(utf8String, buffer);
        Debug.Assert(utf8ByteCount == utf8BytesWritten);
        // encode the UTF-8 data in-place
        OperationStatus status = Base64.EncodeToUtf8InPlace(buffer, utf8BytesWritten, out int base64CharsWritten);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(base64CharCount == base64CharsWritten);
        // read the encoded data as a string
        string base64 = Encoding.UTF8.GetString(buffer, 0, base64CharsWritten);
        // return the rented buffer to be reused
        ArrayPool<byte>.Shared.Return(buffer);
        return base64;
    }
}
