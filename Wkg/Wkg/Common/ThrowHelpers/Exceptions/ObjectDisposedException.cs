using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ODE = System.ObjectDisposedException;

namespace Wkg.Common.ThrowHelpers;

public static partial class Throw
{
    public static class ObjectDisposedException
    {
        private const string DEFAULT_MESSAGE = "Cannot access a disposed object.";

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void If(bool disposedValue, string objectName)
        {
            if (disposedValue)
            {
                Throw(objectName, DEFAULT_MESSAGE);
            }
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void If(bool disposedValue, string objectName, string message)
        {
            if (disposedValue)
            {
                Throw(objectName, message);
            }
        }

        [DoesNotReturn]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Throw(string objectName, string message) =>
            throw new ODE(objectName, message);
    }
}
