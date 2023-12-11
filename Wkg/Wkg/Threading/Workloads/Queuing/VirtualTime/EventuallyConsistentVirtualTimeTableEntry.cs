using System.Diagnostics;

namespace Wkg.Threading.Workloads.Queuing.VirtualTime;

[DebuggerDisplay("Invocations: {_invocationCount}, AvgTicks: {_averageTicksPerExecution}, MAD: {_mad}")]
public class EventuallyConsistentVirtualTimeTableEntry
{
    // we use a double here for the rolling average, because floating point division is up to 10x faster than integer division
    // note that the calculated average is only eventually consistent, and may be slightly off. However, with a large enough
    // sample count, this error term will converge to 0.
    private double _averageTicksPerExecution;
    // invocation count is a long because we need to be able to increment it atomically
    // this will be cast to a double for the division.
    private long _invocationCount;
    // the mean absolute deviation (MAD) is the average of the absolute difference between the average and the measurements.
    // it's a cheap version of the standard deviation, and is used to determine the confidence interval of the average.
    private double _mad;

    public void AddMeasurement(long delta)
    {
        // we increment the invocation count first, and then resample it to be used for the denominator
        // note that between the increment and a successful CAS, the average may be slightly off.
        // we accept this as a tradeoff for not having to lock the entire table. this is a very
        // unlikely scenario, and the average will eventually converge to the correct value.
        // additionally, the introduced error term will converge to 0 as the number of measurements
        // increases.
        Interlocked.Increment(ref _invocationCount);
        double currentAverage, newAverage;
        long currentInvocationCount;
        do
        {
            currentAverage = Volatile.Read(ref _averageTicksPerExecution);
            currentInvocationCount = Volatile.Read(ref _invocationCount);
            newAverage = currentAverage + ((delta - currentAverage) / currentInvocationCount);
        } while (Interlocked.CompareExchange(ref _averageTicksPerExecution, newAverage, currentAverage) != currentAverage);
        // we calculate the mean absolute deviation (MAD) here, which is the average of the absolute
        // difference between the average and the measurements. this is used to determine the
        // confidence interval of the average.
        double currentMad, newMad;
        do
        {
            currentMad = Volatile.Read(ref _mad);
            currentAverage = Volatile.Read(ref _averageTicksPerExecution);
            currentInvocationCount = Volatile.Read(ref _invocationCount);
            newMad = currentMad + ((Math.Abs(delta - currentAverage) - currentMad) / currentInvocationCount);
        } while (Interlocked.CompareExchange(ref _mad, newMad, currentMad) != currentMad);
    }

    public long MeasurementCount => Volatile.Read(ref _invocationCount);

    public double AverageExecutionTime => Volatile.Read(ref _averageTicksPerExecution);

    public double WorstCaseAverageExecutionTime => AverageExecutionTime + Volatile.Read(ref _mad);

    public double BestCaseAverageExecutionTime => AverageExecutionTime - Volatile.Read(ref _mad);
}
