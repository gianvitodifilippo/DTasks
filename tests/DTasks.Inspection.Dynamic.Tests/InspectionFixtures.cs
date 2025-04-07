using DTasks.Infrastructure;
using DTasks.Utils;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTasks.Inspection.Dynamic;

public static class InspectionFixtures
{
    public const string LocalFieldName =
#if DEBUG
        "<local>5__1";
#else
        "<local>5__2";
#endif

    public static readonly Type StateMachineType;

#if DEBUG
    public static readonly ConstructorInfo StateMachineConstructor;
#endif

    static InspectionFixtures()
    {
        MethodInfo method = typeof(AsyncMethodContainer).GetRequiredMethod(
            name: nameof(AsyncMethodContainer.Method),
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(MyType)]);

        StateMachineAttribute? attribute = method.GetCustomAttribute<StateMachineAttribute>();
        Debug.Assert(attribute is not null);

        StateMachineType = attribute.StateMachineType;

#if DEBUG
        StateMachineConstructor = StateMachineType.GetRequiredConstructor(
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: []);
#endif
    }

    public static MethodInfo GetSuspendMethod(Type suspenderType, Type writerParameterType)
    {
        Type stateMachineType = suspenderType.GetGenericArguments()[0];

        return suspenderType.GetRequiredMethod(
            name: InspectionConstants.SuspendMethodName,
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [stateMachineType.MakeByRefType(), typeof(ISuspensionContext), writerParameterType]);
    }

    public static MethodInfo GetResumeWithVoidMethod(Type resumerType, Type readerParameterType) => resumerType.GetRequiredMethod(
        name: InspectionConstants.ResumeMethodName,
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [readerParameterType]);

    public static MethodInfo GetResumeWithResultMethod(Type resumerType, Type readerParameterType) => resumerType.GetRequiredMethod(
        name: InspectionConstants.ResumeMethodName,
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [readerParameterType, Type.MakeGenericMethodParameter(0)]);

    public class AsyncMethodContainer
    {
        private int _field = 0;

        // Generated state machine reference
        // DEBUG: https://sharplab.io/#v2:CYLg1APgAgTAjAWAFBQAwAIpwHQCUCuAdgC4CWAtgKbYDCA9uQA6kA2lATgMocBupAxpQDOAbmRpMcAKxik4mOgCCQgJ6F+AWUrEAFnWD0SAQ1KEOyAN7J0N9KeLoA+gDNSlFsHQBedKlm30a1soAA5MADYAHnsAPnQtXX0ACg0VABUVRkp0I3YAcwBKIJsrJACAqABOdAARKHDsAE03DySC/3KbLAwWOn4jFm8c/Ow0uk5idlM8toBCDs6qiOwa9yMVJLhUbdR24vKlswB3dBoWIyEhRSOTYg5r26MAIzY2hfL7dHZhfBYHHyWNB0rGACT0wDe+wqAHYvj8/ugwE5XO5PEjev0WNgADKUQh5XTvAC+yH2dSisVOwI8YOSBW8cWAlGcRl+xHmyBJciQ9g4LME6AAkjVlGp+AAxXpHWTIXnsfnZYWi9QPUh3diWfZPOh0QaCoT0JhsO6eCzoPLaEToLkVAAspzoJFM+EoSSVqnUkroJ2cUr2SC58ni6UylBlKAU5M1ZWCAGZJOF0M1USKbmrnmwky0IfSvIzmay/rJAxHavVImkYtGKvHVer0ABxbR1jhtBnoJkstnhmtwozAR0sFToISTfD8Bwt9joEBCkUe/hTgA0QpoUzImIAcnQyM4VIbGMbSI79qVOl149rdUKDQxD9pKJ48x2C92oXH0GlG9pcPDiG3n07Qt2XeXsoHtQwyEIF03XnMUvR9P10DNG1zygeNwPQAB5QgD2NR8kiwBR+EdKD8CMMhHXpFD3wvTB7QAVUIIQjGcSgcLwh8ISI9ASKdaCKOPQhqOtfYuRLb5+0HYdR3YccHGTDxU0eF5KGrD8p2/YgpwA/MuyLfZ0L7AdCCHEcxwnJQ03rWd3TFZdV3XAQBm3Xd9zvI8TxjEpaMwS8dT1W8jS49sgLfbyDgw+0m2IX8hDZNsaIisCINI51XTsz0pXQX1vRE1DFii7DcI8rjCLgYi0oEyjhOQ0Tko/TCmJYtiONKk1ysq/jyJq/KxM5UlSzOC4rms+5rIzNSkDPD9hsuTSYp03M9OAjkA0G2BTnOeaxunWy4JVXaV0FNc1WclhXNIPdOJq9S6KvQKbsfULXwMiKjMwmK4oSvr3qKyD0tg5UJWy3Kjl+lL0Ga1j2JK4KOp4viyMEqi6oKvz6OKp7uIq3iqp6oTfqJIA==
        // RELEASE: https://sharplab.io/#v2:D4AQTAjAsAUCAMACEEB0AlArgOwC4EsBbAU1QGEB7QgB3wBtiAnAZSYDd8BjYgZwG5YCZBACsAmILCIAgjwCe2TgFliuABYUAJpTwBDfNiawA3rETnEB3IgD6AM3zE6mxAF5E8cRcRmLIABzIAGwAPFYAfIgq6loAFEpyACpy1MSIuowA5gCUvuamMN7eIACciAAiIEGoAJqOzrHZXkXmKEh0FJy6dG7pWaiJFMy4jAaZjQCEzS2lwajlTrpysRDwa/BNeUWzhgDuiGR0ujw80rv6uExnF7oARgyN00VWiIy8mHTW7rNkavSa0Q0mkeW2KAHZXu9PogANS2BxOFxwjpdOioAAyxGwmXUTwAvrAtpVQhEDn9nIC4tk3JFNMQ7LoPrgprACRIYFYmAzuIgAJLlWQKTgAMQ6u3EsE5jG5aX5gsU13wl0YJi2twoFB6vJ4lBoDEuLmMiEyqj4iDZxQALAcKHgDJhiLE5fJFKKKPs7GLNjA2ZIokkUsQJXApMTVYU/ABmYRBRB1REC85Ku4MOP1YHU1y0+mMz7iX0hipVEKJcLh4rRxXKxAAcVUVaYjRpiDpDKZwYrkN0mltdDkiB4I0wnGsDcYiAAXHyBS7OGOADR8sijAiogByFAIdjkuuo+vwtq2BRarWj6s1fJ1VD3qmILizLZz7dBUcQiVrqnQUNwTYfrdzzJPJ2IDWjoBDYA6TozkKboel6iBGhaJ4gNGIGIAA8tgu76nesQoFInC2uBmC6AQtrUohL6nsg1oAKrYDwuh2MQmHYbewL4YghF2hBpEHtgFHmlsbIFm83a9v2g6MMO1jxs4iY3PcxDlq+Y4frgY6/tmbZ5lsKFdj22B9gOQ4jjISbVlOzpCguS4rlw3QbluO7Xvuh4RvkVHIGeGpaleersc2/7Ph52yodada4F+PBMk2lGhcBoFEfajrWa6YqIJ67qCUhMzhRhWGuexeEQARyW8WRAkIUJCWvmh9GMcxrFFQaJVlTxJGVTlwmsoShaHMcpwWVcFkpspMDHq+A0nGpkWaZm2kASyPp9eABxHDNw3jlZ0EKlti68suSoOXQTn4NubGVSp1Hnn5l13kFT66aF+loZF0Wxd1L35WBKVQfKIoZVluxfYliANUxLGFQFrWcdxxF8eR1W5d5NEFfdHGlVx5WdfxX14kAA===
        public async DTask<int> Method(MyType arg)
        {
            await DTask.Yield();
            string local = arg.ToString()!;
            await Task.Delay(10000);
            await new ClassAwaiterAwaitable();
            int result = await ChildMethod();

            return result + _field + local.Length;
        }

        private DTask<int> ChildMethod() => throw new NotImplementedException();
    }

    public class MyType;

    public class ClassAwaiterAwaitable
    {
        public ClassAwaiter GetAwaiter() => throw new NotImplementedException();
    }

    public class ClassAwaiter : IDAsyncAwaiter, ICriticalNotifyCompletion
    {
        public bool IsCompleted => throw new NotImplementedException();

        public void GetResult() => throw new NotImplementedException();

        public void Continue(IDAsyncRunner runner) => throw new NotImplementedException();

        public void UnsafeOnCompleted(Action continuation) => throw new NotImplementedException();

        public void OnCompleted(Action continuation) => throw new NotImplementedException();
    }


    public interface IStateMachineSuspender1<TStateMachine>
    {
        void Suspend(ref TStateMachine stateMachine, ISuspensionContext suspensionContext, ClassWriter writer);
    }

    public interface IStateMachineResumer1
    {
        IDAsyncRunnable Resume(ClassReader reader);

        IDAsyncRunnable Resume<TResult>(ClassReader reader, TResult result);
    }

    public interface IStateMachineSuspender2<TStateMachine>
    {
        void Suspend(ref TStateMachine stateMachine, ISuspensionContext suspensionContext, StructWriter writer);
    }

    public interface IStateMachineResumer2
    {
        IDAsyncRunnable Resume(StructReader reader);

        IDAsyncRunnable Resume<TResult>(StructReader reader, TResult result);
    }

    public interface IStateMachineSuspender3<TStateMachine>
    {
        void Suspend(ref TStateMachine stateMachine, ISuspensionContext suspensionContext, ref StructWriter writer);
    }

    public interface IStateMachineResumer3
    {
        IDAsyncRunnable Resume(ref StructReader reader);

        IDAsyncRunnable Resume<TResult>(ref StructReader reader, TResult result);
    }

    public interface IStateMachineSuspender4<TStateMachine>
    {
        void Suspend(ref TStateMachine stateMachine, ISuspensionContext suspensionContext, WriterWithSpecializedMethod writer);
    }

    public interface IStateMachineResumer4
    {
        IDAsyncRunnable Resume(ReaderWithSpecializedMethod reader);

        IDAsyncRunnable Resume<TResult>(ReaderWithSpecializedMethod reader, TResult result);
    }

    public class ClassWriter
    {
        public static readonly MethodInfo WriteFieldMethod = typeof(ClassWriter).GetRequiredMethod(
            name: nameof(WriteField),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0)]);

        public void WriteField<TField>(string fieldName, TField field) => throw new NotImplementedException();
    }

    public readonly struct StructWriter
    {
        public static readonly MethodInfo WriteFieldMethod = typeof(StructWriter).GetRequiredMethod(
            name: nameof(WriteField),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0)]);

        public void WriteField<TField>(string fieldName, TField field) => throw new NotImplementedException();
    }

    public class WriterWithSpecializedMethod
    {
        public static readonly MethodInfo WriteFieldMethod = typeof(WriterWithSpecializedMethod).GetRequiredMethod(
            name: nameof(WriteField),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0)]);

        public static readonly MethodInfo SpecializedWriteFieldMethod = typeof(WriterWithSpecializedMethod).GetRequiredMethod(
            name: nameof(WriteField),
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), typeof(int)]);

        public void WriteField<TField>(string fieldName, TField field) => throw new NotImplementedException();
        public void WriteField(string fieldName, int field) => throw new NotImplementedException();
    }

    public class ClassReader
    {
        public static readonly MethodInfo ReadFieldMethod = typeof(ClassReader).GetRequiredMethod(
            name: nameof(ReadField),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);

        public bool ReadField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
    }

    public readonly struct StructReader
    {
        public static readonly MethodInfo ReadFieldMethod = typeof(StructReader).GetRequiredMethod(
            name: nameof(ReadField),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);

        public bool ReadField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
    }

    public class ReaderWithSpecializedMethod
    {
        public static readonly MethodInfo ReadFieldMethod = typeof(ReaderWithSpecializedMethod).GetRequiredMethod(
            name: nameof(ReadField),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);

        public static readonly MethodInfo SpecializedReadFieldMethod = typeof(ReaderWithSpecializedMethod).GetRequiredMethod(
            name: nameof(ReadField),
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), typeof(int).MakeByRefType()]);

        public bool ReadField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
        public bool ReadField(string fieldName, ref int field) => throw new NotImplementedException();
    }
}