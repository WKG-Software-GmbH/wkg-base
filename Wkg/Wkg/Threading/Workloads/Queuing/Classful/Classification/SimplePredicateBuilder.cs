namespace Wkg.Threading.Workloads.Queuing.Classful.Classification;

public class SimplePredicateBuilder : IPredicateBuilder
{
    private readonly List<IPredicate> _predicates = [];

    public SimplePredicateBuilder AddPredicate<TState>(Predicate<TState> predicate)
    {
        _predicates.Add(new PredicateWrapper<TState>(predicate));
        return this;
    }

    public Predicate<object?>? Compile() => _predicates.Count switch
    {
        0 => null,
        1 => new Predicate<object?>(_predicates[0].Invoke),
        _ => new Predicate<object?>(new CompiledPredicates(this).Invoke),
    };

    private class CompiledPredicates(SimplePredicateBuilder predicates) : IPredicate
    {
        private readonly IPredicate[] _predicates = [.. predicates._predicates];

        public bool Invoke(object? state)
        {
            for (int i = 0; i < _predicates.Length; i++)
            {
                if (_predicates[i].Invoke(state))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

