namespace Wkg.Threading.Workloads.Queuing.VirtualTime;

public interface IVirtualTimeTable
{
    EventuallyConsistentVirtualTimeTableEntry GetEntryFor(AbstractWorkloadBase workload);

    EventuallyConsistentVirtualTimeTableEntry GetEntryByHandle(nint handle);

    void StartMeasurement(AbstractWorkloadBase workload);

    long Now();
}
