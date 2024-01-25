using System.Text;
using Wkg.Text;

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
        // rent a string builder big enough to hold the entire snapshot
        StringBuilder builder = StringBuilderPool.Shared.Rent(4096);

        builder.Append(TotalByteCount).AppendLine($" bytes currently allocated.")
            .AppendLine("Allocations:");
        for (int i = 0; i < Allocations.Length; i++)
        {
            ref Allocation allocation = ref Allocations[i];
            builder
                .Append("  ")
                .AppendLine(allocation.ToString());
        }
        string result = builder.ToString();
        StringBuilderPool.Shared.Return(builder);
        return result;
    }
}