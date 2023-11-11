using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Wkg.Common.ThrowHelpers;
using Wkg.Threading;

namespace Wkg.Collections.Concurrent;

using static ConcurrentBoolean;

/// <summary>
/// Represents the state of a <see cref="ConcurrentBitmap56"/> that can be updated atomically.
/// </summary>
/// <remarks>
/// This state is not intended to be used directly. 
/// It only serves as atomic storage for <see cref="ConcurrentBitmap56"/>.
/// To access the state of a <see cref="ConcurrentBitmap56"/>, use the static methods provided by <see cref="ConcurrentBitmap56"/>.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct ConcurrentBitmap56State;

/// <summary>
/// A 56-bit bitmap that can be updated atomically.
/// </summary>
/// <remarks>
/// This type allows for atomic updates of a 56-bit bitmap and is guaranteed to be thread-safe for up to 256 concurrent threads.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public ref struct ConcurrentBitmap56
{
    [FieldOffset(0)]
    private ulong _state;
    [FieldOffset(7)]
    private byte _guardToken;

    internal ConcurrentBitmap56(ulong state) => _state = state;

    /// <summary>
    /// Returns the data region of this <see cref="ConcurrentBitmap56"/> as a <see cref="ulong"/>.
    /// </summary>
    /// <returns>The internal <see cref="ulong"/> value of this <see cref="ConcurrentBitmap56"/> where the LSB corresponds to index 0.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ulong GetRawData() => _state & GetFullMaskUnsafe(56);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly ulong GetFullState() => _state;

    /// <summary>
    /// Determines whether the bit at the specified index is set (1).
    /// </summary>
    /// <param name="index">The index of the bit to check.</param>
    /// <returns><see langword="true"/> if the bit at the specified index is set (1), otherwise <see langword="false"/> (0).</returns>
    public readonly bool IsBitSet(int index)
    {
        ulong mask = 1uL << index;
        return (_state & mask) == mask;
    }

    /// <summary>
    /// Retrieves a token that can be used to check if this <see cref="ConcurrentBitmap56"/> has been updated.
    /// </summary>
    /// <returns>A token that can be used to check if this <see cref="ConcurrentBitmap56"/> has been updated.</returns>
    public readonly byte GetToken() => _guardToken;

    /// <summary>
    /// Determines whether all bits in this bitmap up to the specified <paramref name="capacity"/> are set (1).
    /// </summary>
    /// <param name="capacity">The size of this bitmap in bits.</param>
    public readonly bool IsFull(int capacity)
    {
        ulong mask = GetFullMask(capacity);
        return (_state & mask) == mask;
    }

    internal readonly bool IsFullUnsafe(int capacity)
    {
        ulong mask = GetFullMaskUnsafe(capacity);
        return (_state & mask) == mask;
    }

    /// <summary>
    /// Determines whether all bits are clear (0).
    /// </summary>
    /// <returns><see langword="true"/> if all bits are clear (0), otherwise <see langword="false"/> if at least one bit is set (1).</returns>
    public readonly bool IsEmpty() => (_state & GetFullMaskUnsafe(56)) == 0;

    /// <summary>
    /// Counts the number of bits that are set (1) in this bitmap.
    /// </summary>
    /// <param name="capacity">The size of this bitmap in bits.</param>
    /// <returns>The number of bits that are set (1) in this bitmap.</returns>
    public readonly int PopCount(int capacity)
    {
        ulong mask = GetFullMask(capacity);
        return BitOperations.PopCount(_state & mask);
    }

    internal readonly int PopCountUnsafe(int capacity)
    {
        ulong mask = GetFullMaskUnsafe(capacity);
        return BitOperations.PopCount(_state & mask);
    }

    /// <summary>
    /// Determines whether all bits up to the specified <paramref name="capacity"/> are clear (0).
    /// </summary>
    /// <returns><see langword="true"/> if all bits up to the specified <paramref name="capacity"/> are clear (0), otherwise <see langword="false"/> if at least one bit is set (1).</returns>
    public readonly bool IsEmpty(int capacity) => (_state & GetFullMask(capacity)) == 0;

    internal readonly bool IsEmptyUnsafe(int capacity) => (_state & GetFullMaskUnsafe(capacity)) == 0;

    /// <summary>
    /// Updates the bit at the specified index to the specified value.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    /// <param name="index">The index of the bit to update.</param>
    /// <param name="isSet"><see langword="true"/> to set the bit at the specified index, <see langword="false"/> to clear the bit at the specified index.</param>
    /// <returns>The updated <see cref="ConcurrentBitmap56"/> that was written to the specified <paramref name="state"/>.</returns>
    public static ConcurrentBitmap56 UpdateBit(ref ConcurrentBitmap56State state, int index, ConcurrentBoolean isSet)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, 55, nameof(index));
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        ConcurrentBitmap56 map;
        do
        {
            oldState = Volatile.Read(ref target);
            map = new ConcurrentBitmap56(oldState);
            byte token = map._guardToken;
            map._state = UpdateBitUnsafe(oldState, index, isSet);
            // write the new guard token to prevent ABA issues
            map._guardToken = (byte)(token + 1);
            // the guard token is included in the state, so we can simply write it back
            newState = map._state;
        } while (Interlocked.CompareExchange(ref target, newState, oldState) != oldState);
        return map;
    }

    /// <summary>
    /// Attempts to update the bit at the specified index to the specified value if the specified <paramref name="token"/> is still valid.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    /// <param name="token">A token previously retrieved from <see cref="GetToken"/>.</param>
    /// <param name="index">The index of the bit to update.</param>
    /// <param name="isSet"><see langword="true"/> to set the bit at the specified index to 1, <see langword="false"/> to clear the bit at the specified index to 0.</param>
    /// <returns><see langword="true"/> if the bit was updated, otherwise <see langword="false"/> if the specified <paramref name="token"/> was invalid.</returns>
    public static bool TryUpdateBit(ref ConcurrentBitmap56State state, byte token, int index, ConcurrentBoolean isSet)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, 55, nameof(index));
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        oldState = Volatile.Read(ref target);
        ConcurrentBitmap56 map = new(oldState);
        if (map._guardToken != token)
        {
            return false;
        }
        map._state = UpdateBitUnsafe(oldState, index, isSet);
        // write the new guard token to prevent ABA issues
        map._guardToken = (byte)(token + 1);
        // the guard token is included in the state, so we can simply write it back
        newState = map._state;
        return Interlocked.CompareExchange(ref target, newState, oldState) == oldState;
    }

    /// <summary>
    /// Attempts to write the specified <paramref name="value"/> to the specified <paramref name="state"/> if the token of the specified <paramref name="value"/> is still valid.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    /// <param name="value">The <see cref="ConcurrentBitmap56"/> to write to the specified <paramref name="state"/>.</param>
    /// <returns><see langword="true"/> if the <paramref name="value"/> was written, otherwise <see langword="false"/> if the token of the specified <paramref name="value"/> was invalid.</returns>
    public static bool TryWrite(ref ConcurrentBitmap56State state, ConcurrentBitmap56 value)
    {
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        oldState = Volatile.Read(ref target);
        ConcurrentBitmap56 map = new(oldState);
        if (map._guardToken != value._guardToken)
        {
            return false;
        }
        value._guardToken = (byte)(map._guardToken + 1);
        newState = value._state;
        return Interlocked.CompareExchange(ref target, newState, oldState) == oldState;
    }

    /// <summary>
    /// Creates a new <see cref="ConcurrentBitmap56"/> with all bits set (1) up to the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The size of the <see cref="ConcurrentBitmap56"/> in bits.</param>
    /// <returns>A new <see cref="ConcurrentBitmap56"/> with all bits set (1) up to the specified <paramref name="capacity"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConcurrentBitmap56 Full(int capacity)
    {
        ulong mask = GetFullMask(capacity);
        return new ConcurrentBitmap56(mask);
    }

    /// <summary>
    /// Creates a new <see cref="ConcurrentBitmap56State"/> with all bits set (1) up to the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The size of the <see cref="ConcurrentBitmap56State"/> in bits.</param>
    /// <returns>A new <see cref="ConcurrentBitmap56State"/> with all bits set (1) up to the specified <paramref name="capacity"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ConcurrentBitmap56State FullState(int capacity)
    {
        ulong mask = GetFullMask(capacity);
        return *(ConcurrentBitmap56State*)&mask;
    }

    /// <summary>
    /// Creates a new <see cref="ConcurrentBitmap56"/> with all bits clear (0).
    /// </summary>
    public static ConcurrentBitmap56 Empty => default;

    /// <summary>
    /// Creates a new <see cref="ConcurrentBitmap56State"/> with all bits clear (0).
    /// </summary>
    public static ConcurrentBitmap56State EmptyState => default;

    /// <summary>
    /// Clears all bits in the specified <paramref name="state"/>.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    public static void ClearAll(ref ConcurrentBitmap56State state)
    {
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        do
        {
            oldState = Volatile.Read(ref target);
            ConcurrentBitmap56 map = new(oldState);
            byte token = map._guardToken;
            map._state = 0uL;
            // write the new guard token to prevent ABA issues
            map._guardToken = (byte)(token + 1);
            // the guard token is included in the state, so we can simply write it back
            newState = map._state;
        } while (Interlocked.CompareExchange(ref target, newState, oldState) != oldState);
    }

    /// <summary>
    /// Attempts to clear all bits in the specified <paramref name="state"/> if the specified <paramref name="token"/> is still valid.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    /// <param name="token">A token previously retrieved from <see cref="GetToken"/>.</param>
    /// <returns><see langword="true"/> if the <paramref name="state"/> was cleared, otherwise <see langword="false"/> if the specified <paramref name="token"/> was invalid.</returns>
    public static bool TryClearAll(ref ConcurrentBitmap56State state, byte token)
    {
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        oldState = Volatile.Read(ref target);
        ConcurrentBitmap56 map = new(oldState);
        if (map._guardToken != token)
        {
            return false;
        }
        map._state = 0uL;
        // write the new guard token to prevent ABA issues
        map._guardToken = (byte)(token + 1);
        // the guard token is included in the state, so we can simply write it back
        newState = map._state;
        return Interlocked.CompareExchange(ref target, newState, oldState) == oldState;
    }

    /// <summary>
    /// Sets all bits in the specified <paramref name="state"/> up to the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    /// <param name="capacity">The size of the <paramref name="state"/> in bits.</param>
    public static void SetAll(ref ConcurrentBitmap56State state, int capacity)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(capacity, 1, 56, nameof(capacity));
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        do
        {
            oldState = Volatile.Read(ref target);
            ConcurrentBitmap56 map = new(oldState);
            byte token = map._guardToken;
            map._state = GetFullMask(capacity);
            // write the new guard token to prevent ABA issues
            map._guardToken = (byte)(token + 1);
            // the guard token is included in the state, so we can simply write it back
            newState = map._state;
        } while (Interlocked.CompareExchange(ref target, newState, oldState) != oldState);
    }

    /// <summary>
    /// Attempts to set all bits in the specified <paramref name="state"/> up to the specified <paramref name="capacity"/> if the specified <paramref name="token"/> is still valid.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    /// <param name="token">A token previously retrieved from <see cref="GetToken"/>.</param>
    /// <param name="capacity">The size of the <paramref name="state"/> in bits.</param>
    /// <returns><see langword="true"/> if the <paramref name="state"/> was set, otherwise <see langword="false"/> if the specified <paramref name="token"/> was invalid.</returns>
    public static bool TrySetAll(ref ConcurrentBitmap56State state, byte token, int capacity)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(capacity, 1, 56, nameof(capacity));
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        oldState = Volatile.Read(ref target);
        ConcurrentBitmap56 map = new(oldState);
        if (map._guardToken != token)
        {
            return false;
        }
        map._state = GetFullMask(capacity);
        // write the new guard token to prevent ABA issues
        map._guardToken = (byte)(token + 1);
        // the guard token is included in the state, so we can simply write it back
        newState = map._state;
        return Interlocked.CompareExchange(ref target, newState, oldState) == oldState;
    }

    /// <summary>
    /// Inserts a new bit at the specified <paramref name="index"/> in the specified <paramref name="state"/>. 
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    /// <param name="index">The index at which to insert the new bit.</param>
    /// <param name="isInitiallySet"><see langword="true"/> to set the bit at the specified index, <see langword="false"/> to clear the bit at the specified index.</param>
    public static void InsertBitAt(ref ConcurrentBitmap56State state, int index, ConcurrentBoolean isInitiallySet)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, 55, nameof(index));
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        do
        {
            oldState = Volatile.Read(ref target);
            ConcurrentBitmap56 map = new(oldState);
            byte token = map._guardToken;
            ulong splitMask = (1uL << index) - 1;
            ulong lower = oldState & splitMask;
            // we don't want to shift the token
            ulong upper = oldState & ~splitMask & GetFullMaskUnsafe(56);
            ulong expandedState = upper << 1 | lower;
            // we can expand the boolean mask to 64 for true => ulong.MaxValue and false => 0
            map._state = expandedState | isInitiallySet.As64BitMask() & 1uL << index;
            // write the new guard token to prevent ABA issues
            map._guardToken = (byte)(token + 1);
            // the guard token is included in the state, so we can simply write it back
            newState = map._state;
        } while (Interlocked.CompareExchange(ref target, newState, oldState) != oldState);
    }

    /// <summary>
    /// Removes the bit at the specified <paramref name="index"/> in the specified <paramref name="state"/>.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/>.</param>
    /// <param name="index">The index of the bit to remove.</param>
    public static void RemoveBitAt(ref ConcurrentBitmap56State state, int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, 55, nameof(index));
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        do
        {
            oldState = Volatile.Read(ref target);
            ConcurrentBitmap56 map = new(oldState);
            byte token = map._guardToken;
            // clear the bit at the index we want to remove to prevent it from conflicting with the bit at index - 1
            ulong withUnsetBit = UpdateBitUnsafe(oldState, index, isSet: FALSE);
            ulong splitMask = (1uL << index) - 1;
            ulong lower = withUnsetBit & splitMask;
            ulong upper = withUnsetBit & ~splitMask & GetFullMaskUnsafe(56);
            map._state = upper >> 1 | lower;
            // write the new guard token to prevent ABA issues
            map._guardToken = (byte)(token + 1);
            // the guard token is included in the state, so we can simply write it back
            newState = map._state;
        } while (Interlocked.CompareExchange(ref target, newState, oldState) != oldState);
    }

    /// <inheritdoc/>
    public readonly override string ToString()
    {
        // pre .NET 8 can't do ulong.ToString("B64") :(
        ulong s = _state;
        Span<byte> ascii = stackalloc byte[56];
        for (int i = 0; i < 56; i++)
        {
            // we want the MSB to be on the left, so we need to reverse everything
            // other than that we simply grab the ith bit (from the LSB) 
            // and simply OR that to the ASCII character '0' (0x30).
            // if the bit was 0 the result is '0' itself, otherwise
            // if the bit was 1 then the result is '0' | 1 (0x30 | 1) which 
            // yields 0x31 which is also conveniently the ASCII code for '1'.
            ascii[55 - i] = (byte)((s & (1uL << i)) >> i | '0');
        }
        return $"(Token: {_guardToken}, Value: {Encoding.ASCII.GetString(ascii)})";
    }

    /// <summary>
    /// Converts the specified <see cref="ConcurrentBitmap56"/> to its underlying <see cref="ConcurrentBitmap56State"/> state.
    /// </summary>
    public static unsafe explicit operator ConcurrentBitmap56State(ConcurrentBitmap56 value) => 
        // we can do this because the struct size is the same
        // also: nothing to see here, move along. pretend this is a normal cast ;)
        *(ConcurrentBitmap56State*)&value;

    /// <summary>
    /// Converts the specified <see cref="ConcurrentBitmap56State"/> to a <see cref="ConcurrentBitmap56"/>.
    /// </summary>
    [Obsolete("Explicitly casting to ConcurrentBitmap56 is not safe in a multithreaded environment and may result in non-atomic reads and possibly data corruption. Use the static methods provided by ConcurrentBitmap56 instead.")]
    public static unsafe explicit operator ConcurrentBitmap56(ConcurrentBitmap56State value) =>
        // we can do this because the struct size is the same
        // also: nothing to see here, move along. pretend this is a normal cast ;)
        *(ConcurrentBitmap56*)&value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetFullMask(int capacity)
    {
        Throw.ArgumentOutOfRangeException.IfNegativeOrZero(capacity, nameof(capacity));
        Throw.ArgumentOutOfRangeException.IfGreaterThan(capacity, 56, nameof(capacity));
        return GetFullMaskUnsafe(capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetFullMaskUnsafe(int capacity) => (1uL << capacity) - 1;

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
    private static ref ulong AsUlongPointer(ref ConcurrentBitmap56State state) =>
        ref Unsafe.As<ConcurrentBitmap56State, ulong>(ref state);

    /// <summary>
    /// Atomically writes the specified <paramref name="value"/> to the specified <paramref name="state"/>. 
    /// On systems that require it, inserts a memory barrier that prevents the processor from reordering memory
    /// operations as follows: If a read or write appears before this method in the code, the processor cannot move it after this method.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="updateToken"><see langword="true"/> to update the token of the underlying <see cref="ConcurrentBitmap56"/> to prevent ABA issues, otherwise <see langword="false"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VolatileWrite(ref ConcurrentBitmap56State state, ConcurrentBitmap56 value, bool updateToken = false)
    {
        if (!updateToken)
        {
            Volatile.Write(ref AsUlongPointer(ref state), value._state);
            return;
        }
        ref ulong target = ref AsUlongPointer(ref state);
        ulong oldState, newState;
        do
        {
            oldState = Volatile.Read(ref target);
            ConcurrentBitmap56 map = new(oldState);
            byte token = map._guardToken;
            map._state = value._state;
            // write the new guard token to prevent ABA issues
            map._guardToken = (byte)(token + 1);
            // the guard token is included in the state, so we can simply write it back
            newState = map._state;
        } while (Interlocked.CompareExchange(ref target, newState, oldState) != oldState);
    }

    /// <summary>
    /// Atomically reads the value of the specified field. On systems that require it, inserts a memory barrier that prevents the processor from reordering memory
    /// operations as follows: If a read or write appears after this method in the code, the processor cannot move it before this method.
    /// </summary>
    /// <param name="state">A reference to the <see cref="ConcurrentBitmap56State"/> to read from.</param>
    /// <returns>The value that was read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConcurrentBitmap56 VolatileRead(ref ConcurrentBitmap56State state) =>
        new(Volatile.Read(ref AsUlongPointer(ref state)));
}