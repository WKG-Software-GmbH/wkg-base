﻿using System.Runtime.CompilerServices;

namespace Wkg.Unmanaged;

/// <summary>
/// Provides methods to reinterpret cast between types.
/// </summary>
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
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T? ReinterpretCast<T>(object? from) where T : class =>
        Unsafe.As<T>(from);

    /// <summary>
    /// Reinterprets the specified <paramref name="from"/> unmanaged value type as the specified <typeparamref name="TTo"/> unmanaged value type.
    /// </summary>
    /// <typeparam name="TFrom">The source unmanaged value type to reinterpret cast from.</typeparam>
    /// <typeparam name="TTo">The target unmanaged value type to reinterpret cast to.</typeparam>
    /// <param name="from">The value to reinterpret cast.</param>
    /// <param name="_">(Ignore this) A dummy parameter to allow type inference.</param>
    /// <returns>The specified <paramref name="from"/> unmanaged value type reinterpreted as the specified <typeparamref name="TTo"/> unmanaged value type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe TTo ReinterpretCast<TFrom, TTo>(TFrom from, TTo? _ = default)
        where TFrom : unmanaged
        where TTo : unmanaged => 
            *(TTo*)&from;

    /// <summary>
    /// Reinterprets the specified <paramref name="from"/> unmanaged ByRef value type as the specified <typeparamref name="TTo"/> unmanaged ByRef value type.
    /// </summary>
    /// <typeparam name="TFrom">The source unmanaged ByRef value type to reinterpret cast from.</typeparam>
    /// <typeparam name="TTo">The target unmanaged ByRef value type to reinterpret cast to.</typeparam>
    /// <param name="from">The value to reinterpret cast.</param>
    /// <returns>The specified <paramref name="from"/> unmanaged ByRef value type reinterpreted as the specified <typeparamref name="TTo"/> unmanaged ByRef value type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe ref TTo ReinterpretCastByRef<TFrom, TTo>(ref TFrom from)
        where TFrom : unmanaged
        where TTo : unmanaged =>
            ref Unsafe.AsRef<TTo>(Unsafe.AsPointer(ref from));
}
