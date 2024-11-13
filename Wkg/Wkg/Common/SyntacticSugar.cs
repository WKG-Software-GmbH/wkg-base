using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wkg.Common;

/// <summary>
/// Contains some syntactic sugar :)
/// </summary>
public static class SyntacticSugar
{
    /// <summary>
    /// A dummy object that can be used as a placeholder for empty switch expressions. 
    /// </summary>
    /// <remarks>
    /// Always returns <see langword="null"/>.
    /// </remarks>
    public static object? __ => null;

    /// <summary>
    /// Executes the provided <paramref name="action"/> and returns an empty dummy value. Useful for using <see langword="void"/> methods in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static object? Do(Action action)
    {
        action();
        return __;
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> and returns an empty dummy value. Useful for using <see langword="void"/> methods in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static object? Do<T>(Action<T> action, scoped T arg) where T : allows ref struct
    {
        action(arg);
        return __;
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> and returns an empty dummy value. Useful for using <see langword="void"/> methods in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static object? Do<T1, T2>(Action<T1, T2> action, scoped T1 arg1, scoped T2 arg2) where T1 : allows ref struct where T2 : allows ref struct
    {
        action(arg1, arg2);
        return __;
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> and returns an empty dummy value. Useful for using <see langword="void"/> methods in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static object? Do<T1, T2, T3>(Action<T1, T2, T3> action, scoped T1 arg1, scoped T2 arg2, scoped T3 arg3) 
        where T1 : allows ref struct where T2 : allows ref struct where T3 : allows ref struct
    {
        action(arg1, arg2, arg3);
        return __;
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> and returns an empty dummy value. Useful for using <see langword="void"/> methods in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static object? Do<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, scoped T1 arg1, scoped T2 arg2, scoped T3 arg3, scoped T4 arg4) 
        where T1 : allows ref struct where T2 : allows ref struct 
        where T3 : allows ref struct where T4 : allows ref struct
    {
        action(arg1, arg2, arg3, arg4);
        return __;
    }

    /// <summary>
    /// Explicitly does nothing. Useful for using expression bodied syntax for empty methods. Also explicitly indicates that methods are *supposed* to be empty (as opposed to a missing implementation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pass() { }

    /// <inheritdoc cref="Pass()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pass<T>(scoped T _) where T : allows ref struct { }

    /// <inheritdoc cref="Pass()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pass<T1, T2>(scoped T1 _1, scoped T2 _2) 
        where T1 : allows ref struct where T2 : allows ref struct { }

    /// <inheritdoc cref="Pass()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pass<T1, T2, T3>(scoped T1 _1, scoped T2 _2, scoped T3 _3) 
        where T1 : allows ref struct where T2 : allows ref struct where T3 : allows ref struct { }

    /// <inheritdoc cref="Pass()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pass<T1, T2, T3, T4>(scoped T1 _1, scoped T2 _2, scoped T3 _3, scoped T4 _4)
        where T1 : allows ref struct where T2 : allows ref struct 
        where T3 : allows ref struct where T4 : allows ref struct
    { }

    /// <summary>
    /// Converts the provided <paramref name="value"/> to its nullable equivalent.
    /// </summary>
    /// <remarks>
    /// This method does nothing other than providing Code Analysis with a hint that the value is nullable, which may be required in nullable-value-returning lambda expressions.
    /// </remarks>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to return.</param>
    /// <returns>The provided <paramref name="value"/> but with the Code Analysis hint that it is nullable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: MaybeNull]
    public static T? NullableOf<T>(T? value) where T : allows ref struct => value;
}
