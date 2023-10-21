using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading;

internal interface IClasslessQdisc
{
    void Enqueue(Task task);

    int Count { get; }

    bool TryDequeue([NotNullWhen(true)] out Task? task);
}

internal interface IClasslessQdisc<TTaskDescriptor> : IClasslessQdisc
{
    void Enqueue(Task task, TTaskDescriptor descriptor);
}

internal interface IClassfulQdisc : IClasslessQdisc
{
    void AddChild(IClasslessQdisc child);

    void RemoveChild(IClasslessQdisc child);
}

internal interface IClassfulQdisc<TTaskDescriptor> : IClasslessQdisc<TTaskDescriptor>, IClassfulQdisc
{
}
