using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Wkg.Common.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Span{T}"/>.
/// </summary>
public static class SpanExtensions
{
    // we need an easier way to check for blittable generic types :/
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBlittable<T>() =>
        typeof(T) == typeof(byte)
        || typeof(T) == typeof(sbyte)
        || typeof(T) == typeof(short)
        || typeof(T) == typeof(ushort)
        || typeof(T) == typeof(int)
        || typeof(T) == typeof(uint)
        || typeof(T) == typeof(long)
        || typeof(T) == typeof(ulong)
        || typeof(T) == typeof(float)
        || typeof(T) == typeof(double)
        || typeof(T) == typeof(char)
        || typeof(T) == typeof(bool)
        || typeof(T) == typeof(nint)
        || typeof(T) == typeof(nuint);

    /// <summary>
    /// Counts the number of leading elements in the <see cref="ReadOnlySpan{T}"/> that are equal to the specified value.
    /// </summary>
    /// <typeparam name="T">The type of elements in the <see cref="ReadOnlySpan{T}"/>.</typeparam>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> to count the leading elements in.</param>
    /// <param name="value">The value to compare the elements to.</param>
    /// <returns>The number of leading elements in the <see cref="ReadOnlySpan{T}"/> that are equal to the specified value.</returns>
    public static int CountLeading<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
    {
        if (IsBlittable<T>())
        {
            int size = Unsafe.SizeOf<T>();
            if (size == sizeof(byte))
            {
                return CountLeadingValueType(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.BitCast<T, byte>(value),
                    span.Length);
            }
            else if (size == sizeof(short))
            {
                return CountLeadingValueType(
                    ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.BitCast<T, short>(value),
                    span.Length);
            }
            else if (size == sizeof(int))
            {
                return CountLeadingValueType(
                    ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.BitCast<T, int>(value),
                    span.Length);
            }
            else if (size == sizeof(long))
            {
                return CountLeadingValueType(
                    ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.BitCast<T, long>(value),
                    span.Length);
            }
        }
        return CountLeading(ref MemoryMarshal.GetReference(span), value, span.Length);
    }

    private static int CountLeading<T>(ref T current, T value, int length) where T : IEquatable<T>?
    {
        int count = 0;

        ref T end = ref Unsafe.Add(ref current, length);
        if (value is not null)
        {
            while (Unsafe.IsAddressLessThan(ref current, ref end) && value.Equals(current))
            {
                count++;
                current = ref Unsafe.Add(ref current, 1);
            }
        }
        else
        {
            while (Unsafe.IsAddressLessThan(ref current, ref end) && current is null)
            {
                count++;
                current = ref Unsafe.Add(ref current, 1);
            }
        }

        return count;
    }

    private static unsafe int CountLeadingValueType<T>(ref T current, T value, int length) where T : unmanaged, IEquatable<T>
    {
        int count = 0;
        ref T end = ref Unsafe.Add(ref current, length);
        if (Vector128.IsHardwareAccelerated && length >= Vector128<T>.Count)
        {
            if (Vector512.IsHardwareAccelerated && length >= Vector512<T>.Count)
            {
                Vector512<T> targetVector = Vector512.Create(value);
                ref T oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector512<T>.Count);
                while (Unsafe.IsAddressLessThan(ref current, ref oneVectorAwayFromEnd))
                {
                    int currentCount = BitOperations.TrailingZeroCount(~Vector512.Equals(Vector512.LoadUnsafe(ref current), targetVector).ExtractMostSignificantBits());
                    count += currentCount;
                    if (currentCount != Vector512<T>.Count)
                    {
                        return count;
                    }
                    current = ref Unsafe.Add(ref current, Vector512<T>.Count);
                }
                // Count the last vector and mask off the elements that were already counted (number of elements between oneVectorAwayFromEnd and current).
                ulong mask = Vector512.Equals(Vector512.LoadUnsafe(ref oneVectorAwayFromEnd), targetVector).ExtractMostSignificantBits();
                mask >>= (int)((nuint)Unsafe.ByteOffset(ref oneVectorAwayFromEnd, ref current) / (uint)sizeof(T));
                count += BitOperations.TrailingZeroCount(~mask);
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<T>.Count)
            {
                Vector256<T> targetVector = Vector256.Create(value);
                ref T oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector256<T>.Count);
                while (Unsafe.IsAddressLessThan(ref current, ref oneVectorAwayFromEnd))
                {
                    int currentCount = BitOperations.TrailingZeroCount(~Vector256.Equals(Vector256.LoadUnsafe(ref current), targetVector).ExtractMostSignificantBits());
                    count += currentCount;
                    if (currentCount != Vector256<T>.Count)
                    {
                        return count;
                    }
                    current = ref Unsafe.Add(ref current, Vector256<T>.Count);
                }
                // Count the last vector and mask off the elements that were already counted (number of elements between oneVectorAwayFromEnd and current).
                uint mask = Vector256.Equals(Vector256.LoadUnsafe(ref oneVectorAwayFromEnd), targetVector).ExtractMostSignificantBits();
                mask >>= (int)((nuint)Unsafe.ByteOffset(ref oneVectorAwayFromEnd, ref current) / (uint)sizeof(T));
                count += BitOperations.TrailingZeroCount(~mask);
            }
            else
            {
                Vector128<T> targetVector = Vector128.Create(value);
                ref T oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector128<T>.Count);
                while (Unsafe.IsAddressLessThan(ref current, ref oneVectorAwayFromEnd))
                {
                    int currentCount = BitOperations.TrailingZeroCount(~Vector128.Equals(Vector128.LoadUnsafe(ref current), targetVector).ExtractMostSignificantBits());
                    count += currentCount;
                    if (currentCount != Vector128<T>.Count)
                    {
                        return count;
                    }
                    current = ref Unsafe.Add(ref current, Vector128<T>.Count);
                }
                // Count the last vector and mask off the elements that were already counted (number of elements between oneVectorAwayFromEnd and current).
                uint mask = Vector128.Equals(Vector128.LoadUnsafe(ref oneVectorAwayFromEnd), targetVector).ExtractMostSignificantBits();
                mask >>= (int)((nuint)Unsafe.ByteOffset(ref oneVectorAwayFromEnd, ref current) / (uint)sizeof(T));
                count += BitOperations.TrailingZeroCount(~mask);
            }
        }
        else
        {
            while (Unsafe.IsAddressLessThan(ref current, ref end) && current.Equals(value))
            {
                count++;
                current = ref Unsafe.Add(ref current, 1);
            }
        }
        return count;
    }
}
