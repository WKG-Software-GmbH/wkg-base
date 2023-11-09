using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Wkg.Common.ThrowHelpers;
using Wkg.Threading;

namespace Wkg.Collections.Concurrent;

using static ConcurrentBoolean;

/// <summary>
/// A 64-bit bitmap that can be updated concurrently.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct ConcurrentBitMap64OLD
{
    [FieldOffset(0)]
    private readonly ulong _state;

    private ConcurrentBitMap64OLD(ulong state) => _state = state;

    /// <summary>
    /// Returns this <see cref="ConcurrentBitMap64OLD"/> as a <see cref="ulong"/>.
    /// </summary>
    /// <returns>The internal <see cref="ulong"/> value of this <see cref="ConcurrentBitMap64OLD"/> where the LSB corresponds to index 0.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong AsUInt64() => Volatile.Read(ref AsUlong(in _state));

    /// <summary>
    /// Determines whether the bit at the specified index is set (1).
    /// </summary>
    /// <param name="index">The index of the bit to check.</param>
    /// <returns><see langword="true"/> if the bit at the specified index is set (1), otherwise <see langword="false"/> (0).</returns>
    public bool IsBitSet(int index)
    {
        ulong s = Volatile.Read(ref AsUlong(in _state));
        ulong mask = 1uL << index;
        return (s & mask) == mask;
    }

    /// <summary>
    /// Determines whether all bits are set (1) based on the specified <paramref name="fullBitMap"/> bitmap where all bits are set (1) that should be checked.
    /// </summary>
    /// <param name="fullBitMap">A <see cref="ConcurrentBitMap64OLD"/> where all bits are set (1) that should be checked.</param>
    /// <returns><see langword="true"/> if all bits are set (1) based on the specified <paramref name="fullBitMap"/> bitmap where all bits are set (1) that should be checked, otherwise <see langword="false"/> (0).</returns>
    public bool IsFull(ConcurrentBitMap64OLD fullBitMap)
    {
        ulong s = Volatile.Read(ref AsUlong(in _state));
        return (s & fullBitMap._state) == fullBitMap._state;
    }

    /// <summary>
    /// Determines whether all bits in this bitmap up to the specified <paramref name="capacity"/> are set (1).
    /// </summary>
    /// <param name="capacity">The size of this bitmap in bits.</param>
    public bool IsFull(int capacity)
    {
        ulong mask = GetFullMask(capacity);
        ulong s = Volatile.Read(ref AsUlong(in _state));
        return (s & mask) == mask;
    }

    /// <summary>
    /// Determines whether all bits are clear (0).
    /// </summary>
    /// <returns><see langword="true"/> if all bits are clear (0), otherwise <see langword="false"/> if at least one bit is set (1).</returns>
    public bool IsEmpty
    {
        get
        {
            ulong s = Volatile.Read(ref AsUlong(in _state));
            return s == 0;
        }
    }
    
    /// <summary>
    /// Updates the bit at the specified index to the specified value.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitMap64OLD"/> to update.</param>
    /// <param name="index">The index of the bit to update.</param>
    /// <param name="isSet"><see langword="true"/> to set the bit at the specified index, <see langword="false"/> to clear the bit at the specified index.</param>
    public static void UpdateBit(ref ConcurrentBitMap64OLD state, int index, ConcurrentBoolean isSet)
    { 
        ref ulong target = ref AsUlong(ref state);
        ulong s = Volatile.Read(ref target);
        ulong newState = UpdateBitUnsafe(s, index, isSet);
        while (s != newState)
        {
            ulong oldState = Interlocked.CompareExchange(ref target, newState, s);
            if (oldState == s)
            {
                break;
            }
            s = oldState;
            newState = UpdateBitUnsafe(s, index, isSet);
        }
    }

    /// <summary>
    /// Creates a new <see cref="ConcurrentBitMap64OLD"/> with all bits set (1) up to the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The size of the <see cref="ConcurrentBitMap64OLD"/> in bits.</param>
    /// <returns>A new <see cref="ConcurrentBitMap64OLD"/> with all bits set (1) up to the specified <paramref name="capacity"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConcurrentBitMap64OLD Full(int capacity)
    {
        ulong mask = GetFullMask(capacity);
        return new ConcurrentBitMap64OLD(mask);
    }

    /// <summary>
    /// Creates a new <see cref="ConcurrentBitMap64OLD"/> with all bits clear (0).
    /// </summary>
    public static ConcurrentBitMap64OLD Empty => default;

    /// <summary>
    /// Clears all bits in the specified <paramref name="state"/>.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitMap64OLD"/> to clear.</param>
    public static void ClearAll(ref ConcurrentBitMap64OLD state) => Volatile.Write(ref AsUlong(ref state), 0);

    /// <summary>
    /// Sets all bits in the specified <paramref name="state"/> up to the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitMap64OLD"/> to set.</param>
    /// <param name="capacity">The size of the <paramref name="state"/> in bits.</param>
    public static void SetAll(ref ConcurrentBitMap64OLD state, int capacity) => VolatileWrite(ref state, Full(capacity));

    /// <summary>
    /// Inserts a new bit at the specified <paramref name="index"/> in the specified <paramref name="state"/>. 
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitMap64OLD"/> to update.</param>
    /// <param name="index">The index at which to insert the new bit.</param>
    /// <param name="isInitiallySet"><see langword="true"/> to set the bit at the specified index, <see langword="false"/> to clear the bit at the specified index.</param>
    public static void InsertBitAt(ref ConcurrentBitMap64OLD state, int index, ConcurrentBoolean isInitiallySet)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, 63, nameof(index));
        ref ulong target = ref AsUlong(ref state);
        ulong oldState, newState;
        do
        {
            oldState = Volatile.Read(ref target);
            ulong splitMask = (1uL << index) - 1;
            ulong lower = oldState & splitMask;
            ulong upper = oldState & ~splitMask;
            ulong expandedState = upper << 1 | lower;
            // we can expand the boolean mask to 64 for true => ulong.MaxValue and false => 0
            newState = expandedState | isInitiallySet.As64BitMask() & 1uL << index;

        } while (Interlocked.CompareExchange(ref target, newState, oldState) != oldState);
    }

    /// <summary>
    /// Removes the bit at the specified <paramref name="index"/> in the specified <paramref name="state"/>.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitMap64OLD"/> to update.</param>
    /// <param name="index">The index of the bit to remove.</param>
    public static void RemoveBitAt(ref ConcurrentBitMap64OLD state, int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, 63, nameof(index));
        ref ulong target = ref AsUlong(ref state);
        ulong oldState, newState;
        do
        {
            oldState = Volatile.Read(ref target);
            // clear the bit at the index we want to remove to prevent it from conflicting with the bit at index - 1
            ulong withUnsetBit = UpdateBitUnsafe(oldState, index, isSet: FALSE);
            ulong splitMask = (1uL << index) - 1;
            ulong lower = withUnsetBit & splitMask;
            ulong upper = withUnsetBit & ~splitMask;
            newState = upper >> 1 | lower;
        } while (Interlocked.CompareExchange(ref target, newState, oldState) != oldState);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        // pre .NET 8 can't do ulong.ToString("B64") :(
        ulong s = Volatile.Read(ref AsUlong(in _state));
        Span<byte> ascii = stackalloc byte[64];
        for (int i = 0; i < 64; i++)
        {
            // we want the MSB to be on the left, so we need to reverse everything
            // other than that we simply grab the ith bit (from the LSB) 
            // and simply OR that to the ASCII character '0' (0x30).
            // if the bit was 0 the result is '0' itself, otherwise
            // if the bit was 1 then the result is '0' | 1 (0x30 | 1) which 
            // yields 0x31 which is also conveniently the ASCII code for '1'.
            ascii[63 - i] = (byte)((s & (1uL << i)) >> i | '0');
        }
        return Encoding.ASCII.GetString(ascii);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetFullMask(int capacity)
    {
        Throw.ArgumentOutOfRangeException.IfNegativeOrZero(capacity, nameof(capacity));
        Throw.ArgumentOutOfRangeException.IfGreaterThan(capacity, 64, nameof(capacity));
        // we shift first by capacity - 1 and then by 1 again to allow for 64 bit to 0
        // otherwise, the right value in the shift is taken modulo 64, so we'd get 1 for capacity = 64
        return (1uL << (capacity - 1) << 1) - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong UpdateBitUnsafe(ulong state, int index, ConcurrentBoolean isSet)
    {
        // ulong.isSetMask = 0xFFFFFFFFFFFFFFFF if isSet == TRUE, else 0
        ulong isSetMask = isSet.As64BitMask();
        // a flag with a single bit set at the index we want to update
        ulong flag = 1uL << index;
        // if isSet == TRUE, then the first or is applied (s | flag), if isSet == FALSE, then (s | 0) = s
        // if isSet == FALSE, then the part after the and is applied (s & ~flag) (clear the bit at the index), if isSet == TRUE, then (s & 0xFFFFFFFFFFFFFFFF) = s
        return (state | flag & isSetMask) & (~flag | isSetMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref ulong AsUlong(ref ConcurrentBitMap64OLD state) =>
        ref Unsafe.As<ConcurrentBitMap64OLD, ulong>(ref state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref ulong AsUlong(scoped in ulong state) =>
        ref Unsafe.AsRef(in state);

    /// <summary>
    /// Atomically writes the specified <paramref name="value"/> to the specified <paramref name="state"/>. 
    /// On systems that require it, inserts a memory barrier that prevents the processor from reordering memory
    /// operations as follows: If a read or write appears before this method in the code, the processor cannot move it after this method.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitMap64OLD"/> to update.</param>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VolatileWrite(ref ConcurrentBitMap64OLD state, ConcurrentBitMap64OLD value) =>
        Volatile.Write(ref AsUlong(ref state), value._state);

    /// <summary>
    /// Atomically reads the value of the specified field. On systems that require it, inserts a memory barrier that prevents the processor from reordering memory
    /// operations as follows: If a read or write appears after this method in the code, the processor cannot move it before this method.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitMap64OLD"/> to read.</param>
    /// <returns>The value that was read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConcurrentBitMap64OLD VolatileRead(ref ConcurrentBitMap64OLD state) =>
        new(Volatile.Read(ref AsUlong(ref state)));
}