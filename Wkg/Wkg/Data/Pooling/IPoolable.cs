namespace Wkg.Data.Pooling;

public interface IPoolable<T> where T : class, IPoolable<T>
{
    static abstract T Create(IPool<T> pool);
}