namespace Wkg.Threading.Workloads.Queuing.Classful.Classification;

internal class PredicateWrapper<TState> : IPredicate
{
    private readonly Predicate<TState> _predicate;

    internal PredicateWrapper(Predicate<TState> predicate) => _predicate = predicate;

    public bool Invoke(object? state) => state is TState s && _predicate(s);
}