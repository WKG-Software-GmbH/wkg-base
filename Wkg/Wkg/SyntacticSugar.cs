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
    public static readonly object? __ = null;

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
    /// Explicitly does nothing. Useful for using expression bodied syntax for empty methods. Also explicitly indicates that methods are *supposed* to be empty (as opposed to a missing implimentation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pass() { }
}
