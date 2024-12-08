using DTasks.Generated.Emit;
using DTasks.Inspection.Dynamic.Descriptors;
using DTasks.Marshaling;
using DTasks.Utils;
using FluentAssertions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit.Sdk;
using static DTasks.Inspection.Dynamic.InspectionFixtures;

namespace DTasks.Inspection.Dynamic;

public partial class DynamicStateMachineInspectorTests
{
    private static readonly string s_classAwaiterTypeId = nameof(ClassAwaiter);

    private readonly ITypeResolver _typeResolver;
    private readonly IConverterDescriptor _converterDescriptor;
    private readonly IReaderDescriptor _readerDescriptor;
    private readonly IWriterDescriptor _writerDescriptor;
    private readonly ILGenerator _il;
    private readonly DynamicStateMachineInspector _sut;

    public DynamicStateMachineInspectorTests()
    {
        _typeResolver = Substitute.For<ITypeResolver>();

        _typeResolver
            .GetTypeId(typeof(ClassAwaiter))
            .Returns(new TypeId(s_classAwaiterTypeId));

        _converterDescriptor = Substitute.For<IConverterDescriptor>();
        _readerDescriptor = Substitute.For<IReaderDescriptor>();
        _writerDescriptor = Substitute.For<IWriterDescriptor>();
        _il = Substitute.For<ILGenerator>();

        _converterDescriptor.Reader.Returns(_readerDescriptor);
        _converterDescriptor.Writer.Returns(_writerDescriptor);

        var converterDescriptorFactory = Substitute.For<IConverterDescriptorFactory>();
        converterDescriptorFactory
            .CreateDescriptor(StateMachineType)
            .Returns(_converterDescriptor);

        _sut = new(new DynamicAssembly(), Substitute.For<IAwaiterManager>(), converterDescriptorFactory);
    }

    [Fact]
    public void GetConverter_ShouldBuildCorrectMethods_WhenCallbackIsClass() => RunGenericOverload();

    // IL reference
    // DEBUG
    // - Suspend: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BPsACGwBgFlFIgBZgqDNlUG8VPABQBJAIIB3RWGPqqigOa8iATl4PnvAJQ4A3jgEwUQALPIU4hwMVDAmPAwAZvJKKupaOgwE4qlqGtq6UARmfJHRVOJgrFT6KgAewNllMZXVtQwNRQDqPHauVr3GftghBIEjoyEDfTwkPX0AYmAMEHEARAA8AHzoAPq7OcoMa0WHafmZJPt7B7k+0hOTBNPGc4MMSyvrijxOJ9m5dIFJg/Jz3IJPZ7vWbzFSfVYmTZbUL7YDacT/M55DK6K67FG7NFgcTgx6TF68N6LZYIzYQVgiRQQLYAVmumMBF1xu3pjIguzZu3QpMhUJmVLhNPWGwS4goEGArP2qA5RyBl12svlwAFypFkIpMPe8OlW3E+wAzKrzjimPtzbsLfqnhCnmBkiY5VEWlUatV6sASGZxKVvbEGHEEskserubsKNcfMNReNRaNDRKPlLEQBiZAeHj/YUPNMhZAAdhLkIAvq7JnXRu6CJ7mhVfe0GkGQ62YBH4kkAWquXb45akw2QqnS2LXrCs19c/neJqkv8lzwvC5ZgBxBjAAAqAE9omY4jHh3iE46k1XSxXb5Na2TRhPgk2W2HWn6aB1A8HQ+UvaRgO562pe+yhOOz6Tq+5LQpmJqLgW/yoM6ab3rBT6jE+T44DoxiJBoWQlK2X4dsAAR1gARqwrAQMU3ZhkBGz7tYtjGFs/bJKxNgzO4vgPLh2D4bwhFiMUbEzJuvCUWSR4njABC7gex4MKeJisFRABWDAiI066kkJOQ8BQekEPJakwA8sioCkQ62rJozIBaBD4QQ1z2rkD7OQQFjiIeVAiMgABsqh7porAwAAQhQkC9jwGz4Vs7mEvsVGxasvDeS5qiHhZBCgtlvn+YFYVopFtS2LoPApQSRJSHWPnEIQPIMkyupCkVbkrnKCodagXU0ClDoWkVACaUoACKSUoVEQEwM2uPsV7oEVIWLTVy3KkVmk6WZW2OkVk0hYlNBbCQG0pVeoSCTIwk/jwYnEdNJUiAs9JWNZ90EURxQvQFIgbY5IQ0XRDFsJw80qIp/gEC4wCSAQWFluEtQ6BQDDmP9gXvawVgEIkH2GXdci5RZX1yH5ANlRFMCVZkPAU7Zc48MDwRNeEc4mixJqcc1BNSgAcoojBFPuJoEIITIYz4YxIzgQlyMdwUsVsbNEC5l3KRtJiywAvMlvaEdqX1OS5CSKDA1QQIeAImWZl0gH9VOBRtRRmMwgyiEygusLQiSHhDHBQ76dZTk8Pmg/RwZB1DEYEAbBBG4oJuwT5+5KXuZASNqusJ4bSQpwqACED5m2EBBo1QGNYy7b0fQTRNy8jEcucg4QAPJ+pDe59sQtkiP66PKL6suwy3kwcwQACqFSKIkDBd7HvdxP3BCDzQw+0NUY/y2SOF3RbVtUDbdumY0E1fNNvGzfN6s+Vre46/rBfGwqD6NebDCW9btvGefvkb6uCdmYbGgMgE8Hdp7Ow3sIC+39oHdgwc9yhzJOHSeLko7gyQXHRSidk6p2guzNu4RlLZ16sAPO4804kMrkPaumNQF11xvjQmeNd4T3Lu3AgS8cErxMGvDetBq4jx3s3GhFdZ7iHnovbuyDoYCPQAPehFBRFUA4XWHCQA===
    // - Resume (with void): https://sharplab.io/#v2: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
    // RELEASE
    // - Suspend: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TO2ABDYJQCyU3gAswxSvWJc2s1gAoAkgEEA7lLA6FxKQHM22AJxtLNtgEpUAb1SYf2ACwShAKMlMQAJrqslABmEtKyCsqqlJgC8fKKKmoANJj67EEhxAJgdMQasgAewKmFoSVlFZTVuQDqrOZ2xh067ii+mF79A77dnay47Z0AYmCUEBEARAA8AHwIAPobaTKUi7k7CZnJuFub2+muIsMjmGM6kz2Us/NLUqzW+6npiVnU79Yrt5bncnhMprIXgtdCtVn4tsAVAIvocMkk1KcNvCNoiwAIgTcRvc2I8ZnNoSsIHReFIIKsAKxbOAon7HDEbKk0iAbRkbOAEkGYYG3MCxXQCOrFUrlMpVYC4fQCArBUJhSgRKKxVG/E5bQhnVx9QVDQUDYngp5QpYAYhgjlYXwQAtNPhgAHZroKAL7Cka+gaizDiyUNGWkZryxXKopqjUxb67HXsjb6jYAZkN/t8JpdoPGpMh5JtdrYG01XxLrGctgmAHFKMAACoATxC+gi2rZ1D1Wwzzpd7s9IJ9hIGWZ8geDKqljVlEYVSslsci8c76O7Ka2fkzo+z46JYILzyLMNt9q+/KHA49+5HAxHI9Qqh00UUKXyIelTWqnl9ACM6DoCA8kXadY2WRsTDMHRVhXWJINMcYHDca5HxQZ82Fffg8ig8ZqzYX9CRbNswkwesm1bSh210Og/wAK0oXgakrAk0LSVhCCYzBiKosJrlQdjOJqSREy7QiBhgNNMGfTAzi2VEr2wKTDAEZtiF4GAADY5AbJQ6DCAAhQhIDVVhlmfVZZJxLY/2MhY2EUyTMDkZseMwAFHOU1T1J0xF9IqMw1FYKzsVxYRfScnAsA5alaR5JlPMwABNIsABFcOkP8IGoDK7B7DYEESrTcuC/K4ES2iGK4/K00S1KtPM0hVlwEqrNTPxUNEdDw1YLD33S7zeGmKljH47qXzfPIBrU3gSvE3wAKAkD6CYbLZFIjxMFsYAhEwO9fBgAIKlUQhKAMab1OGuhjEwaIRtYrrxBcnixvEFSZt8vSwgC5JWFeuBMAhAiUBzV0pMOwHLSLCCrVgqLbqLAA5KQqFyRsrUwLhaVO1xBj21A0PEerNIg1Z5rBzBWvIkrdFxgBeSy1VfQgIB2rqJKkqIpDCMoIGbb4OK41qQCm971JK3J9BoHo+FpRG6DIaJmxWxg1ulX1QZGJzFuAxUVbW9VMAZzAmakFm2d3CnGzIht8EEc3aaNxmYjN1mAEJFI5/xMGO4hTvOsWhpG277rx/bbkigIAHkZVWht1V0HAAd4WUTpkaVcc28OtfBgIAFViikaJKBj/X44iJPMBT0g07IMpM/xwkHy6rmeeIPmBaE5K0oyqQssocmlMpxCdBt4Aafp53mdZxSIs5yhud5/nBKFke7BF/QLtmtfWEl6XzFliB5cV5WGFVht1cJTWvZ15az4N0jjdN83PYO3Ox7tiVWcdrP90jn3U5+zOpvQOV0bp3Wug3bOXsIal3vuXROCBk6AMIOneuYc/7vwLgIIuJdY7n3Wog5BNc/ZoOIFA30D4gA===
    // - Resume (with void): https://sharplab.io/#v2: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
    private void _GetConverter_ShouldBuildCorrectMethods_WhenCallbackIsClass<TStateMachine>()
    {
        // Arrange
        Type expectedConverterType = typeof(IStateMachineConverter1<TStateMachine>);
        Type readerType = typeof(ClassReader);
        Type writerType = typeof(ClassWriter);

        Dictionary<Type, MethodInfo> writeFieldMethods = [];
        Dictionary<Type, MethodInfo> readFieldMethods = [];
        var writerParameter = Substitute.For<ParameterInfo>();
        var readerParameter = Substitute.For<ParameterInfo>();
        writerParameter.ParameterType.Returns(writerType);
        readerParameter.ParameterType.Returns(readerType);

        _converterDescriptor.Type.Returns(expectedConverterType);
        _converterDescriptor.SuspendMethod.Returns(IStateMachineConverter1<TStateMachine>.SuspendMethod);
        _converterDescriptor.ResumeWithVoidMethod.Returns(IStateMachineConverter1<TStateMachine>.ResumeWithVoidMethod);
        _converterDescriptor.ResumeWithResultMethod.Returns(IStateMachineConverter1<TStateMachine>.ResumeWithResultMethod);
        _readerDescriptor.Type.Returns(readerType);
        _writerDescriptor.Type.Returns(writerType);
        _writerDescriptor
            .GetWriteFieldMethod(Arg.Any<Type>())
            .Returns(call =>
            {
                Type fieldType = call.Arg<Type>();
                if (!writeFieldMethods.TryGetValue(fieldType, out MethodInfo? method))
                {
                    method = Substitute.For<MethodInfo>();
                    method.GetParameters().Returns([writerParameter]);
                    writeFieldMethods.Add(fieldType, method);
                }

                return method;
            });
        _readerDescriptor
            .GetReadFieldMethod(Arg.Any<Type>())
            .Returns(call =>
            {
                Type fieldType = call.Arg<Type>();
                if (!readFieldMethods.TryGetValue(fieldType, out MethodInfo? method))
                {
                    method = Substitute.For<MethodInfo>();
                    method.GetParameters().Returns([readerParameter]);
                    readFieldMethods.Add(fieldType, method);
                }

                return method;
            });

        // Act
        object converter;
        using (_il.InterceptCalls())
        {
            converter = _sut.GetConverter(typeof(TStateMachine));
        }

        // Assert
        converter.Should().BeAssignableTo(expectedConverterType);
        Received.InOrder(() =>
        {
            #region Constructor

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Stfld, Arg.Is(ConverterField("_awaiterManager")));
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Call, Arg.Is(ObjectConstructor()));
            _il.Emit(OpCodes.Ret);

            #endregion

            #region Suspend

            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(int)]);

            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, "arg");
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(MyType)]);

            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(AsyncMethodContainer)]);

            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(string)]);

#if DEBUG
            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(int)]);

            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(int)]);
#endif

            _il.DefineLabel();
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>u__1")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod(typeof(YieldDAwaitable.Awaiter))));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, InspectionConstants.AwaiterFieldName);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(int)]);
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.DefineLabel();
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>u__3")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod(typeof(object))));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, InspectionConstants.RefAwaiterFieldName);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, Arg.Is(ConverterField("_awaiterManager")));
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>u__3")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetTypeIdMethod()));
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(TypeId)]);
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.DefineLabel();
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>u__4")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod(typeof(DTask<int>.Awaiter))));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldstr, InspectionConstants.AwaiterFieldName);
            _il.Emit(OpCodes.Ldc_I4_2);
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(int)]);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);

            #endregion

            #region Resume (with void)

            _il.Emit(OpCodes.Newobj, StateMachineConstructor);
            _il.Emit(OpCodes.Stloc_0);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(int)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "arg");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(MyType)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(AsyncMethodContainer)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(string)]));
            _il.Emit(OpCodes.Pop);

#if DEBUG
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(int)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(int)]));
            _il.Emit(OpCodes.Pop);
#endif

            _il.DefineLabel();
            _il.DefineLabel();
            _il.DefineLabel();
            _il.DefineLabel();
            _il.DefineLabel();
            _il.DefineLabel();

            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Stloc_1);

            _il.Emit(OpCodes.Ldloca_S, 2);
            _il.Emit(OpCodes.Initobj, typeof(TypeId));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, InspectionConstants.AwaiterFieldName);
            _il.Emit(OpCodes.Ldloca_S, 1);
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(int)]));
            _il.Emit(OpCodes.Brfalse, Arg.Any<Label>());

            _il.Emit(OpCodes.Ldloc_1);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Sub);
            _il.Emit(OpCodes.Switch, Arg.Is<Label[]>(labels => labels.Length == 2));
            _il.Emit(OpCodes.Br, Arg.Any<Label>());

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Br, Arg.Any<Label>());

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldstr, "Invalid attempt to resume a d-async method.");
            _il.Emit(OpCodes.Newobj, Arg.Is(InvalidOperationExceptionConstructor()));
            _il.Emit(OpCodes.Throw);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldstr, "InvalidDAsyncStateException");
            _il.Emit(OpCodes.Newobj, Arg.Is(InvalidOperationExceptionConstructor()));
            _il.Emit(OpCodes.Throw);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, InspectionConstants.RefAwaiterFieldName);
            _il.Emit(OpCodes.Ldloca_S, 2);
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(TypeId)]));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());

            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, Arg.Is(ConverterField("_awaiterManager")));
            _il.Emit(OpCodes.Ldloc_2);
            _il.Emit(OpCodes.Callvirt, Arg.Is(CreateFromVoidMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__3")));
            _il.Emit(OpCodes.Br_S, Arg.Any<Label>());

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldstr, "InvalidDAsyncStateException");
            _il.Emit(OpCodes.Newobj, Arg.Is(InvalidOperationExceptionConstructor()));
            _il.Emit(OpCodes.Throw);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Call, Arg.Is(BuilderCreateMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>t__builder")));

            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Call, Arg.Is(BuilderStartMethod()));

            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Call, Arg.Is(BuilderTaskGetter()));

            _il.Emit(OpCodes.Ret);
            
            #endregion

            #region Resume (with result)
            
            _il.Emit(OpCodes.Ret);
            
            #endregion
        });
    }

    private void RunGenericOverload([CallerMemberName] string methodName = "")
    {
        MethodInfo method = typeof(DynamicStateMachineInspectorTests).GetRequiredMethod(
            name: "_" + methodName,
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
            parameterTypes: []);

        method.MakeGenericMethod(StateMachineType).Invoke(this, BindingFlags.DoNotWrapExceptions, null, [], null);
    }
}
