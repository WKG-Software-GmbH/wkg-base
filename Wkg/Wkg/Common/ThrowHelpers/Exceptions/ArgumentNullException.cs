using ANE = System.ArgumentNullException;

namespace Wkg.Common.ThrowHelpers;

public static partial class Throw
{
    public static class ArgumentNullException
    {
        public static void IfNull<T>(T? value, string paramName) where T : class => _ = value ?? throw new ANE(paramName);

        public static void IfNull<T>(T? value, string paramName, string message) where T : struct => _ = value ?? throw new ANE(paramName, message);
    }
}
