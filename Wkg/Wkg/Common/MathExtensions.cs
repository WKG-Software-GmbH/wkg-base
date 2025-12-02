using System.Runtime.CompilerServices;

namespace Wkg.Common;

/// <summary>
/// Provides performance-oriented implementations of common math functions that are generally faster 
/// than the BCL implementations in the <see cref="Math"/> API and extend the functionality of the <see cref="Math"/> API.
/// </summary>
public static class MathExtensions
{
#pragma warning disable CA1034 // Nested types should not be visible
    // TODO: CA1034 doesn't yet understand C# 14 extension syntax and raises false positives here
    extension(Math)
#pragma warning restore CA1034 // Nested types should not be visible
    {
        /// <summary>
        /// Calculates the minimum of <paramref name="x"/> and <paramref name="y"/> where <c>int.MinValue &lt;= x - y &lt;= int.MaxValue</c>
        /// </summary>
        /// <param name="x">x, where <c>int.MinValue &lt;= x - y &lt;= int.MaxValue</c></param>
        /// <param name="y">y, where <c>int.MinValue &lt;= x - y &lt;= int.MaxValue</c></param>
        /// <returns>The minimum of <paramref name="x"/> and <paramref name="y"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastMin(int x, int y) =>
            y + ((x - y) & ((x - y) >> 31));

        /// <summary>
        /// Calculates the minimum of <paramref name="x"/> and <paramref name="y"/> where <c>long.MinValue &lt;= x - y &lt;= long.MaxValue</c>
        /// </summary>
        /// <param name="x">x, where <c>long.MinValue &lt;= x - y &lt;= long.MaxValue</c></param>
        /// <param name="y">y, where <c>long.MinValue &lt;= x - y &lt;= long.MaxValue</c></param>
        /// <returns>The minimum of <paramref name="x"/> and <paramref name="y"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long FastMin(long x, long y) =>
            y + ((x - y) & ((x - y) >> 63));

        /// <summary>
        /// Calculates the maximum of <paramref name="x"/> and <paramref name="y"/> where <c>int.MinValue &lt;= x - y &lt;= int.MaxValue</c>
        /// </summary>
        /// <param name="x">x, where <c>int.MinValue &lt;= x - y &lt;= int.MaxValue</c></param>
        /// <param name="y">y, where <c>int.MinValue &lt;= x - y &lt;= int.MaxValue</c></param>
        /// <returns>The maximum of <paramref name="x"/> and <paramref name="y"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastMax(int x, int y) =>
            x - ((x - y) & ((x - y) >> 31));

        /// <summary>
        /// Calculates the maximum of <paramref name="x"/> and <paramref name="y"/> where <c>long.MinValue &lt;= x - y &lt;= long.MaxValue</c>
        /// </summary>
        /// <param name="x">x, where <c>long.MinValue &lt;= x - y &lt;= long.MaxValue</c></param>
        /// <param name="y">y, where <c>long.MinValue &lt;= x - y &lt;= long.MaxValue</c></param>
        /// <returns>The maximum of <paramref name="x"/> and <paramref name="y"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long FastMax(long x, long y) =>
            x - ((x - y) & ((x - y) >> 63));

        /// <summary>
        /// Calculates the modulo of two integers, also accommodating negative dividends.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The modulo of <paramref name="a"/> and <paramref name="b"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Modulo(int a, int b) => ((a % b) + b) % b;
    }
}
