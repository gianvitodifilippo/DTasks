using DTasks.Infrastructure.Marshaling;

namespace DTasks.AspNetCore.Infrastructure;

public interface IDAsyncFlowServices
{
    IDAsyncSurrogator Surrogator { get; }
}