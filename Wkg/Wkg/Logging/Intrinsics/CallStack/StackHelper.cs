using System.Diagnostics;
using System.Reflection;

namespace Wkg.Logging.Intrinsics.CallStack;

public static class StackHelper
{
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
