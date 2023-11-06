using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.WorkloadTypes;

namespace Wkg.Threading.Workloads.Factories;

public class ClassfulWorkloadFactoryWithDI<THandle> : AbstractClassfulWorkloadFactoryWithDI<THandle>, 
    IWorkloadFactory<THandle, ClassfulWorkloadFactoryWithDI<THandle>>, 
    IClassfulWorkloadFactory<THandle> 
    where THandle : unmanaged
{
    private ClassfulWorkloadFactoryWithDI(IClassfulQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public IClassfulQdisc<THandle> Root => ClassfulRoot;

    static ClassfulWorkloadFactoryWithDI<THandle> IWorkloadFactory<THandle, ClassfulWorkloadFactoryWithDI<THandle>>
        .Create(IClasslessQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) => 
            new((IClassfulQdisc<THandle>)root, pool, options);
}
