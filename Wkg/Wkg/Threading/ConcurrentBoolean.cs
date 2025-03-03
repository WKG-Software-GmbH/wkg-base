using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wkg.Threading;

/// <summary>
/// Represents a boolean value that can be used for CAS operations.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
[StructLayout(LayoutKind.Explicit, Size = sizeof(uint))]
public readonly struct ConcurrentBoolean
{
    [FieldOffset(0)]
    private readonly uint _value;

    /// <summary>
    /// Converts the specified <see cref="ConcurrentBoolean"/> to its equivalent <see cref="bool"/> value.
    /// </summary>
    public static implicit operator bool(ConcurrentBoolean value) => value._value != FALSE;

    /// <summary>
    /// Converts the specified <see cref="bool"/> value to its equivalent <see cref="ConcurrentBoolean"/>.
    /// </summary>
    public static implicit operator ConcurrentBoolean(bool value) => value ? TRUE : FALSE;

    /// <summary>
    /// Converts the specified <see cref="ConcurrentBoolean"/> to its equivalent <see cref="uint"/> value,
    /// where <see cref="FALSE"/> is <c>0</c> and <see cref="TRUE"/> is <c>0xffffffff</c> (<c>~FALSE</c>).
    /// </summary>
    public static implicit operator uint(ConcurrentBoolean value) =>
        Unsafe.BitCast<ConcurrentBoolean, uint>(value);

    /// <summary>
    /// Converts the specified <see cref="uint"/> value to its equivalent <see cref="ConcurrentBoolean"/>,
    /// where <c>0</c> is <see cref="FALSE"/> and any other value is <see cref="TRUE"/>.
    /// </summary>
    public static implicit operator ConcurrentBoolean(uint value) =>
        Unsafe.BitCast<uint, ConcurrentBoolean>((uint)(-value >> 63));

    /// <summary>
    /// Represents the <see cref="ConcurrentBoolean"/> value that is <see langword="false"/>.
    /// </summary>
    public static ConcurrentBoolean FALSE => 0u;

    /// <summary>
    /// Represents the <see cref="ConcurrentBoolean"/> value that is <see langword="true"/>.
    /// </summary>
    public static ConcurrentBoolean TRUE => ~FALSE;

    /// <summary>
    /// Returns the string representation of the <see cref="ConcurrentBoolean"/>.
    /// </summary>
    public override string ToString() => ((bool)this).ToString();

    /// <summary>
    /// Sign-extends this <see cref="ConcurrentBoolean"/> to a 64-bit signed integer, where <see cref="FALSE"/> is <c>0</c> 
    /// and <see cref="TRUE"/> is <c>0xffffffffffffffff</c> (<c>~FALSE</c>).
    /// </summary>
    public readonly ulong As64BitMask() => unchecked((ulong)(long)(int)_value);
}