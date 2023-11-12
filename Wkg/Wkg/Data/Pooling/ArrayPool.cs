using Std = System.Buffers;

namespace Wkg.Data.Pooling;

/// <summary>
/// Provides a resource pool for arrays
/// </summary>
/// <remarks>
/// This type is a thin wrapper around <see cref="Std::ArrayPool{T}"/> providing a more convenient API.
/// </remarks>
public static class ArrayPool
{
    /// <inheritdoc cref="Std::ArrayPool{T}.Rent(int)"/>
    /// <remarks>
    /// Unlike <see cref="Std::ArrayPool{T}.Rent(int)"/>, this method returns a <see cref="PooledArray{T}"/> 
    /// whose <see cref="PooledArray{T}.Length"/> property is guaranteed to be equal to the <paramref name="length"/> parameter.
    /// </remarks>
    public static PooledArray<T> Rent<T>(int length) => new(Std::ArrayPool<T>.Shared.Rent(length), length);

    /// <inheritdoc cref="Std::ArrayPool{T}.Return(T[], bool)"/>
    public static void Return<T>(PooledArray<T> array, bool clearArray = false) => Std::ArrayPool<T>.Shared.Return(array.Array, clearArray);
}
