using System.Runtime.CompilerServices;

namespace Wkg;

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
    public static object? Do<T>(Action<T> action, T arg)
    {
        action(arg);
        return __;
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> and returns an empty dummy value. Useful for using <see langword="void"/> methods in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static object? Do<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        action(arg1, arg2);
        return __;
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> and returns an empty dummy value. Useful for using <see langword="void"/> methods in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static object? Do<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
    {
        action(arg1, arg2, arg3);
        return __;
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> and returns an empty dummy value. Useful for using <see langword="void"/> methods in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static object? Do<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        action(arg1, arg2, arg3, arg4);
        return __;
    }

    /// <summary>
    /// Explicitly does nothing. Useful for using expression bodied syntax for empty methods. Also explicitly indicates that methods are *supposed* to be empty (as opposed to a missing implimentation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pass() { }
}
