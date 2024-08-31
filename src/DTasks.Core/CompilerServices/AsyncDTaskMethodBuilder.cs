using DTasks;
using System.Diagnostics;

namespace System.Runtime.CompilerServices;

public struct AsyncDTaskMethodBuilder
{
    private DTaskBuilder<VoidDTaskResult>? _builder;

    public static AsyncDTaskMethodBuilder Create() => default;

    private readonly DTaskBuilder<VoidDTaskResult> Builder
    {
        get
        {
            Debug.Assert(_builder is not null, $"'{nameof(Start)}' was not invoked.");
            return _builder;
        }
    }

    public readonly DTask Task => Builder;

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        Start(ref _builder, ref stateMachine);
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        SetStateMachine(ref _builder, stateMachine);
    }

    public readonly void SetResult()
    {
        Builder.SetResult(default);
    }

    public readonly void SetException(Exception exception)
    {
        Builder.SetException(exception);
    }

    public readonly void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        Builder.EnsureSameStateMachine(stateMachine); // Debug only
        Builder.AwaitOnCompleted(ref awaiter);
    }

    public readonly void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        Builder.EnsureSameStateMachine(stateMachine); // Debug only
        Builder.AwaitUnsafeOnCompleted(ref awaiter);
    }

    internal static void Start<TResult, TStateMachine>(ref DTaskBuilder<TResult>? builderField, ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        Debug.Assert(builderField is null, $"'{nameof(Start)}' must be invoked once.");

        // Unlike the runtime implementation, here we allocate a boxed state machine right from the start.
        // We do not have the same strict performance requirements, therefore we trade the possibility
        // of avoiding the allocation of the state machine box if the method completes synchronously for
        // a clearer and more manageable code.

        // This method is called on the builder of the original state machine and it must initialize '_builder'
        // because the calling code will access its 'Task' property.

        builderField = DTaskBuilder<TResult>.Create(ref stateMachine);
        builderField.Start();
    }

    internal static void SetStateMachine<TResult>(ref DTaskBuilder<TResult>? builderField, IAsyncStateMachine stateMachine)
    {
        Debug.Assert(builderField is null, $"'{nameof(SetStateMachine)}' must be invoked once.");

        // This method is called on the builder of the boxed state machine and it must initialize '_builder'
        // because the calling code will access its method to build the task.

        if (stateMachine is not DTaskBuilder<TResult> builder)
            throw new InvalidOperationException($"'{nameof(SetStateMachine)}' may be called from internal code only.");

        builderField = builder;
    }
}

public struct AsyncDTaskMethodBuilder<TResult>
{
    private DTaskBuilder<TResult>? _builder;

    public static AsyncDTaskMethodBuilder<TResult> Create() => default;

    private readonly DTaskBuilder<TResult> Builder
    {
        get
        {
            Debug.Assert(_builder is not null, $"'{nameof(Start)}' was not invoked.");
            return _builder;
        }
    }

    public readonly DTask<TResult> Task => Builder;

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        AsyncDTaskMethodBuilder.Start(ref _builder, ref stateMachine);
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        AsyncDTaskMethodBuilder.SetStateMachine(ref _builder, stateMachine);
    }

    public readonly void SetResult(TResult result)
    {
        Builder.SetResult(result);
    }

    public readonly void SetException(Exception exception)
    {
        Builder.SetException(exception);
    }

    public readonly void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        Builder.EnsureSameStateMachine(stateMachine); // Debug only
        Builder.AwaitOnCompleted(ref awaiter);
    }

    public readonly void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        Builder.EnsureSameStateMachine(stateMachine); // Debug only
        Builder.AwaitUnsafeOnCompleted(ref awaiter);
    }
}