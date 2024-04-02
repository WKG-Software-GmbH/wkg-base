using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Wkg.Logging.Intrinsics.CallStack;

/// <summary>
/// Provides extension methods for <see cref="StackTrace"/>.
/// </summary>
public static class StackHelper
{
    /// <summary>
    /// Retrieves the <see cref="MethodBase"/> of the first stack frame that is not marked with the <see cref="StackTraceHiddenAttribute"/>.
    /// </summary>
    /// <param name="stack">The <see cref="StackTrace"/> to retrieve the <see cref="MethodBase"/> from.</param>
    [RequiresUnreferencedCode("Requires reflective access to calling methods.")]
    public static MethodBase? GetFirstNonHiddenCaller(this StackTrace stack)
    {
        MethodBase? method = null;

        // this is cheaper than calling GetFrames() and filtering afterward
        StackFrame? frame;
        for (int i = 0; (frame = stack.GetFrame(i)) is not null; i++)
        {
            method = frame.GetMethod();
            if (method?.IsDefined(typeof(StackTraceHiddenAttribute), inherit: false) is false)
            {
                break;
            }
        }
        return method;
    }
}
