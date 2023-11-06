using Wkg.Data.Pooling;

namespace Wkg.Threading.Workloads.Queuing.VirtualTime;

internal class FastVirtualTimeTable : VirtualTimeTableBase
{
    public FastVirtualTimeTable(int expectedConcurrencyLevel, int capacity, int measurementCount = -1) 
        : base(expectedConcurrencyLevel, capacity, measurementCount) => Pass();

    public override long Now() => Environment.TickCount64;
}