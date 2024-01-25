namespace Wkg.Threading.Workloads.Queuing.Classful.Classification;

public interface IPredicateBuilder
{
    Predicate<object?>? Compile();
}
