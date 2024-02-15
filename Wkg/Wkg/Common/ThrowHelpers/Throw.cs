using System.Diagnostics;

namespace Wkg.Common.ThrowHelpers;

/// <summary>
/// Complements the static throw helpers introduced in .NET 8 for many exception types.
/// </summary>
[StackTraceHidden]
[DebuggerStepThrough]
public static partial class Throw { }
