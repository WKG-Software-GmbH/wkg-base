namespace Wkg.Data.Pooling;

/// <summary>
/// Represents an object that can be pooled in a <see cref="IPool{T}"/>.
/// </summary>
public interface IPoolable<T> where T : class, IPoolable<T>
{
    /// <summary>
    /// The factory method and entry point for the <see cref="IPool{T}"/> to create new instances of the pooled object.
    /// </summary>
    /// <param name="pool">The <see cref="IPool{T}"/> that is creating the object.</param>
    /// <returns>A new instance of the pooled object.</returns>
    static abstract T Create(IPool<T> pool);
}