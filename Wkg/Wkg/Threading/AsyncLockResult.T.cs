using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading;

/// <summary>
/// Represents the result of an attempt to run a synchronized action within an <see cref="AsyncLock"/>, returning a result of type <typeparamref name="TResult"/> if executed.
/// The <see cref="TaskExecuted"/> property may be used to determine whether the action was actually executed.
/// In practice, <see cref="TaskExecuted"/> will be <see langword="false"/> if the lock was disposed while waiting to enter, allowing the caller to handle this case gracefully.
/// </summary>
/// <typeparam name="TResult">The type of the result returned by the synchronized action.</typeparam>
/// <param name="Result">The result of the synchronized action if it was executed; otherwise, <see langword="null"/>.</param>
/// <param name="TaskExecuted">Indicates whether the synchronized action was executed. If <see langword="false"/>, the action was not executed due to the lock being disposed while waiting to enter.</param>
public readonly record struct AsyncLockResult<TResult>(TResult? Result, bool TaskExecuted)
{
    /// <summary>
    /// Implicitly converts an <see cref="AsyncLockResult{TResult}"/> to an <see cref="AsyncLockResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="AsyncLockResult{TResult}"/> to convert.</param>
    public static implicit operator AsyncLockResult(AsyncLockResult<TResult> result) => new(result.TaskExecuted);

    /// <summary>
    /// Attempts to retrieve the result of the synchronized action.
    /// </summary>
    /// <param name="result">The result of the synchronized action if it was executed; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the synchronized action was executed and the result is available; otherwise, <see langword="false"/>.</returns>
    public bool TryGetResult([MaybeNullWhen(false)] out TResult result)
    {
        result = Result;
        return TaskExecuted;
    }
}
