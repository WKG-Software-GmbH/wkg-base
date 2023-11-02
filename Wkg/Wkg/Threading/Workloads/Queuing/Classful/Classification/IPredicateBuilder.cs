namespace Wkg.Threading.Workloads.Queuing.Classful.Classification;

internal interface IPredicateBuilder
{
    void AddPredicate<TState>(Predicate<TState> predicate);

    Predicate<object?>? Compile();
}
