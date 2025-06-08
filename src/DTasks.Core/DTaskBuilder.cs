﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks;

internal abstract class DTaskBuilder<TResult> : DTask<TResult>
{
    public abstract void AwaitOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        where TAwaiter : INotifyCompletion;

    public abstract void AwaitUnsafeOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        where TAwaiter : ICriticalNotifyCompletion;

    public abstract void SetResult(TResult result);

    public abstract void SetException(Exception exception);

    public static void Create<TStateMachine>(ref TStateMachine stateMachine, [NotNull] ref DTaskBuilder<TResult>? builderField)
        where TStateMachine : IAsyncStateMachine
    {
        if (builderField is not null)
            throw new InvalidOperationException("The builder was not properly initialized.");

        var box = new DAsyncStateMachineBox<TStateMachine>();
        builderField = box; // You know what's important here
        box.StateMachine = stateMachine;
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private sealed class DAsyncStateMachineBox<TStateMachine> : DTaskBuilder<TResult>, IDAsyncStateMachine
        where TStateMachine : IAsyncStateMachine
    {
        private DTaskStatus _status;
        private TResult? _result;
        private object? _stateObject;
        public TStateMachine StateMachine;

        public DAsyncStateMachineBox()
        {
            _status = DTaskStatus.Pending;
            StateMachine = default!;
        }

        public override DTaskStatus Status => _status;

        protected override Exception ExceptionCore => Reinterpret.Cast<Exception>(_stateObject);

        protected override TResult ResultCore => _result!;

        private IDAsyncMethodBuilder Builder
        {
            get
            {
                Debug.Assert(IsRunning);

                return Reinterpret.Cast<IDAsyncMethodBuilder>(_stateObject);
            }
        }

        protected override void Run(IDAsyncRunner runner)
        {
            if (!IsPending)
                throw new InvalidOperationException("The DTask was already run.");

            runner.Start(this);
        }

        public override void SetResult(TResult result)
        {
            if (!IsRunning)
                throw new InvalidOperationException("The DTask should complete when running.");

            IDAsyncMethodBuilder builder = Builder;
            _stateObject = null;
            _status = DTaskStatus.Succeeded;
            _result = result;

            if (typeof(TResult) == typeof(VoidDTaskResult))
            {
                builder.SetResult();
            }
            else
            {
                builder.SetResult(result);
            }
        }

        public override void SetException(Exception exception)
        {
            if (!IsRunning)
                throw new InvalidOperationException("The DTask should complete when running.");

            IDAsyncMethodBuilder builder = Builder;

            _stateObject = exception;
            _status = exception is OperationCanceledException
                ? DTaskStatus.Canceled
                : DTaskStatus.Faulted;

            builder.SetException(exception);
        }

        public override void AwaitOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        {
            if (!IsRunning)
                throw new InvalidOperationException("The DTask is not running.");

            Builder.AwaitOnCompleted(ref awaiter);
        }

        public override void AwaitUnsafeOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        {
            if (!IsRunning)
                throw new InvalidOperationException("The DTask is not running.");

            Builder.AwaitUnsafeOnCompleted(ref awaiter);
        }

        void IDAsyncStateMachine.Start(IDAsyncMethodBuilder builder)
        {
            _status = DTaskStatus.Running;
            _stateObject = builder;
        }

        void IDAsyncStateMachine.MoveNext()
        {
            StateMachine.MoveNext();
        }

        void IDAsyncStateMachine.Suspend()
        {
            IDAsyncMethodBuilder builder = Builder;
            _stateObject = null;
            _status = DTaskStatus.Suspended;

            builder.SetState(ref StateMachine);
        }

        private string DebuggerDisplay
        {
            get
            {
                string stateMachineName = typeof(TStateMachine) != typeof(IAsyncStateMachine)
                    ? typeof(TStateMachine).Name
                    : StateMachine?.ToString() ?? nameof(IAsyncStateMachine);

                if (typeof(TResult) == typeof(VoidDTaskResult))
                    return $"DTask (Status = {Status}, Method = {stateMachineName})";

                return IsSucceeded
                    ? $"DTask<{typeof(TResult).Name}> (Status = {Status}, Method = {stateMachineName}, Result = {_result})"
                    : $"DTask<{typeof(TResult).Name}> (Status = {Status}, Method = {stateMachineName})";
            }
        }
    }
}
