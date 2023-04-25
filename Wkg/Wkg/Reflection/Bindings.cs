using System.Reflection;

namespace Wkg.Reflection;

/// <summary>
/// Provides utilities for working with <see cref="BindingFlags"/>.
/// </summary>
public static class Bindings
{
    /// <summary>
    /// Gets the <see cref="BindingFlags"/> that represent all possible binding flags.
    /// </summary>
    /// <remarks>
    /// This is equivalent to <see cref="BindingFlags.Public"/><c> | </c> <see cref="BindingFlags.NonPublic"/><c> | </c> <see cref="BindingFlags.Instance"/><c> | </c> <see cref="BindingFlags.Static"/>.
    /// </remarks>
    public static BindingFlags All => 
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static;

    /// <summary>
    /// Gets the <see cref="BindingFlags"/> that represent all possible binding flags, including case-insensitive binding.
    /// </summary>
    /// <remarks>
    /// This is equivalent to <see cref="All"/><c> | </c> <see cref="BindingFlags.IgnoreCase"/>.
    /// </remarks>
    public static BindingFlags AllIgnoreCase => 
        All | BindingFlags.IgnoreCase;
}
