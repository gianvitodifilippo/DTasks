using DTasks.Hosting;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection;

public sealed class StateMachineInspector : IStateMachineInspector
{
    private readonly ISuspenderDescriptor _suspenderDescriptor;
    private readonly IResumerDescriptor _resumerDescriptor;
    private readonly ConcurrentDictionary<Type, Delegate> _suspenders;
    private readonly ConcurrentDictionary<Type, Delegate> _resumers;

    internal StateMachineInspector(ISuspenderDescriptor suspenderDescriptor, IResumerDescriptor resumerDescriptor)
    {
        _suspenderDescriptor = suspenderDescriptor;
        _resumerDescriptor = resumerDescriptor;
        _suspenders = [];
        _resumers = [];
    }

    public Delegate GetSuspender(Type stateMachineType)
    {
        return _suspenders.GetOrAdd(stateMachineType, MakeSuspender, this);

        static Delegate MakeSuspender(Type stateMachineType, StateMachineInspector self)
            => self.MakeSuspender(stateMachineType);
    }

    public Delegate GetResumer(Type stateMachineType)
    {
        return _resumers.GetOrAdd(stateMachineType, MakeResumer, this);

        static Delegate MakeResumer(Type stateMachineType, StateMachineInspector self)
            => self.MakeResumer(stateMachineType);
    }

    private Delegate MakeSuspender(Type stateMachineType)
    {
        var stateMachineDescriptor = StateMachineDescriptor.Create(stateMachineType);
        var deconstructorDescriptor = _suspenderDescriptor.DeconstructorDescriptor;
        var deconstructorParameter = _suspenderDescriptor.DeconstructorParameter;

        Type[] parameterTypes = [
            stateMachineType.MakeByRefType(),
            typeof(IStateMachineInfo),
            deconstructorParameter.ParameterType
        ];

        var method = new DynamicMethod($"Suspend{stateMachineType.Name}", typeof(void), parameterTypes);
        method.DefineParameter(1, ParameterAttributes.None, "stateMachine");
        method.DefineParameter(2, ParameterAttributes.None, "info");
        method.DefineParameter(3, deconstructorParameter.Attributes, "deconstructor");

        var il = InspectorILGenerator.Create(method, stateMachineDescriptor, deconstructorDescriptor.Type, deconstructorParameter.ParameterType);

        foreach (FieldInfo userField in stateMachineDescriptor.UserFields)
        {
            MethodInfo handleFieldMethod = deconstructorDescriptor.GetHandleFieldMethod(userField.FieldType);

            // deconstructor.HandleXXX($fieldName, stateMachine.$userField);
            il.LoadDeconstructor();                 // Stack: deconstructor
            il.LoadString(userField.Name);          // Stack: deconstructor, $fieldName
            il.LoadStateMachineArg();               // Stack: deconstructor, $fieldName, stateMachine
            il.LoadField(userField);                // Stack: deconstructor, $fieldName, stateMachine.$userField
            il.CallHandleMethod(handleFieldMethod); // Stack: -
        }

        FieldInfo stateField = stateMachineDescriptor.StateField;

        // deconstructor.HandleState("<>1__state", stateMachine.<>1__state);
        il.LoadDeconstructor();                                         // Stack: deconstructor
        il.LoadString(stateField.Name);                                 // Stack: deconstructor, "<>1__state"
        il.LoadStateMachineArg();                                       // Stack: deconstructor, "<>1__state", stateMachine
        il.LoadField(stateField);                                       // Stack: deconstructor, "<>1__state", stateMachine.<>1__state
        il.CallHandleMethod(deconstructorDescriptor.HandleStateMethod); // Stack: -

        Label ifFalseLabel = default;
        bool wasLabelDefined = false;
        foreach (FieldInfo awaiterField in stateMachineDescriptor.AwaiterFields)
        {
            if (wasLabelDefined)
            {
                // return;
                il.Return();                // Stack: -
                il.MarkLabel(ifFalseLabel); // Stack: -
            }

            ifFalseLabel = il.DefineLabel();
            wasLabelDefined = true;

            // if (IsSuspended<TAwaiter>(info))
            il.LoadStateMachineInfo();                        // Stack: info
            il.CallIsSuspendedMethod(awaiterField.FieldType); // Stack: @result[IsSuspended]
            il.BranchIfFalse(ifFalseLabel);                   // Stack: -

            // deconstructor.HandleAwaiter($awaiterField.Name);
            il.LoadDeconstructor();                                           // Stack: deconstructor
            il.LoadString(awaiterField.Name);                                 // Stack: deconstructor, $awaiterField.Name
            il.CallHandleMethod(deconstructorDescriptor.HandleAwaiterMethod); // Stack: -
        }

        if (wasLabelDefined) // check in case there are no awaiters
        {
            il.MarkLabel(ifFalseLabel);
        }

        il.Return(); // Stack: -

        return method.CreateDelegate(_suspenderDescriptor.DelegateType.MakeGenericType(stateMachineType));
    }

    private Delegate MakeResumer(Type stateMachineType)
    {
        var stateMachineDescriptor = StateMachineDescriptor.Create(stateMachineType);
        var constructorDescriptor = _resumerDescriptor.ConstructorDescriptor;
        var constructorParameter = _resumerDescriptor.ConstructorParameter;

        Type[] parameterTypes = [typeof(DTask), constructorParameter.ParameterType];

        var method = new DynamicMethod($"Resume{stateMachineType.Name}", typeof(DTask), parameterTypes);
        method.DefineParameter(1, ParameterAttributes.None, "resultTask");
        method.DefineParameter(2, constructorParameter.Attributes, "constructor");

        var il = InspectorILGenerator.Create(method, stateMachineDescriptor, constructorDescriptor.Type, constructorParameter.ParameterType);
        il.DeclareStateMachineLocal();

        il.CreateStateMachine(); // Stack: -

        FieldInfo builderField = stateMachineDescriptor.BuilderField;

        // stateMachine.<>t__builder = AsyncTaskMethodBuilder<>.Create()
        il.LoadStateMachineLocal();                          // Stack: stateMachine
        il.CreateAsyncMethodBuilder(builderField.FieldType); // Stack: stateMachine, @result[AsyncDTaskMethodBuilder<>.Create]
        il.StoreField(builderField);                         // Stack: -

        foreach (FieldInfo userField in stateMachineDescriptor.UserFields)
        {
            MethodInfo handleFieldMethod = constructorDescriptor.GetHandleFieldMethod(userField.FieldType);

            // _ = constructor.HandleXXX($userField.Name, ref stateMachine.$userField);
            il.LoadConstructor();                   // Stack: constructor
            il.LoadString(userField.Name);          // Stack: constructor, $userField.Name
            il.LoadStateMachineLocal();             // Stack: constructor, $userField.Name, stateMachine
            il.LoadFieldAddress(userField);         // Stack: constructor, $userField.Name, &stateMachine.$userField
            il.CallHandleMethod(handleFieldMethod); // Stack: @result[HandleXXX]
            il.Pop();                               // Stack: -
        }

        FieldInfo stateField = stateMachineDescriptor.StateField;

        // _ = constructor.HandleState("<>1__state", ref stateMachine.<>1__state);
        il.LoadConstructor();                                         // constructor
        il.LoadString(stateField.Name);                               // constructor, "<>1__state"
        il.LoadStateMachineLocal();                                   // constructor, "<>1__state", stateMachine
        il.LoadFieldAddress(stateField);                              // constructor, "<>1__state", &stateMachine.$stateField
        il.CallHandleMethod(constructorDescriptor.HandleStateMethod); // @result[HandleState]
        il.Pop();                                                     // -

        Label ifFalseLabel = default;
        bool wasLabelDefined = false;
        foreach (FieldInfo awaiterField in stateMachineDescriptor.AwaiterFields)
        {
            if (wasLabelDefined)
            {
                il.MarkLabel(ifFalseLabel);
            }

            ifFalseLabel = il.DefineLabel();
            wasLabelDefined = true;

            // if (constructor.HandleAwaiter($awaiterField.Name))
            il.LoadConstructor();                                           // Stack: constructor
            il.LoadString(awaiterField.Name);                               // Stack: constructor, $awaiterField.Name
            il.CallHandleMethod(constructorDescriptor.HandleAwaiterMethod); // Stack: @result[HandleAwaiter]
            il.BranchIfFalse(ifFalseLabel);                                 // Stack: -

            // The following relies on DTaskAwaiter/DTaskAwaiter<T> having the same layout
            // stateMachine.$awaiterField = resultTask.GetAwaiter()
            il.LoadStateMachineLocal();  // Stack: stateMachine
            il.LoadResultTask();         // Stack: stateMachine, resultTask
            il.CallGetAwaiterMethod();   // Stack: stateMachine, @result[GetAwaiter]
            il.StoreField(awaiterField); // Stack: -
        }

        if (wasLabelDefined) // check in case there are no awaiters
        {
            il.MarkLabel(ifFalseLabel);
        }

        // stateMachine.<>t__builder.Start(ref stateMachine);
        il.LoadStateMachineLocal();                 // Stack: stateMachine
        il.LoadFieldAddress(builderField);          // Stack: stateMachine.<>t__builder
        il.LoadStateMachineLocalAddress();          // Stack: stateMachine.<>t__builder, &stateMachine
        il.CallStartMethod(builderField.FieldType); // Stack: -

        // return stateMachine.<>t__builder.Task;
        il.LoadStateMachineLocal();                // Stack: stateMachine
        il.LoadFieldAddress(builderField);         // Stack: stateMachine.<>t__builder
        il.CallTaskGetter(builderField.FieldType); // Stack: stateMachine.<>t__builder.Task
        il.Return();                               // Stack: -

        return method.CreateDelegate(_resumerDescriptor.DelegateType);
    }

    public static StateMachineInspector Create(Type suspenderType, Type resumerType)
    {
        if (!SuspenderDescriptor.TryCreate(suspenderType, out SuspenderDescriptor? suspenderDescriptor))
            throw new ArgumentException("Invalid suspender or deconstructor type.", nameof(suspenderType));

        if (!ResumerDescriptor.TryCreate(resumerType, out ResumerDescriptor? resumerDescriptor))
            throw new ArgumentException("Invalid resumer or constructor type.", nameof(resumerType));

        return new StateMachineInspector(suspenderDescriptor, resumerDescriptor);
    }
}
