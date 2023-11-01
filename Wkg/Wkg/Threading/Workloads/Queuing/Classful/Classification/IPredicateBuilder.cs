namespace Wkg.Threading.Workloads.Queuing.Classful.Classification;

internal interface IPredicateBuilder
{
    void AddPredicate<TState>(Predicate<TState> predicate) where TState : class;

    Predicate<object?>? Compile();
}
