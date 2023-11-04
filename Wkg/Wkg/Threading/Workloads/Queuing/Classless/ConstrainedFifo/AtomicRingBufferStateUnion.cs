using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

// the state is stored in a 64-bit value comprising a boolean flag for emptiness,
// and 16-bit head and tail pointers for ring buffer positions, supporting up to 65536 workloads.
// the boolean distinguishes between an empty or full queue when head and tail pointers are equal.
// the 64-bit state ensures atomicity and thread-safety across the entire structure.
[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
internal struct AtomicRingBufferStateUnion
{
    // this is a C-style union, so the fields overlap in memory.
    // we can access the entire state as a 64-bit value for atomic loads and stores,
    // or we can access the individual fields for convenience.
    // we could also simply pointer-cast the whole thing to a ulong, but apparently
    // people immediately start screaming when they see pointers in C# code, so we do
    // it like this instead.
    [FieldOffset(0)]
    public ulong __State;
    // tail is accessed most frequently, so we put it first (aligned to 64 bits)
    [FieldOffset(0)]
    public ushort Tail;
    // the boolean is accessed not that frequently, so we're fine with it being a bit misaligned
    // at 16 bits, so loads and stores will have to be done in two steps
    // we only use this struct in managed code, so sizeof(bool) is always 1
    [FieldOffset(2)]
    public bool IsEmpty;
    // head is also accessed frequently, so we align it to 32 bits
    [FieldOffset(4)]
    public ushort Head;
    // two bytes padding to align the whole thing to be loaded and stored atomically (64 bits)

    // we ideally don't want any constructor calls.
    // the JIT should be smart enough to just reinterpret_cast this whole thing from a ulong.
    // we can help it a bit by telling it that this should be inlined if possible.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AtomicRingBufferStateUnion(ulong state) => __State = state;
}