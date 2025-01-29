using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Wkg.Common;

namespace Wkg.Threading;

/// <summary>
/// Provides additional atomic operations, complementing <see cref="Interlocked"/>.
/// </summary>
/// <remarks>
/// Operations in this class are guaranteed to be lock-free, and exhibit atomic behavior to the calling thread.
/// </remarks>
public static class Atomic
{
    #region Interlocked

    /// <summary>
    /// Sets a <see cref="ConcurrentBoolean"/> to the specified value and returns the original value, as an atomic operation.
    /// </summary>
    /// <param name="location1">The variable to set to the specified value.</param>
    /// <param name="value">The value to which the <paramref name="location1"/> parameter is set.</param>
    /// <returns>The original value of <paramref name="location1"/>.</returns>
    /// <exception cref="NullReferenceException">The address of <paramref name="location1"/> is a null pointer.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Exchange(ref ConcurrentBoolean location1, bool value) =>
        Interlocked.Exchange(ref Unsafe.As<ConcurrentBoolean, uint>(ref location1), (uint)(ConcurrentBoolean)value) != ConcurrentBoolean.FALSE;

    /// <summary>
    /// Compares two <see cref="ConcurrentBoolean"/> values for equality and, if they are equal, replaces the first value, as an atomic operation.
    /// </summary>
    /// <param name="location1">The destination, whose value is compared with <paramref name="comparand"/> and possibly replaced.</param>
    /// <param name="value">The value that replaces the destination value if the comparison results in equality.</param>
    /// <param name="comparand">The value that is compared to the value at <paramref name="location1"/>.</param>
    /// <returns>The original value in <paramref name="location1"/>.</returns>
    /// <exception cref="NullReferenceException">The address of <paramref name="location1"/> is a null pointer.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CompareExchange(ref ConcurrentBoolean location1, bool value, bool comparand) =>
        Interlocked.CompareExchange(ref Unsafe.As<ConcurrentBoolean, uint>(ref location1), (uint)(ConcurrentBoolean)value, (uint)(ConcurrentBoolean)comparand) != ConcurrentBoolean.FALSE;

    /// <summary>
    /// Sets a variable of the specified type <typeparamref name="TEnum"/> to a specified value and returns the original value, as an atomic operation.
    /// </summary>
    /// <typeparam name="TEnum">The type to be used for <paramref name="location1"/> and <paramref name="value"/>. 
    /// This type must be an enum type whose underlying size is 32 or 64 bits.</typeparam>
    /// <param name="location1">The variable to set to the specified value. This is a reference parameter (<see langword="ref"/> in C#, <c>ByRef</c> in Visual Basic).</param>
    /// <param name="value">The value to which the <paramref name="location1"/> parameter is set.</param>
    /// <returns>The original value of <paramref name="location1"/>.</returns>
    /// <exception cref="NullReferenceException">The address of <paramref name="location1"/> is a null pointer.</exception>
    /// <exception cref="NotSupportedException">An unsupported <typeparamref name="TEnum"/> is specified.</exception>
    public static TEnum Exchange<TEnum>(ref TEnum location1, TEnum value) where TEnum : unmanaged, Enum
    {
        int size = Unsafe.SizeOf<TEnum>();
        if (size == sizeof(uint))
        {
            // we know that the size of the enum matches uint, so we can safely static_cast the ByRef and all values to uint
            // and CAS-in the uint value.
            uint original = Interlocked.Exchange(ref Unsafe.As<TEnum, uint>(ref location1), Unsafe.BitCast<TEnum, uint>(value));
            // static_cast back to the original enum type
            return Unsafe.BitCast<uint, TEnum>(original);
        }
        if (size == sizeof(ulong))
        {
            ulong original = Interlocked.Exchange(ref Unsafe.As<TEnum, ulong>(ref location1), Unsafe.BitCast<TEnum, ulong>(value));
            return Unsafe.BitCast<ulong, TEnum>(original);
        }
        ThrowUnsupportedEnumSize<TEnum>();
        return default;
    }

    /// <summary>
    /// Compares two <typeparamref name="TEnum"/> values for equality and, if they are equal, replaces the first value, as an atomic operation.
    /// </summary>
    /// <typeparam name="TEnum">The type to be used for <paramref name="location1"/>, <paramref name="value"/>, and <paramref name="comparand"/>. 
    /// This type must be an enum type whose underlying size is 32 or 64 bits.</typeparam>
    /// <param name="location1">The destination, whose value is compared with <paramref name="comparand"/> and possibly replaced.</param>
    /// <param name="value">The value that replaces the destination value if the comparison results in equality.</param>
    /// <param name="comparand">The value that is compared to the value at <paramref name="location1"/>.</param>
    /// <returns>The original value in <paramref name="location1"/>.</returns>
    /// <exception cref="NullReferenceException">The address of <paramref name="location1"/> is a null pointer.</exception>
    /// <exception cref="NotSupportedException">An unsupported <typeparamref name="TEnum"/> is specified.</exception>
    public static TEnum CompareExchange<TEnum>(ref TEnum location1, TEnum value, TEnum comparand) where TEnum : unmanaged, Enum
    {
        int size = Unsafe.SizeOf<TEnum>();
        if (size == sizeof(uint))
        {
            // we know that the size of the enum matches uint, so we can safely static_cast the ByRef and all values to uint
            // and CAS-in the uint value.
            uint original = Interlocked.CompareExchange(ref Unsafe.As<TEnum, uint>(ref location1), Unsafe.BitCast<TEnum, uint>(value), Unsafe.BitCast<TEnum, uint>(comparand));
            // static_cast back to the original enum type
            return Unsafe.BitCast<uint, TEnum>(original);
        }
        if (size == sizeof(ulong))
        {
            ulong original = Interlocked.CompareExchange(ref Unsafe.As<TEnum, ulong>(ref location1), Unsafe.BitCast<TEnum, ulong>(value), Unsafe.BitCast<TEnum, ulong>(comparand));
            return Unsafe.BitCast<ulong, TEnum>(original);
        }
        ThrowUnsupportedEnumSize<TEnum>();
        return default;
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedEnumSize<TEnum>() => 
        throw new NotSupportedException($"Unable to safely perform CAS operation on enum type {typeof(TEnum)}: enum size too small. Must be 32 or 64 bit");

    #endregion Interlocked

    #region Read

    /// <inheritdoc cref="Volatile.Read(ref readonly bool)"/>
    public static bool VolatileRead(ref ConcurrentBoolean location) =>
        Volatile.Read(ref Unsafe.As<ConcurrentBoolean, uint>(ref location)) != ConcurrentBoolean.FALSE;

    #endregion Read

    #region IncrementModulo

    /// <summary>
    /// Atomically increments the value stored in the specified location and wraps it around to zero if it exceeds the specified modulo value.
    /// </summary>
    /// <param name="location">A reference to the integer value to increment.</param>
    /// <param name="modulo">The modulo value. If the incremented value exceeds this modulo, it wraps around to zero.</param>
    /// <returns>The original value stored in <paramref name="location"/> before incrementing.</returns>
    public static int IncrementModulo(ref int location, int modulo)
    {
        int original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = (original + 1) % modulo;
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    /// <inheritdoc cref="IncrementModulo(ref int, int)"/>"
    public static uint IncrementModulo(ref uint location, uint modulo)
    {
        uint original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = (original + 1) % modulo;
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    /// <inheritdoc cref="IncrementModulo(ref int, int)"/>"
    public static long IncrementModulo(ref long location, long modulo)
    {
        long original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = (original + 1) % modulo;
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    /// <inheritdoc cref="IncrementModulo(ref int, int)"/>"
    public static ulong IncrementModulo(ref ulong location, ulong modulo)
    {
        ulong original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = (original + 1) % modulo;
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    #endregion

    #region WriteMax

    /// <summary>
    /// Atomically writes the maximum of the value stored in the specified location and the specified value.
    /// </summary>
    /// <remarks>
    /// The input values must satisfy the following precondition:
    /// <c>int.MinValue &lt;= location - value - 1 &lt;= int.MaxValue</c>
    /// </remarks>
    /// <param name="location">A reference to the integer value to write to.</param>
    /// <param name="value">The value to write if it is greater than the value stored in <paramref name="location"/>.</param>
    /// <returns>The original value stored in <paramref name="location"/> before writing.</returns>
    public static int WriteMaxFast(ref int location, int value)
    {
        int original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = FastMath.Max(original, value);
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    /// <summary>
    /// Atomically writes the maximum of the value stored in the specified location and the specified value.
    /// </summary>
    /// <param name="location">A reference to the integer value to write to.</param>
    /// <param name="value">The value to write if it is greater than the value stored in <paramref name="location"/>.</param>
    /// <returns>The original value stored in <paramref name="location"/> before writing.</returns>
    public static int WriteMax(ref int location, int value)
    {
        int original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = Math.Max(original, value);
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    /// <inheritdoc cref="WriteMax(ref int, int)"/>"
    public static uint WriteMax(ref uint location, uint value)
    {
        uint original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = Math.Max(original, value);
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    /// <inheritdoc cref="WriteMax(ref int, int)"/>"
    public static long WriteMax(ref long location, long value)
    {
        long original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = Math.Max(original, value);
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    /// <inheritdoc cref="WriteMax(ref int, int)"/>"
    public static ulong WriteMax(ref ulong location, ulong value)
    {
        ulong original, newValue;
        do
        {
            original = Volatile.Read(ref location);
            newValue = Math.Max(original, value);
        }
        while (Interlocked.CompareExchange(ref location, newValue, original) != original);
        return original;
    }

    #endregion WriteMax

    #region IncrementClampMax

    /// <summary>
    /// Atomically increments the value stored in the specified location, but only if the incremented value is less than or equal to the specified maximum value.
    /// </summary>
    /// <remarks>
    /// The input values must satisfy the following precondition:
    /// <c>int.MinValue &lt;= location - maxValue + 1 &lt;= int.MaxValue</c>
    /// </remarks>
    /// <param name="location">A reference to the integer value to increment.</param>
    /// <param name="maxValue">The maximum value. If the incremented value would exceed this value, it is clamped to this value.</param>
    /// <returns>The original value stored in <paramref name="location"/> before incrementing.</returns>
    public static int IncrementClampMaxFast(ref int location, int maxValue)
    {
        int original, incremented;
        do
        {
            original = Volatile.Read(ref location);
            // believe it or not, this branchless version alone eliminates two branches and three jump labels
            // in the optimized x64 JIT assembly :0
            incremented = FastMath.Min(original + 1, maxValue);
        }
        while (Interlocked.CompareExchange(ref location, incremented, original) != original);
        return original;
    }

    /// <summary>
    /// Atomically increments the value stored in the specified location, but only if the incremented value is less than or equal to the specified maximum value.
    /// </summary>
    /// <param name="location">A reference to the integer value to increment.</param>
    /// <param name="maxValue">The maximum value. If the incremented value would exceed this value, it is clamped to this value.</param>
    /// <returns>The original value stored in <paramref name="location"/> before incrementing.</returns>
    public static int IncrementClampMax(ref int location, int maxValue)
    {
        int original, incremented;
        do
        {
            original = Volatile.Read(ref location);
            incremented = Math.Min(original + 1, maxValue);
        }
        while (Interlocked.CompareExchange(ref location, incremented, original) != original);
        return original;
    }

    /// <inheritdoc cref="IncrementClampMax(ref int, int)"/>"
    public static uint IncrementClampMax(ref uint location, uint maxValue)
    {
        uint original, incremented;
        do
        {
            original = Volatile.Read(ref location);
            incremented = Math.Min(original + 1, maxValue);
        }
        while (Interlocked.CompareExchange(ref location, incremented, original) != original);
        return original;
    }

    /// <inheritdoc cref="IncrementClampMax(ref int, int)"/>"
    public static long IncrementClampMax(ref long location, long maxValue)
    {
        long original, incremented;
        do
        {
            original = Volatile.Read(ref location);
            incremented = Math.Min(original + 1, maxValue);
        }
        while (Interlocked.CompareExchange(ref location, incremented, original) != original);
        return original;
    }

    /// <inheritdoc cref="IncrementClampMax(ref int, int)"/>"
    public static ulong IncrementClampMax(ref ulong location, ulong maxValue)
    {
        ulong original, incremented;
        do
        {
            original = Volatile.Read(ref location);
            incremented = Math.Min(original + 1, maxValue);
        }
        while (Interlocked.CompareExchange(ref location, incremented, original) != original);
        return original;
    }

    #endregion IncrementClampMax

    #region DecrementClampMin

    /// <summary>
    /// Atomically decrements the value stored in the specified location, but only if the decremented value is greater than or equal to the specified minimum value.
    /// </summary>
    /// <remarks>
    /// The input values must satisfy the following precondition:
    /// <c>int.MinValue &lt;= location - minValue - 1 &lt;= int.MaxValue</c>
    /// </remarks>
    /// <param name="location">A reference to the integer value to decrement.</param>
    /// <param name="minValue">The minimum value. If the decremented value would be less than this value, it is clamped to this value.</param>
    /// <returns>The original value stored in <paramref name="location"/> before decrementing.</returns>
    public static int DecrementClampMinFast(ref int location, int minValue)
    {
        int original, decremented;
        do
        {
            original = Volatile.Read(ref location);
            decremented = FastMath.Max(original - 1, minValue);
        }
        while (Interlocked.CompareExchange(ref location, decremented, original) != original);
        return original;
    }

    /// <summary>
    /// Atomically decrements the value stored in the specified location, but only if the decremented value is greater than or equal to the specified minimum value.
    /// </summary>
    /// <param name="location">A reference to the integer value to decrement.</param>
    /// <param name="minValue">The minimum value. If the decremented value would be less than this value, it is clamped to this value.</param>
    /// <returns>The original value stored in <paramref name="location"/> before decrementing.</returns>
    public static int DecrementClampMin(ref int location, int minValue)
    {
        int original, decremented;
        do
        {
            original = Volatile.Read(ref location);
            decremented = Math.Max(original - 1, minValue);
        }
        while (Interlocked.CompareExchange(ref location, decremented, original) != original);
        return original;
    }

    /// <inheritdoc cref="DecrementClampMin(ref int, int)"/>"
    public static uint DecrementClampMin(ref uint location, uint minValue)
    {
        uint original, decremented;
        do
        {
            original = Volatile.Read(ref location);
            decremented = Math.Max(original - 1, minValue);
        }
        while (Interlocked.CompareExchange(ref location, decremented, original) != original);
        return original;
    }

    /// <inheritdoc cref="DecrementClampMin(ref int, int)"/>"
    public static long DecrementClampMin(ref long location, long minValue)
    {
        long original, decremented;
        do
        {
            original = Volatile.Read(ref location);
            decremented = Math.Max(original - 1, minValue);
        }
        while (Interlocked.CompareExchange(ref location, decremented, original) != original);
        return original;
    }

    /// <inheritdoc cref="DecrementClampMin(ref int, int)"/>"
    public static ulong DecrementClampMin(ref ulong location, ulong minValue)
    {
        ulong original, decremented;
        do
        {
            original = Volatile.Read(ref location);
            decremented = Math.Max(original - 1, minValue);
        }
        while (Interlocked.CompareExchange(ref location, decremented, original) != original);
        return original;
    }

    #endregion DecrementClampMin

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
