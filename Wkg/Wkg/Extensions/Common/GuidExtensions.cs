using System.Runtime.CompilerServices;
using System.Text;

namespace Wkg.Extensions.Common;

/// <summary>
/// Contains extension methods for the <see cref="Guid"/> struct.
/// </summary>
public static class GuidExtensions
{
    /// <summary>
    /// Converts a <see cref="Guid"/> to its big endian string representation.
    /// <para>
    /// <see href="https://stackoverflow.com/questions/48147681/get-a-guid-to-encode-using-big-endian-formatting-c-sharp"/>
    /// </para>
    /// </summary>
    public static string ToStringBigEndian(this Guid guid)
    {
        // allocate enough bytes to store Guid ASCII string
        Span<byte> result = stackalloc byte[36];

        // get bytes from guid
        Span<byte> buffer = stackalloc byte[16];
        _ = guid.TryWriteBytes(buffer);

        int skip = 0;

        // iterate over guid bytes
        for (int i = 0; i < buffer.Length; i++)
        {
            // indices 4, 6, 8 and 10 will contain a '-' delimiter character in the Guid string.
            // --> leave space for those delimiters
            // we can check if i is even and i / 2 is >= 2 and <= 5 to determine if we are at one of those indices
            // 0xF...F if i is odd and 0x0...0 if i is even
            int isOddMask = -(i & 1);

            // 0xF...F if i / 2 is < 2 and 0x0...0 if i / 2 is >= 2
            int less2Mask = ((i >> 1) - 2) >> 31;

            // 0xF...F if i / 2 is > 5 and 0x0...0 if i / 2 is <= 5
            int greater5Mask = ~(((i >> 1) - 6) >> 31);

            // 0xF...F if i is even and 2 <= i / 2 <= 5 otherwise 0x0...0
            int skipIndexMask = ~(isOddMask | less2Mask | greater5Mask);

            // skipIndexMask will be 0xFFFFFFFF for indices 4, 6, 8 and 10 and 0x00000000 for all other indices
            // --> skip those indices
            skip += 1 & skipIndexMask;
            result[(2 * i) + skip] = ToHexCharBranchless(buffer[i] >>> 0x4);
            result[(2 * i) + skip + 1] = ToHexCharBranchless(buffer[i] & 0x0F);
        }

        // add dashes
        const byte dash = (byte)'-';
        result[8] = result[13] = result[18] = result[23] = dash;

        // get string from ASCII encoded guid byte array
        return Encoding.ASCII.GetString(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToHexCharBranchless(int b) =>
        // b + 0x30 for [0-9] if 0 <= b <= 9 and b + 0x30 + 0x27 for [a-f] if 10 <= b <= 15
        (byte)(b + 0x30 + (0x27 & ~((b - 0xA) >> 31)));
}
