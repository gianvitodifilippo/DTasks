using DTasks.AspNetCore.Metadata;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Http;

public static class AsyncResults
{
    private static readonly SuccessAsyncResult s_successInstance = new();

    public static IResult Success() => s_successInstance;

    [return: AsyncResult(genericParameterPosition: 0)]
    public static IResult Success<T>(T value) => new SuccessAsyncResult<T>(value);
}
