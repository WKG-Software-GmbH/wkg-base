using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using WSE = Wkg.Threading.Workloads.Scheduling.WorkloadSchedulingException;

namespace Wkg.Common.ThrowHelpers;

public static partial class Throw
{
    internal static class WorkloadSchedulingException
    {
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfHandleIsDefault<THandle>(THandle handle) where THandle : unmanaged
        {
            if (handle.Equals(default(THandle)))
            {
                Throw($"A qdisc handle must not be the default value of the underlying type '{typeof(THandle).Name}'. Was: '{handle}'.");
            }
        }

        [DoesNotReturn]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Throw(string message) =>
            throw new WSE(message);
    }
}