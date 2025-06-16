using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal sealed class StateMachineDescriptor(
    Type type,
    ConstructorInfo? constructor,
    IReadOnlyCollection<FieldInfo> userFields,
    IReadOnlyCollection<IndexedFieldInfo> awaiterFields,
    FieldInfo? refAwaiterField,
    FieldInfo builderField,
    MethodInfo builderCreateMethod,
    MethodInfo builderStartMethod,
    MethodInfo builderTaskGetter)
{
    private const string CreateMethodName = "Create";
    private const string StartMethodName = "Start";
    private const string TaskGetterName = "get_Task";
    
    public Type Type { get; } = type;

    [MemberNotNullWhen(false, nameof(Constructor))]
    public bool IsValueType => Type.IsValueType;

    public ConstructorInfo? Constructor { get; } = constructor;

    public IReadOnlyCollection<FieldInfo> UserFields { get; } = userFields;

    public IReadOnlyCollection<IndexedFieldInfo> AwaiterFields { get; } = awaiterFields;

    public FieldInfo? RefAwaiterField { get; } = refAwaiterField;

    public FieldInfo BuilderField { get; } = builderField;

    public MethodInfo BuilderCreateMethod { get; } = builderCreateMethod;

    public MethodInfo BuilderStartMethod { get; } = builderStartMethod;

    public MethodInfo BuilderTaskGetter { get; } = builderTaskGetter;

    public static StateMachineDescriptor Create(Type stateMachineType)
    {
        ConstructorInfo? constructor = stateMachineType.GetConstructor([]);
        if (constructor is null && !stateMachineType.IsValueType)
            throw new ArgumentException("The state machine type should have a parameterless constructor.", nameof(stateMachineType));

        List<FieldInfo> userFields = [];
        List<IndexedFieldInfo> awaiterFields = [];
        FieldInfo? refAwaiterField = null;
        FieldInfo? builderField = null;
        int awaiterIndex = 1;

        foreach (FieldInfo field in stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            StateMachineFieldKind fieldKind = StateMachineFacts.GetFieldKind(field);
            switch (fieldKind)
            {
                case StateMachineFieldKind.UserField:
                case StateMachineFieldKind.StateField:
                    userFields.Add(field);
                    break;

                case StateMachineFieldKind.DAsyncAwaiterField:
                    if (field.FieldType.IsValueType)
                    {
                        awaiterFields.Add(new(field, awaiterIndex));
                        awaiterIndex++;
                    }
                    else
                    {
                        if (field.FieldType != typeof(object) || refAwaiterField is not null)
                            throw new ArgumentException("The provided state machine layout is not supported.", nameof(stateMachineType));

                        awaiterFields.Add(new(field, -1));
                        refAwaiterField = field;
                    }

                    break;

                case StateMachineFieldKind.BuilderField:
                    if (builderField is not null)
                        throw new ArgumentException("A state machine should have a single builder field.", nameof(stateMachineType));

                    builderField = field;
                    break;
            }
        }

        if (builderField is null)
            throw new ArgumentException("A state machine should have one builder field.", nameof(stateMachineType));

        // TODO: Create a BuilderDescriptor and check return types
        MethodInfo? createMethod = builderField.FieldType.GetMethod(
            name: CreateMethodName,
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Static | BindingFlags.Public,
            binder: null,
            types: [],
            modifiers: []);

        if (createMethod is null)
            throw new ArgumentException("The builder of a state machine should have a public static 'Create' method.", nameof(stateMachineType));

        MethodInfo? startMethod = builderField.FieldType.GetMethod(
            name: StartMethodName,
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [stateMachineType.MakeByRefType()],
            modifiers: []);
        
        startMethod ??= builderField.FieldType.GetMethod(
            name: StartMethodName,
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [Type.MakeGenericMethodParameter(0).MakeByRefType()],
            modifiers: []);

        if (startMethod is null)
            throw new ArgumentException("The builder of a state machine should have a public 'Start' method.", nameof(stateMachineType));

        if (startMethod.IsGenericMethod)
        {
            startMethod = startMethod.MakeGenericMethod(stateMachineType);
        }

        MethodInfo? taskGetter = builderField.FieldType.GetMethod(
            name: TaskGetterName,
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [],
            modifiers: []);

        if (taskGetter is null)
            throw new ArgumentException("The builder of a state machine should have a public 'Task' property.", nameof(stateMachineType));

        return new StateMachineDescriptor(
            stateMachineType,
            constructor,
            userFields,
            awaiterFields,
            refAwaiterField,
            builderField,
            createMethod,
            startMethod,
            taskGetter);
    }
}
