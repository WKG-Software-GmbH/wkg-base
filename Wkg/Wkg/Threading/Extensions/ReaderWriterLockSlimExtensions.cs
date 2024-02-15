namespace Wkg.Threading.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ReaderWriterLockSlim"/>.
/// </summary>
public static class ReaderWriterLockSlimExtensions
{
    /// <summary>
    /// Acquires a read lock on the <see cref="ReaderWriterLockSlim"/> and returns a <see cref="IDisposable"/> that releases the lock when disposed.
    /// </summary>
    /// <param name="rwls">The <see cref="ReaderWriterLockSlim"/> to acquire the lock on.</param>
    /// <returns>A <see cref="IDisposable"/> that releases the lock when disposed.</returns>
    public static ILockOwnership AcquireReadLock(this ReaderWriterLockSlim rwls) => new ReadLock(rwls);

    /// <summary>
    /// Acquires a write lock on the <see cref="ReaderWriterLockSlim"/> and returns a <see cref="IDisposable"/> that releases the lock when disposed.
    /// </summary>
    /// <param name="rwls">The <see cref="ReaderWriterLockSlim"/> to acquire the lock on.</param>
    /// <returns>A <see cref="IDisposable"/> that releases the lock when disposed.</returns>
    public static ILockOwnership AcquireWriteLock(this ReaderWriterLockSlim rwls) => new WriteLock(rwls);

    /// <summary>
    /// Acquires an upgradable read lock on the <see cref="ReaderWriterLockSlim"/> and returns a <see cref="IDisposable"/> that releases the lock when disposed.
    /// </summary>
    /// <param name="rwls">The <see cref="ReaderWriterLockSlim"/> to acquire the lock on.</param>
    /// <returns>A <see cref="IDisposable"/> that releases the lock when disposed.</returns>
    public static ILockOwnership AcquireUpgradableReadLock(this ReaderWriterLockSlim rwls) => new UpgradableReadLock(rwls);
}

file readonly struct ReadLock : ILockOwnership
{
    private readonly ReaderWriterLockSlim _rwls;

    public ReadLock(ReaderWriterLockSlim rwls)
    {
        _rwls = rwls;
        _rwls.EnterReadLock();
    }

    public void Dispose() => _rwls.ExitReadLock();
}

file readonly struct WriteLock : ILockOwnership
{
    private readonly ReaderWriterLockSlim _rwls;

    public WriteLock(ReaderWriterLockSlim rwls)
    {
        _rwls = rwls;
        _rwls.EnterWriteLock();
    }

    public void Dispose() => _rwls.ExitWriteLock();
}

file readonly struct UpgradableReadLock : ILockOwnership
{
    private readonly ReaderWriterLockSlim _rwls;

    public UpgradableReadLock(ReaderWriterLockSlim rwls)
    {
        _rwls = rwls;
        _rwls.EnterUpgradeableReadLock();
    }

    public void Dispose() => _rwls.ExitUpgradeableReadLock();
}