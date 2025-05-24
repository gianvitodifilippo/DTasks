namespace DTasks.AspNetCore.Metadata;

[AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = true)]
public sealed class AsyncResultAttribute : Attribute
{
    public AsyncResultAttribute(Type resultType)
    {
        ResultType = resultType;
    }

    public AsyncResultAttribute(int genericParameterPosition)
    {
        ResultType = Type.MakeGenericMethodParameter(genericParameterPosition);
    }

    public Type ResultType { get; }
}