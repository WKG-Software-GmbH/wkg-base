namespace Wkg.Common.ThrowHelpers;

using AOORE = ArgumentOutOfRangeException;

public static partial class Throw
{
    /// <summary>
    /// Provides methods for throwing <see cref="AOORE"/>.
    /// </summary>
    public static class ArgumentOutOfRangeException
    {
        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &lt; 1</c>.
        /// </summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; 1</c>.</exception>
        public static void IfNegativeOrZero(string paramName, int value)
        {
            if (value <= 0)
            {
                throw new AOORE(paramName, value, "Value must be positive.");
            }
        }

        /// <inheritdoc cref="IfNegativeOrZero(string, int)"/>
        public static void IfNegativeOrZero(string paramName, long value)
        {
            if (value <= 0)
            {
                throw new AOORE(paramName, value, "Value must be positive.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &lt; 0</c>.
        /// </summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; 0</c>.</exception>
        public static void IfNegative(string paramName, int value)
        {
            if (value < 0)
            {
                throw new AOORE(paramName, value, "Value must be non-negative.");
            }
        }

        /// <inheritdoc cref="IfNegative(string, int)"/>
        public static void IfNegative(string paramName, long value)
        {
            if (value < 0)
            {
                throw new AOORE(paramName, value, "Value must be non-negative.");
            }
        }
    }
}
