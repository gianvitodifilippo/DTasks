﻿using System.Reflection;
using System.Reflection.Emit;
using DTasks.Infrastructure.Marshaling;
using DTasks.Inspection.Dynamic.Descriptors;
using static DTasks.Inspection.Dynamic.InspectionFixtures;

namespace DTasks.Inspection.Dynamic;

public partial class DynamicStateMachineInspectorTests
{
    private static readonly string s_classAwaiterTypeId = nameof(ClassAwaiter);

    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly ISuspenderDescriptor _suspenderDescriptor;
    private readonly IResumerDescriptor _resumerDescriptor;
    private readonly IReaderDescriptor _readerDescriptor;
    private readonly IWriterDescriptor _writerDescriptor;
    private readonly ILGenerator _il;
    private readonly DynamicStateMachineInspector _sut;

    public DynamicStateMachineInspectorTests()
    {
        _typeResolver = Substitute.For<IDAsyncTypeResolver>();

        _typeResolver
            .GetTypeId(typeof(ClassAwaiter))
            .Returns(TypeId.FromConstant(s_classAwaiterTypeId));

        _suspenderDescriptor = Substitute.For<ISuspenderDescriptor>();
        _resumerDescriptor = Substitute.For<IResumerDescriptor>();
        _readerDescriptor = Substitute.For<IReaderDescriptor>();
        _writerDescriptor = Substitute.For<IWriterDescriptor>();
        _il = Substitute.For<ILGenerator>();

        _suspenderDescriptor.Writer.Returns(_writerDescriptor);
        _resumerDescriptor.Reader.Returns(_readerDescriptor);

        var converterDescriptorFactory = Substitute.For<IConverterDescriptorFactory>();
        converterDescriptorFactory
            .CreateSuspenderDescriptor(StateMachineType)
            .Returns(_suspenderDescriptor);
        converterDescriptorFactory.ResumerDescriptor.Returns(_resumerDescriptor);

        _sut = new(new DynamicAssembly(typeof(DynamicStateMachineInspectorTests)), Substitute.For<IAwaiterManager>(), converterDescriptorFactory);
    }

    // IL reference
    // DEBUG
    // - Suspend: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BPsACGwBgFlFIgBZgqDNlUG8VPABQBJAIIB3RWGPqqigOa8iATl4PnvAJQ4A3jgEwUQALPIU4hwMVDAmPAwAZvJKKupaOgwE4qlqGtq6UARmfJHRVOJgrFT6KgAewNllMZXVtQwNRQDqPHauVr3GftghBIEjoyEDfTwkPX0AYmAMEHEARAA8AHzoAPq7OcoMa0WHafmZJPt7B7k+0hOTBNPGc4MMSyvrijxOJ9m5dIFJg/Jz3IJPZ7vWbzFSfVYmTZbUL7YDacT/M55DK6K67FG7NFgcTgx6TF68N6LZYIzYQVgiRQQLYAVmumMBF1xu3pjIguzZu3QpMhUJmVLhNPWGwS4goEGArP2qA5RyBl12svlwAFypFkIpMPe8OlW3E+wAzKrzjimPtzbsLfqnhCnmBkiY5VEWlUatV6sASGZxKVvbEGHEEskserubsKNcfMNReNRaNDRKPlLEQBiZAeHj/YUPNMhZAAdhLkIAvq7JnXRu6CJ7mhVfe0GkGQ62YBH4kkAWquXb45akw2QqnS2LXrCs19c/neJqkv8lzwvC5ZgBxBjAAAqAE9omY4jHh3iE46k1XSxXb5Na2TRhPgk2W2HWn6aB1A8HQ+UvaRgO562pe+yhOOz6Tq+5LQpmJqLgW/yoM6ab3rBT6jE+T44DoxiJBoWQlK2X4dsAAR1gARqwrAQMU3ZhkBGz7tYtjGFs/bJKxNgzO4vgPLh2D4bwhFiMUbEzJuvCUWSR4njABC7gex4MKeJisFRABWDAiI066kkJOQ8BQekEPJakwA8sioCkQ62rJozIBaBD4QQ1z2rkD7OQQFjiIeVAiMgABsqh7porAwAAQhQkC9jwGz4Vs7mEvsVGxasvDeS5qiHhZBCgtlvn+YFYVopFtS2LoPApQSRJSHWPnEIQPIMkyupCkVbkrnKCodagXU0ClDoWkVACaUoACKSUoVEQEwM2uPsV7oEVIWLTVy3KkVmk6WZW2OkVk0hYlNBbCQG0pVeoSCTIwk/jwYnEdNJUiAs9JWNZ90EURxQvQFIgbY5IQ0XRDFsJw80qIp/gEC4wCSAQWFluEtQ6BQDDmP9gXvawVgEIkH2GXdci5RZX1yH5ANlRFMCVZkPAU7Zc48MDwRNeEc4mixJqcc1BNSgAcoojBFPuJoEIITIYz4YxIzgQlyMdwUsVsbNEC5l3KRtJiywAvMlvaEdqX1OS5CSKDA1QQIeAImWZl0gH9VOBRtRRmMwgyiEygusLQiSHhDHBQ76dZTk8Pmg/RwZB1DEYEAbBBG4oJuwT5+5KXuZASNqusJ4bSQpwqACED5m2EBBo1QGNYy7b0fQTRNy8jEcucg4QAPJ+pDe59sQtkiP66PKL6suwy3kwcwQACqFSKIkDBd7HvdxP3BCDzQw+0NUY/y2SOF3RbVtUDbdumY0E1fNNvGzfN6s+Vre46/rBfGwqD6NebDCW9btvGefvkb6uCdmYbGgMgE8Hdp7Ow3sIC+39oHdgwc9yhzJOHSeLko7gyQXHRSidk6p2guzNu4RlLZ16sAPO4804kMrkPaumNQF11xvjQmeNd4T3Lu3AgS8cErxMGvDetBq4jx3s3GhFdZ7iHnovbuyDoYCPQAPehFBRFUA4XWHCQA===
    // RELEASE
    // - Suspend: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TO2ABDYJQCyU3gAswxSvWJc2s1gAoAkgEEA7lLA6FxKQHM22AJxtLNtgEpUAb1SYf2ACwShAKMlMQAJrqslABmEtKyCsqqlJgC8fKKKmoANJj67EEhxAJgdMQasgAewKmFoSVlFZTVuQDqrOZ2xh067ii+mF79A77dnay47Z0AYmCUEBEARAA8AHwIAPobaTKUi7k7CZnJuFub2+muIsMjmGM6kz2Us/NLUqzW+6npiVnU79Yrt5bncnhMprIXgtdCtVn4tsAVAIvocMkk1KcNvCNoiwAIgTcRvc2I8ZnNoSsIHReFIIKsAKxbOAon7HDEbKk0iAbRkbOAEkGYYG3MCxXQCOrFUrlMpVYC4fQCArBUJhSgRKKxVG/E5bQhnVx9QVDQUDYngp5QpYAYhgjlYXwQAtNPhgAHZroKAL7Cka+gaizDiyUNGWkZryxXKopqjUxb67HXsjb6jYAZkN/t8JpdoPGpMh5JtdrYG01XxLrGctgmAHFKMAACoATxC+gi2rZ1D1Wwzzpd7s9IJ9hIGWZ8geDKqljVlEYVSslsci8c76O7Ka2fkzo+z46JYILzyLMNt9q+/KHA49+5HAxHI9Qqh00UUKXyIelTWqnl9ACM6DoCA8kXadY2WRsTDMHRVhXWJINMcYHDca5HxQZ82Fffg8ig8ZqzYX9CRbNswkwesm1bSh210Og/wAK0oXgakrAk0LSVhCCYzBiKosJrlQdjOJqSREy7QiBhgNNMGfTAzi2VEr2wKTDAEZtiF4GAADY5AbJQ6DCAAhQhIDVVhlmfVZZJxLY/2MhY2EUyTMDkZseMwAFHOU1T1J0xF9IqMw1FYKzsVxYRfScnAsA5alaR5JlPMwABNIsABFcOkP8IGoDK7B7DYEESrTcuC/K4ES2iGK4/K00S1KtPM0hVlwEqrNTPxUNEdDw1YLD33S7zeGmKljH47qXzfPIBrU3gSvE3wAKAkD6CYbLZFIjxMFsYAhEwO9fBgAIKlUQhKAMab1OGuhjEwaIRtYrrxBcnixvEFSZt8vSwgC5JWFeuBMAhAiUBzV0pMOwHLSLCCrVgqLbqLAA5KQqFyRsrUwLhaVO1xBj21A0PEerNIg1Z5rBzBWvIkrdFxgBeSy1VfQgIB2rqJKkqIpDCMoIGbb4OK41qQCm971JK3J9BoHo+FpRG6DIaJmxWxg1ulX1QZGJzFuAxUVbW9VMAZzAmakFm2d3CnGzIht8EEc3aaNxmYjN1mAEJFI5/xMGO4hTvOsWhpG277rx/bbkigIAHkZVWht1V0HAAd4WUTpkaVcc28OtfBgIAFViikaJKBj/X44iJPMBT0g07IMpM/xwkHy6rmeeIPmBaE5K0oyqQssocmlMpxCdBt4Aafp53mdZxSIs5yhud5/nBKFke7BF/QLtmtfWEl6XzFliB5cV5WGFVht1cJTWvZ15az4N0jjdN83PYO3Ox7tiVWcdrP90jn3U5+zOpvQOV0bp3Wug3bOXsIal3vuXROCBk6AMIOneuYc/7vwLgIIuJdY7n3Wog5BNc/ZoOIFA30D4gA===
    [Fact]
    public void GetSuspender_ShouldBuildCorrectMethods_WhenCallbackIsClass()
    {
        // Arrange
        Type expectedSuspenderType = typeof(IStateMachineSuspender1<>).MakeGenericType(StateMachineType);
        Type writerType = typeof(ClassWriter);

        MethodInfo suspendMethod = GetSuspendMethod(expectedSuspenderType, writerType);

        Dictionary<Type, MethodInfo> writeFieldMethods = [];
        var writerParameter = Substitute.For<ParameterInfo>();
        writerParameter.ParameterType.Returns(writerType);

        _suspenderDescriptor.Type.Returns(expectedSuspenderType);
        _suspenderDescriptor.SuspendMethod.Returns(suspendMethod);
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

        // Act
        object suspender;
        using (_il.InterceptCalls())
        {
            suspender = _sut.GetSuspender(StateMachineType);
        }

        // Assert
        suspender.Should().BeAssignableTo(expectedSuspenderType);
        Received.InOrder(() =>
        {
            #region Constructor

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Stfld, Arg.Is(SuspenderField("_awaiterManager")));
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
            _il.Emit(OpCodes.Ldfld, Arg.Is(SuspenderField("_awaiterManager")));
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
        });
    }

    // IL reference
    // DEBUG
    // - Resume (with void): https://sharplab.io/#v2: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=
    // - Resume (with result): https://sharplab.io/#v2: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=
    // - Resume (with exception): https://sharplab.io/#v2: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
    // RELEASE
    // - Resume (with void): https://sharplab.io/#v2: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
    // - Resume (with result): https://sharplab.io/#v2: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=
    // - Resume (with exception): https://sharplab.io/#v2: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=
    [Fact]
    public void GetResumer_ShouldBuildCorrectMethods_WhenCallbackIsClass()
    {
        // Arrange
        Type expectedResumerType = typeof(IStateMachineResumer1);
        Type readerType = typeof(ClassReader);

        Dictionary<Type, MethodInfo> readFieldMethods = [];
        var readerParameter = Substitute.For<ParameterInfo>();
        readerParameter.ParameterType.Returns(readerType);

        MethodInfo resumeWithVoidMethod = GetResumeWithVoidMethod(typeof(IStateMachineResumer1), readerType);
        MethodInfo resumeWithResultMethod = GetResumeWithResultMethod(typeof(IStateMachineResumer1), readerType);
        MethodInfo resumeWithExceptionMethod = GetResumeWithExceptionMethod(typeof(IStateMachineResumer1), readerType);

        _resumerDescriptor.Type.Returns(expectedResumerType);
        _resumerDescriptor.ResumeWithVoidMethod.Returns(resumeWithVoidMethod);
        _resumerDescriptor.ResumeWithResultMethod.Returns(resumeWithResultMethod);
        _resumerDescriptor.ResumeWithExceptionMethod.Returns(resumeWithExceptionMethod);
        _readerDescriptor.Type.Returns(readerType);
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
        object resumer;
        using (_il.InterceptCalls())
        {
            resumer = _sut.GetResumer(StateMachineType);
        }

        // Assert
        resumer.Should().BeAssignableTo(expectedResumerType);
        Received.InOrder(() =>
        {
            #region Constructor

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Stfld, Arg.Is(ResumerField("_awaiterManager")));
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Call, Arg.Is(ObjectConstructor()));
            _il.Emit(OpCodes.Ret);

            #endregion

            #region Resume (with void)

#if DEBUG
            _il.Emit(OpCodes.Newobj, StateMachineConstructor);
            _il.Emit(OpCodes.Stloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Initobj, StateMachineType);
#endif

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(int)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "arg");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(MyType)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(AsyncMethodContainer)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
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

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, Arg.Is(ResumerField("_awaiterManager")));
            _il.Emit(OpCodes.Ldloc_2);
            _il.Emit(OpCodes.Callvirt, Arg.Is(CreateFromVoidMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__3")));
            _il.Emit(OpCodes.Br_S, Arg.Any<Label>());

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Call, Arg.Is(BuilderCreateMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>t__builder")));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Call, Arg.Is(BuilderStartMethod()));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Call, Arg.Is(BuilderTaskGetter()));

            _il.Emit(OpCodes.Ret);

            #endregion

            #region Resume (with result)

#if DEBUG
            _il.Emit(OpCodes.Newobj, StateMachineConstructor);
            _il.Emit(OpCodes.Stloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Initobj, StateMachineType);
#endif

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(int)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "arg");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(MyType)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(AsyncMethodContainer)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
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
            _il.Emit(OpCodes.Ldstr, "Invalid attempt to resume a d-async method.");
            _il.Emit(OpCodes.Newobj, Arg.Is(InvalidOperationExceptionConstructor()));
            _il.Emit(OpCodes.Throw);

            _il.MarkLabel(Arg.Any<Label>());
            _il.DefineLabel();
            _il.Emit(OpCodes.Ldtoken, Arg.Is(GenericMethodParameter(0)));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Ldtoken, typeof(int));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Call, Arg.Is(TypeEqualsMethod()));
            _il.Emit(OpCodes.Brtrue_S, Arg.Any<Label>());

            _il.Emit(OpCodes.Ldstr, "Invalid attempt to resume a d-async method.");
            _il.Emit(OpCodes.Newobj, Arg.Is(InvalidOperationExceptionConstructor()));
            _il.Emit(OpCodes.Throw);

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, Arg.Is(DTaskFromResultMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__4")));
            _il.Emit(OpCodes.Br, Arg.Any<Label>());

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

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, Arg.Is(ResumerField("_awaiterManager")));
            _il.Emit(OpCodes.Ldloc_2);
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Callvirt, Arg.Is(CreateFromResultMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__3")));
            _il.Emit(OpCodes.Br_S, Arg.Any<Label>());

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Call, Arg.Is(BuilderCreateMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>t__builder")));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Call, Arg.Is(BuilderStartMethod()));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Call, Arg.Is(BuilderTaskGetter()));

            _il.Emit(OpCodes.Ret);

            #endregion
            
            #region Resume (with exception)

#if DEBUG
            _il.Emit(OpCodes.Newobj, StateMachineConstructor);
            _il.Emit(OpCodes.Stloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Initobj, StateMachineType);
#endif

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(int)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "arg");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(MyType)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(readFieldMethods[typeof(AsyncMethodContainer)]));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
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
            _il.Emit(OpCodes.Ldstr, "Invalid attempt to resume a d-async method.");
            _il.Emit(OpCodes.Newobj, Arg.Is(InvalidOperationExceptionConstructor()));
            _il.Emit(OpCodes.Throw);

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, Arg.Is(DTaskFromExceptionMethod(typeof(int))));
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__4")));
            _il.Emit(OpCodes.Br, Arg.Any<Label>());

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

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, Arg.Is(ResumerField("_awaiterManager")));
            _il.Emit(OpCodes.Ldloc_2);
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Callvirt, Arg.Is(CreateFromExceptionMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__3")));
            _il.Emit(OpCodes.Br_S, Arg.Any<Label>());

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Call, Arg.Is(BuilderCreateMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>t__builder")));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Call, Arg.Is(BuilderStartMethod()));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Call, Arg.Is(BuilderTaskGetter()));

            _il.Emit(OpCodes.Ret);

            #endregion
        });
    }
}
