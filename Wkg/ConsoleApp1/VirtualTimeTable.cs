using System.Collections.Concurrent;

namespace ConsoleApp1;

internal unsafe class VirtualTimeTable
{
    private readonly ConcurrentDictionary<nint, VirtualTimeTableEntry> _table;

    public VirtualTimeTable(int expectedConcurrencyLevel, int capacity)
    {
        _table = new ConcurrentDictionary<nint, VirtualTimeTableEntry>(expectedConcurrencyLevel, capacity);
        _ = Environment.TickCount64;
    }

    private class VirtualTimeTableEntry
    {
        private long _averageTicksPerExecution;
        private long _invocationCount;
    }
}
