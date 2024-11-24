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
        converterDescriptorFactory.CreateDescriptor(StateMachineType).Returns(_converterDescriptor);

        _sut = new(converterDescriptorFactory, _typeResolver);
    }

    [Fact]
    public void GetConverter_ShouldBuildCorrectMethods_WhenCallbackIsClass() => RunGenericOverload();

    // IL reference
    // DEBUG
    // - Suspend: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZgAoAkgBUAnhwZkJrCIN4EeOvbwCUOAN44CNogBYCfCuM1UYCowDNHwAIbAGAFlfEQALMCoGAnE/AOCwiIYoAiUnFwYqcTBWKjYaBgAPYGjnVyycvICi5IB1HjAAngIAd3rG82xbAitOrttWht4SOsGAMTAGCHcAIgAeAD50AH0lmP8GaeS1uJDwyJIV5dXYhlNpXr6WtqGRgPHJmd8eAHNN6JP4vaYn57PrS6ugx4w2u9ymCjm8zsK2A4XEb22QV2iQOS2hS1hYHEfwufQGjRBYwm4LmEFYIl8EHmAFZDgiPsj9ksyRSIEtaUt0DiAYCCbcGGCZrMjOIKBBgDSVqh6etPiiliKxcB2VLuQD8TdQcShfNxCsAMwynYJJmrA1qy7/S5gbwKUXpTLZXI5KrAEhKcRpVwwBjuLzvWWMpgrCiHUwdHk9HldDXA/mCiEAYmQAE5eG9pnhphbo8gAOznHkAXytfVLXRtBDtpQy5Wd+SK7s9Nbcvo8DG8iLlptDS314fLtij0ZsscJd21SdT6eSIt0+mBAHEGMB1JolO4u0HUb39SRl6uNAwFOGSAA1SkUU6FkdEAuDmwl3FdB8ESvVh11yqFN0er0ZH0/Q7ANjS+HcVjsAdnyHV8Y2uOMtQeKc0x4DN0GzG8R3zTC+ifLonyfHAIkaTwQiiVIWy/F0f0sUsACNWF0FJmwdQDZhUABBZpfCBeZ228TjuKBIgUJxQjsGI3hSLEFI1y0YwF1o3E5IIA85IUOSNzfGAcVLTSYFUld9I0o8CGAI8xJkbAYh4CgRGKfSlK6ZB9SIdBCAvCAr26AhnhXSQCAIqy5D4BkTQYJzbBct8aAIQ4VkRHDoo48Q1CoERkAANkCFdQlYGAACEKEgH0eFmYj5jijEVjo4qpl4JLXMCNQVJ+RqCBStKRBy2F8sqHjIiaCCYThdriEIZlyUpFVOXa4i4sVcUZtQObYoSg12oATW1AARLieL8OiICYfbhJDQ4xsy07Giq3sVtLaLWDogArBh7NujaHtcnasvKmh5hIa6DHOtFznEySeGk8i9tS9LRjJZpziI/JIbIlIYa6oGeEimwGKYj02E4Y6AgMixfP8wKHocSoIivZQMbhhGCE8BHLOwWRUAIZq5KR3BOc69KerymB+sSHhebkflsewYcbGi5AHHjbV2MFPjxuZ7UADlfEYZIVEFAhBEvU4fKC9m+YIH7MvY+YcaIVyscM4AsZPAgAF5Kp9UilV55zXKMXwYByCA1HeWz3sdkB0YFkQseSJRmDaURKU11haE8NRCY4YmnVLWW+mivGIGYrPid9d3PY7XwfdfaKVCd7RRXFV2PYIL3q/FABCHC/fseQXVp48lAZkR4dYZpmdZ03a9chWCAAeWdImVzbYhOZEAeqAofwnVMafoLl2eHAAVUyXxPAYRfS5X9w14IDeaFpnecj3sm8NsM2cADoOqBDsO7OKNtB4e0hKHWOnbZKoCDAHhdnvVu7ca64i+oYBggdg6hxsgAjqUCmhR2HjHOOKRE4NGThAVO6dM7sGziuXOuJ869yLiXKhZcDLwKrognk8sHAHkbkqV2b8Z59xplvIeI8x4TxZuPV+lMD72z7lfZhN8FB3wfrQLez8qDSPfpcLhBBT7iHPpfJe1CSbKPQOvTe29aAv33vhHARYgA===
    // RELEASE
    // - Suspend: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDQAUASQAqAT0aV8guhC5tMrVerYBKVAG9UmU9gAsmdoQFLiAE0naAZleABDYJQCy73gAswYkpMAQ8vXwCgygAaTGlrW0piATA6YnpSSgAPYFCbO1T0zK9cuIB1VjAvVkwAdyqagxQzTGMW1rMG6rZcSp6AMTBKCEcAIgAeAD4EAH1ZsM9KMbjFiL9A4Nx5uYXwyj0RDs76xt7+ryGR8fdWAHMV0P3Izepbu8OTE9Oe1j6zq6jSSTKbmebAQICR5rHwbaLbWZg2YQsACT7HTrdGr/QbDIGTCB0XjuCBTACs8zg0OecK2s0JxIgswpszg6O+mC+JzArkkAgKySKGXSpWAuGkAkSdnslEcLieSxe8PmhB2emaHPaHNaWPOALx4wAxDAAJxsR5jdBjdna0wwADsRw5AF8uZ03a0eZg+QKUmlhVlcuLJb6ZXLKK4YUq6bNVbMAMzqj1mLW2n7Yi6UQFG03muLaARqDR/ADilGACiU0kcUdp1BV83juDLFcUlEk6twADUSYQDk60w6BydXRjWsnTF6fUk/cURTkxRKpckw04Iwr1lEY3HzEmxymJ5izn9M9ngcazawLQhrcPtUPD6PWqPR6ggjVnH4QglfUKSgujDdAAjOg1HiEMZzDCZZAAQTqdxfimNdXFg+DfmwS90VfFB3zYT9+HiStlB0YtAIxIjMBbIjJCI6tMDAex0TdWj7Eo8sWJottMGANssNEFAwlYQheDyFiyNaGB42wBAsB7CA+zaTA7nLIRMBffjBOEvJ2BpLdKHEsxJPo0hMB2eYYTvIyYIEeRiF4GAADZvHLfw6HsAAhQhIBlVgJnfKZTOReYgK80Y2EsqTvHkCj3gizBrNs3hnIhNySgQ4JanmJEUWEN0jJwLB6SJElmUpOKAE0DQAETghCPCAiBqFq9CG1mBA4sc5qakCuM4DiuggIAK0oESesbOKqscvzSCmXAus0VrzCObDcNYfDvxqmy7IGQk6iON8sjWr94k2xL5tYAzTBAsCJXoJhGq8VjDCUlS1LyywSiCPsZFO7bdswZxdr4lAxDgTAoqI/a0DBhK7OS1z7DS6JWCh8RMwulBUztKSYEsU8DWg7MkIKgGDQAOXcKg4lkbNMC4XsDkU9SQehzBJoc6Cpku7ApPOtjgHOjtMAAXgCmVP0ICBgChiSpO0dx7HSCB5CeITRr5kATth3hzriaQaEaPgSTJugyGceQ7sYB7/TdLHOiM66IHAy2HtlEWxYjdxJelw8jNkfmVH5KWhdFzBxa9qWAEI71liwJBFL722kX7eB2ug6gBoGmd9nHLAAeWFe7y1lSQcDB3gE+IQhPH9PRs/3bG44AVRSdxnEoAuXeLxwy8wCvSC+mv0jr56nzMZnUHlxXiGV1WtMwSrrhqtD6sa7mrJXzQW0FuvQ/D727zyuXKAVpWVc09XN9qTXk+13X4gN6ojYgE2zYthgrfLG2MTt2PHedj+rtWJ709gfHOccWyB29kLUe4DcbxwHlXJOKc04Z0BunEeb0G48zjp3QB3dS4IHLpXauZBh71w5PlSwLcBBtw7oXT+j1CHEMQaQ2uFDx6oGdEAA
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
            _il.Emit(OpCodes.Ldstr, "0");
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(string)]);
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
            _il.Emit(OpCodes.Ldstr, InspectionConstants.AwaiterFieldName);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, Arg.Is(ConverterField("_typeResolver")));
            _il.Emit(OpCodes.Ldarg_1);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>u__3")));
            _il.Emit(OpCodes.Call, Arg.Is(GetReferenceAwaiterIdMethod()));
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(string)]);
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
            _il.Emit(OpCodes.Ldstr, "1");
            _il.Emit(OpCodes.Callvirt, writeFieldMethods[typeof(string)]);

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
            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
            parameterTypes: []);

        method.MakeGenericMethod(StateMachineType).Invoke(this, BindingFlags.DoNotWrapExceptions, null, [], null);
    }
}
