using System.ComponentModel;

namespace DTasks.Inspection;

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Field)]
public sealed class DAsyncRunnableBuilderFieldAttribute : Attribute;
