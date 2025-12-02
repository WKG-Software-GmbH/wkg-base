namespace Wkg.Threading;

/// <summary>
/// Represents the result of an attempt to run a synchronized action within an <see cref="AsyncLock"/>.
/// The <see cref="TaskExecuted"/> property may be used to determine whether the action was actually executed.
/// In practice, <see cref="TaskExecuted"/> will be <see langword="false"/> if the lock was disposed while waiting to enter, allowing the caller to handle this case gracefully.
/// </summary>
/// <param name="TaskExecuted">Indicates whether the synchronized action was executed. If <see langword="false"/>, the action was not executed due to the lock being disposed while waiting to enter.</param>
public readonly record struct AsyncLockResult(bool TaskExecuted)
{
    internal static AsyncLockResult Skipped() => new(false);

    internal static AsyncLockResult<TResult> Skipped<TResult>() => new(default, TaskExecuted: false);
}
