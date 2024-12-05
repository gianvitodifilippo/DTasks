using DTasks.Inspection.Dynamic.Descriptors;
using DTasks.Marshaling;
using DTasks.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection.Dynamic;

using IndexerAwaiterField = (FieldInfo Field, int Index);

public sealed class DynamicStateMachineInspector : IStateMachineInspector
{
    private static readonly ConstructorInfo s_objectConstructor = typeof(object).GetRequiredConstructor(
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: []);

    private readonly DynamicAssembly _assembly;
    private readonly IAwaiterManager _awaiterManager;
    private readonly IConverterDescriptorFactory _converterDescriptorFactory;
    private readonly ConcurrentDictionary<Type, object> _converters;

    internal DynamicStateMachineInspector(
        DynamicAssembly assembly,
        IAwaiterManager awaiterManager,
        IConverterDescriptorFactory converterDescriptorFactory)
    {
        _assembly = assembly;
        _awaiterManager = awaiterManager;
        _converterDescriptorFactory = converterDescriptorFactory;
        _converters = [];
    }

    public object GetConverter(Type stateMachineType)
    {
        return _converters.GetOrAdd(stateMachineType, CreateConverter, this);

        static object CreateConverter(Type stateMachineType, DynamicStateMachineInspector self)
            => self.CreateConverter(stateMachineType);
    }

    private object CreateConverter(Type stateMachineType)
    {
        FieldInfo[] fields = stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo? stateMachineConstructor = stateMachineType.GetConstructor([]);
        if (stateMachineConstructor is null && !stateMachineType.IsValueType)
            throw new ArgumentException("The state machine type should have a parameterless constructor.", nameof(stateMachineType));

        IConverterDescriptor converterDescriptor = _converterDescriptorFactory.CreateDescriptor(stateMachineType);
        TypeBuilder converterType = _assembly.DefineConverterType(stateMachineType);
        converterType.AddInterfaceImplementation(converterDescriptor.Type);

        FieldBuilder awaiterManagerField = DefineAwaiterManagerField(converterType);
        DefineConstructor(converterType, awaiterManagerField);
        
        MethodBuilder suspendMethod = converterType.DefineMethodOverride(converterDescriptor.SuspendMethod);
        MethodBuilder resumeWithVoidMethod = converterType.DefineMethodOverride(converterDescriptor.ResumeWithVoidMethod);
        MethodBuilder resumeWithResultMethod = converterType.DefineMethodOverride(converterDescriptor.ResumeWithResultMethod);

        SuspendMethodImplementor suspendImplementor = new(suspendMethod, stateMachineType, converterDescriptor);
        ResumeWithVoidMethodImplementor resumeWithVoidImplementor = new(resumeWithVoidMethod, stateMachineType, converterDescriptor);
        ResumeWithResultMethodImplementor resumeWithResultImplementor = new(resumeWithResultMethod, stateMachineType, converterDescriptor);

        List<IndexerAwaiterField> dAsyncAwaiterFields = [];
        int awaiterIndex = 0;
        foreach (FieldInfo field in fields)
        {
            StateMachineFieldKind fieldKind = StateMachineFacts.GetFieldKind(field);

            switch (fieldKind)
            {
                case StateMachineFieldKind.UserField:
                case StateMachineFieldKind.StateField:
                    suspendImplementor.OnUserField(field);
                    break;

                case StateMachineFieldKind.DAsyncAwaiterField:
                    if (field.FieldType.IsValueType)
                    {
                        dAsyncAwaiterFields.Add((field, awaiterIndex));
                        awaiterIndex++;
                    }
                    else
                    {
                        if (field.FieldType != typeof(object))
                            throw new NotSupportedException("The provided state machine layout is not supported.");
                        
                        dAsyncAwaiterFields.Add((field, -1));
                    }

                    break;
            }
        }

        suspendImplementor.OnAwaiterFields(dAsyncAwaiterFields, awaiterManagerField);

        suspendImplementor.Return();
        resumeWithVoidImplementor.Return();
        resumeWithResultImplementor.Return();

        return Activator.CreateInstance(converterType.CreateType(), [_awaiterManager])!;
    }

    private static FieldBuilder DefineAwaiterManagerField(TypeBuilder converterType)
    {
        return converterType.DefineField("_awaiterManager", typeof(IAwaiterManager), FieldAttributes.Private | FieldAttributes.InitOnly);
    }

    private static ConstructorBuilder DefineConstructor(TypeBuilder converterType, FieldInfo awaiterManagerField)
    {
        ConstructorBuilder constructor = converterType.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, [typeof(IAwaiterManager)]);
        constructor.DefineParameter(
            iSequence: 1,
            attributes: ParameterAttributes.None,
            strParamName: "awaiterManager");
        
        ILGenerator il = constructor.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);                     // Stack: this
        il.Emit(OpCodes.Ldarg_1);                     // Stack: this, $awaiterManager
        il.Emit(OpCodes.Stfld, awaiterManagerField);  // Stack: -
        il.Emit(OpCodes.Ldarg_0);                     // Stack: this
        il.Emit(OpCodes.Call, s_objectConstructor);   // Stack: -
        il.Emit(OpCodes.Ret);                         // Stack: -

        return constructor;
    }

    public static DynamicStateMachineInspector Create(Type converterType, ITypeResolver typeResolver)
    {
        if (!ConverterDescriptorFactory.TryCreate(converterType, out ConverterDescriptorFactory? converterDescriptorFactory))
            throw new ArgumentException("The provided converter type is not compliant.", nameof(converterType));

        DynamicAssembly assembly = new();
        AwaiterManager awaiterManager = new(assembly, typeResolver);

        return new(assembly, awaiterManager, converterDescriptorFactory);
    }

    private readonly ref struct SuspendMethodImplementor(MethodBuilder suspendMethod, Type stateMachineType, IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            suspendMethod,
            stateMachineType,
            converterDescriptor.SuspendMethod.GetParameters()[^1].ParameterType,
            converterDescriptor.Writer.Type);

        public void OnUserField(FieldInfo field)
        {
            MethodInfo writeFieldMethod = converterDescriptor.Writer.GetWriteFieldMethod(field.FieldType);

            // writer.WriteField($fieldName, stateMachine.$field);
            _il.LoadWriter();                       // Stack: writer
            _il.LoadString(field.Name);             // Stack: writer, $fieldName
            _il.LoadStateMachineArg();              // Stack: writer, $fieldName, stateMachine
            _il.LoadField(field);                   // Stack: writer, $fieldName, stateMachine.$field
            _il.CallWriterMethod(writeFieldMethod); // Stack: -
        }

        public void OnAwaiterFields(List<IndexerAwaiterField> indexedFields, FieldInfo typeResolverField)
        {
            Label ifFalseLabel = default;
            bool wasLabelDefined = false;
            foreach ((FieldInfo field, int index) in indexedFields)
            {
                if (wasLabelDefined)
                {
                    // return;
                    _il.Return();                // Stack: -
                    _il.MarkLabel(ifFalseLabel); // Stack: -
                }

                ifFalseLabel = _il.DefineLabel();
                wasLabelDefined = true;

                // if (suspensionContext.IsSuspended(ref stateMachine.$field))
                _il.LoadSuspensionContext();                // Stack: suspensionContext
                _il.LoadStateMachineArg();                  // Stack: suspensionContext, stateMachine
                _il.LoadFieldAddress(field);                // Stack: suspensionContext, &stateMachine.$field
                _il.CallIsSuspendedMethod(field.FieldType); // Stack: @result[suspensionContext.IsSuspended(&stateMachine.$field)]
                _il.BranchIfFalse(ifFalseLabel);            // Stack: -

                if (field.FieldType.IsValueType)
                {
                    MethodInfo writeFieldMethod = converterDescriptor.Writer.GetWriteFieldMethod(typeof(int));

                    // writer.WriteField($InspectionConstants.AwaiterFieldName, $field.Name);
                    _il.LoadWriter();                                     // Stack: writer
                    _il.LoadString(InspectionConstants.AwaiterFieldName); // Stack: writer, $InspectionConstants.AwaiterFieldName
                    _il.LoadInt(index);                                   // Stack: writer, $InspectionConstants.AwaiterFieldName, $index
                    _il.CallWriterMethod(writeFieldMethod);               // Stack: -
                }
                else
                {
                    Debug.Assert(field.FieldType == typeof(object));

                    MethodInfo writeFieldMethod = converterDescriptor.Writer.GetWriteFieldMethod(typeof(TypeId));

                    // writer.WriteField($InspectionConstants.AwaiterFieldName, _awaiterManager.GetTypeId(stateMachine.$field));
                    _il.LoadWriter();                                        // Stack: writer
                    _il.LoadString(InspectionConstants.RefAwaiterFieldName); // Stack: writer, $InspectionConstants.RefAwaiterFieldName
                    _il.LoadThis();                                          // Stack: writer, $InspectionConstants.RefAwaiterFieldName, this
                    _il.LoadField(typeResolverField);                        // Stack: writer, $InspectionConstants.RefAwaiterFieldName, _awaiterManager
                    _il.LoadStateMachineArg();                               // Stack: writer, $InspectionConstants.RefAwaiterFieldName, _awaiterManager, stateMachine
                    _il.LoadField(field);                                    // Stack: writer, $InspectionConstants.RefAwaiterFieldName, _awaiterManager, stateMachine.$field
                    _il.CallGetTypeIdMethod();                               // Stack: writer, $InspectionConstants.RefAwaiterFieldName, @result[_awaiterManager.GetTypeId(stateMachine.$field)]
                    _il.CallWriterMethod(writeFieldMethod);                  // Stack: -
                }
            }

            if (wasLabelDefined) // check in case there are no awaiters
            {
                _il.MarkLabel(ifFalseLabel);
            }
        }

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }

    private readonly ref struct ResumeWithVoidMethodImplementor(MethodBuilder resumeWithVoidMethod, Type stateMachineType, IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            resumeWithVoidMethod,
            stateMachineType,
            converterDescriptor.ResumeWithVoidMethod.GetParameters()[0].ParameterType,
            converterDescriptor.Reader.Type);

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }

    private readonly ref struct ResumeWithResultMethodImplementor(MethodBuilder resumeWithResultMethod, Type stateMachineType, IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            resumeWithResultMethod,
            stateMachineType,
            converterDescriptor.ResumeWithResultMethod.GetParameters()[0].ParameterType,
            converterDescriptor.Reader.Type);

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }
}
