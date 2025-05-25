using System.ComponentModel;

namespace DTasks.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class MarshalableAttribute : Attribute;
