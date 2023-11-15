using BenchmarkDotNet.Attributes;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleApp1;

public class Tests
{
    [Benchmark]
    public long StopwatchTimeStamp() => Stopwatch.GetTimestamp();

    [Benchmark]
    public long EnvironmentTickCount() => Environment.TickCount64;
}

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public struct ASDF
{
    [FieldOffset(0)]
    public ulong A;
    [FieldOffset(0)]
    public byte B;
}