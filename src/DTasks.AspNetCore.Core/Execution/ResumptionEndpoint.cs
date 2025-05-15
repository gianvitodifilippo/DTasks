using System.Diagnostics.CodeAnalysis;
using DTasks.AspNetCore.Http;
using DTasks.AspNetCore.Infrastructure;
using DTasks.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.AspNetCore.Execution;

public sealed class ResumptionEndpoint([StringSyntax("Route")] string pattern) : ResumptionEndpointBase(pattern), IEquatable<ResumptionEndpoint>
{
    protected override async Task ResumeAsync(HttpContext httpContext)
    {
        string operationId = (string)httpContext.Request.RouteValues[DTasksHttpConstants.OperationIdParameterName]!;
        if (!DAsyncId.TryParse(operationId, out DAsyncId id))
        {
            await Results.NotFound().ExecuteAsync(httpContext);
            return;
        }

        var configuration = httpContext.RequestServices.GetRequiredService<DTasksConfiguration>();
        AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.Create(httpContext.RequestServices);
        await configuration.ResumeAsync(host, id, httpContext.RequestAborted);
    }
    
    public bool Equals([NotNullWhen(true)] ResumptionEndpoint? other) => Pattern == other?.Pattern;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ResumptionEndpoint other && Equals(other);

    public override int GetHashCode() => Pattern.GetHashCode();

    public override string ToString() => Pattern;

    public static bool operator ==(ResumptionEndpoint left, ResumptionEndpoint right) => left.Equals(right);

    public static bool operator !=(ResumptionEndpoint left, ResumptionEndpoint right) => !left.Equals(right);

    public static implicit operator ResumptionEndpoint([StringSyntax("Route")] string pattern) => new(pattern);
}

public sealed class ResumptionEndpoint<TResult>([StringSyntax("Route")] string pattern, bool allowNull = false) : ResumptionEndpointBase(pattern), IEquatable<ResumptionEndpoint<TResult>>
{
    private readonly bool _allowNull = allowNull;
    
    protected override async Task ResumeAsync(HttpContext httpContext)
    {
        string operationId = (string)httpContext.Request.RouteValues[DTasksHttpConstants.OperationIdParameterName]!;
        if (!DAsyncId.TryParse(operationId, out DAsyncId id))
        {
            await Results.NotFound().ExecuteAsync(httpContext);
            return;
        }
        
        TResult? result = await httpContext.Request.ReadFromJsonAsync<TResult>(httpContext.RequestAborted);
        if (result is null && !_allowNull)
        {
            // TODO: Message
            await Results.BadRequest().ExecuteAsync(httpContext);
            return;
        }
        
        var configuration = httpContext.RequestServices.GetRequiredService<DTasksConfiguration>();
        AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.Create(httpContext.RequestServices);
        await configuration.ResumeAsync(host, id, result, httpContext.RequestAborted);
    }

    public bool Equals([NotNullWhen(true)] ResumptionEndpoint<TResult>? other)
    {
        if (other is null)
            return false;
        
        return Pattern == other.Pattern && _allowNull == other._allowNull;
    }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ResumptionEndpoint<TResult> other && Equals(other);

    public override int GetHashCode() => Pattern.GetHashCode();

    public static bool operator ==(ResumptionEndpoint<TResult> left, ResumptionEndpoint<TResult> right) => left.Equals(right);

    public static bool operator !=(ResumptionEndpoint<TResult> left, ResumptionEndpoint<TResult> right) => !left.Equals(right);

    public static implicit operator ResumptionEndpoint<TResult>([StringSyntax("Route")] string pattern) => new(pattern);
}
