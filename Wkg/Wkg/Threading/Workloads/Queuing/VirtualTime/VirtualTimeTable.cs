namespace Wkg.Threading.Workloads.Queuing.VirtualTime;

public static class VirtualTimeTable
{
    public static IVirtualTimeTable CreatePrecise(int expectedConcurrencyLevel, int capacity, int measurementCount = -1) => 
        new PreciseVirtualTimeTable(expectedConcurrencyLevel, capacity, measurementCount);

    public static IVirtualTimeTable CreateFast(int expectedConcurrencyLevel, int capacity, int measurementCount = -1) =>
        new FastVirtualTimeTable(expectedConcurrencyLevel, capacity, measurementCount);
}
