namespace Wkg.Threading.Workloads.Queuing.Classful.Classification;

internal class PredicateBuilder : IPredicateBuilder
{
    private readonly List<IPredicate> _predicates = new();

    public void AddPredicate<TState>(Predicate<TState> predicate) where TState : class => 
        _predicates.Add(new PredicateWrapper<TState>(predicate));

    public Predicate<object?>? Compile() => _predicates.Count switch
    {
        0 => null,
        1 => new Predicate<object?>(_predicates[0].Invoke),
        _ => new Predicate<object?>(new CompiledPredicates(this).Invoke),
    };

    private class CompiledPredicates : IPredicate
    {
        private readonly IPredicate[] _predicates;

        public CompiledPredicates(PredicateBuilder predicates) => _predicates = predicates._predicates.ToArray();

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

