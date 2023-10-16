using System.Runtime.CompilerServices;

namespace Wkg.Reflection;

/// <summary>
/// Provides extension methods for reflective type checking.
/// </summary>
public static class TypeCheckingExtensions
{
    /// <summary>
    /// Determines whether the specified type is one of the specified types.
    /// </summary>
    /// <typeparam name="T1">The first type.</typeparam>
    /// <typeparam name="T2">The second type.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type is one of the specified types; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOneOf<T1, T2>(this Type type)
        => type == typeof(T1) || type == typeof(T2);

    /// <summary>
    /// Determines whether the specified type is one of the specified types.
    /// </summary>
    /// <typeparam name="T1">The first type.</typeparam>
    /// <typeparam name="T2">The second type.</typeparam>
    /// <typeparam name="T3">The third type.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type is one of the specified types; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOneOf<T1, T2, T3>(this Type type)
        => type == typeof(T1) || type == typeof(T2) || type == typeof(T3);

    /// <summary>
    /// Determines whether the specified type is one of the specified types.
    /// </summary>
    /// <typeparam name="T1">The first type.</typeparam>
    /// <typeparam name="T2">The second type.</typeparam>
    /// <typeparam name="T3">The third type.</typeparam>
    /// <typeparam name="T4">The fourth type.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type is one of the specified types; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOneOf<T1, T2, T3, T4>(this Type type)
        => type == typeof(T1) || type == typeof(T2) || type == typeof(T3) || type == typeof(T4);

    /// <summary>
    /// Determines whether the specified type is one of the specified types.
    /// </summary>
    /// <typeparam name="T1">The first type.</typeparam>
    /// <typeparam name="T2">The second type.</typeparam>
    /// <typeparam name="T3">The third type.</typeparam>
    /// <typeparam name="T4">The fourth type.</typeparam>
    /// <typeparam name="T5">The fifth type.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type is one of the specified types; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOneOf<T1, T2, T3, T4, T5>(this Type type)
        => type == typeof(T1) || type == typeof(T2) || type == typeof(T3) || type == typeof(T4)
        || type == typeof(T5);

    /// <summary>
    /// Determines whether the specified type is one of the specified types.
    /// </summary>
    /// <typeparam name="T1">The first type.</typeparam>
    /// <typeparam name="T2">The second type.</typeparam>
    /// <typeparam name="T3">The third type.</typeparam>
    /// <typeparam name="T4">The fourth type.</typeparam>
    /// <typeparam name="T5">The fifth type.</typeparam>
    /// <typeparam name="T6">The sixth type.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type is one of the specified types; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOneOf<T1, T2, T3, T4, T5, T6>(this Type type)
        => type == typeof(T1) || type == typeof(T2) || type == typeof(T3) || type == typeof(T4)
        || type == typeof(T5) || type == typeof(T6);

    /// <summary>
    /// Determines whether the specified type is one of the specified types.
    /// </summary>
    /// <typeparam name="T1">The first type.</typeparam>
    /// <typeparam name="T2">The second type.</typeparam>
    /// <typeparam name="T3">The third type.</typeparam>
    /// <typeparam name="T4">The fourth type.</typeparam>
    /// <typeparam name="T5">The fifth type.</typeparam>
    /// <typeparam name="T6">The sixth type.</typeparam>
    /// <typeparam name="T7">The seventh type.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type is one of the specified types; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOneOf<T1, T2, T3, T4, T5, T6, T7>(this Type type)
        => type == typeof(T1) || type == typeof(T2) || type == typeof(T3) || type == typeof(T4)
        || type == typeof(T5) || type == typeof(T6) || type == typeof(T7);

    /// <summary>
    /// Determines whether the specified type is one of the specified types.
    /// </summary>
    /// <typeparam name="T1">The first type.</typeparam>
    /// <typeparam name="T2">The second type.</typeparam>
    /// <typeparam name="T3">The third type.</typeparam>
    /// <typeparam name="T4">The fourth type.</typeparam>
    /// <typeparam name="T5">The fifth type.</typeparam>
    /// <typeparam name="T6">The sixth type.</typeparam>
    /// <typeparam name="T7">The seventh type.</typeparam>
    /// <typeparam name="T8">The eighth type.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type is one of the specified types; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOneOf<T1, T2, T3, T4, T5, T6, T7, T8>(this Type type)
        => type == typeof(T1) || type == typeof(T2) || type == typeof(T3) || type == typeof(T4)
        || type == typeof(T5) || type == typeof(T6) || type == typeof(T7) || type == typeof(T8);
}