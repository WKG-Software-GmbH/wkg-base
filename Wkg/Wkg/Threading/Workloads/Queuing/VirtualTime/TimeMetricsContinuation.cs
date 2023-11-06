using Wkg.Data.Pooling;
using Wkg.Internals.Diagnostic;
using Wkg.Threading.Workloads.Continuations;

namespace Wkg.Threading.Workloads.Queuing.VirtualTime;

internal class TimeMetricsContinuation : IWorkloadContinuation, IPoolable<TimeMetricsContinuation>
{
    private readonly IPool<TimeMetricsContinuation> _pool;
    private IVirtualTimeTable _timeTable = null!;
    private long _startTime;

    private TimeMetricsContinuation(IPool<TimeMetricsContinuation> pool) => _pool = pool;

    public static TimeMetricsContinuation Create(IPool<TimeMetricsContinuation> pool) => new(pool);

    public void Initialize(IVirtualTimeTable timeTable)
    {
        _timeTable = timeTable;
        _startTime = timeTable.Now();
    }

    public void Invoke(AbstractWorkloadBase workload)
    {
        long endTime = _timeTable.Now();
        long delta = endTime - _startTime;
        DebugLog.WriteDiagnostic($"Workload {workload} took {delta} ticks to execute.");
        _timeTable.GetEntryFor(workload).AddMeasurement(delta);
        _startTime = 0;
        _pool.Return(this);
    }
}