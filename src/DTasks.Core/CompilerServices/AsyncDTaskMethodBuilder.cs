using DTasks.Utils;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct AsyncDTaskMethodBuilder
{
    private DTaskBuilder<VoidDTaskResult>? _builder;

    public static AsyncDTaskMethodBuilder Create() => default;

    private readonly DTaskBuilder<VoidDTaskResult> Builder
    {
        get
        {
            Assert.NotNull(_builder, $"'{nameof(Start)}' was not invoked.");
            return _builder;
        }
    }

    public readonly DTask Task => Builder;

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        DTaskBuilder<VoidDTaskResult>.Create(ref stateMachine, ref _builder);
    }

    public readonly void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        SetStateMachine(stateMachine, _builder);
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
        Builder.AwaitOnCompleted(ref awaiter);
    }

    public readonly void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        Builder.AwaitUnsafeOnCompleted(ref awaiter);
    }

    internal static void SetStateMachine(IAsyncStateMachine stateMachine, DTask? builder)
    {
        ThrowHelper.ThrowIfNull(stateMachine);

        if (builder is not null)
            throw new InvalidOperationException("The builder was not properly initialized.");

        Debug.Fail($"{nameof(SetStateMachine)} should not be used.");
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
            Assert.NotNull(_builder, $"'{nameof(Start)}' was not invoked.");
            return _builder;
        }
    }

    public readonly DTask<TResult> Task => Builder;

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        DTaskBuilder<TResult>.Create(ref stateMachine, ref _builder);
    }

    public readonly void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        AsyncDTaskMethodBuilder.SetStateMachine(stateMachine, _builder);
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
        Builder.AwaitOnCompleted(ref awaiter);
    }

    public readonly void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        Builder.AwaitUnsafeOnCompleted(ref awaiter);
    }
}
