using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.Factories;

public static class WorkloadFactoryExtensions
{
    public static void ConsumeAll<THandle, TState>(this AbstractClasslessWorkloadFactory<THandle> factory, IEnumerable<TState> states, Action<TState> consumer)
        where THandle : unmanaged
    {
        foreach (TState? state in states)
        {
            factory.Schedule(state, consumer);
        }
    }

    public static void ConsumeAll<THandle, TState>(this AbstractClasslessWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider> consumer)
        where THandle : unmanaged
    {
        foreach (TState? state in states)
        {
            factory.Schedule(state, consumer);
        }
    }

    public static void ConsumeAll<THandle, TState>(this AbstractClassfulWorkloadFactory<THandle> factory, THandle handle, IEnumerable<TState> states, Action<TState> consumer)
        where THandle : unmanaged
    {
        foreach (TState? state in states)
        {
            factory.Schedule(handle, state, consumer);
        }
    }

    public static void ConsumeAll<THandle, TState>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, THandle handle, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider> consumer)
        where THandle : unmanaged
    {
        foreach (TState? state in states)
        {
            factory.Schedule(handle, state, consumer);
        }
    }

    public static ValueTask ConsumeAllAsync<THandle, TState>(this AbstractClasslessWorkloadFactory<THandle> factory, IEnumerable<TState> states, Action<TState, CancellationFlag> consumer)
        where THandle : unmanaged => 
        ConsumeAllAsync(factory, states, consumer, CancellationToken.None);

    public static ValueTask ConsumeAllAsync<THandle, TState>(this AbstractClasslessWorkloadFactory<THandle> factory, IEnumerable<TState> states, Action<TState, CancellationFlag> consumer, CancellationToken cancellationToken)
        where THandle : unmanaged => 
        Workload.WhenAll(states.Select(state => factory.ScheduleAsync(state, consumer, cancellationToken)));

    public static ValueTask ConsumeAllAsync<THandle, TState>(this AbstractClasslessWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider, CancellationFlag> consumer)
        where THandle : unmanaged =>
        ConsumeAllAsync(factory, states, consumer, CancellationToken.None);

    public static ValueTask ConsumeAllAsync<THandle, TState>(this AbstractClasslessWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider, CancellationFlag> consumer, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        Workload.WhenAll(states.Select(state => factory.ScheduleAsync(state, consumer, cancellationToken)));

    public static ValueTask ConsumeAllAsync<THandle, TState>(this AbstractClassfulWorkloadFactory<THandle> factory, THandle handle, IEnumerable<TState> states, Action<TState, CancellationFlag> consumer)
        where THandle : unmanaged =>
        ConsumeAllAsync(factory, handle, states, consumer, CancellationToken.None);

    public static ValueTask ConsumeAllAsync<THandle, TState>(this AbstractClassfulWorkloadFactory<THandle> factory, THandle handle, IEnumerable<TState> states, Action<TState, CancellationFlag> consumer, CancellationToken cancellationToken)
        where THandle : unmanaged => 
        Workload.WhenAll(states.Select(state => factory.ScheduleAsync(handle, state, consumer, cancellationToken)));

    public static ValueTask ConsumeAllAsync<THandle, TState>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, THandle handle, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider, CancellationFlag> consumer)
        where THandle : unmanaged =>
        ConsumeAllAsync(factory, handle, states, consumer, CancellationToken.None);

    public static ValueTask ConsumeAllAsync<THandle, TState>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, THandle handle, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider, CancellationFlag> consumer, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        Workload.WhenAll(states.Select(state => factory.ScheduleAsync(handle, state, consumer, cancellationToken)));

    public static Task<List<TResult>> TransformAllAsync<THandle, TState, TResult>(this AbstractClasslessWorkloadFactory<THandle> factory, IEnumerable<TState> states, Func<TState, CancellationFlag, TResult> transformation)
        where THandle : unmanaged => 
        TransformAllAsync(factory, states, transformation, CancellationToken.None);

    public static Task<List<TResult>> TransformAllAsync<THandle, TState, TResult>(this AbstractClasslessWorkloadFactory<THandle> factory, IEnumerable<TState> states, Func<TState, CancellationFlag, TResult> transformation, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        TransformAllAsyncCore(states.Select(state => factory.ScheduleAsync(state, transformation, cancellationToken)).ToArray());

    public static Task<List<TResult>> TransformAllAsync<THandle, TState, TResult>(this AbstractClasslessWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> transformation)
        where THandle : unmanaged =>
        TransformAllAsync(factory, states, transformation, CancellationToken.None);

    public static Task<List<TResult>> TransformAllAsync<THandle, TState, TResult>(this AbstractClasslessWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> transformation, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        TransformAllAsyncCore(states.Select(state => factory.ScheduleAsync(state, transformation, cancellationToken)).ToArray());

    public static Task<List<TResult>> TransformAllAsync<THandle, TState, TResult>(this AbstractClassfulWorkloadFactory<THandle> factory, THandle handle, IEnumerable<TState> states, Func<TState, CancellationFlag, TResult> transformation)
        where THandle : unmanaged =>
        TransformAllAsync(factory, handle, states, transformation, CancellationToken.None);

    public static Task<List<TResult>> TransformAllAsync<THandle, TState, TResult>(this AbstractClassfulWorkloadFactory<THandle> factory, THandle handle, IEnumerable<TState> states, Func<TState, CancellationFlag, TResult> transformation, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        TransformAllAsyncCore(states.Select(state => factory.ScheduleAsync(handle, state, transformation, cancellationToken)).ToArray());

    public static Task<List<TResult>> TransformAllAsync<THandle, TState, TResult>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, THandle handle, IEnumerable<TState> states, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> transformation)
        where THandle : unmanaged =>
        TransformAllAsync(factory, handle, states, transformation, CancellationToken.None);

    public static Task<List<TResult>> TransformAllAsync<THandle, TState, TResult>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, THandle handle, IEnumerable<TState> states, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> transformation, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        TransformAllAsyncCore(states.Select(state => factory.ScheduleAsync(handle, state, transformation, cancellationToken)).ToArray());

    public static void ClassifyAll<THandle, TState>(this AbstractClassfulWorkloadFactory<THandle> factory, IEnumerable<TState> states, Action<TState> consumer)
        where THandle : unmanaged
    {
        foreach (TState? state in states)
        {
            factory.Classify(state, consumer);
        }
    }

    public static void ClassifyAll<THandle, TState>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider> consumer)
        where THandle : unmanaged
    {
        foreach (TState? state in states)
        {
            factory.Classify(state, consumer);
        }
    }

    public static ValueTask ClassifyAllAsync<THandle, TState>(this AbstractClassfulWorkloadFactory<THandle> factory, IEnumerable<TState> states, Action<TState, CancellationFlag> consumer)
        where THandle : unmanaged =>
        Workload.WhenAll(states.Select(state => factory.ClassifyAsync(state, consumer)));

    public static ValueTask ClassifyAllAsync<THandle, TState>(this AbstractClassfulWorkloadFactory<THandle> factory, IEnumerable<TState> states, Action<TState, CancellationFlag> consumer, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        Workload.WhenAll(states.Select(state => factory.ClassifyAsync(state, consumer, cancellationToken)));

    public static ValueTask ClassifyAllAsync<THandle, TState>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider, CancellationFlag> consumer)
        where THandle : unmanaged =>
        Workload.WhenAll(states.Select(state => factory.ClassifyAsync(state, consumer)));

    public static ValueTask ClassifyAllAsync<THandle, TState>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Action<TState, IWorkloadServiceProvider, CancellationFlag> consumer, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        Workload.WhenAll(states.Select(state => factory.ClassifyAsync(state, consumer, cancellationToken)));

    public static Task<List<TResult>> ClassifyAndTransformAllAsync<THandle, TState, TResult>(this AbstractClassfulWorkloadFactory<THandle> factory, IEnumerable<TState> states, Func<TState, CancellationFlag, TResult> transformation)
        where THandle : unmanaged =>
        TransformAllAsyncCore(states.Select(state => factory.ClassifyAsync(state, transformation)).ToArray());

    public static Task<List<TResult>> ClassifyAndTransformAllAsync<THandle, TState, TResult>(this AbstractClassfulWorkloadFactory<THandle> factory, IEnumerable<TState> states, Func<TState, CancellationFlag, TResult> transformation, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        TransformAllAsyncCore(states.Select(state => factory.ClassifyAsync(state, transformation, cancellationToken)).ToArray());

    public static Task<List<TResult>> ClassifyAndTransformAllAsync<THandle, TState, TResult>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> transformation)
        where THandle : unmanaged =>
        TransformAllAsyncCore(states.Select(state => factory.ClassifyAsync(state, transformation)).ToArray());

    public static Task<List<TResult>> ClassifyAndTransformAllAsync<THandle, TState, TResult>(this AbstractClassfulWorkloadFactoryWithDI<THandle> factory, IEnumerable<TState> states, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> transformation, CancellationToken cancellationToken)
        where THandle : unmanaged =>
        TransformAllAsyncCore(states.Select(state => factory.ClassifyAsync(state, transformation, cancellationToken)).ToArray());

    private static async Task<List<TResult>> TransformAllAsyncCore<TResult>(Workload<TResult>[] workloads)
    {
        await Workload.WhenAll(workloads);
        List<TResult> results = new(workloads.Length);
        List<Exception> errors = [];
        for (int i = 0; i < workloads.Length; i++)
        {
            WorkloadResult<TResult> result = workloads[i].Result;
            if (result.TryGetResult(out TResult? value))
            {
                results.Add(value);
            }
            else
            {
                if (result.IsFaulted)
                {
                    errors.Add(result.Exception);
                }
                else
                {
                    throw new OperationCanceledException("At least one workload was canceled while awaiting the results.");
                }
            }
        }
        if (errors.Count > 0)
        {
            throw new AggregateException(errors);
        }
        return results;
    }
}
