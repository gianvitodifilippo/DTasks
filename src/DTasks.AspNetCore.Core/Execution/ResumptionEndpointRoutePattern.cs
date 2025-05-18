using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing.Patterns;

namespace DTasks.AspNetCore.Execution;

public readonly struct ResumptionEndpointRoutePattern
{
    public ResumptionEndpointRoutePattern(RoutePattern value)
    {
        if (!IsValid(value, out string? validationError))
            throw new ArgumentException(validationError, nameof(value));
        
        Value = value;
    }

    public RoutePattern Value { get; }

    public static ResumptionEndpointRoutePattern Parse(string pattern) => new(RoutePatternFactory.Parse(pattern));

    public static ResumptionEndpointRoutePattern<TResult> Parse<TResult>(string pattern) => new(RoutePatternFactory.Parse(pattern));

    public static implicit operator ResumptionEndpointRoutePattern(RoutePattern value) => new(value);
    
    public static implicit operator ResumptionEndpointRoutePattern(string pattern) => new(RoutePatternFactory.Parse(pattern));

    internal static bool IsValid(RoutePattern value, [NotNullWhen(false)] out string? validationError)
    {
        IReadOnlyList<RoutePatternParameterPart> parameters = value.Parameters;

        if (parameters.Count != 1)
        {
            validationError = "Route must have exactly one parameter.";
            return false;
        }

        var parameter = parameters[0];

        if (!string.Equals(parameter.Name, "operationId", StringComparison.Ordinal))
        {
            validationError = "Route parameter must be named 'operationId'.";
            return false;
        }

        if (parameter.IsOptional)
        {
            validationError = "Route parameter 'operationId' cannot be optional.";
            return false;
        }

        if (parameter.Default != null)
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

public readonly struct ResumptionEndpointRoutePattern<TResult>
{
    public ResumptionEndpointRoutePattern(RoutePattern value)
    {
        if (!ResumptionEndpointRoutePattern.IsValid(value, out string? validationError))
            throw new ArgumentException(validationError, nameof(value));
        
        Value = value;
    }

    public RoutePattern Value { get; }

    public static ResumptionEndpointRoutePattern<TResult> Parse(string pattern) => new(RoutePatternFactory.Parse(pattern));

    public static implicit operator ResumptionEndpointRoutePattern<TResult>(RoutePattern value) => new(value);
    
    public static implicit operator ResumptionEndpointRoutePattern<TResult>(string pattern) => new(RoutePatternFactory.Parse(pattern));
}
