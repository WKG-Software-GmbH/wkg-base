using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;
using ANE = System.ArgumentNullException;

namespace Wkg.Common.ThrowHelpers;

public static partial class Throw
{
    public static class ArgumentNullException
    {
        private const string DEFAULT_MESSAGE = "Value cannot be null.";

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNull<T>([NotNull] T? value, string paramName, T? _ = default) where T : class
        {
            if (value is null)
            {
                Throw(paramName, DEFAULT_MESSAGE);
            }
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNull<T>([NotNull] T? value, string paramName, string message, T? _ = default) where T : class
        {
            if (value is null)
            {
                Throw(paramName, message);
            }
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNull<T>([NotNull] T? value, string paramName) where T : struct
        {
            if (value is null)
            {
                Throw(paramName, DEFAULT_MESSAGE);
            }
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNull<T>([NotNull] T? value, string paramName, string message) where T : struct
        {
            if (value is null)
            {
                Throw(paramName, message);
            }
        }

        [DoesNotReturn]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Throw(string paramName, string message) =>
            throw new ANE(paramName, message);
    }
}
