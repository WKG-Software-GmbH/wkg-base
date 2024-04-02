using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

/// <summary>
/// Represents an unmanaged memory allocation.
/// </summary>
/// <param name="Pointer">The address of the allocated memory.</param>
/// <param name="Size">The size in bytes of the allocated memory.</param>
/// <param name="Trace">The stack trace of where the allocation originated from.</param>
[RequiresUnreferencedCode("Requires reflective access to calling methods.")]
public record Allocation(IntPtr Pointer, ulong Size, StackTrace Trace)
{
    private StackFrame CallSite { get; } = Trace.GetFrame(0)!;

    /// <summary>
    /// Returns a string representation of the allocation.
    /// </summary>
    public override string ToString() => $"0x{Pointer:x}: {Size} bytes requested by {CallSite.GetMethod()?.DeclaringType?.FullName}::{CallSite.GetMethod()} at IL offset {CallSite.GetILOffset()}. Stack trace:\n{Trace}";
}