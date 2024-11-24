namespace DTasks.Utils;

internal static class ArrayExtensions
{
    public static TResult[] Map<TSource, TResult>(this TSource[] array, Func<TSource, TResult> selector)
    {
        if (array.Length == 0)
            return [];

        TResult[] result = new TResult[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            result[i] = selector(array[i]);
        }

        return result;
    }
}
