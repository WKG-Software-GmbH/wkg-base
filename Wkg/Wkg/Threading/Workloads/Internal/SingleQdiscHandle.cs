using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading.Workloads.Internal;

public readonly struct SingleQdiscHandle
{
    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Allows the struct to have a non-default value.")]
    private readonly bool _nonDefault;

    private SingleQdiscHandle(bool nonDefault) => _nonDefault = nonDefault;

    public static SingleQdiscHandle Root => new(true);
}
