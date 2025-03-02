namespace DTasks;

internal interface IDTaskVisitor<TReturn>
{
    TReturn Visit(DTask task);

    TReturn Visit<TResult>(DTask<TResult> task);
}
