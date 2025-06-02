using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure.Generics;

public interface IGenericTypeAction
{
    void Invoke<T1>() => DefaultInvoke();
    
    void Invoke<T1, T2>() => DefaultInvoke();
    
    void Invoke<T1, T2, T3>() => DefaultInvoke();
    
    void Invoke<T1, T2, T3, T4>() => DefaultInvoke();
    
    void Invoke<T1, T2, T3, T4, T5>() => DefaultInvoke();
    
    void Invoke<T1, T2, T3, T4, T5, T6>() => DefaultInvoke();
    
    void Invoke<T1, T2, T3, T4, T5, T6, T7>() => DefaultInvoke();
    
    void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>() => DefaultInvoke();

    [DoesNotReturn]
    private static void DefaultInvoke()
    {
        throw new InvalidOperationException("Unexpected number of generic parameters");
    }
}

public interface IGenericTypeAction<out TReturn>
{
    TReturn Invoke<T1>() => DefaultInvoke();
    
    TReturn Invoke<T1, T2>() => DefaultInvoke();
    
    TReturn Invoke<T1, T2, T3>() => DefaultInvoke();
    
    TReturn Invoke<T1, T2, T3, T4>() => DefaultInvoke();
    
    TReturn Invoke<T1, T2, T3, T4, T5>() => DefaultInvoke();
    
    TReturn Invoke<T1, T2, T3, T4, T5, T6>() => DefaultInvoke();
    
    TReturn Invoke<T1, T2, T3, T4, T5, T6, T7>() => DefaultInvoke();
    
    TReturn Invoke<T1, T2, T3, T4, T5, T6, T7, T8>() => DefaultInvoke();

    [DoesNotReturn]
    private static TReturn DefaultInvoke()
    {
        throw new InvalidOperationException("Unexpected number of generic parameters");
    }
}