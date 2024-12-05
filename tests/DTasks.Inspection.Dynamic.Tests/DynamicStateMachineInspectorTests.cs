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
    // - Suspend: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BPsACGwBgFlFIgBZgqDNlUG8VPABQBJAIIB3RWGPqqigOa8iATl4PnvAJQ4A3jgEwUQALPIU4hwMVDAmPAwAZvJKKupaOgwE4qlqGtq6UARmfJHRVOJgrFT6KgAewNllMZXVtQwNRQDqPHauVr3GftghBIEjoyEDfTwkPX0AYmAMEHEARAA8AHzoAPq7OcoMa0WHafmZJPt7B7k+0hOTBNPGc4MMSyvrijxOJ9m5dIFJg/Jz3IJPZ7vWbzFSfVYmTZbUL7YDacT/M55DK6K67FG7NFgcTgx6TF68N6LZYIzYQVgiRQQLYAVmumMBF1xu3pjIguzZu3QpMhUJmVLhNPWGwS4goEGArP2qA5RyBl12svlwAFypFkIpMPe8OlW3E+wAzKrzjimPtzbsLfqnhCnmBkiY5VEWlUatV6sASGZxKVvbEGHEEskserubsKNcfMNReNRaNDRKPlLEQBiZAeHj/PDOtPIADsD1FAF9XZNa6N3QRPc0Kr72g0gyGWzAI/EkgC1Vy7fHLUn6yFU2nghnYVmvrn87xNUl/oueF4XLMAOIMYAAFQAntEzHEY0O8QnHUnK1OiBXx8Ea2TRg+CI3m2HWn6aB1A8HQ+UPaRv2Z62he+yhGOz4Tq+6bQpmJoLgW/zCjeU7lmhkxPqMT5PjgOjGIkGhZCULZfu2wABLWABGrCsBAxRdmGQEbHu1i2MYWx9skbE2DM7i+A8eHYARvBEWIxTsTMG68FRZKHseMAEDu+5HgwJ4mKw1EAFYMCIjRrqSwk5DwFD6QQCnqTADyyKgKSDracmjMgFpvjQBDXPauSYS5BAWOIB5UCIyAAGyqLumisDAABCFCQD2PAbARWweYS+zUXFqy8D5rmqAelkEKCOV+QFQXhWiUW1LYug8KlBJElIta+cQhA8gyTK6kKxUER5WoKp1qDde59qWsVACaUoACJSUo1EQEwM2uPsl7oMVoWLbVy3KsVWm6eZW2OsVk2hUlNBbCQG2pZeoRCTIIk/jw4kkdNpUiAs9JWDZ92EcRxQvYFIgbU5IS0fRjFsJw80qEp/gEC4wCSAQ2EhMg4S1DoFAMOY/1Be9rBWAQiQfUZd1yHlllfXI/kA+VkUwFVmQ8JTdmzjwwPBM14SziarEmlxLWE1KAByiiMEUe4mgQghMpjPhjEjODCXIx0haxWzs0QrmXSpG0mHLAC8KU9kR2pfc5rkJIoMDVBAB4AqZ5mXSAf3U0FG1FGYzCDKITJC6wtCJAeEMcFDvq1pOTy+aDDHBsHUMRgQhsEMbiim6+vl7spu5kBI2p64nRtJKnCoAISYebYQEOjVCY9jrtvR9hPE/LyOR65qMEAA8n6kO7r2xB2SI/oY8ovpy7DreTJzBAAKoVIoiQMN3cd93EA8EEPNAj7Q1TjwrZK4XdlvW1Qtv22ZjQTV8018bN80a752u7rrBuFybCqYU1FsMFbNt2yZF8/K31cM7MwONAbAJ4B7L2dgfYQD9gHIO7AQ67jDmSCOU9XLR3Bsg+OSkk4pzTtBDm7dwgqRznKBU+cJ7p1IVXYeNcsZgPrnjAmRN8Z70nhXDuy9cGrxMOvTetAa6j13i3Whlc57iAXkvHuKDoYCPQIPBhFBRFUE4bWXCQA===
    // RELEASE
    // - Suspend: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TO2ABDYJQCyU3gAswxSvWJc2s1gAoAkgEEA7lLA6FxKQHM22AJxtLNtgEpUAb1SYf2ACwShAKMlMQAJrqslABmEtKyCsqqlJgC8fKKKmoANJj67EEhxAJgdMQasgAewKmFoSVlFZTVuQDqrOZ2xh067ii+mF79A77dnay47Z0AYmCUEBEARAA8AHwIAPobaTKUi7k7CZnJuFub2+muIsMjmGM6kz2Us/NLUqzW+6npiVnU79Yrt5bncnhMprIXgtdCtVn4tsAVAIvocMkk1KcNvCNoiwAIgTcRvc2I8ZnNoSsIHReFIIKsAKxbOAon7HDEbKk0iAbRkbOAEkGYYG3MCxXQCOrFUrlMpVYC4fQCArBUJhSgRKKxVG/E5bQhnVx9QVDQUDYngp5QpYAYhgjlYX3QAtNPhgAHZroKAL7Cka+gaizDiyUNGWkZryxXKopqjUxb67HXsjb6jYAZkN/t8JpdoPGpMh5JtdrYG01XxLrGctgmAHFKMAACoATxC+gi2rZ1D1Wwzzpd7s9IJ9hIGWZ8geDKqljVlEYVSslsci8c76O7Ka2fkzo+z46JYILzyLMNt9q+CH7psH+5HAxHI9Qqh00UUKXyIelTWqnl9ACM6DoCA8kXadY2WRsTDMHRVhXWJINMcYHDca5HxQZ82Fffg8ig8ZqzYX9CRbNswkwesm1bSh210Og/wAK0oXgakrAk0LSVhCCYzBiKosJrlQdjOJqSREy7QiBhgNNMGfTAzi2VEh1dKTDAEZtiF4GAADY5AbJQ6DCAAhQhIDVVhlmfVZZJxLY/2MhY2EU7ApLkZseMwAFHMkzAVLU3gdMRfSKjMNRWCs7FcWEX0vJwLAOWpWkeSZTypIATSLAARXDpD/CBqCyuwew2BBkuwTT8tCwq4BK2iGK4wq0xK9KtPM0hVlwcqrNTPxUNEdDw1YLD30y1T1OmKljH4vqXzfPJht88rxN8ACgJA+gmFy2RSI8TBbGAIRMDvXwYACCpVEISgDDm0bxswaJxtY3rxBcnjJvEHz1P8vSwiC5JWFeuBMAhAiUBzJT/EBy0iwgq1YJi26iwAOSkKhckbK1MC4WlztcQYDtQNDxCazSINWRawY68jyt0HGAF5LLVV9CAgPbeokqSoikMIyggZtvg4riOpAWb3t4crcn0Ggej4WkEboMhombNbGA26VfVBkYvOW4DFSVjb1UwOnMAZqQmZZ3cwcbMiG3wQRTepg36ZiE3mYAQkctnwdO4hzsukWxroYxbvu3HDtuaKAgAeRldaG3VXQcAB3hZTOmRpRx7bQ41qTjswABVYopGiSgo912OIgTzAk9IFOyDKdO8cJB9eo5rniB5vmhMwNLXkyxDstysmnO8vu7Epke9Fpx3GeZxyovZyhOe53nBIF8fMCF/QrtF8fxcl8xpYgWX5cVhhlYbVXCXVj2tdW0+9dIw3jdN92juzgJyJtiVmftjP93DzAvY+03n7G6d0A710zh7HOJc75l3jggROydvapzriHP+b884FyLjAmOm14GIOrsg2uxAIG+gfEAA
    private void _GetConverter_ShouldBuildCorrectMethods_WhenCallbackIsClass<TStateMachine>()
    {
        // Arrange
        Type expectedConverterType = typeof(IStateMachineConverter1<TStateMachine>);
        Type readerType = typeof(ClassReader);
        Type writerType = typeof(ClassWriter);

        Dictionary<Type, MethodInfo> writeFieldMethods = [];
        var writerParameter = Substitute.For<ParameterInfo>();
        writerParameter.ParameterType.Returns(writerType);

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
            _il.Emit(OpCodes.Ldc_I4_0);
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
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(int)]);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);

#endregion
            
            #region Resume (with void)

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
