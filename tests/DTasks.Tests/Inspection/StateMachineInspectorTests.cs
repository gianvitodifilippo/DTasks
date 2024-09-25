using DTasks.Generated.Emit;
using System.Reflection;
using System.Reflection.Emit;
using static DTasks.Inspection.InspectionFixtures;

namespace DTasks.Inspection;

public partial class StateMachineInspectorTests
{
    private readonly ISuspenderDescriptor _suspenderDescriptor;
    private readonly IResumerDescriptor _resumerDescriptor;
    private readonly IDeconstructorDescriptor _deconstructorDescriptor;
    private readonly IConstructorDescriptor _constructorDescriptor;
    private readonly ILGenerator _il;
    private readonly StateMachineInspector _sut;

    public StateMachineInspectorTests()
    {
        _suspenderDescriptor = Substitute.For<ISuspenderDescriptor>();
        _resumerDescriptor = Substitute.For<IResumerDescriptor>();
        _deconstructorDescriptor = Substitute.For<IDeconstructorDescriptor>();
        _constructorDescriptor = Substitute.For<IConstructorDescriptor>();
        _il = Substitute.For<ILGenerator>();

        _suspenderDescriptor.DeconstructorDescriptor.Returns(_deconstructorDescriptor);
        _resumerDescriptor.ConstructorDescriptor.Returns(_constructorDescriptor);

        _sut = new(_suspenderDescriptor, _resumerDescriptor);
    }

    // IL reference
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0o3NqsWlFSKVnd0wvQNDowA8AHzaAPpXwO7i094R/tFMN3dg4qs4AMRg1ikAKIAIQAqgBxOobLZtXb7Hr9QbDUwjU4QVhbc4AVhu6Eesz8UUCJBu6K233W0NaOz2XQRx2RqIs4goEGA2JuqHxzyJixuzNZwApG021PadMOiJOKIu4huAGZuXMXsSbl81j8huIGFDmjCafDJQyzmSShyrlyZjyFm8rqaIBTNSZ/rqmq61PrxQdeuE5jLzug1RElYSbSSrgSGMKRe7VP8CKYkqwSCFxPpDMYYAwzMAAJ5GViWUwATSRMAAggB3WxgOxlXokUsnKs1hw8aod2MEBoivViuES3ot2u8f0UXEjaO91TIADsa17AF8u1344m4snU+mjCZs6Y8wWi8gNMO26cPOyO7VKc0e9PRdsvQjT6PUedx1dFVPmsub7/fzgF68JYLheGE1qvLE8RKLqZSsKwEChGmBg7lmZgACr5l4yAAJy8JhRgUgB2B/ACIIQrIqC/FqOrYNkuTANRzqWDgvqhq8ME3sg8qJDQBBBnMC5qLq3EEOW4i5lQIjHo4DB3KwMDAhQkBZjw540Oc/FXGUynDLwQmqCJPGOLmBFeMsBkEEZYkSVJsnyTAbA0DWniFO89yWdZxCEKSGIlJ5JHxkCYKQlxPEXlpApsgFTSiRFaoapmLo3tZTbDKetj1kwaUVtWI5ubctjoDFaiiceL4FXYqAlTOPHlXlZ4Xpp7y2PKazEXIJlmd2BAdagNmSSI9muApTl2IEhSKL1Mh0cAOR5AQaQZFkc0MQUnGxXVuiDgwFWmN5BCWGWABytiMNUBAALyaXcPCsJWBCeA9x2sMAIScL0jA0NmgIAB5iBwtCZKY35ldt3oMGxDD7eghBHScp2MMEEWCCUFBRldN2uHdD1PQQL1vR9DBfQ4MB/QDQNUCDllgwQO3Gqc6HGucMNwydZ1BAQTNlgQqMQOjF3XQQt33Y9DDPa970cJ9xik+TDCA2AwOg1tdMQ8arOHezSMEHBCG82jGNCyLuPi/jktEyTP3/QrlPU154P0mWmvw8MiOc244wG/zRtYzjYsS4T0vE7L1sU0rVMqzoatO9KB2uzA7szCNPDAN7AuY8L2Oi3jBNSzL31kzbivKzTqv0878fa5zKOG4Lfs52beeW6HRfh6XDsx0alew1rCMc8E6JUBM6e+1n/u5xbwdW23tsR/bYXRxXce9wnScEAYKdp3zGfG9npuB/nIeF/LJeR2XS/qz3bP9zryl8TvY8mwH5tBwXcvF3bUfII73crzfbsB4byHiPR+9dx6N0Pi3E+n957f1/kca+fdAE60sOiewo9wHP0nm/Y+H927n07svZEVdb6cxgKwCgWVMGZ2wU3Ke78w5zw7ovH+XdEH/2QYnIBWYRD0BKDQveE96G4JnqfL+F82HELMKQlBnNvIAH5BENwPq/I+YjYEsM2pfWOJDV7V1SHMdC9AvBgNofvF+zdp6t3EXAyRCCpR6IAdwnW4JlIwGURA1RVjGGzzPgvbRUir6cLXkAsouYHAAG0AC6ni6FQOsTAghATSrl2CU4rh68yAMFsDAAA8lQCAuZZJ0AKLmU44SHCaTMUIyBajoH4OYYQ7Ai4gA=
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIjV1cnkFxbwlLW26Hd29AwA8AHwqAPqnwI4CY67BnmHU55dgAguoAMRgpvEAogBCAFUAOKVZarRobLbtLo9Pr6foHCB0VZHACs5wQNwmHlC3lw5yRqzeSzBDXWm1a0L2cIRRgEhAgwDR5zgWLuuJm5zpDOAxOWKzJTUpOxh+3hxwE5wAzGzJvc8edXot3r0BJRQXVweSoSLqYdCflmadWeN2dNHqcDRBiSq9F8NbUHfItULth0gpNxUcEIrgrKceb8adsZQ+fynXIvph9LE6Lh/AINFodLpKAZgABPbR0Yz6ACasN0AEEAO7mMAWQodXAF/al8tWVhlZsRzDVfmawWQ4UdesVthewgY/phjtyGAAdkWHYAvq3W1GY5E4wmk9o9Gn9Jns7mYIo+42Dk4mc2KiS6u2xwK1q7oQeBwijkPTjLR3U5+ePx/UMe2MY7C4gRmg8ERRNIGqFHQdAQAEiaaOuqYGAAKlmLgwAAnGwKHaMS34oJ83z/MCYhwB8qrqigaQZMAZF2sYqAegGDzgeeMBSjEpCYL6kzTvIGpsZgRYCBmxC8Hu1iUJcdC6H8hCQKmrBHqQRxcachRyX0bC8XI/HsdYGbYS4czaZgumCcJokSVJuj0KQ5bODkTxXCZZk4FgBLIvkLn4VGvyAiCrHsceqncoy3m1AJwWKsqKb2ueZm1n0B7mFW1CJcWZb9o5FzmAg4XyAJe73tlFhwPl47sUVmWHseKlPOYUqLHh4j6YZbaYM1cDmSJvBWfY0m2RY3g5FIHWiJRwDpJkmCJMkqSTdR2QsRFlVqD2lDFfobmYMYhYAHLmFQZSYAAvCplysHQJaYM4117XQwD+EwHRUKQaY/AAHvwjBkCk+hvoVa1upQjGUFtCBYLt+wHVQfjBVw+SEKGp3nfYl3XbdmD3Y9z2UK9Vi6J932/cQ/0mYDmDrXqBxIXqRzg5D+2Hb4mC04WmAIxASPHWdmAXVdN2UHdD1PYwL06ATROUD9YB/QDq2U8DeoMztTOw5gkHQRziPI7z/MY0LWMi7j+PvV90sk2TrlA1ShYq1DfQwyzDhDNrXO66j6OC8LONi3jEtm8Tsuk/LqiK7bYrbQ7uhO+M/WsMAbvcyjfNowLmPY6L4tvYT5sy3L5MK1TdtR2rLPwzrPOe+nhuZybAe50HBfW+HuolxDqvQ8zfhIsQwxJx7qdexnxt+6bjcW8HVuBWHxeRx30ex5gmjx4nnPJ3racGz7Wf+znUv5yHhez0r7eM136tyZx6+D/r3tG772eS3nluhzANtt/P5+O93y+9/3N8q5Dxrjveu+8X5Tzfh/XYZ9O4/3VsYJElgB5ALviPR+e9n5NyPi3OecJS4XxZroOghBUooJTmg2uo8n6B0ns3Ge79W4wK/nAmOv9Uy8AoPkchm9h5UIwePA+r9j6MLwQYAh8CWZuQAPw8OrtvB+u9BEQPoStE+Ed8ELzLgkSYSEKAuEARQre9865jwbkIyBIjoGik0d/Nh6sgRyV0HI4BCjTE0InofaeajRGnxYYvX+hQMxWAANoAF0XGUNAWY8B2DvEFSLn42xrCl74EoOYXQAB5YgEAMwSXINkDMBwglWBUoY3hIDFFgKwXQnBKAZxAA==
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsStructNotPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            58;
#else
            42;
#endif
        Type delegateType = typeof(StructSuspender<>);
        Type callbackType = typeof(CallbackStruct);
        Type expectedType = delegateType.MakeGenericType(StateMachineType);

        var callbackParameter = Substitute.For<ParameterInfo>();
        callbackParameter.ParameterType.Returns(callbackType);

        _suspenderDescriptor.DeconstructorParameter.Returns(callbackParameter);
        _suspenderDescriptor.DelegateType.Returns(delegateType);
        _deconstructorDescriptor.Type.Returns(callbackType);
        _deconstructorDescriptor.HandleAwaiterMethod.Returns(Methods.HandleAwaiterMethod);
        _deconstructorDescriptor.HandleStateMethod.Returns(Methods.HandleStateMethod);
        _deconstructorDescriptor.GetHandleFieldMethod(Arg.Any<Type>()).Returns(Methods.HandleFieldMethod);

        // Act
        Delegate suspender;
        using (_il.InterceptCalls())
        {
            suspender = _sut.GetSuspender(StateMachineType);
        }

        // Assert
        suspender.Should().BeOfType(expectedType);
        _il.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        _il.Received(2).DefineLabel();
        Received.InOrder(() =>
        {
            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "arg");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));

#if DEBUG
            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
#endif

            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleStateMethod));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldtoken, typeof(DTask.Awaiter));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldtoken, typeof(DTask<int>.Awaiter));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwRbWACIMIqxUPjwUIsCsPAQithAQAEYuANYAlCpqythqLSVllTUkABK2JhAMAGJgDBBmAES2PADmY8E+9pEBniSTU7XSza2qpeVVItXdvTD9QyPjADwAfNoA+jfA7uKz3hH+0Ux3D2Di6zgAxGB0gBRABCAFUAOINLY7Dr7Q59QbDUamMbnCCsHaXACsd3Qz3mfiigRIdwxO1+mxh7T2Bx6iNOKLRFnEFAgwBxd1QBNexOWdxZbOAlK22xpnXpxyRZ1RV3EdwAzDyFm8SXcfhs/iNxAxoa1YbSEVLGRdyWVOTduXNeUsPjczRBKVqTIC9S03WoDRKjv1wgtZZd0OqIsqibbSTdCQwRaKPapAQRTElWCQQuJ9IZjDAGGZgABPIysSymACayJgAEEAO62MB2Cr9Ehls7V2sOHi1TtxghNUX68XwyX9Vt13gBih4sYxvuqZAAdg2fYAvt3uwmk3EU2mM0YTDnTPnC8XkBoR+3zh4OZ36lTWr2Z2Ldt7EWex2jLhObkrp60V7e/3+OCXrwlguF4YQ2u8sTxEoeoVKwrAQKE6YGLu2ZmAAKgWXjIAAnLwWFGJSgHYACwLglCuCoP82q6tguT5MANEupYOB+mG7ywbeyAKokNAEMGCyLmoeo8QQFbiHmVAiCejgMA8rAwCCFCQNmPAXjQlwCTcFQqaMvDCaoom8Y4eaEV4qyGQQxniZJ0lyQpMBsDQtaeMUnyPFZNnEIQZKYmUXmkQmaSgpCNmXtpgrsoFLRiRF6qalmrq3jZzajGetgNkwaWVjWo7ufctjoDFahiSer4FXYqAlbOvHlXl56Xlpny2AqGwkXIpnmT2BAdagtlSSIDmuIpzl2IExSKL1Mj0cAeQFAQGRZDkc2MUUXGxXVuhDgwFWmD5BCWOWABytiMLUBAALxaQ8PCsFWBCeA9x2sMAIScP0jA0DmQIAB5iBwtDZKYP5ldtPoMOxDD7eghBHWcp2MMEEWCGUFDRldN2uHdD1PQQL1vR9DBfQ4MB/QDQNUCDVlgwQO0mucGEmpcMNwydZ1BAQTPlgQqMQOjF3XQQt33Y9DDPa970cJ9xik+TDCA2AwOg1tdMQyarOHezSMEPBiG82jGNCyLuPi/jktEyTP3/QrlPU954MMuWmvw6MiOc24kwG/zRtYzjYsS4T0vE7L1sU0rVMqzoatOzKB2uzA7tzCNPDAN7AuY8L2Oi3jBNSzL31kzbivKzTqv0878fa5zKOG4Lfs52beeW6HRfh6XDsx8alew1rCMc8EGJUFM6e+1n/u5xbwdW23tsR/b3Hl+rPds/3OsGCnad8xnxvZ6bgf5yHhfyyXkdl9HFdx73CdJwQKn8dvY8mwH5tBwXcvF3bUfII73dX6vbsB53yHiPR+9dx6NwPi3Y+n957f1/icFefdAE60sBiewo9wHP0nm/I+H925n07pfFEVc16cxgKwCgWVMGZ2wU3Ke78w5zw7ovC+y9/7IMTkA7MIh6BlBobvCe9DcEzxPl/c+P8u6II4TfIBPkAD8AiG771fofURsCWGbTYbHEh19q7BDSAsDC9AvBgNoXvF+zdp6tzEXAiRCDpS6IAVwnWEIVIwCURAlRVjGGz1PgvLRkjiFmFISgzmFQ8wOAANoAF1PF0KgdYmBBCAmlSXjokJeiyHBDIAwWwMAADyVAIB5jknQIoeZzgRIcFpMxgjIGqOgfg5hhDsBLiAA=
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfkamACKUvHTEbqyEvMB0rJi85hAQAEZ2ANYAlLLyMijy9fmFJeW4ABLmehCUAGJglBAGAETmrADmg35uliFezrgjoxUidQ1yBUWlvGVtHbpdvf1DADwAfCoA+ufAjgITrsGeYdSX12ACS6gAxGBJAKIAQgBVADi1VW62aWx2nR6fQG+kGRwgdHWJwArJcEHcph5Qt5cJdkesPitwU1Ntt2jCDvDEUYBIQIMB0Zc4NiHni5pd6YzgCTVmtyS0qXtYYcEacBJcAMzs6aPfGXd7LT79ASUMENCEU6GimnHImFFnnNmTDmzZ7nQ0QEmqvQ/TX1R3ybXC3ZdILTCUnBBK4Jy3EWgnnHGUfkC51yH6YfSxOi4fwCDRaHS6SgGYAAT20dGM+gAmnDdABBADu5jAFmKXVwhcOZYrVlYFRbkcwtQFWqFUJFXQblbY3sImMG4c7chgAHZlp2AL5ttvR2OReOJ5PaPTp/RZnN5mCKftNo5OZktqqkhod8eCjZumGHweIk7D86yscNecXz+f1AntjGOwXECc0ngiKJpE1Yo6DoCAAiTTQNzTAwABVsxcGAAE42FQ7QSR/FBvj+IFQTQOAvjVDUUAyLJgHI+1jFQT1AyeCCLxgaUYlITA/WmGd5E1djMGLARM2IXh92sShrjoXR/kISA01YY9SBObjzmKeSBjYPi5AEjjrEzHCXAWHTMD0oSRLEyTpN0ehSArZw8heG5TPMnAsEJFFClcgjo0SAEQXMk81J5JkfPqQTgqVFVUwdC9zLrAZD3MatqESktywHJyrnMBBwvkQT9wfbKLDgfKJw4orMqPE9VJecxpWWfDxAMoz20wZq4As0TeGs+wZLsixvDyKQOtEKjgEybJMGSVJ0kmmjclYiLKrUXtKGK/R3MwYwiwAOXMKgKkwABeVTrlYOhS0wZxrr2uhgH8JguioUh01+AAPfhGDINJ9HfQq1vdSgmMoLaECwXbDgOqg/GCrhCkIMNTvO+xLuu27MHux7nsoV6rF0T7vt+4h/tMwHMHW/UjmQ/UTnByH9sO3xMFpotMARiAkeOs7MAuq6bsoO6HqexgXp0AmicoH6wD+gHVsp4H9QZnamdhzAoJgjnEeR3n+YxoWsZF3H8fer7pZJsm3KB6kixVqGBhhlmHBGbWud11H0cF4WcbFvGJbN4nZdJ+XVEV23xW2h3dCdyZ+tYYA3e5lG+bRgXMex0XxbewnzZluXyYVqm7ajtWWfhnWec99PDczk2A9zoOC+t8O9RLiHVeh5m/GRYhRiTj3U69jPjb903G4t4OrbYoulfbxmu/VzR48Tznk71tODZ9rP/ZzqX85Dwuw+LyOO+j2PMHkri18H/XvaN33s8lvPLdDmAbbb0+F8d7vL97/ub5VyHjXbe9c94vynm/D++x56dx/urYwyJLADyAXfEej9d7PybofFuJ94Sl0XizXQdBCCpRQSnNBtdR5P0DpPZuM9j5zy/nAmOv80y8AoIUchG9h5UIwePfer8j7v1bjA5h59f7uQAPzcOrlvB+O8BEQPoStRhEd8FnzLn4RI0xkIUBcIAihm9751zHg3QRkDhHQLFBo7+rD1bAnkroWRwD5GmJoRPA+09VEiLwQYAh8CWbFEzFYAA2gAXRcZQ0BZjwHYO8QVWe6j/GaMIX4fAlBzC6AAPLEAgJmSS5BciZiOMEqwqlDE8JAQosBWC6E4JQLOIAA==
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsStructPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            58;
#else
            42;
#endif
        Type delegateType = typeof(ByRefStructSuspender<>);
        Type callbackType = typeof(CallbackStruct);
        Type expectedType = delegateType.MakeGenericType(StateMachineType);

        var callbackParameter = Substitute.For<ParameterInfo>();
        callbackParameter.ParameterType.Returns(callbackType.MakeByRefType());

        _suspenderDescriptor.DeconstructorParameter.Returns(callbackParameter);
        _suspenderDescriptor.DelegateType.Returns(delegateType);
        _deconstructorDescriptor.Type.Returns(callbackType);
        _deconstructorDescriptor.HandleAwaiterMethod.Returns(Methods.HandleAwaiterMethod);
        _deconstructorDescriptor.HandleStateMethod.Returns(Methods.HandleStateMethod);
        _deconstructorDescriptor.GetHandleFieldMethod(Arg.Any<Type>()).Returns(Methods.HandleFieldMethod);

        // Act
        Delegate suspender;
        using (_il.InterceptCalls())
        {
            suspender = _sut.GetSuspender(StateMachineType);
        }

        // Assert
        suspender.Should().BeOfType(expectedType);
        _il.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        _il.Received(2).DefineLabel();
        Received.InOrder(() =>
        {
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "arg");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));

#if DEBUG
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
#endif

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleStateMethod));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldtoken, typeof(DTask.Awaiter));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldtoken, typeof(DTask<int>.Awaiter));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0o3NqsWlFSKVnd0wvQNDowA8AHzaAPpXwO7i094R/tFMN3dg4qs4AMRg1ikAKIAIQAqgBxOobLZtXb7Hr9QbDUwjU4QVhbc4AVhu6Eesz8UUCJBu6K233W0NaOz2XQRx2RqIs4goEGA2JuqHxzyJixuzNZwApG021PadMOiJOKIu4huAGZuXMXsSbl81j8huIGFDmjCafDJQyzmSShyrlyZjyFm8rqaIBTNSZ/rqmq61PrxQdeuE5jLzug1RElYSbSSrgSGMKRe7VP8CKYkqwSCFxPpDMYYAwzMAAJ5GViWUwATSRMAAggB3WxgOxlXokUsnKs1hw8aod2MEBoivViuES3ot2u8f0UXEjaO91TIADsa17AF8u1344m4snU+mjCZs6Y8wWi8gNMO26cPOyO7VKc0e9PRdsvQjT6PUedx1dFVPmsub7/fzgF68JYLheGE1qvLE8RKLqZSsKwEChGmBg7lmZgACr5l4yAAJy8JhRgUgB2B/ACIIQrIqC/FqOrYNkuTANRzqWDgvqhq8ME3sg8qJDQBBBnMC5qLq3EEOW4i5lQIjHo4DB3KwMDAhQkBZjw540Oc/FXGUynDLwQmqCJPGOLmBFeMsBkEEZYkSVJsnyTAbA0DWniFO89yWdZxCEKSGIlJ5JHxkCYKQlxPEXlpApsgFTSiRFaoapmLo3tZTbDKetj1kwaUVtWI5ubctjoDFaiiceL4FXYqAlTOPHlXlZ4Xpp7y2PKazEXIJlmd2BAdagNmSSI9muApTl2IEhSKL1Mi4P1aQZFkwA5HkBScbFdW6IODAVaY3kEJYZYAHK2Iw1QEAAvJpdw8KwlYEJ4d2HawwAhJwvSMDQ2aAgAHmIHC0JkpjfmVm3egwbEMLt6CEAdJzHYwwQRYIJQUFGF1Xa4N13Q9BBPS9b0MB9DgwD9f0A1QQOWSDBBbcapzoca5xQzDR0nUEBAM2WBDIxAqNnZdBDXbd90MI9z2vRw73GMTpMMP9YCA8DG002DxrM/trMIwQcEIdzKNowLQvY6LuPiwTRNfb9cvk5TXmg/SZbq7Dwzw+zbjjHrvMGxjWMi2L+OS4T0uW2TCsU0rOgqw70p7c7MCuzMI08MAnt8+jguY8LON4xLUufSTVvy4rVPK7Tjux5r7NI/r/M+1nJs5+bwcF6Hxd21HRrl9DGtw2zwTolQEyp97Ge+9nZuBxbLfW2HtthZHZcx93ccJwQBhJynPNp4bmfG/7udB/nstF+HJcL6rXcs73WvKXxW8j0bfumwHecy4XNsR8g9ud0vV8u33a8B5D3vrXUe9d95NyPu/Wen9v5HEvj3f+WtLDonsMPUBj9x4v0Pm/Vup926L2RBXa+7MYCsAoFldB6dMENwnq/EOM827zy/h3eBv9EHxwAVmEQ9AShUJ3mPWh2Cp7Hw/mfFhhCzDEKQezbyAB+fhdc97PwPiI6BTD1rn2jkQ5eldUhzHQvQLwIDqG7yfo3SezdREwPEXAqUOi/6cK1uCZSMBFFgOURY+h08T5z00RIi+7CV4ALKLmBwABtAAuu4mhEDLFQLwX40qpdAkOI4avMgDBbAwAAPJUAgLmWSdACi5lOKEhwmkTECPASoyBuDGH4OwIuIAA
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIjV1cnkFxbwlLW26Hd29AwA8AHwqAPqnwI4CY67BnmHU55dgAguoAMRgpvEAogBCAFUAOKVZarRobLbtLo9Pr6foHCB0VZHACs5wQNwmHlC3lw5yRqzeSzBDXWm1a0L2cIRRgEhAgwDR5zgWLuuJm5zpDOAxOWKzJTUpOxh+3hxwE5wAzGzJvc8edXot3r0BJRQXVweSoSLqYdCflmadWeN2dNHqcDRBiSq9F8NbUHfItULth0gpNxUcEIrgrKceb8adsZQ+fynXIvph9LE6Lh/AINFodLpKAZgABPbR0Yz6ACasN0AEEAO7mMAWQodXAF/al8tWVhlZsRzDVfmawWQ4UdesVthewgY/phjtyGAAdkWHYAvq3W1GY5E4wmk9o9Gn9Jns7mYIo+42Dk4mc2KiS6u2xwK1q7oQeBwijkPTjLR3U5+ePx/UMe2MY7C4gRmg8ERRNIGqFHQdAQAEiaaOuqYGAAKlmLgwAAnGwKHaMS34oJ83z/MCYhwB8qrqigaQZMAZF2sYqAegGDzgeeMBSjEpCYL6kzTvIGpsZgRYCBmxC8Hu1iUJcdC6H8hCQKmrBHqQRxcachRyX0bC8XI/HsdYGbYS4czaZgumCcJokSVJuj0KQ5bODkTxXCZZk4FgBLIvkLn4VGvyAiCrHsceqncoy3m1AJwWKsqKb2ueZm1n0B7mFW1CJcWZb9o5FzmAg4XyAJe73tlFhwPl47sUVmWHseKlPOYUqLHh4j6YZbaYM1cDmSJvBWfY0m2RY3g5FIHWiGgXWJMkqTAOkmTZCxEWVWoPaUMV+huZgxiFgAcuYVBlJgAC8KmXKwdAlpgziXTtdDAP4TAdFQpBpj8AAe/CMGQKT6G+hUrW6lCMZQG0IFg237HtVB+MFXD5IQobHad9jnZd12YLd92PZQz1WLo72fd9xC/SZ/2YKteoHEhepHKD4O7ftviYNThaYHDEAI4dJ2YGdF1XZQN13Q9jBPToeME5QX1gD9f3LeTgN6nTW0M9DmCQdBbPw4j3O82jAsY0L2O469H2S0TJOuQDVKFkrEN9FDTMOEMmsc9ryOo/zgtYyLONiybhPS8TsuqPL1tiptdu6A74z9awwAu5zSM8yjfPo5jwuiy9+Om1LMuk3LFM2xHKtM7DWtc+7qf6+nRt+9nAd55boe6kXYPK5DjN+EixDDAnbvJx7aeGz7xv12bgcW4FIeF+HbeR9HmCaLH8fs4nOsp3rXsZ77WcS7nQf59PCut/THeq3JnGr/3uuewb3uZ+LOfm8HMBWy3s+n/bneL93vdXxXA8q5b1rrvJ+E8X5v12CfduX9VbGCRJYPuACb5D3vjvR+DcD5NxnnCYuZ8ma6DoIQVKSCk4oOrsPB+/tx6Nynq/ZuUCP4wKjt/VMvAKD5FIevQeFC0Gjz3s/Q+9CcEGDwbApmbkAD8XDK6bzvtvfhYDaFLSPmHXBc8S4JEmEhCgLh/5kI3rfGuI864CPAUIyBop1GfxYarIEcldAyMAXI4xVCx770nio4Rx8mHz2/oUDMVgADaABdJx5DgEmNAZgzxBUC4+Oscwhe+BKDmF0AAeWIBADMElyDZAzAcAJVgVL6O4UA+RICME0KwSgGcQA==
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsClassNotPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            58;
#else
            42;
#endif
        Type delegateType = typeof(ClassSuspender<>);
        Type callbackType = typeof(CallbackClass);
        Type expectedType = delegateType.MakeGenericType(StateMachineType);

        var callbackParameter = Substitute.For<ParameterInfo>();
        callbackParameter.ParameterType.Returns(callbackType);

        _suspenderDescriptor.DeconstructorParameter.Returns(callbackParameter);
        _suspenderDescriptor.DelegateType.Returns(delegateType);
        _deconstructorDescriptor.Type.Returns(callbackType);
        _deconstructorDescriptor.HandleAwaiterMethod.Returns(Methods.HandleAwaiterMethod);
        _deconstructorDescriptor.HandleStateMethod.Returns(Methods.HandleStateMethod);
        _deconstructorDescriptor.GetHandleFieldMethod(Arg.Any<Type>()).Returns(Methods.HandleFieldMethod);

        // Act
        Delegate suspender;
        using (_il.InterceptCalls())
        {
            suspender = _sut.GetSuspender(StateMachineType);
        }

        // Assert
        suspender.Should().BeOfType(expectedType);
        _il.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        _il.Received(2).DefineLabel();
        Received.InOrder(() =>
        {
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "arg");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));

#if DEBUG
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
#endif

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleStateMethod));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldtoken, typeof(DTask.Awaiter));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldtoken, typeof(DTask<int>.Awaiter));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0o3NqsWlFSKVnd0wvQNDowA8AHzaAPpXwO7i094R/tFMN3dg4qs4AMRg1ikAKIAIQAqgBxOobLZtXb7Hr9QbDUwjU4QVhbc4AVhu6Eesz8UUCJBu6K233W0NaOz2XQRx2RqIs4goEGA2JuqHxzyJixuzNZwApG021PadMOiJOKIu4huAGZuXMXsSbl81j8huIGFDmjCafDJQyzmSShyrlyZjyFm8rqaIBTNSZ/rqmq61PrxQdeuE5jLzug1RElYSbSSrgSGMKRe7VP8CKYkqwSCFxPpDMYYAwzMAAJ5GViWUwATSRMAAggB3WxgOxlXokUsnKs1hw8aod2MEBoivViuES3ot2u8f0UXEjaO91TIADsa17AF8u1344m4snU+mjCZs6Y8wWi8gNMO26cPOyO7VKc0e9PRdsvQjT6PUedx1dFVPmsub7/fzgF68JYLheGE1qvLE8RKLqZSsKwEChGmBg7lmZgACr5l4yAAJy8JhRgUgB2B/ACIIQrIqC/FqOrYNkuTANRzqWDgvqhq8ME3sg8qJDQBBBnMC5qLq3EEOW4i5lQIjHo4DB3KwMDAhQkBZjw540Oc/FXGUynDLwQmqCJPGOLmBFeMsBkEEZYkSVJsnyTAbA0DWniFO89yWdZxCEKSGIlJ5JHxkCYKQlxPEXlpApsgFTSiRFaoapmLo3tZTbDKetj1kwaUVtWI5ubctjoDFaiiceL4FXYqAlTOPHlXlZ4Xpp7y2PKazEXIJlmd2BAdagNmSSI9muApTl2IEhSKL1Mi4P1aQZFkwA5HkBScbFdW6IODAVaY3kEJYZYAHK2Iw1QEAAvJpdw8KwlYEJ4d2HawwAhJwvSMDQ2aAgAHmIHC0JkpjfmVm3egwbEMLt6CEAdJzHYwwQRYIJQUFGF1Xa4N13Q9BBPS9b0MB9DgwD9f0A1QQOWSDBBbcapzoca5xQzDR0nUEBAM2WBDIxAqNnZdBDXbd90MI9z2vRw73GMTpMMP9YCA8DG002DxrM/trMIwQcEIdzKNowLQvY6LuPiwTRNfb9cvk5TXmg/SZbq7Dwzw+zbjjHrvMGxjWMi2L+OS4T0uW2TCsU0rOgqw70p7c7MCuzMI08MAnt8+jguY8LON4xLUufSTVvy4rVPK7Tjux5r7NI/r/M+1nJs5+bwcF6Hxd21HRrl9DGtw2zwTolQEyp97Ge+9nZuBxbLfW2HtthZHZcx93ccJwQBhJynPNp4bmfG/7udB/nstF+HJcL6rXcs73WvKXxW8j0bfumwHecy4XNsR8g9ud0vV8u33a8B5D3vrXUe9d95NyPu/Wen9v5HEvj3f+WtLDonsMPUBj9x4v0Pm/Vup926L2RBXa+7MYCsAoFldB6dMENwnq/EOM827zy/h3eBv9EHxwAVmEQ9AShUJ3mPWh2Cp7Hw/mfFhhCzDEKQezbyAB+fhdc97PwPiI6BTD1rn2jkQ5eldUhzHQvQLwIDqG7yfo3SezdREwPEXAqUOi/6cK1uCZSMBFFgOURY+h08T5z00RIi+7CV4ALKLmBwABtAAuu4mhEDLFQLwX40qpdAkOI4avMgDBbAwAAPJUAgLmWSdACi5lOKEhwmkTECPASoyBuDGH4OwIuIAA
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIjV1cnkFxbwlLW26Hd29AwA8AHwqAPqnwI4CY67BnmHU55dgAguoAMRgpvEAogBCAFUAOKVZarRobLbtLo9Pr6foHCB0VZHACs5wQNwmHlC3lw5yRqzeSzBDXWm1a0L2cIRRgEhAgwDR5zgWLuuJm5zpDOAxOWKzJTUpOxh+3hxwE5wAzGzJvc8edXot3r0BJRQXVweSoSLqYdCflmadWeN2dNHqcDRBiSq9F8NbUHfItULth0gpNxUcEIrgrKceb8adsZQ+fynXIvph9LE6Lh/AINFodLpKAZgABPbR0Yz6ACasN0AEEAO7mMAWQodXAF/al8tWVhlZsRzDVfmawWQ4UdesVthewgY/phjtyGAAdkWHYAvq3W1GY5E4wmk9o9Gn9Jns7mYIo+42Dk4mc2KiS6u2xwK1q7oQeBwijkPTjLR3U5+ePx/UMe2MY7C4gRmg8ERRNIGqFHQdAQAEiaaOuqYGAAKlmLgwAAnGwKHaMS34oJ83z/MCYhwB8qrqigaQZMAZF2sYqAegGDzgeeMBSjEpCYL6kzTvIGpsZgRYCBmxC8Hu1iUJcdC6H8hCQKmrBHqQRxcachRyX0bC8XI/HsdYGbYS4czaZgumCcJokSVJuj0KQ5bODkTxXCZZk4FgBLIvkLn4VGvyAiCrHsceqncoy3m1AJwWKsqKb2ueZm1n0B7mFW1CJcWZb9o5FzmAg4XyAJe73tlFhwPl47sUVmWHseKlPOYUqLHh4j6YZbaYM1cDmSJvBWfY0m2RY3g5FIHWiGgXWJMkqTAOkmTZCxEWVWoPaUMV+huZgxiFgAcuYVBlJgAC8KmXKwdAlpgziXTtdDAP4TAdFQpBpj8AAe/CMGQKT6G+hUrW6lCMZQG0IFg237HtVB+MFXD5IQobHad9jnZd12YLd92PZQz1WLo72fd9xC/SZ/2YKteoHEhepHKD4O7ftviYNThaYHDEAI4dJ2YGdF1XZQN13Q9jBPToeME5QX1gD9f3LeTgN6nTW0M9DmCQdBbPw4j3O82jAsY0L2O469H2S0TJOuQDVKFkrEN9FDTMOEMmsc9ryOo/zgtYyLONiybhPS8TsuqPL1tiptdu6A74z9awwAu5zSM8yjfPo5jwuiy9+Om1LMuk3LFM2xHKtM7DWtc+7qf6+nRt+9nAd55boe6kXYPK5DjN+EixDDAnbvJx7aeGz7xv12bgcW4FIeF+HbeR9HmCaLH8fs4nOsp3rXsZ77WcS7nQf59PCut/THeq3JnGr/3uuewb3uZ+LOfm8HMBWy3s+n/bneL93vdXxXA8q5b1rrvJ+E8X5v12CfduX9VbGCRJYPuACb5D3vjvR+DcD5NxnnCYuZ8ma6DoIQVKSCk4oOrsPB+/tx6Nynq/ZuUCP4wKjt/VMvAKD5FIevQeFC0Gjz3s/Q+9CcEGDwbApmbkAD8XDK6bzvtvfhYDaFLSPmHXBc8S4JEmEhCgLh/5kI3rfGuI864CPAUIyBop1GfxYarIEcldAyMAXI4xVCx770nio4Rx8mHz2/oUDMVgADaABdJx5DgEmNAZgzxBUC4+Oscwhe+BKDmF0AAeWIBADMElyDZAzAcAJVgVL6O4UA+RICME0KwSgGcQA==
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsClassPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            66;
#else
            48;
#endif
        Type delegateType = typeof(ByRefClassSuspender<>);
        Type callbackType = typeof(CallbackClass);
        Type expectedType = delegateType.MakeGenericType(StateMachineType);

        var callbackParameter = Substitute.For<ParameterInfo>();
        callbackParameter.ParameterType.Returns(callbackType.MakeByRefType());

        _suspenderDescriptor.DeconstructorParameter.Returns(callbackParameter);
        _suspenderDescriptor.DelegateType.Returns(delegateType);
        _deconstructorDescriptor.Type.Returns(callbackType);
        _deconstructorDescriptor.HandleAwaiterMethod.Returns(Methods.HandleAwaiterMethod);
        _deconstructorDescriptor.HandleStateMethod.Returns(Methods.HandleStateMethod);
        _deconstructorDescriptor.GetHandleFieldMethod(Arg.Any<Type>()).Returns(Methods.HandleFieldMethod);

        // Act
        Delegate suspender;
        using (_il.InterceptCalls())
        {
            suspender = _sut.GetSuspender(StateMachineType);
        }

        // Assert
        suspender.Should().BeOfType(expectedType);
        _il.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        _il.Received(2).DefineLabel();
        Received.InOrder(() =>
        {
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "arg");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));

#if DEBUG
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
#endif

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
            _il.Emit(OpCodes.Ldarg_0);
#if DEBUG
            _il.Emit(OpCodes.Ldind_Ref);
#endif
            _il.Emit(OpCodes.Ldfld, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleStateMethod));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldtoken, typeof(DTask.Awaiter));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldtoken, typeof(DTask<int>.Awaiter));
            _il.Emit(OpCodes.Call, Arg.Is(GetTypeFromHandleMethod()));
            _il.Emit(OpCodes.Callvirt, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference (except we do not box and unbox the awaiters)
    // DEBUG: https://sharplab.io/#v2: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
    // RELEASE: https://sharplab.io/#v2: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=
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsStructNotPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            70;
#else
            58;
#endif
        Type delegateType = typeof(StructResumer);
        Type callbackType = typeof(CallbackStruct);

        var callbackParameter = Substitute.For<ParameterInfo>();
        callbackParameter.ParameterType.Returns(callbackType);

        _resumerDescriptor.ConstructorParameter.Returns(callbackParameter);
        _resumerDescriptor.DelegateType.Returns(delegateType);
        _constructorDescriptor.Type.Returns(callbackType);
        _constructorDescriptor.HandleAwaiterMethod.Returns(Methods.HandleAwaiterMethod);
        _constructorDescriptor.HandleStateMethod.Returns(Methods.HandleStateMethod);
        _constructorDescriptor.GetHandleFieldMethod(Arg.Any<Type>()).Returns(Methods.HandleFieldMethod);

        // Act
        Delegate resumer;
        using (_il.InterceptCalls())
        {
            resumer = _sut.GetResumer(StateMachineType);
        }

        // Assert
        resumer.Should().BeOfType(delegateType);
        _il.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        _il.Received(1).DeclareLocal(StateMachineType);
        _il.Received(2).DefineLabel();
        Received.InOrder(() =>
        {
#if DEBUG
            _il.Emit(OpCodes.Newobj, StateMachineConstructor);
            _il.Emit(OpCodes.Stloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Initobj, StateMachineType);
#endif

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Call, Arg.Is(CreateMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>t__builder")));

            _il.Emit(OpCodes.Ldarga_S, 1);
            _il.Emit(OpCodes.Ldstr, "arg");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarga_S, 1);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarga_S, 1);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

#if DEBUG
            _il.Emit(OpCodes.Ldarga_S, 1);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarga_S, 1);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);
#endif

            _il.Emit(OpCodes.Ldarga_S, 1);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleStateMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarga_S, 1);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__1")));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarga_S, 1);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__3")));

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Call, Arg.Is(StartMethod()));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Call, Arg.Is(TaskGetter()));

            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference (except we do not box and unbox the awaiters)
    // DEBUG: https://sharplab.io/#v2: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
    // RELEASE: https://sharplab.io/#v2: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
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsStructPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            70;
#else
            58;
#endif
        Type delegateType = typeof(ByRefStructResumer);
        Type callbackType = typeof(CallbackStruct);

        var callbackParameter = Substitute.For<ParameterInfo>();
        callbackParameter.ParameterType.Returns(callbackType.MakeByRefType());

        _resumerDescriptor.ConstructorParameter.Returns(callbackParameter);
        _resumerDescriptor.DelegateType.Returns(delegateType);
        _constructorDescriptor.Type.Returns(callbackType);
        _constructorDescriptor.HandleAwaiterMethod.Returns(Methods.HandleAwaiterMethod);
        _constructorDescriptor.HandleStateMethod.Returns(Methods.HandleStateMethod);
        _constructorDescriptor.GetHandleFieldMethod(Arg.Any<Type>()).Returns(Methods.HandleFieldMethod);

        // Act
        Delegate resumer;
        using (_il.InterceptCalls())
        {
            resumer = _sut.GetResumer(StateMachineType);
        }

        // Assert
        resumer.Should().BeOfType(delegateType);
        _il.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        _il.Received(1).DeclareLocal(StateMachineType);
        _il.Received(2).DefineLabel();
        Received.InOrder(() =>
        {
#if DEBUG
            _il.Emit(OpCodes.Newobj, StateMachineConstructor);
            _il.Emit(OpCodes.Stloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Initobj, StateMachineType);
#endif

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Call, Arg.Is(CreateMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>t__builder")));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "arg");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

#if DEBUG
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);
#endif

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleStateMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__1")));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__3")));

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Call, Arg.Is(StartMethod()));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Call, Arg.Is(TaskGetter()));

            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference (except we do not box and unbox the awaiters)
    // DEBUG: https://sharplab.io/#v2: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
    // RELEASE: https://sharplab.io/#v2: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=
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsClassNotPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            70;
#else
            58;
#endif
        Type delegateType = typeof(ClassResumer);
        Type callbackType = typeof(CallbackClass);

        var callbackParameter = Substitute.For<ParameterInfo>();
        callbackParameter.ParameterType.Returns(callbackType);

        _resumerDescriptor.ConstructorParameter.Returns(callbackParameter);
        _resumerDescriptor.DelegateType.Returns(delegateType);
        _constructorDescriptor.Type.Returns(callbackType);
        _constructorDescriptor.HandleAwaiterMethod.Returns(Methods.HandleAwaiterMethod);
        _constructorDescriptor.HandleStateMethod.Returns(Methods.HandleStateMethod);
        _constructorDescriptor.GetHandleFieldMethod(Arg.Any<Type>()).Returns(Methods.HandleFieldMethod);

        // Act
        Delegate resumer;
        using (_il.InterceptCalls())
        {
            resumer = _sut.GetResumer(StateMachineType);
        }

        // Assert
        resumer.Should().BeOfType(delegateType);
        _il.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        _il.Received(1).DeclareLocal(StateMachineType);
        _il.Received(2).DefineLabel();
        Received.InOrder(() =>
        {
#if DEBUG
            _il.Emit(OpCodes.Newobj, StateMachineConstructor);
            _il.Emit(OpCodes.Stloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Initobj, StateMachineType);
#endif

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Call, Arg.Is(CreateMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>t__builder")));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "arg");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

#if DEBUG
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);
#endif

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleStateMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__1")));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__3")));

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Call, Arg.Is(StartMethod()));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Call, Arg.Is(TaskGetter()));

            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference (except we do not box and unbox the awaiters)
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhz4i6AKzTsM1AWY4A3jgI65ANiJ6APGBoA+AmQkVGACmQGe1iMAdQCTgGZrWVccB4KEWBWHgIRAEMICAAjCJEAawBKbV0tbF1Mgj5gCOAGAFl4gAtTBgJ/PMKSsoIAXhwAYjBvABEAUQAhAFUAcVSsrKoGAHds3PyikVLh2ySlRoYIcQYBwd0YBk8IihcFhioYFrW1zMrJmuGSAH1rmIpITbC6ggBBcQBPKhEHAoZgYqsGCdB4QJ4mcwsJxVOZKTKnXSRaJxRIkAASEUOEAYADEwEsYLYAEQRHgAcyJ7i8FQm1WmZRIpLJ8wROiRsXiCXRmJg2LxBOJRjMABZbgCwOJKR4tjSqlMZkwxaVxCzsM02l0+qzwlEOaiMVjcfiwYKIKwkWZ5Ld0FLqec6QqbtczUjVesdcjOdzDfyTUSjE5xLtgJbbqhbTL7fKGbdA8G3et2SiuQbeUaBf6zOJbgBmCPeKOXRXXFX7ZarDKJ3XJ71p32E/0uqKh67hqmR2nRq63JsQVWLQ7HSuDbVJr2p7E5GGZ9C3e352UXendku0hODFoEWxj/U87GvEYRMD5HiCswUa1EpIpYdZdLus6dotO3LoepbgCaxpgB6PuRi2IkF+BK/sevBJLYrAxAAVgwwRJHGLgOCQvT/KBJ6wtqAC+2rapu27VuOe4MOhvBnhe1x5te2r3g+OiFsuxa5Dm772HopE8BCIYQVBsHwYhrh6ChaGHmBp7rlkOG3vC0m6Axjq3Pcjy8CQU48MAth2k+jESTJ7rIAA7IuDoxncoJPCQDhwroUlSU0m4dD0/S4KgTRLCsOD+IEwRuYOng4FOS4KgQIAEAAku8XwiIFJnDJoazICxpjAAQc60tZOgJSxkXfL8/yAsC5m8FxFiKUVPAZQQWUEAUHwACofBw5RMpV1U5SIfwAkCbA0EewxhEqEqtbeiVyIQPbmlEw3qgQjlaiNSU0Kl1wCcNmSjcly1SL5Rz+be1XAWC6ERABTCHT+oknstr5rboo0OBx10RKgt06Pd7GXcVyWldczGvUQLHIMKNWsIIDAAHIMAAHhpST1BYAI8KwYzDGM4OsMAYWcNijA0AwMDtFDYgcLQviYQtRDAwIwAxV2DC2BFnzfLTRbGXTcN1AjxRIyjowEOjmPYwwuP5ATRMMCTYBk6qdkuTV9WNeUGgELLaBvEzHX5d1vi5GUYTK6rqg9V5QQhDw8UUzErCsBABATiRn2nsQhCeN+4MRIwHNczzBCo/zGNYxwOMHKLhPE6TVDk+tLFWzbdvETF9joC7bseww7beJtghRBQDBewQiPI77fMC4Hwd42L4dS5HEmjbHtv2/WRh1fWZhJynBLu4wGcEC334ENnEC5/nhe82jAdCyL+NhxLEdR3dMfWw3xH1u3BCu53ac9/XA853n8MF9zRd+6Xk8h9P4uS9LlV10v8c+t+a8b2CXfp9K3jTKSu9D/vnOHz7J8J5B2FufSus9q7zzeovOOjdH7O3Xqnbu78KiAnUt/YeB9R7F3HoLYBU8wFXxrjfaBy8H4Cngc/GAr8e5Zz3iPI+Y9/a4PLqHS+c9a4kPvnWOBycEGbyQdSM0VAyToN/t7Y+JcgEsIvlXa+1Ud6wPIbwyh1DkEUHEKglKg8MF/ywYA5hICK4z0IZAgGBAFErx4R3F+W81G0J/vQgBkiDH4OMew4h5i76KJNBQxBb9qS7F8CI7RYj/4SJwWXQxrDZFEPkV4yxSjrFUNsdSTwZo8iiMceEphkTXFsIgRwzxMCEk+OUX4nuMBWAUFOpkzBDDsE5LPkY/JcjLbxLIaUpJqjqSbBEPQKItTdH1P0bk0BbiCkeIsR0wkvj+H+JlM7AA/IM8RjDT54LGS02JbTinTKfuU5BrQqh1XoOUEJWS1lSKiTI8BrTo5FNIdwxJfCbECJlL0B4MAVlhMuS4zZMTTG312U8zpLzklvO8DED4+QADaABdb5ejnGjOaQCwpUyQUzLKXMnuVgIgwAAPJUAgB8P4dBQgfCMFC/IFhzl1KcREpp0TbnbKwkAA
    // RELEASE: https://sharplab.io/#v2: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
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsClassPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            78;
#else
            64;
#endif
        Type delegateType = typeof(ByRefClassResumer);
        Type callbackType = typeof(CallbackClass);

        var callbackParameter = Substitute.For<ParameterInfo>();
        callbackParameter.ParameterType.Returns(callbackType.MakeByRefType());

        _resumerDescriptor.ConstructorParameter.Returns(callbackParameter);
        _resumerDescriptor.DelegateType.Returns(delegateType);
        _constructorDescriptor.Type.Returns(callbackType);
        _constructorDescriptor.HandleAwaiterMethod.Returns(Methods.HandleAwaiterMethod);
        _constructorDescriptor.HandleStateMethod.Returns(Methods.HandleStateMethod);
        _constructorDescriptor.GetHandleFieldMethod(Arg.Any<Type>()).Returns(Methods.HandleFieldMethod);

        // Act
        Delegate resumer;
        using (_il.InterceptCalls())
        {
            resumer = _sut.GetResumer(StateMachineType);
        }

        // Assert
        resumer.Should().BeOfType(delegateType);
        _il.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        _il.Received(1).DeclareLocal(StateMachineType);
        _il.Received(2).DefineLabel();
        Received.InOrder(() =>
        {
#if DEBUG
            _il.Emit(OpCodes.Newobj, StateMachineConstructor);
            _il.Emit(OpCodes.Stloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Initobj, StateMachineType);
#endif

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Call, Arg.Is(CreateMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>t__builder")));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "arg");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("arg")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>4__this");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>4__this")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, LocalFieldName);
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField(LocalFieldName)));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

#if DEBUG
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<result>5__2");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<result>5__2")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>s__3");
            _il.Emit(OpCodes.Ldloc_0);
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>s__3")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleFieldMethod));
            _il.Emit(OpCodes.Pop);
#endif

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>1__state");
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>1__state")));
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleStateMethod));
            _il.Emit(OpCodes.Pop);

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__1")));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Callvirt, Arg.Is(GetAwaiterMethod()));
            _il.Emit(OpCodes.Stfld, Arg.Is(StateMachineField("<>u__3")));

            _il.MarkLabel(Arg.Any<Label>());
#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Ldloca_S, 0);
            _il.Emit(OpCodes.Call, Arg.Is(StartMethod()));

#if DEBUG
            _il.Emit(OpCodes.Ldloc_0);
#else
            _il.Emit(OpCodes.Ldloca_S, 0);
#endif
            _il.Emit(OpCodes.Ldflda, Arg.Is(StateMachineField("<>t__builder")));
            _il.Emit(OpCodes.Call, Arg.Is(TaskGetter()));

            _il.Emit(OpCodes.Ret);
        });
    }
}
