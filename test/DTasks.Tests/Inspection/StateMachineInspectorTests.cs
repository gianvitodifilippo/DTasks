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
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0tgAxGDWKQCiAEIAqgDidc0tpRUilZ3dML0DQ6MAPBCsxRAAfACsAPo/6NNvBF/NEmH9Xu9Vqdmu82lcbj1+oNhqYRk8LOIKBBgN8/qhAbM/FFAiQ/hiscAoY0zuc4dcuoiHii0R9xH8AMwE4HExZ/cRU9ZDcQMaFNWGXem3e7I54Qkq4n74mbchZgn5yiAC4wwLaitR61Ti9oMu4McJzVFPD7oPkRLlzEEk21zKk0ggGxLWUwhcT6QzahgwJ4ATRlAEEAO62MB2Mq9Eihx6R6MOHgfUxJVjVWrUs4NN1i1oShGm5Mx3iWj4UP4A10F1TIADsazdAF8PR6tgRvb6DEYTIGnsgNGXU08PDj05nsx78/XacWTb1RxWWdWfpy62d27mCDumtCODwwII5uotGVWKwIKFe/6B0GACortPe81E1WxeKJOJZggAXg+H94hIP1+xgQMX0fABPIwAP/AhgFghhWEsUxnyjcseCpHccAnXhLBcLwwhVUEv1YJRoRguCwIDGAoOQghFAICYGGASQ9xwXCNi7XZDhOXBUBwQUIGFHBslyYBhO1XVsHfeZQUo3dkHZH9gAIZ0HBbfVlNUsNxGgqgRGHRw2NcVgYD2ChIAgnhxxoIC/jKazhl4bTVGhFSCEcaDqK8ZZ3PdXSCH0wyRFM4BzJgNgaGjTxCj+SKwCkaFPNU4hCHBN4SkC4TeP2Y40rUjSfnJbFcuCicSpSjYZMsVLgsTYZR1sOMmCa+jMNTEq7HQCqmi84cXx62xUH6tRBpHLreHsnERvZNZuLkHy/KYzjsFkVAQoMoyIqimK7ECQpmO4iS8gINIMiyYAcjyAolIG9LdCXBgX1MDKCEsGUADlbEYaoAKAyKeFYCMCE8MHvtYYAQk4XpGBoQMdgADzEDhaEyUwtyIJ6CBe+T3vQQgvseX7GGCKrTwgCgGABwDENcEGwYhggoZhuGGARhwYBRtGMaoLHAsG56pSRR4nkfJkYHTD6SeGMmggISWZQIKmabpoHGdB8GGEh6HYY4eHjG53mGHRsBMex4W8dFqXCeJn6/sVy9r1Vkp1cBhmmZ1vX2cNznjaR1Gzf5wWiuQEXGRle3Psd8milccY3ep2nPeB7WWbZg2jcRnng/Ny2hdxl67dluPFfEcyeHUtXU/p9Pmd11n9Y5rmg75i2Bat4vbejsvSadimaGTj3661xvfezgPc9Nguu6LnQbajx4Y7lmAFeCV4qAmEe68173M5b/227zjvC/DyPTVLonY4H+ODCrmv3b3r2M6brPW8D0+Q87sPgojpeV8+43zXhvAg1lh61w1q/Cezc/Y5xNvnUO3dF4l2AQ7O+issSZB3lAtO48fZwKnifWeyCF4ALQSvfu8tB6fVePYXe0CG6EI/sfL+pDf4oIob3KhIDy7BBgKwCgbVGH4IPu/I+CD24/3Pv/S+0peEYJofHCCIh6AlFEWPcRk9P4zyQZw8h8ixYomoevWhGUAD8mj95vx0WwvRZ954X0AQokxfDMGpDmI+egXg8FaNsUQ3RiDHF/0eqgnhbilFmPjkcayMBrEwJYZI6ewSZFOLkS44xZhTFgLKNBBwABtAAugk5hh94EpOkXPUJE0e7L0ibfZRisyAMFsDAAA8lQCA0FTJ0AKNBJ4eSHBAT8TY2BrCpHf2qThIAA=
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIigAxGCm8QCiAEIAqgDilXX1BcW8JS1tuh3dvQMAPBB0eRAAfACsAPrvCGOuwZ5h1E+DyeCwOdSejVO53aXR6fX0/VuRgEhAgwDenzgPwmHlC3lwn2RqOAoJqhyOkLOrRh13hiOeAk+AGZsX88TNPgJSUtegJKGDahCTlSLlc4XdgfkMe8seM2dNAe9JRBuTpdKsBfJNXIhU1qZdKEFJgjbs8EJzgqzJv98RbJqTyZhtTFTPp/AINFo1ZRdLcAJrigCCAHdzGALIUOrgAzcQ2GrKxnvpYnQyhUyYdqo7BQ1hdCDXHw2wTc9CJ9vg7s3IYAB2RaOgC+zudq0wbo9mm0eh9txgikLCduTnRSZTaedWarFLz+o6A+L9LL7xZlcOTYzmHXtTBjFYYC4kwUykKdDoEACHa93d9ABV54m3UbcQqIlEYpFU5gALzPd9RXCel2ug+veN4AJ7aN+X6YMAEGUHQxj6HeoZFqwpLrqgw5sMYdguIE8oAq+dDSGC4GQYB3q6KBcGYFImDDJQwBCJuqAYcsrYbDs+xoHAqA8hAfKoGkGTAHxaoaigT5TACJEbjATLvsAmB2lY9ZanJCmBgIYHELwfbWIx9h0LomyEJAwGsEOpC/p8hRmX0bBqXIYLyZg1hgWRLhzE5ToaZgWk6bwBnAEZuj0KQYbODknwhWAwhgi5Ck4FgQKPPkPl8RxWx7IlinKe8RJohlfnDvl8XLOJxgJX5MZ9AO5iRtQtVUShCb5RYCDFbUrl9ve7XmHAXXyD1/atWwVnov1TKLGx4juZ5tEsSgYhwP52m6cFoXhRY3g5HRbHCZkmCJMkqTAOkmTZLJ3VJWos6UPe+jJZgxjigAcuYVBlN+v4hawdDBpgziA29dDAP4TAdFQpA+usAAe/CMGQKT6Ku2C3Zg91SU9CBYK9NwfVQfilQeECEJQ30/jB9j/YDwOYKD4OQ5Q0NWLo8OI8jxCoz5PV3aKsI3LcN60roSbPfjfSE74mAi+KmCk+TlO/TTANA5QINgxDjBQzobMc5QSNgCjaN85jAuizjePvZ9MsnmeCv5ErP3U7T6ua0zOss3rsMI4bXM87lMD8zS4pWy9NtE7k9hDI7ZMUy7f1q/TjPa7rMPs37Rsm7zGP3ZbEuRzLAhGawSmKwnVNJ3TGsM1rzOs77nPG9zpt5xbYeFwTtvE6QcfO1Xqs1x7afexnBvZ63ueqOboc3OHku6NLfgPMQwz95XKtuyn9de43mfNznQchwaBe4xH3dR5opfl07m+u8ntepw3PsH/7LeB35wez6fnfn4vy9MBmT7hXZWD9h5109unfWWcA5txnvnP+1tL4y1RCkdeoDE5D3dpA0e+8J5wOnt/RB88u5Sx7i9B4lgN5gOrjg5+e9X4EI/vA4hHdSH/yLn4XQdBCCNRoVg7eT9d7QKbu/I+X8T5ig4cg8hUdgK8AoPkARg8hEjxfuPWBLCiFSMFvCMhS8KHJQAPwqK3o/dRjDNGHynsfH+0j9GcJQQkSYN4KAuEwaoixuCNEwJsZ/G6CD2GONkYYqOuwzK6DMeA+hIix5+PEbYyR9i9EGAMYAwoYErAAG0AC60S6E7ygfEsRk8AnDXbnPEJF85Ey3wJQcwugADyxAIBgQMuQbIYFbiZKsL+Tx5iIEMNEW/Mp6EgA===
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsStructNotPassedByRef()
    {
        // Arrange
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
            _il.Emit(OpCodes.Call, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarga_S, 2);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwRbWACIMIqxUPjwUIsCsPAQithAQAEYuANYAlCpqythqLSVllTUkABK2JhAMAGJgDBBmAES2PADmY8E+9pEBniSTU7XS2ADEYOkAogBCAKoA4g2tbeVVItXdvTD9QyPjADwQrKUQAHwArAD6v+hZt4Iv5okx/m8Puszq0Ph1rrc+oNhqNTGNnhZxBQIMAfv9UED5n4ooESP9MdjgNDmucLvCbj0kY9UejPuJ/gBmQkgknLf7iambEbiBgwlpwq4Mu4PFEvSFlPG/AlzHlLcG/eUQQXGGA7MVqfWqCWdRn3BjhBZo56fdD8iLchag0l2hbU2kEQ2JaymELifSGHUMGDPACasoAggB3WxgOwVfokMNPKMxhw8T6mJKsWr1GnnJru8XtSWIs0p2O8K2fCj/QFuwuqZAAdg27oAvp7PTsCD6/QYjCYg89kBpy2nnh5cRmsznPQWG3SS6b+mPK6ya78ufXzh28wRdy0YRweGBBAt1FoKqxWBBQn2A4PgwAVVfpn0W4lq2LxRJxbMEABeT5f3iEh/QHGAg1fJ8AE8jEAgCCGAOCGFYSxTBfaMKx4aldxwSdeEsFwvDCVUwW/VglBhWD4PAwMYGglCCEUAgpgYYBJH3HA8K2bs0gOE5ZFQHAhQgEUcFyfJgBEnU9WwD9FjBKi92QDlf2AAgXQcVsDRUtTw3EGCqBEEdHHY1xWBgfYKEgSCeAnGhgP+CobNGXgdNUGFVIIRwYJorxVg8j09IIAyjJEMzgAsmA2BoGNPGKf4orAKQYS8tTiEICF3jKIKRL4gTThCydNN+CkcTy4qaFK1Ktlkyw0pCpNRjHWx4yYZqGKwtNSrsdBKpabyR1fXrbFQAa1CG0dut4BzcVGjkNh4uRfP85iuOwITQsM4zIui2K7ECYoWJ4ySCgIDIshyYA8gKIplMGjLdGXBhX1MTKCEsWUADlbEYWpAOAqKeFYSMCE8MHvtYYAQk4fpGBoINdgADzEDhaGyUxtyIJ6CBehT3vQQgvqeX7GGCEqzwgCgGABoCkNcEGwYhggoZhuGGARhwYBRtGMaoLGgqG57pWRJ5nifZkYAzD6SdGMmggISXZQIKmabpoHGdB8GGEh6HYY4eHjG53mGHRsBMex4W8dFqXCeJn6/sVq8b1Vsp1cBhmmZ1vX2cNznjaR1Gzf5wX0p0G2mVle3Psd8mSlcSY3ep2nPeB7WWbZg2jcRnng/Ny2hdxl67dluPFfECyeA0tXU/p9Pmd11n9Y5rmg75i2Bat4vbejsvSadinqtrjWvYzpus9bwO847wvw+QEWo6eGO5ZgBXgjeKgpmTj3661xvfezgPc9Nguu6LiOS77onY4H+ODCrmv3brzXvczlv/bbmeQ87sOQoXyOZpS431XuvAgNlh7P1Hg3H2zc/Y5xNvnUO3dL692Xv3eWg9wGb23iPNO+9YGT0/tPU+yCL4AKvugkB5dgiWDePYHeL8x4HzgUfL+pDf4oIoWg1EGC15YJgKwCg7VGHQIIe/eBx9EGz3PvPReQDr4OzvorSCIh6BlFEfgt+E8P4IPbj/Oe/95EyioUozB8dMoAH5NF720YfKeJ8kGcPIcYsWvDqHKOCGkBYT56BeDwbY8e9jiGOJkX/R6qCl7uLMfw+OxwbIwBsa/IJrCHHSIMbIoxgCTHRNvuY52MEHAAG0AC6STmGEN0VI/RZ9wmTR7lEswfCwFkAYLYGAAB5KgEAYJmToEUGCzwKiFIYMBAJySWFEL0d/WpuEgA==
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfkamACKUvHTEbqyEvMB0rJi85hAQAEZ2ANYAlLLyMijy9fmFJeW4ABLmehCUAGJglBAGAETmrADmg35uliFezrgjoxUiKADEYEkAogBCAKoA4tUNjUWlvGVtHbpdvf1DADwQdAUQAHwArAD6HwgTrsGeYWoX0ezyWhwaz2aZwunR6fQG+kGdyMAkIEGA7y+cF+Uw8oW8uC+KLRwDBdSOxyh53asJuCKRLwEXwAzDj/vi5l8BGSVv0BJRwfVIadqZdrvD7iDCpiPtjJuzZkCPlKIDydLp1oL5Fq5MKWjSrpQgtNEXcXggucE2dMAQTLdMyRTMDqYqZ9P4BBotOrKLo7gBNCUAQQA7uYwBZil1cIHbqHw1ZWC99LE6BUquSjrUnUKmiKYYb4xG2KaXoQvj9HTm5DAAOzLJ0AXxdLvWmHdns02j0vruMEURcTdycGOTqfTLuz1cp+YNXUHJYZ5Y+rKrR2bmcwG/q4MYrDAXGmCmUxTodAgAU73p7foAKguk+7jXjFREojFImnMABeF4fqK4F63a6L6D63gAntoP7fpgwCQZQdDGPo95hsWrBkhuqAjmwxh2C4gQKoCb50NI4IQVBQE+roYHwZgUiYKMlDAEIW6oJhqxtok2z7GIcCoLyED8qgGRZMA/HqpqKDPjMgKkZuMDMh+wCYPaVgNtq8mKUGAjgcQvD9tYTH2HQuhbIQkAgaww6kH+XzFOZAxsOpcjggpmDWOB5EuAsznOppmDabpvCGcAxm6PQpDhs4eRfKFYDCOCrmKTgWDAk8hS+fxnHcQc/kjipHzEuimV5aQBUJasEnGIl/mxgMg7mFG1B1dRqGJgVFgICV9Ruf2D4deYcDdfIvUDm1bDWRiA3Mss7HiB5Xl0axKC8QFOl6SFYURRY3h5PR7EidkmDJKk6TAJk2S5HJPXJWoc6UA++gpZgxgSgAcuYVAVD+f6hawdAhpgziA29dDAP4TBdFQpC+hsAAe/CMGQaT6Gu2C3Zg93SU9CBYK9twfVQfj5YeECEJQ32/rB9j/YDwOYKD4OQ5Q0NWLo8OI8jxCo75vV3WKcK3Hct50roybPfjAyE74mAixKmCk+TlO/TTANA5QINgxDjBQzobMc5QSNgCjaN85jAuizjePvZ9MunueCuFErP3U7T6ua0zOss3rsMI4bXM80lqjm7SEpWy9NtE/k9gjI7ZMUy7f1q/TjPa7rMPs37Rsm7zGP3ZbEuRzLAjGawymKwnVNJ3TGsM1rzOs77nPG9zpt5xbYeFwTtvE2VFfK67ye16nDc+5nzc50HMD86Htzh5LujS34jzEKMcfO1Xqs1x7afexnBvZ63ufB/nne4xH3dR5opfl07lcq27Kf117jfj/7LeB/508h4aBfnwvS9MDmT7nfAe1d3Z109unfWWcA5txPh3OeXcpY9yASvNe/dE5bwgSPF+Y8D5wOPt/U+SD/5Fz8MYR4lh1730HtvSBu9X4EI/vA4hiCETIMXqg3QdBCBNRoWA7BT8oF7xgRPI+U8Z6/zPtbS+MsQK8AoIUARWDH7D2ftApu79J5fykeKUhsiUFRxSgAfhUZvNRO9R771gSwohejBYcLIXIvwiRpi3goC4TBFih5WLwTY8Rn8boINnk4wxXCo57HMrocxD9fEMOsWI7REjdE/30WEi+Ri7bgSsAAbQALqxLoTgjRoitGHyCSNduoSDCcMAfgSg5hdAAHliAQHAoZcguRwJ3GKDkygf5vFxPobgzRb8KkYSAA===
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsStructPassedByRef()
    {
        // Arrange
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
            _il.Emit(OpCodes.Call, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Call, Arg.Is(Methods.HandleAwaiterMethod));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0tgAxGDWKQCiAEIAqgDidc0tpRUilZ3dML0DQ6MAPBCsxRAAfACsAPo/6NNvBF/NEmH9Xu9Vqdmu82lcbj1+oNhqYRk8LOIKBBgN8/qhAbM/FFAiQ/hiscAoY0zuc4dcuoiHii0R9xH8AMwE4HExZ/cRU9ZDcQMaFNWGXem3e7I54Qkq4n74mbchZgn5yiAC4wwLaitR61Ti9oMu4McJzVFPD7oPkRLlzEEk21zKk0ggGxLWUwhcT6QzahgwJ4ATRlAEEAO62MB2Mq9Eihx6R6MOHgfUxJVjVWrUs4NN1i1oShGm5Mx3iWj4UP4A10F1TIADsazdAF8PR6tgRvb6DEYTIGnsgNGXU08PDj05nsx78/XacWTb1RxWWdWfpy62d27mCDumtCODwwII5uotGVWKwIKFe/6B0GACortPe81E1WxeKJOJZggAXg+H94hIP1+xgQMX0fABPIwAP/AhgFghhWEsUxnyjcseCpHccAnXhLBcLwwhVUEv1YJRoRguCwIDGAoOQghFAICYGGASQ9xwXCNi7XZDhOXBUBwQUIGFHBslyYBhO1XVsHfeZQUo3dkHZH9gAIZ0HBbfVlNUsNxGgqgRGHRw2NcVgYD2ChIAgnhxxoIC/jKazhl4bTVGhFSCEcaDqK8ZZ3PdXSCH0wyRFM4BzJgNgaGjTxCj+SKwCkaFPNU4hCHBN4SkC4TeP2Y40rUjSfnJbFcuCicSpSjYZMsVLgsTYZR1sOMmCa+jMNTEq7HQCqmi84cXx62xUH6tRBpHLreHsnERvZNZuLkHy/KYzjsFkVAQoMoyIqimK7ECQpmKWra0gyLJgByPICiUgb0t0JcGBfUwMoISwZQAOVsRhqgAoDIp4VgIwITwQc+1hgBCThekYGhAx2AAPMQOFoTJTC3IgHoIJ75Ne9BCA+x5vsYYIqtPCAKAYP7AMQ1wgZBsGCAhqGYYYOGHBgJGUbRqgMcCwbHqlJFHieR8mRgdM3qJ4YSaCAhxZlAgKapmmAfp4HQYYcHIehjhYeMTnuYYVGwHRzHBZx4WJfxwmvp++XL2vZWSlV/66YZrWddZ/X2cNhHkZN3n+aK5AhcZGVbfe+3SaKVxxhdynqfdwHNaZlm9YN+GucD03zYF7Gnpt6WY/l8RzJ4dSVeT2nU8Z7Xmd1tmOYDnmzb5i3C+tyOS+Jh2yZoRO3drjX6+9zO/ez42847gudCtiPHijmWYDl4JXioCYh5r9XPfTpvfZbnO2/z0Pw9NYuCejvvY4MCuq9dnePbThuM+b/3j6D9uQ+CsOF4vnuV8V5rwINZQe1c1bPzHo3H2Wcja52Dp3eeRdAF2xvvLLEmQt4QJTqPL2MCJ5H2noguef8UFL17rLfu71Xj2G3pAuu+C36Hw/sQ7+SCyHdwoUA0uwQYCsAoG1ehuC96vwPnA1uX9T6/3PtKbhaCqGxwgiIegJRhEj1EePd+U8EHsNIbIkWKJKGr2oRlAA/Oo3eL8tEsJ0SfWeZ9/5yKMTw9BqQ5iPnoF4HBGjrEEO0fA+xP97rIK4S4hRJjY5HGsjASxUCmHiMnoEqRDiZFOMMWYYxICyjQQcAAbQALpxMYfvWBSTJEz2CRNLui9wnX0UfLMgDBbAwAAPJUAgNBUydACjQSeDkhwQEfFWOgcwiRn9Kk4SAA=
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIigAxGCm8QCiAEIAqgDilXX1BcW8JS1tuh3dvQMAPBB0eRAAfACsAPrvCGOuwZ5h1E+DyeCwOdSejVO53aXR6fX0/VuRgEhAgwDenzgPwmHlC3lwn2RqOAoJqhyOkLOrRh13hiOeAk+AGZsX88TNPgJSUtegJKGDahCTlSLlc4XdgfkMe8seM2dNAe9JRBuTpdKsBfJNXIhU1qZdKEFJgjbs8EJzgqzJv98RbJqTyZhtTFTPp/AINFo1ZRdLcAJrigCCAHdzGALIUOrgAzcQ2GrKxnvpYnQyhUyYdqo7BQ1hdCDXHw2wTc9CJ9vg7s3IYAB2RaOgC+zudq0wbo9mm0eh9txgikLCduTnRSZTaedWarFLz+o6A+L9LL7xZlcOTYzmHXtTBjFYYC4kwUykKdDoEACHa93d9ABV54m3UbcQqIlEYpFU5gALzPd9RXCel2ug+veN4AJ7aN+X6YMAEGUHQxj6HeoZFqwpLrqgw5sMYdguIE8oAq+dDSGC4GQYB3q6KBcGYFImDDJQwBCJuqAYcsrYbDs+xoHAqA8hAfKoGkGTAHxaoaigT5TACJEbjATLvsAmB2lY9ZanJCmBgIYHELwfbWIx9h0LomyEJAwGsEOpC/p8hRmX0bBqXIYLyZg1hgWRLhzE5ToaZgWk6bwBnAEZuj0KQYbODknwhWAwhgi5Ck4FgQKPPkPl8RxWx7IlinKe8RJohlfnDvl8XLOJxgJX5MZ9AO5iRtQtVUShCb5RYCDFbUrl9ve7XmHAXXyD1/atWwVnov1TKLGx4juZ5tEsSgYhwP52m6cFoXhRY3g5HRs2rYkySpMA6SZNksndUlaizpQ976MlmDGOKABy5hUGU36/iFrB0MGmDOP9L10MA/hMB0VCkD66wAB78IwZApPoq7YNdmC3VJD0IFgz03G9VB+KVB4QIQlCfT+MH2L9/2A5gwOg+DlCQ1Yuiw/DiPEMjPk9Tdoqwjctw3rSuhJo9uN9PjviYEL4qYMTpPk99VN/QDlBAyDYOMBDOgs2zlAI2ASMozz6N88LWM46971SyeZ5y/kCtfZT1Oq+rDNa0zOvQ3D+sc1zuUwLzNLihbT1WwTuT2EM9sk2TTs/SrtP05r2tQ6zPsG0b3No7d5ti+HUsCEZrBKfLccUwnNNq3TGuM8z3vs4bnPGznZsh/nePW4TpAx47FfK1Xbsp57ad65nzfZ6opvBzcofi7okt+A8xDDL35dKy7Se1x79fp43WcB0HBp59jYedxHmjF6XDvr87ifV8ndde3vvtN/7fmB9Px/t6f8+L5gZke5l0VnfQeNd3ap11hnP2Lcp65x/pbc+UtUQpFXsA+OA9XbgOHrvMeMDJ6f3gbPDuEsu5PQeJYNeIDK5YMfjvZ+eC36wMIW3Yhv8C5+F0HQQgjUqEYM3g/bekCG6vwPh/I+Yo2GINIRHYCvAKD5D4f3ARQ8n6j2gUwghEj+bwhIQvMhyUAD8SiN731UfQ9R+8J6Hy/pI3R7CkEJEmDeCgLh0HKLMdgtRUCrHvyunA1h9jpH6IjrsMyugTGgNoUIkePjRHWPEbYnRBg9H/0KGBKwABtAAupEmhW8IGxJEePPxw1W4zyCWfGRUt8CUHMLoAA8sQCAYEDLkGyGBW46SrC/ncaYsBdDhEvxKehIAA==
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsClassNotPassedByRef()
    {
        // Arrange
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
            _il.Emit(OpCodes.Call, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldstr, "<>u__3");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ret);
        });
    }

    // IL reference
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0tgAxGDWKQCiAEIAqgDidc0tpRUilZ3dML0DQ6MAPBCsxRAAfACsAPo/6NNvBF/NEmH9Xu9Vqdmu82lcbj1+oNhqYRk8LOIKBBgN8/qhAbM/FFAiQ/hiscAoY0zuc4dcuoiHii0R9xH8AMwE4HExZ/cRU9ZDcQMaFNWGXem3e7I54Qkq4n74mbchZgn5yiAC4wwLaitR61Ti9oMu4McJzVFPD7oPkRLlzEEk21zKk0ggGxLWUwhcT6QzahgwJ4ATRlAEEAO62MB2Mq9Eihx6R6MOHgfUxJVjVWrUs4NN1i1oShGm5Mx3iWj4UP4A10F1TIADsazdAF8PR6tgRvb6DEYTIGnsgNGXU08PDj05nsx78/XacWTb1RxWWdWfpy62d27mCDumtCODwwII5uotGVWKwIKFe/6B0GACortPe81E1WxeKJOJZggAXg+H94hIP1+xgQMX0fABPIwAP/AhgFghhWEsUxnyjcseCpHccAnXhLBcLwwhVUEv1YJRoRguCwIDGAoOQghFAICYGGASQ9xwXCNi7XZDhOXBUBwQUIGFHBslyYBhO1XVsHfeZQUo3dkHZH9gAIZ0HBbfVlNUsNxGgqgRGHRw2NcVgYD2ChIAgnhxxoIC/jKazhl4bTVGhFSCEcaDqK8ZZ3PdXSCH0wyRFM4BzJgNgaGjTxCj+SKwCkaFPNU4hCHBN4SkC4TeP2Y40rUjSfnJbFcuCicSpSjYZMsVLgsTYZR1sOMmCa+jMNTEq7HQCqmi84cXx62xUH6tRBpHLreHsnERvZNZuLkHy/KYzjsFkVAQoMoyIqimK7ECQpmKWra0gyLJgByPICiUgb0t0JcGBfUwMoISwZQAOVsRhqgAoDIp4VgIwITwQc+1hgBCThekYGhAx2AAPMQOFoTJTC3IgHoIJ75Ne9BCA+x5vsYYIqtPCAKAYP7AMQ1wgZBsGCAhqGYYYOGHBgJGUbRqgMcCwbHqlJFHieR8mRgdM3qJ4YSaCAhxZlAgKapmmAfp4HQYYcHIehjhYeMTnuYYVGwHRzHBZx4WJfxwmvp++XL2vZWSlV/66YZrWddZ/X2cNhHkZN3n+aK5AhcZGVbfe+3SaKVxxhdynqfdwHNaZlm9YN+GucD03zYF7Gnpt6WY/l8RzJ4dSVeT2nU8Z7Xmd1tmOYDnmzb5i3C+tyOS+Jh2yZoRO3drjX6+9zO/ez42847gudCtiPHijmWYDl4JXioCYh5r9XPfTpvfZbnO2/z0Pw9NYuCejvvY4MCuq9dnePbThuM+b/3j6D9uQ+CsOF4vnuV8V5rwINZQe1c1bPzHo3H2Wcja52Dp3eeRdAF2xvvLLEmQt4QJTqPL2MCJ5H2noguef8UFL17rLfu71Xj2G3pAuu+C36Hw/sQ7+SCyHdwoUA0uwQYCsAoG1ehuC96vwPnA1uX9T6/3PtKbhaCqGxwgiIegJRhEj1EePd+U8EHsNIbIkWKJKGr2oRlAA/Oo3eL8tEsJ0SfWeZ9/5yKMTw9BqQ5iPnoF4HBGjrEEO0fA+xP97rIK4S4hRJjY5HGsjASxUCmHiMnoEqRDiZFOMMWYYxICyjQQcAAbQALpxMYfvWBSTJEz2CRNLui9wnX0UfLMgDBbAwAAPJUAgNBUydACjQSeDkhwQEfFWOgcwiRn9Kk4SAA=
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIigAxGCm8QCiAEIAqgDilXX1BcW8JS1tuh3dvQMAPBB0eRAAfACsAPrvCGOuwZ5h1E+DyeCwOdSejVO53aXR6fX0/VuRgEhAgwDenzgPwmHlC3lwn2RqOAoJqhyOkLOrRh13hiOeAk+AGZsX88TNPgJSUtegJKGDahCTlSLlc4XdgfkMe8seM2dNAe9JRBuTpdKsBfJNXIhU1qZdKEFJgjbs8EJzgqzJv98RbJqTyZhtTFTPp/AINFo1ZRdLcAJrigCCAHdzGALIUOrgAzcQ2GrKxnvpYnQyhUyYdqo7BQ1hdCDXHw2wTc9CJ9vg7s3IYAB2RaOgC+zudq0wbo9mm0eh9txgikLCduTnRSZTaedWarFLz+o6A+L9LL7xZlcOTYzmHXtTBjFYYC4kwUykKdDoEACHa93d9ABV54m3UbcQqIlEYpFU5gALzPd9RXCel2ug+veN4AJ7aN+X6YMAEGUHQxj6HeoZFqwpLrqgw5sMYdguIE8oAq+dDSGC4GQYB3q6KBcGYFImDDJQwBCJuqAYcsrYbDs+xoHAqA8hAfKoGkGTAHxaoaigT5TACJEbjATLvsAmB2lY9ZanJCmBgIYHELwfbWIx9h0LomyEJAwGsEOpC/p8hRmX0bBqXIYLyZg1hgWRLhzE5ToaZgWk6bwBnAEZuj0KQYbODknwhWAwhgi5Ck4FgQKPPkPl8RxWx7IlinKe8RJohlfnDvl8XLOJxgJX5MZ9AO5iRtQtVUShCb5RYCDFbUrl9ve7XmHAXXyD1/atWwVnov1TKLGx4juZ5tEsSgYhwP52m6cFoXhRY3g5HRs2rYkySpMA6SZNksndUlaizpQ976MlmDGOKABy5hUGU36/iFrB0MGmDOP9L10MA/hMB0VCkD66wAB78IwZApPoq7YNdmC3VJD0IFgz03G9VB+KVB4QIQlCfT+MH2L9/2A5gwOg+DlCQ1Yuiw/DiPEMjPk9Tdoqwjctw3rSuhJo9uN9PjviYEL4qYMTpPk99VN/QDlBAyDYOMBDOgs2zlAI2ASMozz6N88LWM46971SyeZ5y/kCtfZT1Oq+rDNa0zOvQ3D+sc1zuUwLzNLihbT1WwTuT2EM9sk2TTs/SrtP05r2tQ6zPsG0b3No7d5ti+HUsCEZrBKfLccUwnNNq3TGuM8z3vs4bnPGznZsh/nePW4TpAx47FfK1Xbsp57ad65nzfZ6opvBzcofi7okt+A8xDDL35dKy7Se1x79fp43WcB0HBp59jYedxHmjF6XDvr87ifV8ndde3vvtN/7fmB9Px/t6f8+L5gZke5l0VnfQeNd3ap11hnP2Lcp65x/pbc+UtUQpFXsA+OA9XbgOHrvMeMDJ6f3gbPDuEsu5PQeJYNeIDK5YMfjvZ+eC36wMIW3Yhv8C5+F0HQQgjUqEYM3g/bekCG6vwPh/I+Yo2GINIRHYCvAKD5D4f3ARQ8n6j2gUwghEj+bwhIQvMhyUAD8SiN731UfQ9R+8J6Hy/pI3R7CkEJEmDeCgLh0HKLMdgtRUCrHvyunA1h9jpH6IjrsMyugTGgNoUIkePjRHWPEbYnRBg9H/0KGBKwABtAAupEmhW8IGxJEePPxw1W4zyCWfGRUt8CUHMLoAA8sQCAYEDLkGyGBW46SrC/ncaYsBdDhEvxKehIAA==
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsClassPassedByRef()
    {
        // Arrange
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
            _il.Emit(OpCodes.Call, Arg.Is(IsSuspendedMethod()));
            _il.Emit(OpCodes.Brfalse_S, Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldind_Ref);
            _il.Emit(OpCodes.Ldstr, "<>u__1");
            _il.Emit(OpCodes.Callvirt, Arg.Is(Methods.HandleAwaiterMethod));
            _il.Emit(OpCodes.Ret);

            _il.MarkLabel(Arg.Any<Label>());
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, Arg.Is(IsSuspendedMethod()));
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
    // DEBUG: https://sharplab.io/#v2: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=
    // RELEASE: https://sharplab.io/#v2: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
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsStructNotPassedByRef()
    {
        // Arrange
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
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhz4i6AKzTsM1AWY4A3jgI65ANiJ6APGBoA+AmQkVGACmQGe1iMAdQCTgGZrWVccB4KEWBWHgIRAEMICAAjCJEAawBKbV0tbF1Mgj5gCOAGAFl4gAtTBgJ/PMKSsoIAXhwAYjBvABEAUQAhAFUAcVSsrKoGAHds3PyikVLh2ySlRoYIcQYBwd0YBk8IihcFhioYFrW1zMrJmuGSAH1rmIpITbC6ggBBcQBPKhEHAoZgYqsGCdB4QJ4mcwsJxVOZKTKnXSRaJxRIkAASEUOEAYADEwEsYLYAEQRHgAcyJ7i8FQm1WmZRIpLJ8yaLQIHR6/Qy6yRsXiCXRmJg2LxBOJRggrCRZnkt3QlI8WxpVSmMyYt0lSJZ3MGvJRAoxWNx+LB4qc4l2wBlt1QCup5zpapu13Nlu163CUT5qMNwuNYqJRjM4luAGY7UqHaqGbdxNrFstVjqsnr+YKjaLTYHNVFrddbVTI7To1cNVKovGDkdPAidLXPci077sTkYYGzOhY7SI94o5d1dcHe71mzbKmfULsa8RhEwPkeOKzBQ5USkilk5l0h6zsX+87cuh6gRbABNE0waez3IxbEkM8Ey9z3hJWysGIAKwYwSSrpcDhIvT/I+86wvWAC+9b1qO44GpODDAbwi7Ltc4ZrvWW7bjofb0qW1y5KGR72HoCE8BCVovm+n7fr+rh6ABQEzk+C7DoMEEbro9bYU6tz3I8vAkK2PDALY9q7jhDAsVk9bIAA7MqFzic6vFgvxDhwrobFsaybRdH0KhNEsKw4P4gTBAZhzHNgrYKWqBAgAQACS7xfCI1mOmUmhrMgBGmMABBdlU6l1sm3lvJ83y/P8gLAqC4K+RYPGxbwQUEF5BEFB8AAqHwcOUTIpWlYUuX8AJAmwNCzsMYS3ACYBSCcIUEcQhBlkiBXYM0OmcoVvn+S6zjAO1mShb1sb7BZNbJoV95gsBEQ3kwM0Xox859QeQ26KFDgkWtESoBtOhbcRK28GRCV4REoYHUQTUACwEAUrCCAwAByDAAB7CUk9QWACPCsGMwxjC9rDAA5nDYowNAMDA7TvWIHC0L4oGNUQ90CMAbklgwthOeFrlibZXFlN9dS/cU/2A6MBAg2DEMMFD+Sw/DDCI2AyPalpuCqBl2W5QQGgEFzaBFd8JXReVuRlGEgtcyZQR+eV8vBKEnmozErCsBABDNvBJ0Ls1BCeOeL0RIwpPk5TBBAzToPgxwkMHEzcMI0jVAo8NBEa1rOtwW59joIQxsEqbjCFt4vWCFEFAST9BB/QD1vU7T9uO9DzOu+z7uSaF3va7rmYwEYmWF2YAdBybZsMOHBAl+eBBRxAMcW/HFOJzbKf04zMMu6zbse5tXua/ncGF+XRuV2HireHnDfR7HZOt1bHd213Ts9yzbMcylufD77GbnuPwdgqH1fT+ExSknPTcL5b7fJ6vDsM+vGd91nA+HUPPsF4fhvHzAp8a7iEBEJa+zc44JypsDR+adnab37jnL+I8D5ij/pPM+1JI7zxbpApO0C6ZP27q/Le2cd5IP3n6MeaCQ5VxrpKKgZIwG3yXvffBqdn7p17iQj+N0CCzx/qgwOE8aFT2pBQYBoQ/KN3AYvXBK8CGwI3pnbehV+Gj1/kI/+gDz4PBoEwnBbcoG2wURwuByjSGqL3gI001CT60J0fQxh0jmFyIfiYohXCEFkL4VY9RgiK4iIwUqTwko8j6IgYYvBxj2EePge/RBPjv5+JsZo9BNcYCsAoAtcJsjInyJiS/Tx8TvFqJQSkgJdjRFKk2CIegUQcl3yMZ3QhhS4kqPVr4sphJbEAPsdSZqAB+BpLCmkwNMUot+7TPaJOQZQjRFTelVLaFUTK9ByjOIMcvNxBTOFtIsR0pJXSj5pPPr0B4MBhmuLYWvXZ5ieG70OXM/xwjKlBJnh8fIABtAAupcvJ2yblmMmfs6ZpSnnlJeYst5lgGARBgAAeSoBAD4fw6ChA+EYGIHyGAWA2RErZ1yWm3OBSjMCQA==
    // RELEASE: https://sharplab.io/#v2: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=
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsStructPassedByRef()
    {
        // Arrange
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
    // DEBUG: https://sharplab.io/#v2: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=
    // RELEASE: https://sharplab.io/#v2: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
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsClassPassedByRef()
    {
        // Arrange
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
