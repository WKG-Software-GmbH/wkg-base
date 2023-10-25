using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Wkg.Unmanaged;

namespace Wkg.Threading.Workloads;

using static TypeReinterpreter;

/// <summary>
/// Represents the status of a workload.
/// </summary>
// we use a simple unsafe reinterpret_cast to convert between the uint and the enum
// which makes conversions a zero-cost operation
// so DO NOT change the field offset or the size of the struct!!!
// otherwise, there WILL be segmentation faults!
[StructLayout(LayoutKind.Explicit, Size = sizeof(uint))]
[DebuggerDisplay("{ToString()}")]
public readonly struct WorkloadStatus
{
    private const uint INVALID_VALUE = 0x00u;
    private const uint CREATED_VALUE = 0x01u;
    private const uint SCHEDULED_VALUE = 0x02u;
    private const uint RUNNING_VALUE = 0x04u;
    private const uint RAN_TO_COMPLETION_VALUE = 0x08u;
    private const uint FAULTED_VALUE = 0x10u;
    private const uint CANCELED_VALUE = 0x20u;
    private const uint CANCELLATION_REQUESTED_VALUE = 0x40u;

    // we use a simple unsafe reinterpret_cast to convert between the uint and the enum
    // which makes conversions a zero-cost operation
    // so DO NOT change the field offset or the size of the struct!!!
    // otherwise, there WILL be segmentation faults!
    [FieldOffset(0)]
    private readonly uint _value;

    /// <summary>
    /// The workload has not been initialized and is in an invalid state.
    /// </summary>
    public static WorkloadStatus Invalid => INVALID_VALUE;

    /// <summary>
    /// The workload has been created but not yet scheduled.
    /// </summary>
    public static WorkloadStatus Created => CREATED_VALUE;

    /// <summary>
    /// The workload has been scheduled for execution and is tracked by a scheduler.
    /// </summary>
    public static WorkloadStatus Scheduled => SCHEDULED_VALUE;

    /// <summary>
    /// The workload is currently executing.
    /// </summary>
    public static WorkloadStatus Running => RUNNING_VALUE;

    /// <summary>
    /// The workload has completed execution successfully.
    /// </summary>
    public static WorkloadStatus RanToCompletion => RAN_TO_COMPLETION_VALUE;

    /// <summary>
    /// The workload has completed execution with an error.
    /// </summary>
    public static WorkloadStatus Faulted => FAULTED_VALUE;

    /// <summary>
    /// The workload has been canceled.
    /// </summary>
    public static WorkloadStatus Canceled => CANCELED_VALUE;

    /// <summary>
    /// The workload is currently executing and cancellation has been requested.
    /// </summary>
    public static WorkloadStatus CancellationRequested => CANCELLATION_REQUESTED_VALUE;

    /// <summary>
    /// Reinterprets the specified <paramref name="status"/> as a <see cref="uint"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(WorkloadStatus status) =>
        ReinterpretCast<WorkloadStatus, uint>(status);

    /// <summary>
    /// Reinterprets the specified <paramref name="value"/> as a <see cref="WorkloadStatus"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator WorkloadStatus(uint value) =>
        ReinterpretCast<uint, WorkloadStatus>(value);

    /// <inheritdoc/>
    public override string ToString() => _value switch
    {
        INVALID_VALUE => nameof(Invalid),
        CREATED_VALUE => nameof(Created),
        SCHEDULED_VALUE => nameof(Scheduled),
        RUNNING_VALUE => nameof(Running),
        RAN_TO_COMPLETION_VALUE => nameof(RanToCompletion),
        FAULTED_VALUE => nameof(Faulted),
        CANCELED_VALUE => nameof(Canceled),
        CANCELLATION_REQUESTED_VALUE => nameof(CancellationRequested),
        _ => $"Unknown ({_value})"
    };

    /// <summary>
    /// Determines at least one of the specified <paramref name="flags"/> is set in the current <see cref="WorkloadStatus"/>.
    /// </summary>
    /// <param name="flags">The flag combination of <see cref="WorkloadStatus"/> to check against.</param>
    /// <returns><see langword="true"/> if at least one of the specified <paramref name="flags"/> is set in the current <see cref="WorkloadStatus"/>; otherwise, <see langword="false"/>.</returns>
    // force inlining to allow the JIT to do constant folding
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOneOf(WorkloadStatus flags) => (_value & flags) != 0;

    internal static class CommonFlags
    {
        // we must use the raw values here in order to allow the JIT to do constant folding
        // otherwise, there would be a lot of unnecessary call instructions. 
        // so don't touch these values unless you know what you're doing :P

        /// <summary>
        /// The workload has finished execution and reached a terminal state.
        /// </summary>
        /// <remarks>
        /// <code>RanToCompletion | Faulted | Canceled</code>
        /// </remarks>
        public static WorkloadStatus Completed => RAN_TO_COMPLETION_VALUE | FAULTED_VALUE | CANCELED_VALUE;

        /// <summary>
        /// Workload completion will be considered successful if the action delegate returns in this state.
        /// </summary>
        /// <remarks>
        /// <code>Running | CancellationRequested</code>
        /// </remarks>
        public static WorkloadStatus WillCompleteSuccessfully => RUNNING_VALUE | CANCELLATION_REQUESTED_VALUE;

        /// <summary>
        /// The workload is valid, but has not been executed yet.
        /// </summary>
        /// <remarks>
        /// <code>Created | Scheduled</code>
        /// </remarks>
        public static WorkloadStatus PreExecution => CREATED_VALUE | SCHEDULED_VALUE;
    }
}
