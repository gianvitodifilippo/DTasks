namespace DTasks.AspNetCore.Execution;

public interface IWebSuspensionContext
{
    DAsyncId OperationId { get; }
    
    string CallbackUrl { get; }
    
    string CallbackPath { get; }
}