using System.ComponentModel;

namespace DTasks.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ConfigurationBuilderAttribute : Attribute;
