using System.Runtime.CompilerServices;

namespace Wkg.Common;

/// <summary>
/// A collection of additional math methods that should really be part of the .NET BCL :P
/// </summary>
public static class MathExtensions
{
    /// <summary>
    /// Calculates the modulo of two integers.
    /// </summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The modulo of <paramref name="a"/> and <paramref name="b"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Modulo(int a, int b) => (a % b + b) % b;
}
