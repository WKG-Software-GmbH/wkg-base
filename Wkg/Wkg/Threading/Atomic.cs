using System.Runtime.CompilerServices;

namespace Wkg.Threading;

/// <summary>
/// Provides additional atomic operations, complementing <see cref="Interlocked"/>.
/// </summary>
/// <remarks>
/// Operations in this class are guaranteed to be lock-free, and exhibit atomic behavior to the calling thread.
/// </remarks>
public static class Atomic
{
    #region TestAllFlagsExchange

    /// <summary>
    /// Tests whether the specified flags is set in the specified location (<c>(location &amp; flags) == flags</c>), and if so, replaces the value stored in that location with the specified value.
    /// </summary>
    /// <param name="location">The location to test and exchange.</param>
    /// <param name="value">The value to exchange.</param>
    /// <param name="flags">The flags to test for.</param>
    /// <returns>The original value stored in <paramref name="location"/>.</returns>
    public static int TestAllFlagsExchange(ref int location, int value, int flags)
    {
        bool isFlagSet;
        int original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) == flags;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return original;
    }

    /// <inheritdoc cref="TestAllFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint TestAllFlagsExchange(ref uint location, uint value, uint flags) =>
        (uint)TestAllFlagsExchange(ref Unsafe.As<uint, int>(ref location), (int)value, (int)flags);

    /// <inheritdoc cref="TestAllFlagsExchange(ref int, int, int)"/>"
    public static long TestAllFlagsExchange(ref long location, long value, long flags)
    {
        bool isFlagSet;
        long original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) == flags;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return original;
    }

    /// <inheritdoc cref="TestAllFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong TestAllFlagsExchange(ref ulong location, ulong value, ulong flags) =>
        (ulong)TestAllFlagsExchange(ref Unsafe.As<ulong, long>(ref location), (long)value, (long)flags);

    /// <inheritdoc cref="TestAllFlagsExchange(ref int, int, int)"/>"
    public static nint TestAllFlagsExchange(ref nint location, nint value, nint flags)
    {
        bool isFlagSet;
        nint original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) == flags;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return original;
    }

    /// <inheritdoc cref="TestAllFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint TestAllFlagsExchange(ref nuint location, nuint value, nuint flags) =>
        (nuint)TestAllFlagsExchange(ref Unsafe.As<nuint, nint>(ref location), (nint)value, (nint)flags);

    #endregion TestAllFlagsExchange

    #region TryTestAllFlagsExchange

    /// <summary>
    /// Tests whether the specified flags is set in the specified location (<c>(location &amp; flags) == flags</c>), and if so, replaces the value stored in that location with the specified value.
    /// </summary>
    /// <param name="location">The location to test and exchange.</param>
    /// <param name="value">The value to exchange.</param>
    /// <param name="flags">The flags to test for.</param>
    /// <returns><see langword="true"/> if original value stored in <paramref name="location"/> was replaced; otherwise, <see langword="false"/>.</returns>
    public static bool TryTestAllFlagsExchange(ref int location, int value, int flags)
    {
        bool isFlagSet;
        int original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) == flags;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return isFlagSet;
    }

    /// <inheritdoc cref="TryTestAllFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryTestAllFlagsExchange(ref uint location, uint value, uint flags) =>
        TryTestAllFlagsExchange(ref Unsafe.As<uint, int>(ref location), (int)value, (int)flags);

    /// <inheritdoc cref="TryTestAllFlagsExchange(ref int, int, int)"/>"
    public static bool TryTestAllFlagsExchange(ref long location, long value, long flags)
    {
        bool isFlagSet;
        long original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) == flags;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return isFlagSet;
    }

    /// <inheritdoc cref="TryTestAllFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryTestAllFlagsExchange(ref ulong location, ulong value, ulong flags) =>
        TryTestAllFlagsExchange(ref Unsafe.As<ulong, long>(ref location), (long)value, (long)flags);

    /// <inheritdoc cref="TryTestAllFlagsExchange(ref int, int, int)"/>"
    public static bool TryTestAllFlagsExchange(ref nint location, nint value, nint flags)
    {
        bool isFlagSet;
        nint original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) == flags;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return isFlagSet;
    }

    /// <inheritdoc cref="TryTestAllFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryTestAllFlagsExchange(ref nuint location, nuint value, nuint flags) =>
        TryTestAllFlagsExchange(ref Unsafe.As<nuint, nint>(ref location), (nint)value, (nint)flags);

    #endregion TryTestAllFlagsExchange

    #region TestAnyFlagsExchange

    /// <summary>
    /// Tests whether any of the specified flags are set in the specified location (<c>(location &amp; flags) != 0</c>), and if so, replaces the value stored in that location with the specified value.
    /// </summary>
    /// <param name="location">The location to test and exchange.</param>
    /// <param name="value">The value to exchange.</param>
    /// <param name="flags">The flags to test for.</param>
    /// <returns>The original value stored in <paramref name="location"/>.</returns>
    public static int TestAnyFlagsExchange(ref int location, int value, int flags)
    {
        bool isFlagSet;
        int original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) != 0;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return original;
    }

    /// <inheritdoc cref="TestAnyFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint TestAnyFlagsExchange(ref uint location, uint value, uint flags) =>
        (uint)TestAnyFlagsExchange(ref Unsafe.As<uint, int>(ref location), (int)value, (int)flags);

    /// <inheritdoc cref="TestAnyFlagsExchange(ref int, int, int)"/>"
    public static long TestAnyFlagsExchange(ref long location, long value, long flags)
    {
        bool isFlagSet;
        long original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) != 0;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return original;
    }

    /// <inheritdoc cref="TestAnyFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong TestAnyFlagsExchange(ref ulong location, ulong value, ulong flags) =>
        (ulong)TestAnyFlagsExchange(ref Unsafe.As<ulong, long>(ref location), (long)value, (long)flags);

    /// <inheritdoc cref="TestAnyFlagsExchange(ref int, int, int)"/>"
    public static nint TestAnyFlagsExchange(ref nint location, nint value, nint flags)
    {
        bool isFlagSet;
        nint original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) != 0;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return original;
    }

    /// <inheritdoc cref="TestAnyFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint TestAnyFlagsExchange(ref nuint location, nuint value, nuint flags) =>
        (nuint)TestAnyFlagsExchange(ref Unsafe.As<nuint, nint>(ref location), (nint)value, (nint)flags);

    #endregion TestAnyFlagsExchange

    #region TryTestAnyFlagsExchange

    /// <summary>
    /// Tests whether any of the specified flags are set in the specified location (<c>(location &amp; flags) != 0</c>), and if so, replaces the value stored in that location with the specified value.
    /// </summary>
    /// <param name="location">The location to test and exchange.</param>
    /// <param name="value">The value to exchange.</param>
    /// <param name="flags">The flags to test for.</param>
    /// <returns><see langword="true"/> if original value stored in <paramref name="location"/> was replaced; otherwise, <see langword="false"/>.</returns>
    public static bool TryTestAnyFlagsExchange(ref int location, int value, int flags)
    {
        bool isFlagSet;
        int original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) != 0;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return isFlagSet;
    }

    /// <inheritdoc cref="TryTestAnyFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryTestAnyFlagsExchange(ref uint location, uint value, uint flags) =>
        TryTestAnyFlagsExchange(ref Unsafe.As<uint, int>(ref location), (int)value, (int)flags);

    /// <inheritdoc cref="TryTestAnyFlagsExchange(ref int, int, int)"/>"
    public static bool TryTestAnyFlagsExchange(ref long location, long value, long flags)
    {
        bool isFlagSet;
        long original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) != 0;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return isFlagSet;
    }

    /// <inheritdoc cref="TryTestAnyFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryTestAnyFlagsExchange(ref ulong location, ulong value, ulong flags) =>
        TryTestAnyFlagsExchange(ref Unsafe.As<ulong, long>(ref location), (long)value, (long)flags);

    /// <inheritdoc cref="TryTestAnyFlagsExchange(ref int, int, int)"/>"
    public static bool TryTestAnyFlagsExchange(ref nint location, nint value, nint flags)
    {
        bool isFlagSet;
        nint original;
        do
        {
            original = Volatile.Read(ref location);
            isFlagSet = (original & flags) != nint.Zero;
        }
        while (isFlagSet && Interlocked.CompareExchange(ref location, value, original) != original);
        return isFlagSet;
    }

    /// <inheritdoc cref="TryTestAnyFlagsExchange(ref int, int, int)"/>"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryTestAnyFlagsExchange(ref nuint location, nuint value, nuint flags) =>
        TryTestAnyFlagsExchange(ref Unsafe.As<nuint, nint>(ref location), (nint)value, (nint)flags);

    #endregion TryTestAnyFlagsExchange
}
