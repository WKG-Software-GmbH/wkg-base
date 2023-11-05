using Wkg.Threading.Workloads.Continuations;

namespace Wkg.Threading.Workloads;

using static ConcurrentBoolean;

public partial class Workload
{
    #region WhenAll

    public static ValueTask WhenAll(IEnumerable<AwaitableWorkload> workloads)
    {
        AwaitableWorkload[] array = workloads.ToArray();
        return WhenAllCore(new WhenAllAwaiterState(array.Length), array);
    }

    public static ValueTask WhenAll(params AwaitableWorkload[] workloads) => WhenAllCore(new WhenAllAwaiterState(workloads.Length), workloads);

    private static ValueTask WhenAllCore(WhenAllAwaiterState state, AwaitableWorkload[] workloads)
    {
        for (int i = 0; i < workloads.Length; i++)
        {
            AwaitableWorkload workload = workloads[i];
            if (workload.IsCompleted)
            {
                Interlocked.Decrement(ref state._count);
            }
            else
            {
                workload.AddOrRunInlineContinuationAction(state.ContinuationCallback, scheduleBeforeOthers: false);
            }
        }
        if (state.Count > 0)
        {
            return new ValueTask(state._tcs.Task);
        }
        else
        {
            return ValueTask.CompletedTask;
        }
    }

    private class WhenAllAwaiterState
    {
        public readonly TaskCompletionSource _tcs = new();
        public int _count;

        public WhenAllAwaiterState(int count)
        {
            _count = count;
            ContinuationCallback = new Action(OnWorkloadCompleted);
        }

        public object ContinuationCallback { get; }

        public int Count => Volatile.Read(ref _count);

        public void OnWorkloadCompleted()
        {
            if (Interlocked.Decrement(ref _count) == 0)
            {
                _tcs.TrySetResult();
            }
        }
    }
    #endregion

    #region WhenAny

    public static ValueTask<AwaitableWorkload> WhenAny(params AwaitableWorkload[] workloads) => WhenAnyCore(workloads);

    private static ValueTask<AwaitableWorkload> WhenAnyCore(AwaitableWorkload[] workloads)
    {
        WhenAnyAwaiterState state = new(workloads);
        for (int i = 0; i < workloads.Length; i++)
        {
            AwaitableWorkload workload = workloads[i];
            if (state.IsCompleted)
            {
                break;
            }
            if (workload.IsCompleted)
            {
                state.Invoke(workload);
                return ValueTask.FromResult(workload);
            }
            workload.AddOrRunInlineContinuationAction(state, scheduleBeforeOthers: false);
            if (state.IsCompleted)
            {
                // it could be that after we added the continuation, the state was completed
                // and that the cleanup task already run, which would cause this continuation
                // to be orphaned, so we need to remove it. if it's been removed already, then
                // this will safely no-op
                workload.RemoveContinuation(state);
                break;
            }
        }
        return new ValueTask<AwaitableWorkload>(state._tcs.Task);
    }

    private class WhenAnyAwaiterState : IWorkloadContinuation
    {
        public readonly TaskCompletionSource<AwaitableWorkload> _tcs = new();
        private readonly IList<AwaitableWorkload> _workloads;
        private uint _completed;

        public WhenAnyAwaiterState(IList<AwaitableWorkload> workloads)
        {
            _workloads = workloads;
        }

        public bool IsCompleted => Volatile.Read(ref _completed) == TRUE;

        public void Invoke(AbstractWorkloadBase workload)
        {
            // fire the TCS only once (the first time this method is invoked)
            if (Interlocked.CompareExchange(ref _completed, TRUE, FALSE) == FALSE)
            {
                // we know that the workload is an AwaitableWorkload because that's the only type
                // we subscribe to continuations on
                _tcs.TrySetResult((AwaitableWorkload)workload);
                // schedule a cleanup task to remove the continuations from the remaining workloads
                // do this on a threadpool thread to avoid blocking the worker thread that will invoke
                // this continuation inlined
                ThreadPool.QueueUserWorkItem(Cleanup, this);
            }
        }

        private static void Cleanup(object? state)
        {
            // the only possible state we can get here is the WhenAnyAwaiterState instance
            WhenAnyAwaiterState self = ReinterpretCast<WhenAnyAwaiterState>(state)!;
            foreach (AwaitableWorkload workload in self._workloads)
            {
                // only remove the continuation if it hasn't been invoked yet
                if (!workload.IsCompleted)
                {
                    // there is a potential for a race here, but RemoveContinuation will safely
                    // no-op if the continuation has already been invoked
                    workload.RemoveContinuation(self);
                }
            }
        }
    }

    #endregion WhenAny
}