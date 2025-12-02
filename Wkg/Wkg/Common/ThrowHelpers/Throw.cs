using System.Diagnostics;

namespace Wkg.Common.ThrowHelpers;

/// <summary>
/// Complements the static throw helpers introduced in .NET 8 for many exception types.
/// </summary>
[StackTraceHidden]
[DebuggerStepThrough]
[Obsolete(message: "The Throw.<SomeException>.<ThrowHelperMethod> API is deprecated and has been replaced with static extensions members. The legacy Throw API will be removed in a future version of this library")]
public static partial class Throw { }
