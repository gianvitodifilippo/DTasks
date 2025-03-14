using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore;

public static class AsyncResults
{
    private static readonly SuccessDAsyncResult s_successInstance = new();

    public static IResult Success() => s_successInstance;

    public static IResult Success<T>(T value) => new SuccessDAsyncResult<T>(value);
}
