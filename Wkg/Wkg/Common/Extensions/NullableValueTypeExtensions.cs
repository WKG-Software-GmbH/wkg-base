using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wkg.Common.Extensions;

/// <summary>
/// Contains extension methods for <see cref="Nullable{T}"/> value types.
/// </summary>
public static class NullableValueTypeExtensions
{
    /// <summary>
    /// Checks if the provided <see cref="Nullable{T}"/> is <see langword="null"/> or if its <see cref="Nullable{T}.Value"/> property is set to the <see langword="default"/> value of value type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="t">The <see cref="Nullable{T}"/> instance to check.</param>
    /// <returns><see langword="true"/> if the <see cref="Nullable{T}"/> is <see langword="null"/> or if its <see cref="Nullable{T}.Value"/> property is set to the <see langword="default"/> value of value type <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrDefault<T>([NotNullWhen(false)] this T? t) where T : struct =>
        t is null || t.Value.Equals(default(T));

    /// <summary>
    /// Checks that the provided <see cref="Nullable{T}"/> is not <see langword="null"/> and that its <see cref="Nullable{T}.Value"/> property is not set to the <see langword="default"/> value of value type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="t">The <see cref="Nullable{T}"/> instance to check.</param>
    /// <returns><see langword="true"/> if the <see cref="Nullable{T}"/> is not <see langword="null"/> and if its <see cref="Nullable{T}.Value"/> property is not set to the <see langword="default"/> value of value type <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasDefinedValue<T>([NotNullWhen(true)] this T? t) where T : struct =>
        !t.IsNullOrDefault();
}
