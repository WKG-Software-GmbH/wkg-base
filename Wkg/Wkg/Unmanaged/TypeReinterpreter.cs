using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wkg.Unmanaged;

/// <summary>
/// Provides methods to reinterpret cast between types.
/// </summary>
[Obsolete("Use System.Runtime.CompilerServices.Unsafe instead.")]
public static class TypeReinterpreter
{
    /// <summary>
    /// Reinterprets the specified <paramref name="from"/> reference type as the specified <typeparamref name="TTo"/> reference type.
    /// </summary>
    /// <typeparam name="TFrom">The source reference type to reinterpret cast from.</typeparam>
    /// <typeparam name="TTo">The target reference type to reinterpret cast to.</typeparam>
    /// <param name="from">The value to reinterpret cast.</param>
    /// <param name="_">(Ignore this) A dummy parameter to allow type inference.</param>
    /// <returns>The specified <paramref name="from"/> reference type reinterpreted as the specified <typeparamref name="TTo"/> reference type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TTo ReinterpretCast<TFrom, TTo>(TFrom from, TTo? _ = default)
        where TFrom : class
        where TTo : class => 
            Unsafe.As<TFrom, TTo>(ref from);

    /// <summary>
    /// Reinterprets the specified <paramref name="from"/> object as the specified <typeparamref name="T"/> reference type.
    /// </summary>
    /// <typeparam name="T">The target reference type to reinterpret cast to.</typeparam>
    /// <param name="from">The value to reinterpret cast.</param>
    /// <returns>The specified <paramref name="from"/> object reinterpreted as the specified <typeparamref name="T"/> reference type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(from))]
    public static T? ReinterpretCast<T>(object? from) where T : class =>
        Unsafe.As<T>(from);

    /// <summary>
    /// Reinterprets the specified <paramref name="from"/> object as the specified <typeparamref name="T"/> reference type.
    /// </summary>
    /// <typeparam name="T">The target reference type to reinterpret cast to.</typeparam>
    /// <param name="from">The value to reinterpret cast.</param>
    /// <returns>The specified <paramref name="from"/> object reinterpreted as the specified <typeparamref name="T"/> reference type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(from))]
    public static T? ReinterpretAs<T>(this object? from) where T : class =>
        ReinterpretCast<T>(from);

    /// <summary>
    /// Reinterprets the provided <typeparamref name="TFrom"/> as a <typeparamref name="TTo"/>.
    /// </summary>
    /// <typeparam name="TFrom">The source unmanaged value type to reinterpret cast from.</typeparam>
    /// <typeparam name="TTo">The target unmanaged value type to reinterpret cast to.</typeparam>
    /// <param name="from">The value to reinterpret cast.</param>
    /// <returns>The specified <paramref name="from"/> unmanaged value type reinterpreted as the specified <typeparamref name="TTo"/> unmanaged value type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TTo ReinterpretCast<TFrom, TTo>(TFrom from)
        where TFrom : struct, allows ref struct
        where TTo : struct, allows ref struct =>
            Unsafe.BitCast<TFrom, TTo>(from);

    /// <summary>
    /// Reinterprets the specified <paramref name="from"/> ByRef type as the specified <typeparamref name="TTo"/> ByRef type.
    /// </summary>
    /// <typeparam name="TFrom">The source ByRef type to reinterpret cast from.</typeparam>
    /// <typeparam name="TTo">The target ByRef type to reinterpret cast to.</typeparam>
    /// <param name="from">The value to reinterpret cast.</param>
    /// <returns>The specified <paramref name="from"/> ByRef type reinterpreted as the specified <typeparamref name="TTo"/> ByRef type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo ReinterpretCastByRef<TFrom, TTo>(ref TFrom from)
        where TFrom : allows ref struct
        where TTo : allows ref struct =>
            ref Unsafe.As<TFrom, TTo>(ref from);
}
