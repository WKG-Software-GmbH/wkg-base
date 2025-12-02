using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wkg.Common;

/// <summary>
/// Provides performance-oriented implementations of common math functions that are generally faster 
/// than the BCL implementations in the <see cref="Math"/> API, but some additional preconditions may apply.
/// </summary>
[DebuggerStepThrough]
[Obsolete(OBSOLETE_MESSAGE)]
public static class FastMath
{
    private const string OBSOLETE_MESSAGE = "The FastMath.<SomeMethod> API is deprecated and has been replaced with Math.<SomeMethod> extension methods. The legacy FastMath API will be removed in a future version of this library";

    /// <inheritdoc cref="MathExtensions.FastMin(int, int)" />
    [Obsolete(OBSOLETE_MESSAGE)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Min(int x, int y) => Math.FastMin(x, y);

    /// <inheritdoc cref="MathExtensions.FastMin(long, long)" />
    [Obsolete(OBSOLETE_MESSAGE)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Min(long x, long y) => Math.FastMin(x, y);

    /// <inheritdoc cref="MathExtensions.FastMax(int, int)" />
    [Obsolete(OBSOLETE_MESSAGE)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Max(int x, int y) => Math.FastMax(x, y);

    /// <inheritdoc cref="MathExtensions.FastMax(long, long)" />
    [Obsolete(OBSOLETE_MESSAGE)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Max(long x, long y) => Math.FastMax(x, y);
}