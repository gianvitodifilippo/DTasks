using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DTasks.Inspection;

internal readonly struct StateMachineDescriptor
{
    private const string AwaiterFieldPrefix = "<>u";
    private const string StateFieldName = "<>1__state";
    private const string BuilderFieldName = "<>t__builder";

    private readonly IEnumerable<FieldInfo> _fields;

    private StateMachineDescriptor(Type type, ConstructorInfo? constructor, IEnumerable<FieldInfo> fields)
    {
        Type = type;
        Constructor = constructor;
        _fields = fields;
    }

    public Type Type { get; }

    public ConstructorInfo? Constructor { get; }

    [MemberNotNullWhen(false, nameof(Constructor))]
    public bool IsValueType => Type.IsValueType;

    public IEnumerable<FieldInfo> UserFields => _fields.Where(IsUserField);

    public IEnumerable<FieldInfo> AwaiterFields => _fields.Where(IsAwaiterField);

    public FieldInfo StateField => _fields.First(IsStateField);

    public FieldInfo BuilderField => _fields.First(IsBuilderField);

    private static bool IsUserField(FieldInfo field) =>
        !field.Name.StartsWith(AwaiterFieldPrefix) &&
        !IsStateField(field) &&
        !IsBuilderField(field);

    private static bool IsAwaiterField(FieldInfo field) => field.Name.StartsWith(AwaiterFieldPrefix) && IsDTaskAwaiterType(field.FieldType);

    private static bool IsStateField(FieldInfo field) => field.Name == StateFieldName;

    private static bool IsBuilderField(FieldInfo field) => field.Name == BuilderFieldName;

    private static bool IsDTaskAwaiterType(Type awaiterType) =>
        awaiterType == typeof(DTask.Awaiter) ||
        awaiterType.IsGenericType && awaiterType.GetGenericTypeDefinition() == typeof(DTask<>.Awaiter);

    public static StateMachineDescriptor Create(Type stateMachineType)
    {
        ConstructorInfo? constructor = stateMachineType.GetConstructor([]);
        if (constructor is null && !stateMachineType.IsValueType)
            throw new ArgumentException("The state machine type should have a parameterless constructor.", nameof(stateMachineType));

        FieldInfo[] fields = stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return new StateMachineDescriptor(stateMachineType, constructor, fields);
    }
}
