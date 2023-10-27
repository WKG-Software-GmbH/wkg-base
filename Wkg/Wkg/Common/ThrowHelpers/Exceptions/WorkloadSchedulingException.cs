using WSE = Wkg.Threading.Workloads.Scheduling.WorkloadSchedulingException;

namespace Wkg.Common.ThrowHelpers;

public static partial class Throw
{
    internal static class WorkloadSchedulingException
    {
        public static void IfHandleIsDefault<THandle>(THandle handle) where THandle : unmanaged
        {
            if (handle.Equals(default(THandle)))
            {
                throw new WSE($"A qdisc handle must not be the default value of the underlying type '{typeof(THandle).Name}'. Was: '{handle}'.");
            }
        }
    }
}