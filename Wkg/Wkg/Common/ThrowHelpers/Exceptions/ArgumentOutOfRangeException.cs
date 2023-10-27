namespace Wkg.Common.ThrowHelpers;

using AOORE = ArgumentOutOfRangeException;

public static partial class Throw
{
    public static class ArgumentOutOfRangeException
    {
        public static void IfNegativeOrZero(string paramName, int value)
        {
            if (value <= 0)
            {
                throw new AOORE(paramName, value, "Value must be positive.");
            }
        }

        public static void IfNegativeOrZero(string paramName, long value)
        {
            if (value <= 0)
            {
                throw new AOORE(paramName, value, "Value must be positive.");
            }
        }

        public static void IfNegative(string paramName, int value)
        {
            if (value < 0)
            {
                throw new AOORE(paramName, value, "Value must be non-negative.");
            }
        }

        public static void IfNegative(string paramName, long value)
        {
            if (value < 0)
            {
                throw new AOORE(paramName, value, "Value must be non-negative.");
            }
        }
    }
}
