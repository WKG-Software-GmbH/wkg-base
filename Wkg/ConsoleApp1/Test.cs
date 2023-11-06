using BenchmarkDotNet.Attributes;
using System.Diagnostics;

namespace ConsoleApp1;

public class Tests
{
    [Benchmark]
    public long StopwatchTimeStamp() => Stopwatch.GetTimestamp();

    [Benchmark]
    public long EnvironmentTickCount() => Environment.TickCount64;
}
