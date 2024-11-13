using System.Diagnostics;
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

    public static ValueTask WhenAll(AwaitableWorkload[] workloads) => WhenAllCore(new WhenAllAwaiterState(workloads.Length), workloads);

    public static ValueTask WhenAll(params ReadOnlySpan<AwaitableWorkload> workloads) => WhenAllCore(new WhenAllAwaiterState(workloads.Length), workloads);

    private static ValueTask WhenAllCore(WhenAllAwaiterState state, ReadOnlySpan<AwaitableWorkload> workloads)
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
        internal readonly TaskCompletionSource _tcs = new();
        internal int _count;

        public WhenAllAwaiterState(int count)
        {
            _count = count;
            // we don't want to deal with worker threads being promoted to run the TCS completion inlined
            // se we dispatch it to a threadpool thread
            ContinuationCallback = new Action(() => ThreadPool.QueueUserWorkItem(OnWorkloadCompleted, this));
        }

        public object ContinuationCallback { get; }

        public int Count => Volatile.Read(ref _count);

        public void OnWorkloadCompleted(object? state)
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

    private class WhenAnyAwaiterState(IReadOnlyList<AwaitableWorkload> _workloads) : IWorkloadContinuation
    {
        internal readonly TaskCompletionSource<AwaitableWorkload> _tcs = new();
        private readonly IReadOnlyList<AwaitableWorkload> _workloads = _workloads;
        private uint _completed;
        private volatile AwaitableWorkload? _completedWorkload;

        public bool IsCompleted => Volatile.Read(ref _completed) == TRUE;

        public void Invoke(AbstractWorkloadBase workload)
        {
            // fire the TCS only once (the first time this method is invoked)
            if (Interlocked.CompareExchange(ref _completed, TRUE, FALSE) == FALSE)
            {
                // we know that the workload is an AwaitableWorkload because that's the only type
                // we subscribe to continuations on
                // ensure not to set the result here, because we can't do that on the worker thread
                // as the continuation may be invoked inlined (causing us to lose a worker thread)
                _completedWorkload = (AwaitableWorkload)workload;
                // schedule a cleanup task to remove the continuations from the remaining workloads
                // and to set the result on the TCS
                // do this on a threadpool thread to avoid accidentally promoting a worker thread
                // to run the TCS completion callback (which would cause a deadlock)
                ThreadPool.QueueUserWorkItem(Cleanup, this);
            }
        }

        public void InvokeInline(AbstractWorkloadBase workload)
        {
            // fire the TCS only once (the first time this method is invoked)
            if (Interlocked.CompareExchange(ref _completed, TRUE, FALSE) == FALSE)
            {
                // we know that the workload is an AwaitableWorkload because that's the only type
                // we subscribe to continuations on
                _completedWorkload = (AwaitableWorkload)workload;
                // we can simply call the cleanup method here, because we are running inlined
                Cleanup(this);
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
            // set the result on the TCS
            Debug.Assert(self._completedWorkload is not null);
            self._tcs.TrySetResult(self._completedWorkload!);
        }
    }

    #endregion WhenAny
}