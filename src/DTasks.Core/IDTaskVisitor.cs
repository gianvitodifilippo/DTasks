namespace DTasks;

internal interface IDTaskVisitor<out TReturn>
{
    TReturn Visit(DTask task);

    TReturn Visit<TResult>(DTask<TResult> task);
}
