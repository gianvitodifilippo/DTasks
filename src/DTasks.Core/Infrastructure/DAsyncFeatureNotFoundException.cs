using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public class DAsyncFeatureNotFoundException : Exception
{
    private static readonly string s_defaultMessage = "The required feature was not found.";
    
    public DAsyncFeatureNotFoundException()
        : base(s_defaultMessage)
    {
    }

    public DAsyncFeatureNotFoundException(string? message)
        : base(message ?? s_defaultMessage)
    {
    }

    public DAsyncFeatureNotFoundException(string? message, Exception? innerException)
        : base(message ?? s_defaultMessage, innerException)
    {
    }

    public DAsyncFeatureNotFoundException(Type featureType)
        : base($"The required feature '{featureType.Name}' was not found.")
    {
        FeatureType = featureType;
    }
    
    public Type? FeatureType { get; }
}