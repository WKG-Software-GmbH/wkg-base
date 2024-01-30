namespace Wkg.Common.Extensions;

/// <summary>
/// Contains extension methods for instances of <see cref="ThreadLocal{T}"/>.
/// </summary>
public static class ThreadLocalValueTypeExtensions
{
    /// <summary>
    /// Gets the value of the nullable value type stored in the <see cref="ThreadLocal{T}"/> or the default value of the value type if the <see cref="ThreadLocal{T}"/> has not been initialized.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="threadLocal">The <see cref="ThreadLocal{T}"/> instance.</param>
    /// <returns>The value of the nullable value type stored in the <see cref="ThreadLocal{T}"/> or the default value of the value type if the <see cref="ThreadLocal{T}"/> has not been initialized.</returns>
    public static T? GetValueOrDefault<T>(this ThreadLocal<T?> threadLocal) where T : struct =>
        threadLocal.Value.GetValueOrDefault();
}
