using DTasks.Utils;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection.Dynamic;

internal sealed class DynamicAssembly
{
    private const string Name = "DTasks.Inspection.Dynamic.Generated";

    private readonly AssemblyBuilder _assembly;
    private readonly ModuleBuilder _module;
    private readonly ConstructorInfo _ignoresAccessChecksToAttributeConstructor;
    private readonly HashSet<Assembly> _userAssemblies;

    public DynamicAssembly(Type converterType)
    {
        AssemblyName assemblyName = new(Name);

        _userAssemblies = [];
        _assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        _module = _assembly.DefineDynamicModule(Name);

        TypeBuilder ignoresAccessChecksToAttributeType = _module.DefineType(
            name: "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
            attr: TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class,
            parent: typeof(Attribute));

        ConstructorInfo attributeUsageConstructor = typeof(AttributeUsageAttribute).GetRequiredConstructor(
            bindingAttr: BindingFlags.Public | BindingFlags.Instance,
            parameterTypes: [typeof(AttributeTargets)]);

        PropertyInfo allowMultipleProperty = typeof(AttributeUsageAttribute).GetProperty(
            name: nameof(AttributeUsageAttribute.AllowMultiple),
            bindingAttr: BindingFlags.Public | BindingFlags.Instance)!;

        ignoresAccessChecksToAttributeType.SetCustomAttribute(new CustomAttributeBuilder(
            con: attributeUsageConstructor,
            constructorArgs: [AttributeTargets.Assembly],
            namedProperties: [allowMultipleProperty],
            propertyValues: [true]));

        FieldBuilder assemblyNameField = ignoresAccessChecksToAttributeType.DefineField(
            fieldName: "<AssemblyName>k__BackingField",
            type: typeof(string),
            attributes: FieldAttributes.Private | FieldAttributes.InitOnly);

        ConstructorBuilder constructor = ignoresAccessChecksToAttributeType.DefineConstructor(
            attributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            callingConvention: CallingConventions.Standard | CallingConventions.HasThis,
            parameterTypes: [typeof(string)]);

        ILGenerator constructorIL = constructor.GetILGenerator();
        constructorIL.Emit(OpCodes.Ldarg_0);                  // Stack: this
        constructorIL.Emit(OpCodes.Ldarg_1);                  // Stack: this, $args[1]
        constructorIL.Emit(OpCodes.Stfld, assemblyNameField); // Stack: -
        constructorIL.Emit(OpCodes.Ret);                      // Stack: -

        MethodBuilder assemblyNameGetter = ignoresAccessChecksToAttributeType.DefineMethod(
            name: "get_AssemblyName",
            attributes: MethodAttributes.Public | MethodAttributes.SpecialName,
            returnType: typeof(string),
            parameterTypes: []);

        ILGenerator assemblyNameGetterIL = assemblyNameGetter.GetILGenerator();
        assemblyNameGetterIL.Emit(OpCodes.Ldarg_0);                  // Stack: this
        assemblyNameGetterIL.Emit(OpCodes.Ldfld, assemblyNameField); // Stack: this.<AssemblyName>k__BackingField
        assemblyNameGetterIL.Emit(OpCodes.Ret);                      // Stack: -

        PropertyBuilder assemblyNameProperty = ignoresAccessChecksToAttributeType.DefineProperty(
            name: "AssemblyName",
            attributes: PropertyAttributes.None,
            returnType: typeof(string),
            parameterTypes: []);

        assemblyNameProperty.SetGetMethod(assemblyNameGetter);

        _ignoresAccessChecksToAttributeConstructor = ignoresAccessChecksToAttributeType.CreateTypeInfo()!.GetRequiredConstructor(
            bindingAttr: BindingFlags.Public | BindingFlags.Instance,
            parameterTypes: [typeof(string)]);

        EnsureAccess(converterType.Assembly);
    }

    public TypeBuilder DefineSuspenderType(Type stateMachineType)
    {
        EnsureAccess(stateMachineType.Assembly);

        return _module.DefineType(stateMachineType.FullName + "Suspender", TypeAttributes.Public | TypeAttributes.Class);
    }

    public TypeBuilder DefineResumerType(Type stateMachineType)
    {
        EnsureAccess(stateMachineType.Assembly);

        return _module.DefineType(stateMachineType.FullName + "Resumer", TypeAttributes.Public | TypeAttributes.Class);
    }

    public TypeBuilder DefineAwaiterFactoryType(Type awaiterType)
    {
        return _module.DefineType(awaiterType.FullName + "Factory", TypeAttributes.NestedAssembly | TypeAttributes.Class);
    }

    private void EnsureAccess(Assembly assembly)
    {
        if (_userAssemblies.Contains(assembly))
            return;

        lock (_userAssemblies)
        {
            if (_userAssemblies.Contains(assembly))
                return;

            _userAssemblies.Add(assembly);
            _assembly.SetCustomAttribute(new CustomAttributeBuilder(
                con: _ignoresAccessChecksToAttributeConstructor,
                constructorArgs: [assembly.GetName().Name]));
        }
    }
}
