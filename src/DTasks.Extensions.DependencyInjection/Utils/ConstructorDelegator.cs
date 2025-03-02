using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace DTasks.Extensions.DependencyInjection.Utils;

[ExcludeFromCodeCoverage]
internal class ConstructorDelegator(ConstructorInfo constructor) : ConstructorInfo
{
    protected readonly ConstructorInfo _constructor = constructor;

    public override MethodAttributes Attributes => _constructor.Attributes;

    public override Type? DeclaringType => _constructor.DeclaringType;

    public override bool IsConstructedGenericMethod => _constructor.IsConstructedGenericMethod;

    public override bool IsGenericMethod => _constructor.IsGenericMethod;

    public override bool IsGenericMethodDefinition => _constructor.IsGenericMethodDefinition;

    public override bool IsSecurityCritical => _constructor.IsSecurityCritical;

    public override bool IsSecuritySafeCritical => _constructor.IsSecuritySafeCritical;

    public override bool IsSecurityTransparent => _constructor.IsSecurityTransparent;

    public override MemberTypes MemberType => _constructor.MemberType;

    public override int MetadataToken => _constructor.MetadataToken;

    public override MethodImplAttributes MethodImplementationFlags => _constructor.MethodImplementationFlags;

    public override RuntimeMethodHandle MethodHandle => _constructor.MethodHandle;

    public override Module Module => _constructor.Module;

    public override string Name => _constructor.Name;

    public override Type? ReflectedType => _constructor.ReflectedType;

    public override object[] GetCustomAttributes(bool inherit) => _constructor.GetCustomAttributes(inherit);

    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _constructor.GetCustomAttributes(attributeType, inherit);

    public override MethodImplAttributes GetMethodImplementationFlags() => _constructor.GetMethodImplementationFlags();

    public override ParameterInfo[] GetParameters() => _constructor.GetParameters();

    public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) => _constructor.Invoke(invokeAttr, binder, parameters, culture);

    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) => _constructor.Invoke(obj, invokeAttr, binder, parameters, culture);

    public override bool IsDefined(Type attributeType, bool inherit) => _constructor.IsDefined(attributeType, inherit);

    public override CallingConventions CallingConvention => _constructor.CallingConvention;

    public override bool ContainsGenericParameters => _constructor.ContainsGenericParameters;

    public override IEnumerable<CustomAttributeData> CustomAttributes => _constructor.CustomAttributes;

    public override IList<CustomAttributeData> GetCustomAttributesData() => _constructor.GetCustomAttributesData();

    public override Type[] GetGenericArguments() => _constructor.GetGenericArguments();

    public override MethodBody? GetMethodBody() => _constructor.GetMethodBody();

    public override bool HasSameMetadataDefinitionAs(MemberInfo other) => _constructor.HasSameMetadataDefinitionAs(other);

    public override bool Equals(object? obj) => _constructor.Equals(obj);

    public override int GetHashCode() => _constructor.GetHashCode();

    public override string? ToString() => _constructor.ToString();
}
