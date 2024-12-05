namespace DTasks.Inspection.Dynamic;

internal interface IStateMachineConverter<TStateMachine>
{
    void SetObjectAwaiter(ref TStateMachine stateMachine, object awaiter);
    
    void SetAwaiterFromResult(ref TStateMachine stateMachine, int awaiterIndex);
    
    void SetAwaiterFromResult<TResult>(ref TStateMachine stateMachine, int awaiterIndex, TResult result);
    
    void SetAwaiterFromException(ref TStateMachine stateMachine, int awaiterIndex, Exception exception);
}