using System.Reflection;

namespace DTasks.Inspection;

internal readonly struct StateMachineDescriptor
{
    private readonly IEnumerable<FieldInfo> _fields;

    private StateMachineDescriptor(Type type, ConstructorInfo constructor, IEnumerable<FieldInfo> fields)
    {
        Type = type;
        Constructor = constructor;
        _fields = fields;
    }

    public Type Type { get; }

    public ConstructorInfo Constructor { get; }

    public IEnumerable<FieldInfo> UserFields => _fields.Where(IsUserField);

    public IEnumerable<FieldInfo> AwaiterFields => _fields.Where(IsAwaiterField);

    public FieldInfo StateField => _fields.First(IsStateField);

    public FieldInfo BuilderField => _fields.First(IsBuilderField);

    private static bool IsUserField(FieldInfo field) =>
        !field.Name.StartsWith("<>u") &&
        !field.Name.StartsWith("<>4") &&
        !IsStateField(field) &&
        !IsBuilderField(field);

    private static bool IsAwaiterField(FieldInfo field) => field.Name.StartsWith("<>u") && IsDTaskAwaiterType(field.FieldType);

    private static bool IsStateField(FieldInfo field) => field.Name == "<>1__state";

    private static bool IsBuilderField(FieldInfo field) => field.Name == "<>t__builder";

    private static bool IsDTaskAwaiterType(Type awaiterType) =>
        awaiterType == typeof(DTask.Awaiter) ||
        awaiterType.IsGenericType && awaiterType.GetGenericTypeDefinition() == typeof(DTask<>.Awaiter);

    public static StateMachineDescriptor Create(Type stateMachineType)
    {
        ConstructorInfo? constructor = stateMachineType.GetConstructor([]);
        if (constructor is null)
            throw new ArgumentException("The state machine type should have a parameterless constructor.", nameof(stateMachineType));

        FieldInfo[] fields = stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return new StateMachineDescriptor(stateMachineType, constructor, fields);
    }
}
