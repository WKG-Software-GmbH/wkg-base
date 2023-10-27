using System.Runtime.CompilerServices;

namespace ConsoleApp1;

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
class MyTask<T>
{
    public Awaiter<T> GetAwaiter() => new();
}

class Awaiter<T> : ICriticalNotifyCompletion
{
    public bool IsCompleted { get; }
    public T GetResult() => throw new NotImplementedException();
    public void OnCompleted(Action completion) { }
    public void UnsafeOnCompleted(Action continuation) => throw new NotImplementedException();
}

struct MyTaskMethodBuilder<T>
{
    public static MyTaskMethodBuilder<T> Create() => default;

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    public void SetException(Exception exception) { }

    public void SetResult(T result) { }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    { }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    { }

    public MyTask<T> Task { get; }
}

class Test
{
    public static async MyTask<int> TestTask()
    {
        await Task.Delay(1000);
        return 42;
    }
}
