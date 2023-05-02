using System.Text;

namespace Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

/// <summary>
/// Represents a snapshot of all allocations currently tracked by the corresponding <see cref="IMemoryManager"/>.
/// </summary>
public record AllocationSnapshot
{
    /// <summary>
    /// The allocations currently tracked by the corresponding <see cref="IMemoryManager"/>.
    /// </summary>
    public Allocation[] Allocations { get; }

    /// <summary>
    /// The total number of bytes currently allocated in unmanaged memory.
    /// </summary>
    public ulong TotalByteCount { get; }

    /// <summary>
    /// Creates a new <see cref="AllocationSnapshot"/> from the given allocations.
    /// </summary>
    /// <param name="allocations">The allocations to create the snapshot from.</param>
    public AllocationSnapshot(Allocation[] allocations)
    {
        Allocations = allocations;
        TotalByteCount = allocations.Aggregate(0ul, (sum, allocation) => sum + allocation.Size, sum => sum);
    }

    /// <summary>
    /// Returns a string representation of the snapshot.
    /// </summary>
    public override string ToString()
    {
        StringBuilder builder = new($"{TotalByteCount} bytes currently allocated.{Environment.NewLine}Allocations:{Environment.NewLine}");
        for (int i = 0; i < Allocations.Length; i++)
        {
            ref Allocation allocation = ref Allocations[i];
            builder
                .Append("  ")
                .Append(allocation.ToString())
                .Append(Environment.NewLine);
        }
        return builder.ToString();
    }
}