namespace Wkg.Threading.Workloads.Internal;

public readonly struct SingleQdiscHandle
{
    private readonly bool _nonDefault;

    private SingleQdiscHandle(bool nonDefault) => _nonDefault = nonDefault;

    public static SingleQdiscHandle Root => new(true);
}
