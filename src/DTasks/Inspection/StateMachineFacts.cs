using DTasks.Infrastructure;
using System.Reflection;

namespace DTasks.Inspection;

internal static class StateMachineFacts
{
    public static StateMachineFieldKind GetFieldKind(FieldInfo field)
    {
        Type fieldType = field.FieldType;
        string fieldName = field.Name;

        if (fieldName.StartsWith("<>u"))
            return !fieldType.IsValueType || typeof(IDAsyncAwaiter).IsAssignableFrom(fieldType)
                ? StateMachineFieldKind.DAsyncAwaiterField
                : StateMachineFieldKind.AwaiterField;

        if (field.IsDefined(typeof(DAsyncAwaiterFieldAttribute)))
            return StateMachineFieldKind.DAsyncAwaiterField;

        if (fieldName == "<>t__builder" || field.IsDefined(typeof(DAsyncRunnableBuilderFieldAttribute)))
            return StateMachineFieldKind.BuilderField;

        if (fieldName == "<>1__state")
            return StateMachineFieldKind.StateField;

        return StateMachineFieldKind.UserField;
    }
}
