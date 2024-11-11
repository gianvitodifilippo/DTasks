using DTasks.Inspection;
using DTasks.Marshaling;
using System.Diagnostics;
using System.Reflection;
using Xunit.Sdk;

namespace DTasks.Hosting;

internal sealed class FakeDAsyncStateManager(IDAsyncMarshaler marshaler) : IDAsyncStateManager
{
    private readonly Dictionary<DAsyncId, DehydratedRunnable> _runnables = [];
    private Action<DAsyncId>? _onDehydrate;

    public void OnDehydrate(Action<DAsyncId> onDehydrate)
    {
        _onDehydrate = onDehydrate;
    }

    public int Count => _runnables.Count;

    public ValueTask DehydrateAsync<TStateMachine>(DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine, ISuspensionContext suspensionContext, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        if (id == default)
            throw FailException.ForFailure($"'{nameof(id)}' cannot be default.");

        if (id == DAsyncId.RootId)
            throw FailException.ForFailure($"'{nameof(id)}' cannot be {nameof(DAsyncId.RootId)}.");

        Debug.WriteLine($"Dehydrating runnable {id} ({stateMachine}) with parent {parentId}.");
        _onDehydrate?.Invoke(id);
        DehydratedRunnable runnable = DehydratedRunnable.Create<TStateMachine>(parentId, stateMachine, suspensionContext, marshaler);
        _runnables[id] = runnable;
        return ValueTask.CompletedTask;
    }

    public ValueTask<DAsyncLink> HydrateAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = _runnables[id];
        _runnables.Remove(id);
        DAsyncLink link = runnable.Hydrate(marshaler);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return ValueTask.FromResult(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = _runnables[id];
        _runnables.Remove(id);
        DAsyncLink link = runnable.Hydrate(result, marshaler);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return ValueTask.FromResult(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = _runnables[id];
        _runnables.Remove(id);
        DAsyncLink link = runnable.Hydrate(exception, marshaler);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return ValueTask.FromResult(link);
    }

    public ValueTask<DAsyncId> DeleteAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        if (!_runnables.Remove(id, out DehydratedRunnable? runnable))
            throw FailException.ForFailure($"Runnable '{id}' was not found.");

        return ValueTask.FromResult(runnable.ParentId);
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    private delegate void FieldHydrator(object boxedStateMachine, IDAsyncMarshaler marshaler);

    private delegate void StartAction<TBuilder, TStateMachine>(ref TBuilder builder, ref TStateMachine stateMachine);

    private delegate IDAsyncRunnable StateMachineStarter(object boxedStateMachine);

    private abstract class DehydratedRunnable(
        DAsyncId parentId,
        object boxedStateMachine,
        IEnumerable<FieldHydrator> fieldHydrators,
        StateMachineStarter starter)
    {
        private static readonly MethodInfo s_createStateMachineStarterGenericMethod = typeof(DehydratedRunnable).GetMethod(
            name: nameof(CreateStateMachineStarter),
            bindingAttr: BindingFlags.Static | BindingFlags.NonPublic)!;

        private static readonly MethodInfo s_isSuspendedGenericMethod = typeof(DehydratedRunnable).GetMethod(
            name: nameof(IsSuspended),
            bindingAttr: BindingFlags.Static | BindingFlags.NonPublic)!;

        private static readonly MethodInfo s_createFieldHydratorGenericMethod = typeof(DehydratedRunnable).GetMethod(
            nameof(CreateFieldHydrator),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly Type s_genericParameterType = Type.MakeGenericMethodParameter(0);

        public DAsyncId ParentId => parentId;

        protected abstract void DehydrateAwaiter(object boxedStateMachine);

        protected abstract void DehydrateAwaiter<TResult>(object boxedStateMachine, TResult result);

        protected abstract void DehydrateAwaiter(object boxedStateMachine, Exception exception);

        public DAsyncLink Hydrate(IDAsyncMarshaler marshaler)
        {
            DehydrateAwaiter(boxedStateMachine);
            foreach (FieldHydrator fieldHydrator in fieldHydrators)
            {
                fieldHydrator(boxedStateMachine, marshaler);
            }

            IDAsyncRunnable runnable = starter(boxedStateMachine);
            return new DAsyncLink(parentId, runnable);
        }

        public DAsyncLink Hydrate<TResult>(TResult result, IDAsyncMarshaler marshaler)
        {
            DehydrateAwaiter(boxedStateMachine, result);
            foreach (FieldHydrator fieldHydrator in fieldHydrators)
            {
                fieldHydrator(boxedStateMachine, marshaler);
            }

            IDAsyncRunnable runnable = starter(boxedStateMachine);
            return new DAsyncLink(parentId, runnable);
        }

        public DAsyncLink Hydrate(Exception exception, IDAsyncMarshaler marshaler)
        {
            DehydrateAwaiter(boxedStateMachine, exception);
            foreach (FieldHydrator fieldHydrator in fieldHydrators)
            {
                fieldHydrator(boxedStateMachine, marshaler);
            }

            IDAsyncRunnable runnable = starter(boxedStateMachine);
            return new DAsyncLink(parentId, runnable);
        }

        public static DehydratedRunnable Create<TStateMachine>(DAsyncId parentId, object boxedStateMachine, ISuspensionContext suspensionContext, IDAsyncMarshaler marshaler)
            where TStateMachine : notnull
        {
            Type stateMachineType = typeof(TStateMachine);
            FieldInfo[] fields = stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            List<FieldHydrator> fieldHydrators = [];
            StateMachineStarter? starter = null;
            FieldInfo? awaiterField = null;
            int? state = null;

            foreach (FieldInfo field in fields)
            {
                if (StateMachineFacts.IsBuilderField(field))
                {
                    if (starter is not null)
                        throw FailException.ForFailure($"Multiple async method builder fields found on type '{typeof(TStateMachine).Name}'.");

                    MethodInfo coreMethod = s_createStateMachineStarterGenericMethod.MakeGenericMethod(typeof(TStateMachine), field.FieldType);
                    starter = (StateMachineStarter)coreMethod.Invoke(null, [field, boxedStateMachine, marshaler])!;
                }
                else if (StateMachineFacts.IsAwaiterField(field))
                {
                    MethodInfo isSuspendedMethod = s_isSuspendedGenericMethod.MakeGenericMethod(field.FieldType);
                    bool isSuspended = (bool)isSuspendedMethod.Invoke(null, [boxedStateMachine, field, suspensionContext])!;

                    if (!isSuspended)
                        continue;

                    if (awaiterField is not null)
                        throw FailException.ForFailure($"Multiple suspended awaiters fouund on type '{typeof(TStateMachine).Name}'.");

                    awaiterField = field;
                }
                else
                {
                    if (StateMachineFacts.IsStateField(field))
                    {
                        state = (int?)field.GetValue(boxedStateMachine);
                    }

                    MethodInfo createFieldHydratorMethod = s_createFieldHydratorGenericMethod.MakeGenericMethod(typeof(TStateMachine), field.FieldType);
                    FieldHydrator hydrator = (FieldHydrator)createFieldHydratorMethod.Invoke(null, [marshaler, boxedStateMachine, field])!;
                    fieldHydrators.Add(hydrator);
                }
            }

            if (starter is null)
                throw FailException.ForFailure($"No async method builder fields found on type '{typeof(TStateMachine).Name}'.");

            if (awaiterField is null)
            {
                if (state is not -1)
                    throw FailException.ForFailure($"No suspender awaiter found on type '{typeof(TStateMachine).Name}'.");

                return new PendingDehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, starter);
            }

            Type suspendedAwaiterType = awaiterField.FieldType;
            if (suspendedAwaiterType == typeof(DTask.Awaiter))
                return new DTaskAwaiterDehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, awaiterField, starter);

            if (suspendedAwaiterType.IsGenericType && suspendedAwaiterType.GetGenericTypeDefinition() == typeof(DTask<>.Awaiter))
            {
                Type dehydratedRunnableType = typeof(DTaskAwaiterDehydratedRunnable<>).MakeGenericType(suspendedAwaiterType.GenericTypeArguments);
                return (DehydratedRunnable)Activator.CreateInstance(dehydratedRunnableType, [parentId, boxedStateMachine, fieldHydrators, awaiterField, starter])!;
            }

            if (suspendedAwaiterType == typeof(YieldDAwaitable.Awaiter))
                return new YieldAwaiterDehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, starter);

            MethodInfo? fromVoidResultMethod = GetFromVoidResultMethod(suspendedAwaiterType);
            Dictionary<Type, MethodInfo> fromResultMethods = GetFromResultMethods(suspendedAwaiterType);
            MethodInfo? fromExceptionMethod = GetFromExceptionMethod(suspendedAwaiterType);

            return new OtherAwaiterDehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, awaiterField, fromVoidResultMethod, fromResultMethods, fromExceptionMethod, starter);
        }

        private static bool IsSuspended<TAwaiter>(object boxedStateMachine, FieldInfo awaiterField, ISuspensionContext suspensionContext)
        {
            TAwaiter awaiter = (TAwaiter)awaiterField.GetValue(boxedStateMachine)!;
            return suspensionContext.IsSuspended(ref awaiter);
        }

        private static FieldHydrator CreateFieldHydrator<TStateMachine, TField>(IDAsyncMarshaler marshaler, object boxedStateMachine, FieldInfo field)
            where TStateMachine : notnull
        {
            TField? value = (TField?)field.GetValue(boxedStateMachine);
            MarshalingAction<TField> action = new(field);
            _ = marshaler.TryMarshal(field.Name, in value, action);

            return action.Hydrator;
        }

        private static StateMachineStarter CreateStateMachineStarter<TStateMachine, TBuilder>(FieldInfo builderField, object boxedStateMachine, IDAsyncMarshaler marshaler)
        {
            Type stateMachineType = typeof(TStateMachine);
            Type builderType = typeof(TBuilder);

            MethodInfo createMethod = GetCreateMethod(builderType);
            MethodInfo startMethod = GetStartMethod(builderType, stateMachineType);
            MethodInfo taskGetter = GetTaskGetter(builderType);
            if (startMethod.IsGenericMethod)
            {
                startMethod = startMethod.MakeGenericMethod(stateMachineType);
            }

            StartAction<TBuilder, TStateMachine> start = startMethod.CreateDelegate<StartAction<TBuilder, TStateMachine>>();

            return delegate (object boxedStateMachine)
            {
                TBuilder builder = (TBuilder)createMethod.Invoke(null, null)!;

                builderField.SetValue(boxedStateMachine, builder);
                TStateMachine stateMachine = (TStateMachine)boxedStateMachine;
                start(ref builder, ref stateMachine);
                builderField.SetValue(boxedStateMachine, builder);

                return (IDAsyncRunnable)taskGetter.Invoke(builder, null)!;
            };
        }

        private static MethodInfo GetCreateMethod(Type builderType)
        {
            return builderType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public, []) ?? throw new MissingMethodException($"No 'Start' method found on type '{builderType.Name}'.");
        }

        private static MethodInfo GetStartMethod(Type builderType, Type stateMachineType)
        {
            MethodInfo? result = null;

            foreach (MethodInfo method in builderType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.Name != "Start")
                    continue;

                Type[] genericParameters = method.GetGenericArguments();
                int arity = genericParameters.Length;
                if (arity is not 0 and not 1)
                    continue;

                if (method.GetParameters() is not [ParameterInfo parameter])
                    continue;

                Type parameterType = parameter.ParameterType;
                if (!parameterType.IsByRef)
                    continue;

                Type elementType = parameterType.GetElementType()!;
                if (elementType != stateMachineType && (arity == 0 || elementType != genericParameters[0]))
                    continue;

                if (result is not null)
                    throw new AmbiguousMatchException($"Multiple 'Start' methods found on type '{builderType.Name}'.");

                result = method;
            }

            return result ?? throw new MissingMethodException($"No 'Start' method found on type '{builderType.Name}'.");
        }

        private static MethodInfo GetTaskGetter(Type builderType)
        {
            MethodInfo? getter = builderType.GetMethod("get_Task", BindingFlags.Instance | BindingFlags.Public);
            if (getter is null || !getter.ReturnType.IsAssignableTo(typeof(IDAsyncRunnable)))
                throw new MissingMethodException($"No Task getter found on type '{builderType.Name}'.");

            return getter;
        }

        private static MethodInfo? GetFromVoidResultMethod(Type awaiterType)
        {
            return awaiterType.GetMethod("FromResult", BindingFlags.Static | BindingFlags.Public, []);
        }

        private static MethodInfo? GetFromExceptionMethod(Type awaiterType)
        {
            return awaiterType.GetMethod("FromException", BindingFlags.Static | BindingFlags.Public, [typeof(Exception)]);
        }

        private static Dictionary<Type, MethodInfo> GetFromResultMethods(Type awaiterType)
        {
            Dictionary<Type, MethodInfo> result = [];

            foreach (MethodInfo method in awaiterType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (method.Name != "FromResult")
                    continue;

                if (method.GetParameters() is not [{ ParameterType: Type parameterType }])
                    continue;

                Type[] genericArguments = method.GetGenericArguments();
                switch (genericArguments.Length)
                {
                    case 0:
                        result.Add(parameterType, method);
                        break;

                    case 1:
                        if (parameterType != genericArguments[0])
                            break;

                        result.Add(s_genericParameterType, method);
                        break;
                }
            }

            return result;
        }

        private sealed class PendingDehydratedRunnable(
            DAsyncId parentId,
            object boxedStateMachine,
            IEnumerable<FieldHydrator> fieldHydrators,
            StateMachineStarter starter) : DehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, starter)
        {
            protected override void DehydrateAwaiter(object boxedStateMachine)
            {
            }

            protected override void DehydrateAwaiter<TResult>(object boxedStateMachine, TResult result)
            {
                throw FailException.ForFailure("Cannot resume a pending runnable with a result.");
            }

            protected override void DehydrateAwaiter(object boxedStateMachine, Exception exception)
            {
                throw FailException.ForFailure("Cannot resume a pending runnable with an exception.");
            }
        }

        private sealed class OtherAwaiterDehydratedRunnable(
            DAsyncId parentId,
            object boxedStateMachine,
            IEnumerable<FieldHydrator> fieldHydrators,
            FieldInfo awaiterField,
            MethodInfo? fromVoidResultMethod,
            Dictionary<Type, MethodInfo> fromResultMethods,
            MethodInfo? fromExceptionMethod,
            StateMachineStarter starter) : DehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, starter)
        {
            protected override void DehydrateAwaiter(object boxedStateMachine)
            {
                if (fromVoidResultMethod is null)
                    throw FailException.ForFailure($"'{awaiterField.FieldType.Name}' does not support resuming without a result.");

                object? awaiter = fromVoidResultMethod.Invoke(null, null);
                awaiterField.SetValue(boxedStateMachine, awaiter);
            }

            protected override void DehydrateAwaiter<TResult>(object boxedStateMachine, TResult result)
            {
                object? awaiter;
                if (fromResultMethods.TryGetValue(typeof(TResult), out MethodInfo? fromResultMethod))
                {
                    awaiter = fromResultMethod.Invoke(null, [result]);
                }
                else if (fromResultMethods.TryGetValue(s_genericParameterType, out MethodInfo? fromResultGenericMethod))
                {
                    awaiter = fromResultGenericMethod.MakeGenericMethod(typeof(TResult)).Invoke(null, [result]);
                }
                else
                {
                    throw FailException.ForFailure($"'{awaiterField.FieldType.Name}' does not support resuming with result of type '{typeof(TResult)}'.");
                }

                awaiterField.SetValue(boxedStateMachine, awaiter);
            }

            protected override void DehydrateAwaiter(object boxedStateMachine, Exception exception)
            {
                if (fromExceptionMethod is null)
                    throw FailException.ForFailure($"'{awaiterField.FieldType.Name}' does not support resuming with an exception.");

                object? awaiter = fromExceptionMethod.Invoke(null, [exception]);
                awaiterField.SetValue(boxedStateMachine, awaiter);
            }
        }

        private sealed class DTaskAwaiterDehydratedRunnable(
            DAsyncId parentId,
            object boxedStateMachine,
            IEnumerable<FieldHydrator> fieldHydrators,
            FieldInfo awaiterField,
            StateMachineStarter starter) : DehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, starter)
        {
            protected override void DehydrateAwaiter(object boxedStateMachine)
            {
                DTask.Awaiter awaiter = DTask.CompletedDTask.GetAwaiter();
                awaiterField.SetValue(boxedStateMachine, awaiter);
            }

            protected override void DehydrateAwaiter<TResult>(object boxedStateMachine, TResult result)
            {
                DTask.Awaiter awaiter = DTask.CompletedDTask.GetAwaiter();
                awaiterField.SetValue(boxedStateMachine, awaiter);
            }

            protected override void DehydrateAwaiter(object boxedStateMachine, Exception exception)
            {
                DTask.Awaiter awaiter = DTask.FromException(exception).GetAwaiter();
                awaiterField.SetValue(boxedStateMachine, awaiter);
            }
        }

        private sealed class DTaskAwaiterDehydratedRunnable<TResult>(
            DAsyncId parentId,
            object boxedStateMachine,
            IEnumerable<FieldHydrator> fieldHydrators,
            FieldInfo awaiterField,
            StateMachineStarter starter) : DehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, starter)
        {
            protected override void DehydrateAwaiter(object boxedStateMachine)
            {
                DTask.Awaiter awaiter = DTask.CompletedDTask.GetAwaiter();
                awaiterField.SetValue(boxedStateMachine, awaiter);
            }

            protected override void DehydrateAwaiter<TActualResult>(object boxedStateMachine, TActualResult actualResult)
            {
                if (actualResult is not TResult result)
                    throw FailException.ForFailure($"Invalid result type. Expected a type assignable to '{typeof(TResult)}'.");

                DTask<TResult>.Awaiter awaiter = DTask.FromResult(result).GetAwaiter();
                awaiterField.SetValue(boxedStateMachine, awaiter);
            }

            protected override void DehydrateAwaiter(object boxedStateMachine, Exception exception)
            {
                DTask<TResult>.Awaiter awaiter = DTask<TResult>.FromException(exception).GetAwaiter();
                awaiterField.SetValue(boxedStateMachine, awaiter);
            }
        }

        private sealed class YieldAwaiterDehydratedRunnable(
            DAsyncId parentId,
            object boxedStateMachine,
            IEnumerable<FieldHydrator> fieldHydrators,
            StateMachineStarter starter) : DehydratedRunnable(parentId, boxedStateMachine, fieldHydrators, starter)
        {
            protected override void DehydrateAwaiter(object boxedStateMachine)
            {
            }

            protected override void DehydrateAwaiter<TResult>(object boxedStateMachine, TResult result)
            {
                throw FailException.ForFailure("DTask.Yield should be resumed without a result.");
            }

            protected override void DehydrateAwaiter(object boxedStateMachine, Exception exception)
            {
                throw FailException.ForFailure("DTask.Yield cannot be resumed with an exception.");
            }
        }

        private class MarshalingAction<T>(FieldInfo field) : IMarshalingAction
        {
            private FieldHydrator? _hydrator;

            public FieldHydrator Hydrator => _hydrator ?? static delegate (object boxedStateMachine, IDAsyncMarshaler marshaler) { };

            public void MarshalAs<TToken>(TypeId typeId, TToken token)
            {
                _hydrator = delegate (object boxedStateMachine, IDAsyncMarshaler marshaler)
                {
                    UnmarshalingAction<TToken, T> action = new(token);
                    if (!marshaler.TryUnmarshal<T>(field.Name, typeId, action))
                        throw FailException.ForFailure("Expected the marshaler to be able to unmarshal its own token.");

                    field.SetValue(boxedStateMachine, action.Value);
                };
            }
        }

        private class UnmarshalingAction<TToken, T>(TToken token) : IUnmarshalingAction
        {
            public T? Value;

            public void UnmarshalAs<TConverter>(Type tokenType, ref TConverter converter)
                where TConverter : struct, ITokenConverter
            {
                Value = converter.Convert<TToken, T>(token);
            }
        }
    }
}