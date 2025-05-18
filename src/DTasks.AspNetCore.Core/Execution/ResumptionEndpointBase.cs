using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace DTasks.AspNetCore.Execution;

public abstract class ResumptionEndpointBase : IResumptionEndpoint
{
    private const int DAsyncIdCharCount = 16;
    private static readonly int s_operationIdPlaceholderLength = DTasksHttpConstants.OperationIdParameterName.Length + 2;

    private readonly int _callbackPathLength;
    private readonly int _operationIdIndex;
    
    protected ResumptionEndpointBase([StringSyntax("Route")] string pattern)
    {
        if (!IsValid(pattern, out string? validationError))
            throw new ArgumentException(validationError, nameof(pattern));

        _operationIdIndex = pattern.IndexOf(DTasksHttpConstants.OperationIdParameterName, StringComparison.Ordinal) - 1;
        _callbackPathLength = pattern.Length - s_operationIdPlaceholderLength + DAsyncIdCharCount;
        Pattern = pattern;
        
        Debug.Assert(_operationIdIndex >= 0);
    }
    
    public string Pattern { get; }
    
    protected abstract Task ResumeAsync(HttpContext httpContext);

    public string MakeCallbackUrl(string basePath, DAsyncId operationId)
    {
        int resultLength = basePath.Length + _callbackPathLength;

        return string.Create(resultLength, (this, basePath, operationId), static (span, state) =>
        {
            (ResumptionEndpointBase self, string basePath, DAsyncId operationId) = state;
            int offset;

            basePath.AsSpan().CopyTo(span);
            offset = basePath.Length;
            
            self.Pattern.AsSpan(0, self._operationIdIndex).CopyTo(span[offset..]);
            offset += self._operationIdIndex;

            bool operationIdWriteResult = operationId.TryWriteChars(span[offset..]);
            offset += DAsyncIdCharCount;
            Debug.Assert(operationIdWriteResult);

            self.Pattern.AsSpan(self._operationIdIndex + s_operationIdPlaceholderLength).CopyTo(span[offset..]);
        });
    }

    public string MakeCallbackPath(DAsyncId operationId)
    {
        if (operationId == default)
            throw new ArgumentException("Invalid operation id.", nameof(operationId));
        
        return string.Create(_callbackPathLength, (this, operationId), static (span, state) =>
        {
            (ResumptionEndpointBase self, DAsyncId operationId) = state;
            int offset;
            
            self.Pattern.AsSpan(0, self._operationIdIndex).CopyTo(span);
            offset = self._operationIdIndex;

            bool operationIdWriteResult = operationId.TryWriteChars(span[offset..]);
            offset += DAsyncIdCharCount;
            Debug.Assert(operationIdWriteResult);

            self.Pattern.AsSpan(self._operationIdIndex + s_operationIdPlaceholderLength).CopyTo(span[offset..]);
        });
    }

    Task IResumptionEndpoint.ResumeAsync(HttpContext httpContext) => ResumeAsync(httpContext);

    private static bool IsValid(string patternValue, [NotNullWhen(false)] out string? validationError)
    {
        RoutePattern pattern = RoutePatternFactory.Parse(patternValue);
        IReadOnlyList<RoutePatternParameterPart> parameters = pattern.Parameters;

        if (parameters.Count != 1)
        {
            validationError = "Route must have exactly one parameter.";
            return false;
        }

        var parameter = parameters[0];

        if (parameter.Name != DTasksHttpConstants.OperationIdParameterName)
        {
            validationError = "Route parameter must be named 'operationId'.";
            return false;
        }

        if (parameter.IsOptional)
        {
            validationError = "Route parameter 'operationId' cannot be optional.";
            return false;
        }

        if (parameter.Default is not null)
        {
            validationError = "Route parameter 'operationId' must not have a default value.";
            return false;
        }

        if (parameter.IsCatchAll)
        {
            validationError = "Route parameter 'operationId' must not be a catch-all.";
            return false;
        }

        validationError = null;
        return true;
    }
}
