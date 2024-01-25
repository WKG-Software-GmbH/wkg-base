using System.Collections.Concurrent;
using System.Diagnostics;
using Wkg.Data.Pooling;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing.VirtualTime;

internal abstract class VirtualTimeTableBase : IVirtualTimeTable
{
    private protected readonly ConcurrentDictionary<nint, EventuallyConsistentVirtualTimeTableEntry> _table;
    private protected readonly int _measurementCount;
    private protected readonly WeakPool<TimeMetricsContinuation> _pool;

    private protected VirtualTimeTableBase(int expectedConcurrencyLevel, int capacity, int measurementCount = -1)
    {
        Debug.Assert(expectedConcurrencyLevel > 0);
        Debug.Assert(capacity > 0);
        Debug.Assert(measurementCount is -1 or > 0);
        _table = new ConcurrentDictionary<nint, EventuallyConsistentVirtualTimeTableEntry>(expectedConcurrencyLevel, capacity);
        _pool = new WeakPool<TimeMetricsContinuation>(expectedConcurrencyLevel, suppressContentionWarnings: true);
        _measurementCount = measurementCount;
    }

    public virtual EventuallyConsistentVirtualTimeTableEntry GetEntryFor(AbstractWorkloadBase workload)
    {
        nint functionPointer = workload.GetPayloadFunctionPointer();
        return GetEntryByHandle(functionPointer);
    }

    public virtual EventuallyConsistentVirtualTimeTableEntry GetEntryByHandle(nint handle)
    {
        if (!_table.TryGetValue(handle, out EventuallyConsistentVirtualTimeTableEntry? entry))
        {
            entry = new EventuallyConsistentVirtualTimeTableEntry();
            _ = _table.TryAdd(handle, entry);
        }
        // we never remove entries from the table, so this should never be null
        Debug.Assert(entry is not null);
        return entry;
    }

    public virtual void StartMeasurement(AbstractWorkloadBase workload)
    {
        if (_measurementCount == -1 || GetEntryFor(workload).MeasurementCount < _measurementCount)
        {
            TimeMetricsContinuation continuation = _pool.Rent();
            continuation.Initialize(this);
            if (!workload.TryAddContinuation(continuation, scheduleBeforeOthers: true))
            {
                WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Failed to add {nameof(TimeMetricsContinuation)} to workload: {workload}.");
                DebugLog.WriteException(exception, LogWriter.Blocking);
            }
        }
    }

    public abstract long Now();
}
