using System.Reflection;

namespace DTasks.Inspection;

public partial class ResumerDescriptorTests
{
    public delegate DTask StructResumer(DTask resultTask, StructConstructor constructor);

    public delegate DTask ByRefStructResumer(DTask resultTask, ref StructConstructor constructor);

    public delegate DTask ClassResumer(DTask resultTask, ClassConstructor constructor);

    public delegate DTask ByRefClassResumer(DTask resultTask, ref ClassConstructor constructor);


    public delegate DTask ResumerWithHandleStateMethod(DTask resultTask, ConstructorWithHandleStateMethod constructor);

    public delegate DTask ResumerWithSpecializedMethod(DTask resultTask, ConstructorWithSpecializedMethod constructor);

    public delegate DTask ResumerWithSpecializedMethodAndHandleStateMethod(DTask resultTask, ConstructorWithSpecializedMethodAndHandleStateMethod constructor);


    public delegate DTask ResumerWithoutHandleField(DTask resultTask, ConstructorWithoutHandleField constructor);

    public delegate DTask ResumerWithoutHandleAwaiter(DTask resultTask, ConstructorWithoutHandleAwaiter constructor);


    private static MethodInfo GetRequiredMethod<T>(string methodName)
    {
        MethodInfo? method = typeof(T).GetMethod(methodName);
        Assert.NotNull(method);
        return method;
    }

    private static MethodInfo GetRequiredMethod<T>(string methodName, Type[] parameterTypes)
    {
        MethodInfo? method = typeof(T).GetMethod(methodName, parameterTypes);
        Assert.NotNull(method);
        return method;
    }

    public class ConstructorWithoutHandleField
    {
        public bool HandleAwaiter(string fieldName) => throw new NotImplementedException();
    }

    public class ConstructorWithoutHandleAwaiter
    {
        public bool HandleField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
    }

    public struct StructConstructor
    {
        public bool HandleField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
        public bool HandleAwaiter(string fieldName) => throw new NotImplementedException();
    }

    public class ClassConstructor
    {
        public bool HandleField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
        public bool HandleAwaiter(string fieldName) => throw new NotImplementedException();
    }

    public class ConstructorWithHandleStateMethod
    {
        public bool HandleField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
        public bool HandleAwaiter(string fieldName) => throw new NotImplementedException();
        public bool HandleState(string fieldName, ref int state) => throw new NotImplementedException();
    }

    public class ConstructorWithSpecializedMethod
    {
        public bool HandleField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
        public bool HandleAwaiter(string fieldName) => throw new NotImplementedException();
        public bool HandleField(string fieldName, ref int field) => throw new NotImplementedException();
    }

    public class ConstructorWithSpecializedMethodAndHandleStateMethod
    {
        public bool HandleField<TField>(string fieldName, ref TField field) => throw new NotImplementedException();
        public bool HandleAwaiter(string fieldName) => throw new NotImplementedException();
        public bool HandleField(string fieldName, ref int field) => throw new NotImplementedException();
        public bool HandleState(string fieldName, ref int state) => throw new NotImplementedException();
    }
}
