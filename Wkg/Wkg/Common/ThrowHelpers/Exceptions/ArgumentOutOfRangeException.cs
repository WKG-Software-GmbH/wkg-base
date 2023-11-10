namespace Wkg.Common.ThrowHelpers;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNegativeOrZero(int value, string paramName)
        {
            if (value <= 0)
            {
                Throw(paramName, value, "Value must be positive.");
            }
        }

        /// <inheritdoc cref="IfNegativeOrZero(int, string)"/>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNegativeOrZero(long value, string paramName)
        {
            if (value <= 0)
            {
                Throw(paramName, value, "Value must be positive.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &lt; 0</c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; 0</c>.</exception>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNegative(int value, string paramName)
        {
            if (value < 0)
            {
                Throw(paramName, value, "Value must be non-negative.");
            }
        }

        /// <inheritdoc cref="IfNegative(int, string)"/>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNegative(long value, string paramName)
        {
            if (value < 0)
            {
                Throw(paramName, value, "Value must be non-negative.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &lt; <paramref name="min"/></c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="min">The minimum allowed value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; <paramref name="min"/></c>.</exception>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfLessThan(int value, int min, string paramName)
        {
            if (value < min)
            {
                Throw(paramName, value, $"Value must be greater than or equal to {min}.");
            }
        }

        /// <inheritdoc cref="IfLessThan(int, int, string)"/>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfLessThan(long value, long min, string paramName)
        {
            if (value < min)
            {
                Throw(paramName, value, $"Value must be greater than or equal to {min}.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &gt; <paramref name="maxValue"/></c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="maxValue">The maximum allowed value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &gt; <paramref name="maxValue"/></c>.</exception>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfGreaterThan(int value, int maxValue, string paramName)
        {
            if (value > maxValue)
            {
                Throw(paramName, value, $"Value must be less than or equal to {maxValue}.");
            }
        }

        /// <inheritdoc cref="IfGreaterThan(int, int, string)"/>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfGreaterThan(long value, long maxValue, string paramName)
        {
            if (value > maxValue)
            {
                Throw(paramName, value, $"Value must be less than or equal to {maxValue}.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> &lt; <paramref name="min"/> || <paramref name="value"/> &gt; <paramref name="max"/></c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="min">The minimum allowed value of the parameter.</param>
        /// <param name="max">The maximum allowed value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> &lt; <paramref name="min"/> || <paramref name="value"/> &gt; <paramref name="max"/></c>.</exception>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNotInRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
            {
                Throw(paramName, value, $"Value must be between {min} and {max}.");
            }
        }

        /// <inheritdoc cref="IfNotInRange(int, int, int, string)"/>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNotInRange(long value, long min, long max, string paramName)
        {
            if (value < min || value > max)
            {
                Throw(paramName, value, $"Value must be between {min} and {max}.");
            }
        }

        /// <summary>
        /// Throws a new <see cref="AOORE"/> if <c><paramref name="value"/> == 0</c>.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="AOORE">Thrown if <c><paramref name="value"/> == 0</c>.</exception>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfZero(int value, string paramName)
        {
            if (value == 0)
            {
                Throw(paramName, value, "Value must be non-zero.");
            }
        }

        /// <inheritdoc cref="IfZero(int, string)"/>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfZero(long value, string paramName)
        {
            if (value == 0)
            {
                Throw(paramName, value, "Value must be non-zero.");
            }
        }

        [DoesNotReturn]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Throw(string paramName, object? actualValue, string message) =>
            throw new AOORE(paramName, actualValue, message);
    }
}
