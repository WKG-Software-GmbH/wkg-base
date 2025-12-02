using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wkg.Common.ThrowHelpers;

/// <summary>
/// Provides methods for throwing instances of <see cref="ArgumentOutOfRangeException"/>.
/// </summary>
[StackTraceHidden]
public static class ArgumentOutOfRangeExceptionExtensions
{
#pragma warning disable CA1034 // Nested types should not be visible
    // TODO: CA1034 doesn't yet understand C# 14 extension syntax and raises false positives here
    extension(ArgumentOutOfRangeException)
#pragma warning restore CA1034 // Nested types should not be visible
    {
        /// <summary>
        /// Throws a new <see cref="ArgumentOutOfRangeException"/> if <c><paramref name="value"/> &lt; <paramref name="min"/> || <paramref name="value"/> &gt; <paramref name="max"/></c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="min">The minimum allowed value of the parameter.</param>
        /// <param name="max">The maximum allowed value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <c><paramref name="value"/> &lt; <paramref name="min"/> || <paramref name="value"/> &gt; <paramref name="max"/></c>.</exception>
        public static void ThrowIfNotInRange(int value, int min, int max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value < min || value > max)
            {
                Throw(paramName, value, $"Value must be between {min} and {max}.");
            }
        }

        /// <inheritdoc cref="ThrowIfNotInRange(int, int, int, string)"/>
        public static void ThrowIfNotInRange(long value, long min, long max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value < min || value > max)
            {
                Throw(paramName, value, $"Value must be between {min} and {max}.");
            }
        }

        [DoesNotReturn]
        private static void Throw(string? paramName, object? actualValue, string message) =>
            throw new ArgumentOutOfRangeException(paramName, actualValue, message);
    }
}
