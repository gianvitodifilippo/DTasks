﻿using DTasks.Inspection.Dynamic.Descriptors;
using DTasks.Marshaling;
using DTasks.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection.Dynamic;

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
        StateMachineDescriptor stateMachineDescriptor = StateMachineDescriptor.Create(stateMachineType);

        IConverterDescriptor converterDescriptor = _converterDescriptorFactory.CreateDescriptor(stateMachineType);
        TypeBuilder converterType = _assembly.DefineConverterType(stateMachineType);
        converterType.AddInterfaceImplementation(converterDescriptor.Type);

        FieldBuilder awaiterManagerField = DefineAwaiterManagerField(converterType);
        DefineConstructor(converterType, awaiterManagerField);
        
        MethodBuilder suspendMethod = converterType.DefineMethodOverride(converterDescriptor.SuspendMethod);
        MethodBuilder resumeWithVoidMethod = converterType.DefineMethodOverride(converterDescriptor.ResumeWithVoidMethod);
        MethodBuilder resumeWithResultMethod = converterType.DefineMethodOverride(converterDescriptor.ResumeWithResultMethod);

        SuspendMethodImplementor suspendImplementor = new(suspendMethod, awaiterManagerField, stateMachineDescriptor, converterDescriptor);
        ResumeWithVoidMethodImplementor resumeWithVoidImplementor = new(resumeWithVoidMethod, awaiterManagerField, stateMachineDescriptor, converterDescriptor);
        ResumeWithResultMethodImplementor resumeWithResultImplementor = new(resumeWithResultMethod, awaiterManagerField, stateMachineDescriptor, converterDescriptor);

        foreach (FieldInfo userField in stateMachineDescriptor.UserFields)
        {
            suspendImplementor.OnUserField(userField);
        }
        suspendImplementor.OnAwaiterFields(stateMachineDescriptor.AwaiterFields);
        suspendImplementor.Return();

        resumeWithVoidImplementor.DeclareLocals();
        resumeWithVoidImplementor.InitStateMachine();
        foreach (FieldInfo userField in stateMachineDescriptor.UserFields)
        {
            resumeWithVoidImplementor.OnUserField(userField);
        }
        resumeWithVoidImplementor.OnAwaiterFields(stateMachineDescriptor.AwaiterFields);
        resumeWithVoidImplementor.Return();

        resumeWithResultImplementor.DeclareLocals();
        resumeWithResultImplementor.InitStateMachine();
        foreach (FieldInfo userField in stateMachineDescriptor.UserFields)
        {
            resumeWithResultImplementor.OnUserField(userField);
        }
        resumeWithResultImplementor.OnAwaiterFields(stateMachineDescriptor.AwaiterFields);
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

        il.Emit(OpCodes.Ldarg_0);                    // Stack: this
        il.Emit(OpCodes.Ldarg_1);                    // Stack: this, $awaiterManager
        il.Emit(OpCodes.Stfld, awaiterManagerField); // Stack: -
        il.Emit(OpCodes.Ldarg_0);                    // Stack: this
        il.Emit(OpCodes.Call, s_objectConstructor);  // Stack: -
        il.Emit(OpCodes.Ret);                        // Stack: -

        return constructor;
    }

    // TODO: Separate converter into suspender and resumer, since callers of Resume are not generic methods
    public static DynamicStateMachineInspector Create(Type converterType, ITypeResolver typeResolver)
    {
        if (!ConverterDescriptorFactory.TryCreate(converterType, out ConverterDescriptorFactory? converterDescriptorFactory))
            throw new ArgumentException("The provided converter type is not compliant.", nameof(converterType));

        DynamicAssembly assembly = new(converterType);
        AwaiterManager awaiterManager = new(assembly, typeResolver);

        return new(assembly, awaiterManager, converterDescriptorFactory);
    }

    private readonly ref struct SuspendMethodImplementor(
        MethodBuilder suspendMethod,
        FieldInfo awaiterManagerField,
        StateMachineDescriptor stateMachineDescriptor,
        IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            suspendMethod,
            stateMachineDescriptor,
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

        public void OnAwaiterFields(IReadOnlyCollection<IndexedFieldInfo> indexedFields)
        {
            Label ifFalseLabel = default;
            bool wasLabelDefined = false;
            foreach ((FieldInfo field, int index) in indexedFields)
            {
                if (wasLabelDefined)
                {
                    // return;
                    _il.Return();                // Stack: -
                    _il.MarkLabel(ifFalseLabel);
                }

                ifFalseLabel = _il.DefineLabel();
                wasLabelDefined = true;

                // if (suspensionContext.IsSuspended(ref stateMachine.$field))
                _il.LoadSuspensionContext();                      // Stack: suspensionContext
                _il.LoadStateMachineArg();                        // Stack: suspensionContext, stateMachine
                _il.LoadFieldAddress(field);                      // Stack: suspensionContext, ref stateMachine.$field
                _il.CallIsSuspendedMethod(field.FieldType);       // Stack: @result[suspensionContext.IsSuspended(ref stateMachine.$field)]
                _il.BranchIfFalse(ifFalseLabel, shortForm: true); // Stack: -

                if (field.FieldType.IsValueType)
                {
                    MethodInfo writeFieldMethod = converterDescriptor.Writer.GetWriteFieldMethod(typeof(int));

                    // writer.WriteField($AwaiterFieldName, $field.Name);
                    _il.LoadWriter();                                     // Stack: writer
                    _il.LoadString(InspectionConstants.AwaiterFieldName); // Stack: writer, $AwaiterFieldName
                    _il.LoadInt(index);                                   // Stack: writer, $AwaiterFieldName, $index
                    _il.CallWriterMethod(writeFieldMethod);               // Stack: -
                }
                else
                {
                    Debug.Assert(field.FieldType == typeof(object));

                    MethodInfo writeFieldMethod = converterDescriptor.Writer.GetWriteFieldMethod(typeof(TypeId));

                    // writer.WriteField($AwaiterFieldName, _awaiterManager.GetTypeId(stateMachine.$field));
                    _il.LoadWriter();                                        // Stack: writer
                    _il.LoadString(InspectionConstants.RefAwaiterFieldName); // Stack: writer, $RefAwaiterFieldName
                    _il.LoadThis();                                          // Stack: writer, $RefAwaiterFieldName, this
                    _il.LoadField(awaiterManagerField);                      // Stack: writer, $RefAwaiterFieldName, _awaiterManager
                    _il.LoadStateMachineArg();                               // Stack: writer, $RefAwaiterFieldName, _awaiterManager, stateMachine
                    _il.LoadField(field);                                    // Stack: writer, $RefAwaiterFieldName, _awaiterManager, stateMachine.$field
                    _il.CallGetTypeIdMethod();                               // Stack: writer, $RefAwaiterFieldName, @result[_awaiterManager.GetTypeId(stateMachine.$field)]
                    _il.CallWriterMethod(writeFieldMethod);                  // Stack: -
                }
            }

            if (wasLabelDefined)
            {
                _il.MarkLabel(ifFalseLabel);
            }
        }

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }

    private readonly ref struct ResumeWithVoidMethodImplementor(
        MethodBuilder resumeWithVoidMethod,
        FieldInfo awaiterManagerField,
        StateMachineDescriptor stateMachineDescriptor,
        IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            resumeWithVoidMethod,
            stateMachineDescriptor,
            converterDescriptor.ResumeWithVoidMethod.GetParameters()[0].ParameterType,
            converterDescriptor.Reader.Type);

        public void DeclareLocals()
        {
            _il.DeclareStateMachineLocal();
            _il.DeclareAwaiterIndexLocal();
            _il.DeclareAwaiterIdLocal();
        }

        public void InitStateMachine()
        {
            _il.InitStateMachine(); // Stack: -
        }

        public void OnUserField(FieldInfo field)
        {
            MethodInfo readFieldMethod = converterDescriptor.Reader.GetReadFieldMethod(field.FieldType);

            _il.LoadReader();                      // Stack: reader
            _il.LoadString(field.Name);            // Stack: reader, $field.Name
            _il.LoadStateMachineLocal();           // Stack: reader, $field.Name, stateMachine
            _il.LoadFieldAddress(field);           // Stack: reader, $field.Name, stateMachine.$field
            _il.CallReaderMethod(readFieldMethod); // Stack: @result[reader.ReadField($field.Name, stateMachine.$field)]
            _il.Pop();                             // Stack: -
        }

        public void OnAwaiterFields(IReadOnlyCollection<IndexedFieldInfo> indexedFields)
        {
            MethodInfo readIntFieldMethod = converterDescriptor.Reader.GetReadFieldMethod(typeof(int));
            MethodInfo readTypeIdFieldMethod = converterDescriptor.Reader.GetReadFieldMethod(typeof(TypeId));
            FieldInfo? refAwaiterField = stateMachineDescriptor.RefAwaiterField;
            bool hasRefAwaiterField = refAwaiterField is not null;

            int switchCaseCount = hasRefAwaiterField
                ? indexedFields.Count - 1
                : indexedFields.Count;

            Label refAwaiterLabel = hasRefAwaiterField
                ? _il.DefineLabel()
                : default;
            Label[] switchCaseLabels = new Label[switchCaseCount];
            Label switchDefaultCaseLabel = _il.DefineLabel();
            Label endOfMethodLabel = _il.DefineLabel();

            for (int i = 0; i < switchCaseCount; i++)
            {
                switchCaseLabels[i] = _il.DefineLabel();
            }

            Label awaiterIndexNextLabel = hasRefAwaiterField
                ? refAwaiterLabel
                : endOfMethodLabel;

            // if (reader.ReadField(InspectionConstants.AwaiterFieldName, ref awaiterIndex))
            _il.InitAwaiterIndex();                               // Stack: -
            _il.InitAwaiterId();                                  // Stack: -
            _il.LoadReader();                                     // Stack: reader
            _il.LoadString(InspectionConstants.AwaiterFieldName); // Stack: reader, $AwaiterFieldName
            _il.LoadAwaiterIndexAddress();                        // Stack: reader, $AwaiterFieldName, ref awaiterIndex
            _il.CallReaderMethod(readIntFieldMethod);             // Stack: @result[reader.ReadField($AwaiterFieldName, ref awaiterIndex)]
            _il.BranchIfFalse(awaiterIndexNextLabel);             // Stack: -

            // switch (awaiterIndex - 1)
            _il.LoadAwaiterIndex();             // Stack: awaiterIndex
            _il.LoadInt(1);                     // Stack: awaiterIndex, 1
            _il.Subtract();                     // Stack: awaiterIndex - 1
            _il.Switch(switchCaseLabels);       // Stack: -
            _il.Branch(switchDefaultCaseLabel); // Stack: -

            foreach ((FieldInfo field, int index) in indexedFields)
            {
                if (index == -1)
                {
                    refAwaiterField = field;
                    continue;
                }

                Debug.Assert(index >= 1 && index <= switchCaseLabels.Length, "Awaiter indexes should be in the range [1, count].");

                Type awaiterType = field.FieldType;

                // case $index - 1:
                _il.MarkLabel(switchCaseLabels[index - 1]);
                if (awaiterType == typeof(DTask.Awaiter))
                {
                    // stateMachine.$field = DTask.CompletedDTask.GetAwaiter();
                    _il.LoadStateMachineLocal();    // Stack: stateMachine
                    _il.CallCompletedDTaskGetter(); // Stack: stateMachine, $CompletedDTask
                    _il.CallGetAwaiterMethod();     // Stack: stateMachine, $CompletedDTask.GetAwaiter()
                    _il.StoreField(field);          // Stack: -
                    _il.Branch(endOfMethodLabel);   // Stack: -
                }
                else if (awaiterType == typeof(YieldDAwaitable.Awaiter))
                {
                    // No-op
                    _il.Branch(endOfMethodLabel); // Stack: -
                }
                else
                {
                    MethodInfo? awaiterFromVoidMethod = awaiterType.GetMethod(
                        name: "FromResult",
                        genericParameterCount: 0,
                        bindingAttr: BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        types: [],
                        modifiers: null);

                    if (awaiterFromVoidMethod is null)
                    {
                        // throw new InvalidOperationException("...");
                        _il.LoadString("Invalid attempt to resume a d-async method."); // "Invalid attempt to resume a d-async method."
                        _il.NewInvalidOperationException();                            // new InvalidOperationException("...")
                        _il.Throw();                                                   // -
                    }
                    else
                    {
                        // stateMachine.$field = $awaiterType.FromResult();
                        _il.LoadStateMachineLocal();     // Stack: stateMachine
                        _il.Call(awaiterFromVoidMethod); // Stack: stateMachine, @result[$awaiterType.FromResult()]
                        _il.StoreField(field);           // Stack: -
                        _il.Branch(endOfMethodLabel);    // Stack: -
                    }
                }
            }

            // TODO: InvalidDAsyncStateException
            // default:
            _il.MarkLabel(switchDefaultCaseLabel);
            _il.LoadString("InvalidDAsyncStateException"); // "InvalidDAsyncStateException"
            _il.NewInvalidOperationException();            // new InvalidOperationException("...")
            _il.Throw();                                   // -

            if (refAwaiterField is not null)
            {
                _il.MarkLabel(refAwaiterLabel);

                // else if (reader.ReadField(InspectionConstants.RefAwaiterFieldName, ref awaiterId))
                _il.LoadReader();                                        // Stack: reader
                _il.LoadString(InspectionConstants.RefAwaiterFieldName); // Stack: reader, $RefAwaiterFieldName
                _il.LoadAwaiterIdAddress();                              // Stack: reader, $RefAwaiterFieldName, ref awaiterId
                _il.CallReaderMethod(readTypeIdFieldMethod);             // Stack: @result[reader.ReadField($RefAwaiterFieldName, ref awaiterId)
                _il.BranchIfFalse(endOfMethodLabel, shortForm: true);    // Stack: -

                // stateMachine.$field = _awaiterManager.CreateFromResult(awaiterId);
                _il.LoadStateMachineLocal();                   // Stack: stateMachine
                _il.LoadThis();                                // Stack: stateMachine, this
                _il.LoadField(awaiterManagerField);            // Stack: stateMachine, _awaiterManager
                _il.LoadAwaiterId();                           // Stack: stateMachine, _awaiterManager, awaiterId
                _il.CallCreateFromVoidMethod();                // Stack: stateMachine, @result[_awaiterManager.CreateFromResult(awaiterId)]
                _il.StoreField(refAwaiterField);               // Stack: -
                _il.Branch(endOfMethodLabel, shortForm: true); // Stack: -
            }

            _il.MarkLabel(endOfMethodLabel);
            _il.LoadStateMachineLocal();                         // Stack: stateMachine
            _il.CallBuilderCreateMethod();                       // Stack: stateMachine, @result[$builderType.Create()]
            _il.StoreField(stateMachineDescriptor.BuilderField); // Stack: -

            _il.LoadStateMachineLocal();                               // Stack: stateMachine
            _il.LoadFieldAddress(stateMachineDescriptor.BuilderField); // Stack: ref stateMachine.$builderField
            _il.LoadStateMachineLocalAddress();                        // Stack: ref stateMachine.$builderField, ref stateMachine
            _il.CallBuilderStartMethod();                              // Stack: -

            _il.LoadStateMachineLocal();                               // Stack: stateMachine
            _il.LoadFieldAddress(stateMachineDescriptor.BuilderField); // Stack: ref stateMachine.$builderField
            _il.CallBuilderTaskGetter();                               // Stack: stateMachine.$builderField.Task
        }

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }

    private readonly ref struct ResumeWithResultMethodImplementor(
        MethodBuilder resumeWithResultMethod,
        FieldInfo awaiterManagerField,
        StateMachineDescriptor stateMachineDescriptor,
        IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            resumeWithResultMethod,
            stateMachineDescriptor,
            converterDescriptor.ResumeWithResultMethod.GetParameters()[0].ParameterType,
            converterDescriptor.Reader.Type);

        public void DeclareLocals()
        {
            _il.DeclareStateMachineLocal();
            _il.DeclareAwaiterIndexLocal();
            _il.DeclareAwaiterIdLocal();
        }

        public void InitStateMachine()
        {
            _il.InitStateMachine(); // Stack: -
        }

        public void OnUserField(FieldInfo field)
        {
            MethodInfo readFieldMethod = converterDescriptor.Reader.GetReadFieldMethod(field.FieldType);

            _il.LoadReader();                      // Stack: reader
            _il.LoadString(field.Name);            // Stack: reader, $field.Name
            _il.LoadStateMachineLocal();           // Stack: reader, $field.Name, stateMachine
            _il.LoadFieldAddress(field);           // Stack: reader, $field.Name, stateMachine.$field
            _il.CallReaderMethod(readFieldMethod); // Stack: @result[reader.ReadField($field.Name, stateMachine.$field)]
            _il.Pop();                             // Stack: -
        }

        public void OnAwaiterFields(IReadOnlyCollection<IndexedFieldInfo> indexedFields)
        {
            Type resultType = converterDescriptor.ResumeWithResultMethod.GetGenericArguments()[0];
            MethodInfo readIntFieldMethod = converterDescriptor.Reader.GetReadFieldMethod(typeof(int));
            MethodInfo readTypeIdFieldMethod = converterDescriptor.Reader.GetReadFieldMethod(typeof(TypeId));
            FieldInfo? refAwaiterField = stateMachineDescriptor.RefAwaiterField;
            bool hasRefAwaiterField = refAwaiterField is not null;

            int switchCaseCount = hasRefAwaiterField
                ? indexedFields.Count - 1
                : indexedFields.Count;

            Label refAwaiterLabel = hasRefAwaiterField
                ? _il.DefineLabel()
                : default;
            Label[] switchCaseLabels = new Label[switchCaseCount];
            Label switchDefaultCaseLabel = _il.DefineLabel();
            Label endOfMethodLabel = _il.DefineLabel();

            for (int i = 0; i < switchCaseCount; i++)
            {
                switchCaseLabels[i] = _il.DefineLabel();
            }

            Label awaiterIndexNextLabel = hasRefAwaiterField
                ? refAwaiterLabel
                : endOfMethodLabel;

            // if (reader.ReadField(InspectionConstants.AwaiterFieldName, ref awaiterIndex))
            _il.InitAwaiterIndex();                               // Stack: -
            _il.InitAwaiterId();                                  // Stack: -
            _il.LoadReader();                                     // Stack: reader
            _il.LoadString(InspectionConstants.AwaiterFieldName); // Stack: reader, $AwaiterFieldName
            _il.LoadAwaiterIndexAddress();                        // Stack: reader, $AwaiterFieldName, ref awaiterIndex
            _il.CallReaderMethod(readIntFieldMethod);             // Stack: @result[reader.ReadField($AwaiterFieldName, ref awaiterIndex)]
            _il.BranchIfFalse(awaiterIndexNextLabel);             // Stack: -

            // switch (awaiterIndex - 1)
            _il.LoadAwaiterIndex();             // Stack: awaiterIndex
            _il.LoadInt(1);                     // Stack: awaiterIndex, 1
            _il.Subtract();                     // Stack: awaiterIndex - 1
            _il.Switch(switchCaseLabels);       // Stack: -
            _il.Branch(switchDefaultCaseLabel); // Stack: -

            foreach ((FieldInfo field, int index) in indexedFields)
            {
                if (index == -1)
                {
                    refAwaiterField = field;
                    continue;
                }

                Debug.Assert(index >= 1 && index <= switchCaseLabels.Length, "Awaiter indexes should be in the range [1, count].");

                Type awaiterType = field.FieldType;

                // case $index - 1:
                _il.MarkLabel(switchCaseLabels[index - 1]);
                if (awaiterType.IsGenericType && awaiterType.GetGenericTypeDefinition() == typeof(DTask<>.Awaiter))
                {
                    Label fromResultLabel = _il.DefineLabel();
                    Type expectedResultType = awaiterType.GetGenericArguments()[0];

                    // if (typeof(TResult) != $expectedResultType)
                    _il.LoadToken(resultType);                           // Stack: $resultType
                    _il.LoadToken(expectedResultType);                   // Stack: $expectedResultType
                    _il.CallRuntimeTypeHandleEqualsMethod();             // Stack: $resultType == $expectedResultType
                    _il.BranchIfFalse(fromResultLabel, shortForm: true); // Stack: -

                    // throw new InvalidOperationException("...");
                    _il.LoadString("Invalid attempt to resume a d-async method."); // "Invalid attempt to resume a d-async method."
                    _il.NewInvalidOperationException();                            // new InvalidOperationException("...")
                    _il.Throw();                                                   // -

                    // else
                    // stateMachine.$field = DTask.FromResult(result).GetAwaiter();
                    _il.MarkLabel(fromResultLabel);
                    _il.LoadStateMachineLocal();          // Stack: stateMachine
                    _il.LoadResult();                     // Stack: stateMachine, result
                    _il.CallFromResultMethod(resultType); // Stack: stateMachine, DTask.FromResult(result)
                    _il.CallGetAwaiterMethod();           // Stack: stateMachine, DTask.FromResult(result).GetAwaiter()
                    _il.StoreField(field);                // Stack: -
                    _il.Branch(endOfMethodLabel);         // Stack: -
                }
                else if (awaiterType == typeof(DTask.Awaiter))
                {
                    // stateMachine.$field = DTask.CompletedDTask.GetAwaiter();
                    _il.LoadStateMachineLocal();    // Stack: stateMachine
                    _il.CallCompletedDTaskGetter(); // Stack: stateMachine, $CompletedDTask
                    _il.CallGetAwaiterMethod();     // Stack: stateMachine, $CompletedDTask.GetAwaiter()
                    _il.StoreField(field);          // Stack: -
                    _il.Branch(endOfMethodLabel);   // Stack: -
                }
                else if (awaiterType == typeof(YieldDAwaitable.Awaiter))
                {
                    // throw new InvalidOperationException("...");
                    _il.LoadString("Invalid attempt to resume a d-async method."); // "Invalid attempt to resume a d-async method."
                    _il.NewInvalidOperationException();                            // new InvalidOperationException("...")
                    _il.Throw();                                                   // -
                }
                else
                {
                    // TODO: Support specialized FromResult methods
                    MethodInfo? awaiterFromResultGenericMethod = awaiterType.GetMethod(
                        name: "FromResult",
                        genericParameterCount: 1,
                        bindingAttr: BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        types: [Type.MakeGenericMethodParameter(0)],
                        modifiers: null);

                    if (awaiterFromResultGenericMethod is null)
                    {
                        // throw new InvalidOperationException("...");
                        _il.LoadString("Invalid attempt to resume a d-async method."); // "Invalid attempt to resume a d-async method."
                        _il.NewInvalidOperationException();                            // new InvalidOperationException("...")
                        _il.Throw();                                                   // -
                    }
                    else
                    {
                        MethodInfo awaiterFromResultMethod = awaiterFromResultGenericMethod.MakeGenericMethod(resultType);

                        // stateMachine.$field = $awaiterType.FromResult(result);
                        _il.LoadStateMachineLocal();       // Stack: stateMachine
                        _il.LoadResult();                  // Stack: stateMachine, result
                        _il.Call(awaiterFromResultMethod); // Stack: stateMachine, @result[$awaiterType.FromResult(result)]
                        _il.StoreField(field);             // Stack: -
                        _il.Branch(endOfMethodLabel);      // Stack: -
                    }
                }
            }

            // TODO: InvalidDAsyncStateException
            // default:
            _il.MarkLabel(switchDefaultCaseLabel);
            _il.LoadString("InvalidDAsyncStateException"); // "InvalidDAsyncStateException"
            _il.NewInvalidOperationException();            // new InvalidOperationException("...")
            _il.Throw();                                   // -

            if (refAwaiterField is not null)
            {
                _il.MarkLabel(refAwaiterLabel);

                // else if (reader.ReadField(InspectionConstants.RefAwaiterFieldName, ref awaiterId))
                _il.LoadReader();                                        // Stack: reader
                _il.LoadString(InspectionConstants.RefAwaiterFieldName); // Stack: reader, $RefAwaiterFieldName
                _il.LoadAwaiterIdAddress();                              // Stack: reader, $RefAwaiterFieldName, ref awaiterId
                _il.CallReaderMethod(readTypeIdFieldMethod);             // Stack: @result[reader.ReadField($RefAwaiterFieldName, ref awaiterId)
                _il.BranchIfFalse(endOfMethodLabel, shortForm: true);    // Stack: -

                // stateMachine.$field = _awaiterManager.CreateFromResult(awaiterId, result);
                _il.LoadStateMachineLocal();                   // Stack: stateMachine
                _il.LoadThis();                                // Stack: stateMachine, this
                _il.LoadField(awaiterManagerField);            // Stack: stateMachine, _awaiterManager
                _il.LoadAwaiterId();                           // Stack: stateMachine, _awaiterManager, awaiterId
                _il.LoadResult();                              // Stack: stateMachine, _awaiterManager, awaiterId, result
                _il.CallCreateFromResultMethod(resultType);    // Stack: stateMachine, @result[_awaiterManager.CreateFromResult(awaiterId, result)]
                _il.StoreField(refAwaiterField);               // Stack: -
                _il.Branch(endOfMethodLabel, shortForm: true); // Stack: -
            }

            _il.MarkLabel(endOfMethodLabel);
            _il.LoadStateMachineLocal();                         // Stack: stateMachine
            _il.CallBuilderCreateMethod();                       // Stack: stateMachine, @result[$builderType.Create()]
            _il.StoreField(stateMachineDescriptor.BuilderField); // Stack: -

            _il.LoadStateMachineLocal();                               // Stack: stateMachine
            _il.LoadFieldAddress(stateMachineDescriptor.BuilderField); // Stack: ref stateMachine.$builderField
            _il.LoadStateMachineLocalAddress();                        // Stack: ref stateMachine.$builderField, ref stateMachine
            _il.CallBuilderStartMethod();                              // Stack: -

            _il.LoadStateMachineLocal();                               // Stack: stateMachine
            _il.LoadFieldAddress(stateMachineDescriptor.BuilderField); // Stack: ref stateMachine.$builderField
            _il.CallBuilderTaskGetter();                               // Stack: stateMachine.$builderField.Task
        }

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }
}
