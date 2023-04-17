using System.Runtime.InteropServices;

namespace Wkg.Unmanaged;

/// <summary>
/// This class is used to pin an object in memory.
/// </summary>
public readonly struct PinnedHandle : IDisposable
{
    /// <summary>
    /// The GCHandle of the pinned object.
    /// </summary>
    public readonly GCHandle GCHandle;

    /// <summary>
    /// Creates a new instance of the <see cref="PinnedHandle"/> class.
    /// </summary>
    /// <param name="o">The object to pin.</param>
    public PinnedHandle(object o)
    {
        GCHandle = GCHandle.Alloc(o, GCHandleType.Pinned);
    }

    /// <summary>
    /// Frees the pinned object.
    /// </summary>
    public void Dispose() => GCHandle.Free();
}
