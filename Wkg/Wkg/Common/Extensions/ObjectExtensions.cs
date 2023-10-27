using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wkg.Common.Extensions;

/// <summary>
/// Provides extension methods for instances of <see cref="object"/>.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Casts the specified <paramref name="obj"/> to the specified <typeparamref name="TResult"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is designed to be used in cases where non-public interface members are accessed and replaces the need for excessive brackets when casting.
    /// </para>
    /// <para>
    /// This method does not perform any type checks and is intended to be used in cases where the type of <paramref name="obj"/> is known at compile time.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult">The type to cast the specified <paramref name="obj"/> to.</typeparam>
    /// <param name="obj">The object to cast.</param>
    /// <returns>The specified <paramref name="obj"/> cast to the specified <typeparamref name="TResult"/>.</returns>
    /// <exception cref="InvalidCastException">The specified <paramref name="obj"/> cannot be cast to the specified <typeparamref name="TResult"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult To<TResult>(this object obj) => (TResult)obj;

    /// <summary>
    /// Soft casts the specified <paramref name="obj"/> to the specified <typeparamref name="TResult"/>.
    /// </summary>
    /// <remarks>
    /// This method is designed to be used for method chaining where the need for excessive bracket usage may be inconvenient.
    /// </remarks>
    /// <typeparam name="TResult">The type to cast the specified <paramref name="obj"/> to.</typeparam>
    /// <param name="obj">The object to cast.</param>
    /// <returns>The specified <paramref name="obj"/> cast to the specified <typeparamref name="TResult"/> if the cast is valid; otherwise, <see langword="null"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult? As<TResult>(this object? obj) where TResult : class => obj as TResult;

    /// <summary>
    /// Returns the specified <paramref name="value"/> if it is not <see langword="null"/>; otherwise, returns the specified <paramref name="fallback"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is designed to be used for method chaining where the need for excessive bracket usage may be inconvenient.
    /// </para>
    /// <para>
    /// This method is equivalent to the null-coalescing operator: <c>value ?? fallback</c>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="fallback">The fallback value to return if the specified <paramref name="value"/> is <see langword="null"/>.</param>
    /// <returns>The specified <paramref name="value"/> if it is not <see langword="null"/>; otherwise, the specified <paramref name="fallback"/>.</returns>
    [return: NotNullIfNotNull(nameof(fallback))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult? Coalesce<TResult>(this TResult? value, TResult? fallback) where TResult : class =>
        value ?? fallback;
}