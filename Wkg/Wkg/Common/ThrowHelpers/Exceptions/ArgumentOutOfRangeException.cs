using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Wkg.Common.ThrowHelpers;

using AOORE = ArgumentOutOfRangeException;

public static partial class Throw
{
    /// <summary>
    /// Provides methods for throwing <see cref="AOORE"/>.
    /// </summary>
    [StackTraceHidden]
    public static class ArgumentOutOfRangeException
    {
        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &lt; <paramref name="min"/> || <paramref name="value"/> &gt; <paramref name="max"/></c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="min">The minimum allowed value of the parameter.</param>
        /// <param name="max">The maximum allowed value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; <paramref name="min"/> || <paramref name="value"/> &gt; <paramref name="max"/></c>.</exception>
        public static void IfNotInRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
            {
                Throw(paramName, value, $"Value must be between {min} and {max}.");
            }
        }

        /// <inheritdoc cref="IfNotInRange(int, int, int, string)"/>
        public static void IfNotInRange(long value, long min, long max, string paramName)
        {
            if (value < min || value > max)
            {
                Throw(paramName, value, $"Value must be between {min} and {max}.");
            }
        }

        [DoesNotReturn]
        private static void Throw(string paramName, object? actualValue, string message) =>
            throw new AOORE(paramName, actualValue, message);
    }
}
