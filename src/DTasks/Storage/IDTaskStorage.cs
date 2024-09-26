namespace DTasks.Storage;

public interface IDTaskStorage<TStack>
    where TStack : IFlowStack
{
    TStack CreateStack();

    ValueTask<TStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken = default)
        where TFlowId : notnull;

    Task SaveStackAsync<TFlowId>(TFlowId flowId, ref TStack stack, CancellationToken cancellationToken = default)
        where TFlowId : notnull;
}
