using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Wkg.Threading;

[DebuggerDisplay("{ToString(),nq}")]
[StructLayout(LayoutKind.Explicit, Size = sizeof(uint))]
public readonly struct ConcurrentBoolean
{
    [FieldOffset(0)]
    private readonly uint _value;

    public static implicit operator bool(ConcurrentBoolean value) => value._value != FALSE;

    public static unsafe implicit operator ConcurrentBoolean(bool value) => value ? TRUE : FALSE;

    public static implicit operator uint(ConcurrentBoolean value) =>
        ReinterpretCast<ConcurrentBoolean, uint>(value);

    public static implicit operator ConcurrentBoolean(uint value) =>
        ReinterpretCast<uint, ConcurrentBoolean>(value);

    public static ConcurrentBoolean FALSE => 0u;

    public static ConcurrentBoolean TRUE => ~FALSE;

    public override string ToString() => ((bool)this).ToString();
}
