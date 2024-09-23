using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Utils;

[ExcludeFromCodeCoverage]
internal class ParameterDelegator(ParameterInfo parameter) : ParameterInfo
{
    protected readonly ParameterInfo _parameter = parameter;

    public override ParameterAttributes Attributes => _parameter.Attributes;

    public override IEnumerable<CustomAttributeData> CustomAttributes => _parameter.CustomAttributes;

    public override object DefaultValue => _parameter.DefaultValue;

    public override bool HasDefaultValue => _parameter.HasDefaultValue;

    public override MemberInfo Member => _parameter.Member;

    public override int MetadataToken => _parameter.MetadataToken;

    public override string Name => _parameter.Name;

    public override Type ParameterType => _parameter.ParameterType;

    public override int Position => _parameter.Position;

    public override object RawDefaultValue => _parameter.RawDefaultValue;

    public override object[] GetCustomAttributes(bool inherit) => _parameter.GetCustomAttributes(inherit);

    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _parameter.GetCustomAttributes(attributeType, inherit);

    public override IList<CustomAttributeData> GetCustomAttributesData() => _parameter.GetCustomAttributesData();

    public override Type[] GetOptionalCustomModifiers() => _parameter.GetOptionalCustomModifiers();

    public override Type[] GetRequiredCustomModifiers() => _parameter.GetRequiredCustomModifiers();

    public override bool IsDefined(Type attributeType, bool inherit) => _parameter.IsDefined(attributeType, inherit);

    public override bool Equals(object obj) => _parameter.Equals(obj);

    public override int GetHashCode() => _parameter.GetHashCode();

    public override string ToString() => _parameter.ToString();
}
