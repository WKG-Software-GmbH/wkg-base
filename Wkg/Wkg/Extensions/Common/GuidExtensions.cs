using System.Text;

namespace Wkg.Extensions.Common;

public static class GuidExtensions
{
    /// <summary>
    /// Converts the <paramref name="guid"/> to it's big endian string representation.
    /// <para>
    /// <see href="https://stackoverflow.com/questions/48147681/get-a-guid-to-encode-using-big-endian-formatting-c-sharp"/>
    /// </para>
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static string ToStringBigEndian(this Guid guid)
    {
        // allocate enough bytes to store Guid ASCII string
        Span<byte> result = stackalloc byte[36];

        // set all bytes to 0xFF (to be able to distinguish them from real data)
        result.Fill(0xFF);

        // get bytes from guid
        Span<byte> buffer = stackalloc byte[16];
        _ = guid.TryWriteBytes(buffer);

        int skip = 0;

        // iterate over guid bytes
        for (int i = 0; i < buffer.Length; i++)
        {
            // indices 4, 6, 8 and 10 will contain a '-' delimiter character in the Guid string.
            // --> leave space for those delimiters
            int mask = ~(((-(i ^ 4)) >> 31) & ((-(i ^ 6)) >> 31) & ((-(i ^ 8)) >> 31) & ((-(i ^ 10)) >> 31));

            // mask will be 0xFFFFFFFF for indices 4, 6, 8 and 10 and 0x00000000 for all other indices
            // --> skip those indices
            skip += 1 & mask;

            // stretch high and low bytes of every single byte into two bytes (skipping '-' delimiter characters)
            result[(2 * i) + skip] = (byte)(buffer[i] >> 0x4);
            result[(2 * i) + 1 + skip] = (byte)(buffer[i] & 0x0Fu);
        }

        // iterate over precomputed byte array.
        // values 0x0 to 0xF are final hex values, but must be mapped to ASCII characters.
        // value 0xFF is to be mapped to '-' delimiter character.
        for (int i = 0; i < result.Length; i++)
        {
            // map bytes to ASCII values (a-f will be lowercase)
            ref byte b = ref result[i];
            b = b switch
            {
                0xFF => 0x2D,                // Map 0xFF to '-' character
                < 0xA => (byte)(b + 0x30u),  // Map 0x0 - 0x9 to '0' - '9'
                _ => (byte)(b + 0x57u)       // Map 0xA - 0xF to 'a' - 'f'
            };
        }

        // get string from ASCII encoded guid byte array
        return Encoding.ASCII.GetString(result);
    }
}
