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
        /// <param name="value">The value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; 1</c>.</exception>
        public static void IfNegativeOrZero(int value, string paramName)
        {
            if (value <= 0)
            {
                throw new AOORE(paramName, value, "Value must be positive.");
            }
        }

        /// <inheritdoc cref="IfNegativeOrZero(int, string)"/>
        public static void IfNegativeOrZero(long value, string paramName)
        {
            if (value <= 0)
            {
                throw new AOORE(paramName, value, "Value must be positive.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &lt; 0</c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; 0</c>.</exception>
        public static void IfNegative(int value, string paramName)
        {
            if (value < 0)
            {
                throw new AOORE(paramName, value, "Value must be non-negative.");
            }
        }

        /// <inheritdoc cref="IfNegative(int, string)"/>
        public static void IfNegative(long value, string paramName)
        {
            if (value < 0)
            {
                throw new AOORE(paramName, value, "Value must be non-negative.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &lt; <paramref name="min"/></c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="min">The minimum allowed value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; <paramref name="min"/></c>.</exception>
        public static void IfLessThan(int value, int min, string paramName)
        {
            if (value < min)
            {
                throw new AOORE(paramName, value, $"Value must be greater than or equal to {min}.");
            }
        }

        /// <inheritdoc cref="IfLessThan(int, int, string)"/>
        public static void IfLessThan(long value, long min, string paramName)
        {
            if (value < min)
            {
                throw new AOORE(paramName, value, $"Value must be greater than or equal to {min}.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &gt; <paramref name="maxValue"/></c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="maxValue">The maximum allowed value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &gt; <paramref name="maxValue"/></c>.</exception>
        public static void IfGreaterThan(int value, int maxValue, string paramName)
        {
            if (value > maxValue)
            {
                throw new AOORE(paramName, value, $"Value must be less than or equal to {maxValue}.");
            }
        }

        /// <inheritdoc cref="IfGreaterThan(int, int, string)"/>
        public static void IfGreaterThan(long value, long maxValue, string paramName)
        {
            if (value > maxValue)
            {
                throw new AOORE(paramName, value, $"Value must be less than or equal to {maxValue}.");
            }
        }
    }
}
