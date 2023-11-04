using BenchmarkDotNet.Attributes;
using System.Diagnostics;

namespace ConsoleApp1;

public class Tests
{
    [Benchmark]
    public int GetTickCount() => Environment.TickCount;

    [Benchmark]
    public long GetTickCount64() => Environment.TickCount64;

    [Benchmark]
    public long DatetimeToFileTime() => DateTime.UtcNow.ToFileTimeUtc();

    [Benchmark]
    public long StopwatchGetTimestamp() => Stopwatch.GetTimestamp();

    [Benchmark(Baseline = true)]
    public long DatetimeTicks() => DateTime.UtcNow.Ticks;
}
