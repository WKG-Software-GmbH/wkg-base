using System.Runtime.InteropServices;

namespace Wkg.Threading;

[StructLayout(LayoutKind.Explicit, Size = sizeof(uint))]
public readonly struct ConcurrentBoolean
{
    [FieldOffset(0)]
    private readonly uint _value;

    public static implicit operator bool(ConcurrentBoolean value) => value._value != 0;

    public static unsafe implicit operator ConcurrentBoolean(bool value) => *(byte*)&value;

    public static implicit operator uint(ConcurrentBoolean value) =>
        ReinterpretCast<ConcurrentBoolean, uint>(value);

    public static implicit operator ConcurrentBoolean(uint value) =>
        ReinterpretCast<uint, ConcurrentBoolean>(value);

    public static ConcurrentBoolean FALSE => 0u;

    public static ConcurrentBoolean TRUE => ~FALSE;
}
