using System.Runtime.CompilerServices;

namespace Wkg;

/// <summary>
/// Contains some syntactic sugar :)
/// </summary>
public static class SyntacticSugar
{
    /// <summary>
    /// Explicitly does nothing. Useful for using expression bodied syntax for empty methods. Also explicitly indicates that methods are *supposed* to be empty (as opposed to a missing implimentation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pass() { }
}
