using System.Reflection;

namespace DTasks.Inspection;

internal static class StateMachineFacts
{
    public static bool IsBuilderField(FieldInfo field) => field.Name == "<>t__builder" || field.IsDefined(typeof(DAsyncRunnableBuilderFieldAttribute));

    public static bool IsStateField(FieldInfo field) => field.Name == "<>1__state";

    public static bool IsAwaiterField(FieldInfo field) => field.Name.StartsWith("<>u") || field.IsDefined(typeof(DAsyncAwaiterFieldAttribute));
}
