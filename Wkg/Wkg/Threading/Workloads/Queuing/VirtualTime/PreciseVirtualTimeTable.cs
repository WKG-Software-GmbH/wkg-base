using System.Diagnostics;

namespace Wkg.Threading.Workloads.Queuing.VirtualTime;

internal class PreciseVirtualTimeTable : VirtualTimeTableBase
{
    public PreciseVirtualTimeTable(int expectedConcurrencyLevel, int capacity, int measurementCount = -1)
        : base(expectedConcurrencyLevel, capacity, measurementCount) => Pass();

    public override long Now() => Stopwatch.GetTimestamp();
}
