using System.Runtime.CompilerServices;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.WorkloadTypes;

namespace Wkg.Threading.Workloads.Factories;

public class ClassfulWorkloadFactoryWithDI<THandle> : AbstractClasslessWorkloadFactoryWithDI<THandle>, 
    IWorkloadFactory<THandle, ClassfulWorkloadFactoryWithDI<THandle>>, 
    IClassfulWorkloadFactory<THandle> 
    where THandle : unmanaged
{
    private ClassfulWorkloadFactoryWithDI(IClassfulQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public IClassfulQdisc<THandle> Root => Unsafe.As<IClassifyingQdisc<THandle>, IClassfulQdisc<THandle>>(ref RootRef);

    static ClassfulWorkloadFactoryWithDI<THandle> IWorkloadFactory<THandle, ClassfulWorkloadFactoryWithDI<THandle>>
        .Create(IClassifyingQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) => 
            new((IClassfulQdisc<THandle>)root, pool, options);
}
