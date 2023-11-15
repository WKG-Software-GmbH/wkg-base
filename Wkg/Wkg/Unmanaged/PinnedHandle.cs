using System.Runtime.InteropServices;

namespace Wkg.Unmanaged;

/// <summary>
/// This class is used to pin an object in memory.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="PinnedHandle"/> class.
/// </remarks>
/// <param name="o">The object to pin.</param>
public readonly struct PinnedHandle(object o) : IDisposable
{
    /// <summary>
    /// The GCHandle of the pinned object.
    /// </summary>
    public readonly GCHandle GCHandle = GCHandle.Alloc(o, GCHandleType.Pinned);

    /// <summary>
    /// Frees the pinned object.
    /// </summary>
    public void Dispose() => GCHandle.Free();
}
