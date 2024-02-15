using System.Text;

namespace Wkg.Text;

/// <summary>
/// Provides a thread-safe resource pool that enables reusing instances of <see cref="StringBuilder"/>.
/// </summary>
public abstract class StringBuilderPool
{
    /// <summary>
    /// Retrieves a <see cref="StringBuilder"/> that has at least <paramref name="minimumCapacity"/> capacity.
    /// </summary>
    /// <param name="minimumCapacity">The minimum capacity required for the <see cref="StringBuilder"/>.</param>
    /// <returns>
    /// A <see cref="StringBuilder"/> with at least <paramref name="minimumCapacity"/> capacity.
    /// </returns>
    /// <remarks>
    /// This builder is loaned to the caller and should be returned to the same pool via
    /// <see cref="Return"/> so that it may be reused in subsequent usage of <see cref="Rent"/>.
    /// It is not a fatal error to not return a rented builder, but failure to do so may lead to
    /// decreased application performance, as the pool may need to create a new builder to replace
    /// the one lost.
    /// </remarks>
    public abstract StringBuilder Rent(int minimumCapacity);

    /// <summary>
    /// Returns to the pool a <see cref="StringBuilder"/> that was previously obtained via <see cref="Rent"/> on the same
    /// <see cref="StringBuilderPool"/> instance.
    /// </summary>
    /// <param name="builder">
    /// The builder previously obtained from <see cref="Rent"/> to return to the pool.
    /// </param>
    /// <remarks>
    /// Once a builder has been returned to the pool, the caller gives up all ownership of the builder
    /// and must not use it. The reference returned from a given call to <see cref="Rent"/> must only be
    /// returned via <see cref="Return"/> once. The default <see cref="StringBuilderPool"/>
    /// may hold onto the returned builder in order to rent it again, or it may release the returned builder
    /// if it's determined that the pool already has enough builders stored.
    /// </remarks>
    public abstract void Return(StringBuilder builder);

    /// <summary>
    /// Gets a shared <see cref="StringBuilderPool"/> instance.
    /// </summary>
    public static StringBuilderPool Shared { get; } = new ConfigurableStringBuilderPool();
}