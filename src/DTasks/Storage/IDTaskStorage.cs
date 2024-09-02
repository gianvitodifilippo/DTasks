namespace DTasks.Storage;

public interface IDTaskStorage<TStack>
    where TStack : notnull, IFlowStack
{
    TStack CreateStack();

    Task<TStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken)
        where TFlowId : notnull;

    Task SaveStackAsync<TFlowId>(TFlowId flowId, in TStack stack, CancellationToken cancellationToken)
        where TFlowId : notnull;
}
