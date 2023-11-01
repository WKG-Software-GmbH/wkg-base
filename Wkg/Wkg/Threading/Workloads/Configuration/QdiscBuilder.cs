namespace Wkg.Threading.Workloads.Configuration;

public static class QdiscBuilder
{
    public static QdiscBuilder<THandle> Create<THandle>() where THandle : unmanaged => new();
}
