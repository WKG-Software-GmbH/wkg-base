using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AOORE = System.ArgumentOutOfRangeException;

namespace Wkg.Common.ThrowHelpers;

public static partial class Throw
{
    /// <summary>
    /// Provides methods for throwing <see cref="AOORE"/>.
    /// </summary>
    [StackTraceHidden]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Legacy design choice for grouping exception throwing helpers.")]
    [Obsolete(message: "The Throw.<SomeException>.<ThrowHelperMethod> API is deprecated and has been replaced with static extensions members. The legacy Throw API will be removed in a future version of this library")]
    public static class ArgumentOutOfRangeException
    {
        /// <inheritdoc cref="ArgumentOutOfRangeExceptionExtensions.ThrowIfNotInRange(int, int, int, string)"/>
        public static void IfNotInRange(int value, int min, int max, string paramName) => AOORE.ThrowIfNotInRange(value, min, max, paramName);

        /// <inheritdoc cref="ArgumentOutOfRangeExceptionExtensions.ThrowIfNotInRange(long, long, long, string)"/>
        public static void IfNotInRange(long value, long min, long max, string paramName) => AOORE.ThrowIfNotInRange(value, min, max, paramName);
    }
}
