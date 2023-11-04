using Wkg.Common.ThrowHelpers;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.WorkloadTypes.Pooling;

internal class AnonymousWorkloadPool<TWorkload> where TWorkload : AnonymousWorkload, IPoolableAnonymousWorkload<TWorkload>
{
    private readonly TWorkload?[] _workloads;

    /// <summary>
    /// Points to the the next free index in the array.
    /// </summary>
    private int _index;

    public AnonymousWorkloadPool(int capacity)
    {
        Throw.ArgumentOutOfRangeException.IfNegativeOrZero(capacity, nameof(capacity));
        _workloads = new TWorkload[capacity];
        _index = 0;
    }

    public int Capacity => _workloads.Length;

    public TWorkload Rent()
    {
        DebugLog.WriteDiagnostic("Renting a workload from the AnonymousWorkloadPool.", LogWriter.Blocking);
        int original = Atomic.DecrementClampMin(ref _index, 0);
        int myIndex = original - 1;
        if (myIndex < 0)
        {
            return TWorkload.Create(this);
        }
        TWorkload? workload = Volatile.Read(ref _workloads[myIndex]);
        if (workload is null)
        {
            DebugLog.WriteError("Workload pool is corrupted! Got a non-negative index, but the workload at that index is null. Check the WorkloadFactory implementations!", LogWriter.Blocking);
            return TWorkload.Create(this);
        }
        return workload;
    }

    public void Return(TWorkload workload)
    {
        DebugLog.WriteDiagnostic("Returning a workload to the AnonymousWorkloadPool.", LogWriter.Blocking);
        // don't need to check for null because that should never happen
        // if it does, that's not too big of a deal either, as we'll just create a new workload as needed
        // we do the null checking on the caller thread in Rent() to avoid the overhead of the null check on the worker thread
        int myIndex = Atomic.IncrementClampMax(ref _index, _workloads.Length - 1);
        if (myIndex + 1 < _workloads.Length)
        {
            Volatile.Write(ref _workloads[myIndex], workload);
        }
    }
}
