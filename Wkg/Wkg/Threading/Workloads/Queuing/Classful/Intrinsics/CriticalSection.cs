namespace Wkg.Threading.Workloads.Queuing.Classful.Intrinsics;

internal readonly ref struct CriticalSection
{
    private readonly ref int _count;

    private CriticalSection(ref int state)
    {
        _count = ref state;
    }

    public static CriticalSection Enter(ref int state)
    {
        Interlocked.Increment(ref state);
        return new CriticalSection(ref state);
    }

    public void Exit() => Interlocked.Decrement(ref _count);

    public void SpinUntilEmpty()
    {
        SpinWait spinner = default;
        while (Interlocked.CompareExchange(ref _count, 0, 0) != 0)
        {
            spinner.SpinOnce();
        }
    }
}