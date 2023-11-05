﻿using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.Queuing.Classless.Lifo;

/// <summary>
/// A qdisc that implements the Last-In-First-Out (LIFO) scheduling algorithm.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
internal sealed class LifoQdisc<THandle> : ClasslessQdisc<THandle>, IClasslessQdisc<THandle> where THandle : unmanaged
{
    private readonly ConcurrentStack<AbstractWorkloadBase> _stack;

    public LifoQdisc(THandle handle) : base(handle)
    {
        _stack = new ConcurrentStack<AbstractWorkloadBase>();
    }

    /// <inheritdoc/>
    public override bool IsEmpty => _stack.IsEmpty;

    /// <inheritdoc/>
    public override int Count => _stack.Count;

    /// <inheritdoc/>
    protected override void EnqueueDirect(AbstractWorkloadBase workload)
    {
        if (TryBindWorkload(workload))
        {
            _stack.Push(workload);
            NotifyWorkScheduled();
        }
        else
        {
            DebugLog.WriteWarning("A workload was scheduled, but could not be bound to the qdisc. This is a likely a bug in the qdisc scheduler implementation.", LogWriter.Blocking);
        }
    }

    /// <inheritdoc/>
    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _stack.TryPop(out workload);

    /// <inheritdoc/>
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;
}
