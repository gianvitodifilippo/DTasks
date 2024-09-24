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
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0tgAxGDWKQCiAEIAqgDidc0tpRUilZ3dML0DQ6MAPBCsxRAAfACsAPo/6NNvBF/NEmH9Xu9Vqdmu82lcbj1+oNhqYRk8LOIKBBgN8/qhAbM/FFAiQ/hiscAoY0zuc4dcuoiHii0R9xH8AMwE4HExZ/cRU9ZDcQMaFNWGXem3e7I54Qkq4n74mbchZgn5yiAC4wwLaitR61Ti9oMu4McJzVFPD7oPkRLlzEEk21zKk0ggGxLWUxJVgkELifSGbUMMzAACeRlYllMAE0ZQBBADutjAdjKvRIcceSZTDh41QLHoabrFrQlCNNOdTvEtHwofwBrpLqmQAHY1m6AL4ej1bAjeuK+/2BowmEOmcOR6PIDRVvNPDw4gu1alnYvN2nlk29Oc1ln1n6cptnburgin084Re8SwuLxhFWg2LxJTQsqsVgQUIBgyjmDjgAVCMvGQABOXggKMKlLw2PtdkOE5cFQHBBQgYUcGyXJgBQ7VdWwc0iVVV8z2QdlEhoAhnQcDt9RIsj43EMMqBEGdHAYYBXFYGA9goSB/x4BcaA+SifjKXjhl4GjVGhUiCEcMNIK8ZYpPdOiCAYpiRDYjiuLYGgU08Qo/g4sApGhGSyOIQhwTeEoVJQuD9mOCzyOAETyWxey1MXESzI2XDLHMtSs2GOdbHTJgQpgXcjJ+Ox0C8ppZJnGKRLsVBErUZLZ2TasBMXYTjNsdk1hguR5MUghFHPGQkPUxjmO0ziYD0uxAkKaqYMwvICDSDIsmAHI8gKYikss3RtwYGLTCsghLBlAA5WxGGqAgAF5hI4nhWETAhPF2hbWGAEJOF6RgaBDHYAA8xA4WhMlMY8iHGghJoIhgZvQQh5seJbGGCHzBBKCgGFWjaCC2na9oYA6jpOjgzuMBwYGu277qoR6VOSiapSRR4ngApkYA+T7vsW5aggIQmZQIIGIBBsHNtcbbdv2ghDuO06GHO5HUYYO6wAep7sde3GidJubyf+gh30/WngdB9amZZ6HYc5hHuaRy6bv59HMZc5AccZGUJZ+4Y/sptxxnl+nFfByHWZh9m4a5nntbRwWMeFl7JvF2azZgC2Zk4ng3LphmlYh5mobZjn4cRi6UZ1gWhaxn2xZN/2pcpwGFcZqOVdjl2NbdpOPdTg2jdNP2vsl36KeCV4qAmG2I/t6PHbV+PNcTvmU69tOdFF43HlN7PggMEOw7zyOHdV531YT3nk7172h99zPa4DoOCF4ijw7t5WY6duPXa1svdc9/W1MN4fq83sn6+lrFMhbg/87novF575fy4Hyu77SlHlnJ+lNLCvHsK3Q+Bdj5dzPr3FeV8163w3sAre48CAwFYBQCKUCP4d3nqfEu58+6r0HigjOaDH7mwbpg9I9ASh4NngQr+3dS6kKQeQquQCUQgJodLKyAB+Jh7dC4n2Lkvd2l8K4324XjXh6DQGpDmABegXh37MLEXA4hCC/7XzGuvShCjqGB1oUcXiMARFH07gvNhJDEEyIMRQkexi678MpmUMMDgADaABdKxMCbFEMkRffu+isrpxcWYPhpjpZkAYLYGAAB5KgEAwxsToAUMMTxPEOGEho0RsDbHwN/tI/+2BOxAA==
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIigAxGCm8QCiAEIAqgDilXX1BcW8JS1tuh3dvQMAPBB0eRAAfACsAPrvCGOuwZ5h1E+DyeCwOdSejVO53aXR6fX0/VuRgEhAgwDenzgPwmHlC3lwn2RqOAoJqhyOkLOrRh13hiOeAk+AGZsX88TNPgJSUtegJKGDahCTlSLlc4XdgfkMe8seM2dNAe9JRBuTpdKsBfJNXIhU1qZdKEFJgjbs8EJzgqzJv98RbJqTyZhtTFTPpYnRcP4BBotGrKAZgABPbR0Yz6ACa4oAggB3cxgCyFDq4SM3WPxqysMrZ53VR2ChrC6EG9MJtgm56ET7fB35uQwADsi0dAF9nc7Vpg3ZEPV6fdo9P79EGQ2GYIpS5nbk50dmKmTDnm6xSi/qOpPy/Sq+8WbXDm2F5gDwfUDO2MY7C5AvKAREotIwYU6HQIAFvZoB7ohwAVYMuGAAJxsL+2ikieyydhsOz7GgcCoDyEB8qgaQZMA8FqhqKBGriCoPoeMBMjEpCYHaVjNlq+GEVGAiBsQvDjtYlDAPYdC6JshCQF+rDTqQzwke8hQcX0bDkXIYIEZg1iBiBLhzKJTqUZg1G0bwjHMax9CkPGzg5J8zFgMIYLiYROBYECjz5PJ8GQVsezGURwD8USaJWYpM78YZywYcYRmKamfSTuYSbUP5ugbrp7wWAgrm1BJ47hfxFhwDF8hxROcZltxM58Xp5hMos4HiFJMmYFIR6iLBSk0XRaksbomkWN4ORleBKGZJgiTJKkwDpJk2R4bFJlqGulDhfopmYMY4oAHLmFQZSYAAvHxzGsHQMaYM4G3TXQwD+EwHRUKQ/rrAAHvwjBkCk+h7tgQ2YCN2GUONCBYFNNyzVQfjuVw+SEJQC3LZgq3rZtlDbbt+2MIdOhWLoZ0XVdxA3fJcXDaKsI3Lc360rozwvW9M1zb4mA4+KmC/RA/2Ayt9hrRtW2YDte0HZQR1wwjlCXWA123WjD0Y7jBOTUTX2YE+L4U39ANLbT9NgxDLPQ2zsMnedXNIyj9kwOjNLisL719J9JMOEMUtUzLQMgwz4NM5DrPs2riM88jfP3SNQsTYbujG+MLGsI5lPU7LwN06DjPM1DMPHfD6vc7zqPu4L+te6LJM/dLNOh/LEf28rjux87Cfa7rBqe69IsfcTfgPMQwzm8HVthzbitRyrMec/HruJ6oAt6zcBtp34mj+4Hmch9bCt20r0cc3Hmtu73HspxX3u+5gHHEUHlty+HtuRw7quFxrLta4pOt92XK+E1XYuoik9fb1nk+5zP7dz0X3cl5fYoD6nt8k2MA8SwDcd7Zz3q3Q+Hd56n0XhfZef9V5D0wLoOghBgqgOfs3KeB985H07gvHu8Dk6IJvkbauKCkgUHyJgie2DX5twLgQ2BRDS6/3hP/chYtTIAH5aFNxzvvPOs8nYn2LufNhmMOFIIAQkSY34KAuCfnQwRkC8HQM/mfQaS8SHSLIT7ChuwOK6H4bvFu09GH4JgeI7RxD+56MrlwkmhRAxWAANoAF1THgPMbgkRx8u5aNSknexBhOEGLFvgSg5hdAAHliAQEDIxcg2RAy3BcVYPiyiBEQIsVAj+Yiv4oBbEAA===
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsStructNotPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            52;
#else
            37;
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
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwRbWACIMIqxUPjwUIsCsPAQithAQAEYuANYAlCpqythqLSVllTUkABK2JhAMAGJgDBBmAES2PADmY8E+9pEBniSTU7XS2ADEYOkAogBCAKoA4g2tbeVVItXdvTD9QyPjADwQrKUQAHwArAD6v+hZt4Iv5okx/m8Puszq0Ph1rrc+oNhqNTGNnhZxBQIMAfv9UED5n4ooESP9MdjgNDmucLvCbj0kY9UejPuJ/gBmQkgknLf7iambEbiBgwlpwq4Mu4PFEvSFlPG/AlzHlLcG/eUQQXGGA7MVqfWqCWdRn3BjhBZo56fdD8iLchag0l2hbU2kEQ2JaymJKsEghcT6Qw6hhmYAATyMrEspgAmrKAIIAd1sYDsFX6JHjT2TqYcPFqhc9TXd4vaksRZtzad4Vs+FH+gLdpdUyAA7Bt3QBfT2enYEH1xP0BoNGEyh0wRqMx5Aaav554eXGF+o084llt0ium/rz2ushu/LnN849tcEM9nnBL3iWFxeMKqsGxeJKGEVVisCChQMGMcwCcABVIy8ZAAE5eGAoxqSvLZ+zSA4TlkVAcCFCARRwXJ8mAVCdT1bALWJNU33PZAOUSGgCBdBxOwNUjyITcRwyoERZ0cBhgFcVgYH2ChIAAnhFxoT4qN+Co+NGXhaNUGEyIIRxwygrxVmkj16IIRjmJEdjOO4tgaFTTxin+TiwCkGFZPI4hCAhd4ylU1D4MQ051KXUSKRxBzXMo/kNiFEx8JaSyCGzUZ51sDMmFCmA92M347HQLyWjk2dYtEuxUCStQUrnFMa0EpcRJM2wOQ2WC5AUpSCEUC8ZFwVANKYlidK4mB9LsQJihq2CsIKAgMiyHJgDyAoihI5KrN0HcGFi0xrIISxZQAOVsRhagIABeETOJ4VgkwITx9qW1hgBCTh+kYGhQ12AAPMQOFobJTBPIhJoIabCIYOb0EIRanhWxhgjcwQygoBh1q2ggdr2g6GCOk6zo4C7jAcGBbvux6qGe1SUqm6VkSeZ5AOZGBPm+37ltWoICGJ2UCBBiAwYh7bXF2/bDoIY7TvOhhLtR9GGAesAnpe3H3vxknyYWynAYID8v3p0Hwc2lm2dh+HuaR3mUeuu7Bcx7HguQPGmVlKW/tGAHqbcSZFcZ5XIeh9m4c5hGeb53WMeFrHRbe6bJfmi2YCtuYuJ4YA7aZlWodZmGOa5xHkautG9aFkWcb9iWzcDmXqeBpXmZjtX47drWPZTr306Nk2zQDn7pf+qngjeKgpkjh3Vbjl2E/dnWK/173DfU43xdNp5zdz4IDDDiOGajx3Y+djXE+15OBbTn2M50Ufa+z+ug5Dgg+MoueO6Lrvl97tfU4N33t/9veKcb2XsWyNvT8Lp31ddzWk/5m/B53xHg/ceOdn7U0sG8ew7dP6L2/j3Mufd163y3sArOoD96TwIDAVgFBIowOjl/Euv9V7/0rpvauO8ZQYKfpbJu2DMj0DKAQhexdu6lz/p7AeVdh412oaiMBdDZbWQAPwsM7kvH+K9y7IMAagvhBMBGYPAcENICxAL0C8B/QhcDiHSKQQAnhE177oKUbQ4O9Djh8RgOI8+kiEGcP7hvIexi0FjzMQ3IR1MKjhgcAAbQALq2KIewkhMjDEUN4VQxRZhBEWNlmQBgtgYAAHkqAQHDOxOgRRwzPB8Q4ES2jWEXykVfMh3DIldiAA
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfkamACKUvHTEbqyEvMB0rJi85hAQAEZ2ANYAlLLyMijy9fmFJeW4ABLmehCUAGJglBAGAETmrADmg35uliFezrgjoxUiKADEYEkAogBCAKoA4tUNjUWlvGVtHbpdvf1DADwQdAUQAHwArAD6HwgTrsGeYWoX0ezyWhwaz2aZwunR6fQG+kGdyMAkIEGA7y+cF+Uw8oW8uC+KLRwDBdSOxyh53asJuCKRLwEXwAzDj/vi5l8BGSVv0BJRwfVIadqZdrvD7iDCpiPtjJuzZkCPlKIDydLp1oL5Fq5MKWjSrpQgtNEXcXggucE2dMAQTLdMyRTMDqYqZ9LE6Lh/AINFp1ZQDMAAJ7aOjGfQATQlAEEAO7mMAWYpdXBR25xhNWVgVHMu2pOoVNEUww0ZxNsU0vQhfH6OgtyGAAdmWToAvi6XetMO7Ip7vb7tHoA/pg6HwzBFGWs3cnBic1VyUd8/XKcWDV0pxWGdWPqy60d24vMIfD6hZ2xjHYXIEFYCIlFpODinQ6BAAj7NIPdMOACohlwwAAnGwf7aGSp6rF2iTbPsYhwKgvIQPyqAZFkwAIeqmooMaeKKo+R4wMyMSkJg9pWC22oEUR0YCEGxC8BO1iUMA9h0LoWyEJA36sDOpAvKRHzFJxAxsBRcjgoRmDWEGoEuAsYnOlRmA0XRvBMSxbH0KQCbOHkXwsWAwjghJRE4FgwJPIUCkIVBMEHEps4CcS6LWQ5JFcssvJ6Fh9QmZgaYDFO5jJtQAW6JuekfBYCCufUkkThFAkWHAsXyPFk7xuWPGzvx+nmMyywQeI0myZgUjHqIaBwMptH0eprG6FpFjeHk5UQah2SYMkqTpMAmTZLk+FxaZajrpQEX6GZmDGBKABy5hUBUmAALz8SxrB0LGmDOFts10MA/hMF0VCkAGGwAB78IwZBpPo+7YCNmBjThlCTQgWAzbc81UH4jlcIUhCUEtq2YOtm3bZQu37YdjDHToVi6BdV03cQd0KfFo1inCtx3D+dK6C8b0fXNC2+JgeMSpg/0QIDwNrfYG1bTtmB7QdR2UCdCNI5Q11gLd90Y09WP40T00kz9mDPq+VMA0DK304zENQ2zsMc/DZ2XTzKNo35MCY7SEqi59AzfWTDgjDLNNyyDYNM5DLPQ+znMa8jfOowLj1jSLU3G7opuTKxrDAJbtPy6DDPg8zrMw3Dp2I5rvP8+jnvC4bPvi2Tf2y3T4eK1Hjuq878eu0nuv64a3vvWLX2k34jzEKMIfWwrkf29HTvq8XWtuzrSl60LBu3EbGd+JogfB9Toc2xHdvKzHatx9zifu8nqgDxXadV77/uYJxJGT83uet3PHeLwn2se2vXub8TNcS2iaSNwfOe20rDsq7HXPnz3l/99fQ/pzvmTYwjxLBNxfjPN+7dC6dyXhfVef9U4AK3iPTAug6CEBCuAsOr984fwXl/EuK8y7r3FMg2+Jta5oJSBQQo2Dp55zbgXT+Ltu6lz7uXMhCJAGUIlmZAA/PQlus937zyLnAn+CDOHY24SgoBfhEjTB/BQFwz8cGQLwWI2B392HDSvkg2RFC/ZUL2JxXQQij4iOgSwruy9e56MQYPQx1deFk2KEGKwABtAAuhY3BTD8HiJ0cQjhpCZEGB4cYiW+BKDmF0AAeWIBAIMTFyC5CDHcdxVh+JqIYcfURp9CFsJCa2IAA=
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsStructPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            52;
#else
            37;
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
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0tgAxGDWKQCiAEIAqgDidc0tpRUilZ3dML0DQ6MAPBCsxRAAfACsAPo/6NNvBF/NEmH9Xu9Vqdmu82lcbj1+oNhqYRk8LOIKBBgN8/qhAbM/FFAiQ/hiscAoY0zuc4dcuoiHii0R9xH8AMwE4HExZ/cRU9ZDcQMaFNWGXem3e7I54Qkq4n74mbchZgn5yiAC4wwLaitR61Ti9oMu4McJzVFPD7oPkRLlzEEk21zKk0ggGxLWUxJVgkELifSGbUMMzAACeRlYllMAE0ZQBBADutjAdjKvRIcceSZTDh41QLHoabrFrQlCNNOdTvEtHwofwBrpLqmQAHY1m6AL4ej1bAjeuK+/2BowmEOmcOR6PIDRVvNPDw4gu1alnYvN2nlk29Oc1ln1n6cptnburgin084Re8SwuLxhFWg2LxJTQsqsVgQUIBgyjmDjgAVCMvGQABOXggKMKlLw2PtdkOE5cFQHBBQgYUcGyXJgBQ7VdWwc0iVVV8z2QdlEhoAhnQcDt9RIsj43EMMqBEGdHAYYBXFYGA9goSB/x4BcaA+SifjKXjhl4GjVGhUiCEcMNIK8ZYpPdOiCAYpiRDYjiuLYGgU08Qo/g4sApGhGSyOIQhwTeEoVJQuD9mOCzyOAETyWxey1MXESzI2XDLHMtSs2GOdbHTJgQpgXcjJ+Ox0C8ppZJnGKRLsVBErUZLZ2TasBMXYTjNsdk1hguR5MUghFHPGQkPUxjmO0ziYD0uxAkKaqytQAg0gyLJgByPICmIpLLN0bcGBi0wrIISwZQAOVsRhqgIABeYSOJ4VhEwITwdvm1hgBCThekYGgQx2AAPMQOFoTJTGPIgxoICaCIYab0EIObHkWxhgh8wQSgoBgVvWghNu23aGH2w7jo4U7jAcGArpuu6qAelTkvGqUkUeJ4AKZGAPg+r6FqWoICAJmUCEBiBgdBjbXC2na9oIA6jpOhgzqRlGGFusB7serGXpxwmSdmsm/oId9PxpoGQbWxnmahmGOfhrnEYu66+bRjGXOQbHGRlcXvuGX6KbccY5bphWwYhlnobZ2HOe5rXUYF9GheeiaxZm02YHNmZOJ4NzafpxXwaZyHWfZuGEfO5Htf5wXMe90Xjb9yWKYB+WGcj5WY+d9XXcT92U/1w3TV9z6JZ+8ngleKgJmt8O7ajh3VbjjWE955PPdTnQRaNx4Taz4IDGD0Pc4j+2VadtX455pPda9wefYzmv/cDgheIosPbaV6PHdjl3NdLnWPb1tSDaHquN9JuupaxTJm/3vPZ8Lhfu6Xsv+4r2/pQj0zo/CmlhXj2BbgffOR9O6nx7svS+q8b7ryAZvMeBAYCsAoBFSB7925zxPsXM+vcV4D2QenVBD8zb1wwekegJRcEz3wZ/LuJcSGILIZXQBKJgHUKllZAA/IwtuBdj5F0Xm7C+5dr5cNxjwtBIDUhzAAvQLwb8mGiNgUQ+Bv8r6jTXhQ+RVCA40KOLxGAwjD4d3nqw4hCDpH6PIcPIxtc+EUzKGGBwABtAAupY6B1jCESPPn3PRWU07OLMLwkxUsyAMFsDAAA8lQCAYY2J0AKGGJ4HiHDCXUSImBNi4E/ykX/bAnYgA
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIigAxGCm8QCiAEIAqgDilXX1BcW8JS1tuh3dvQMAPBB0eRAAfACsAPrvCGOuwZ5h1E+DyeCwOdSejVO53aXR6fX0/VuRgEhAgwDenzgPwmHlC3lwn2RqOAoJqhyOkLOrRh13hiOeAk+AGZsX88TNPgJSUtegJKGDahCTlSLlc4XdgfkMe8seM2dNAe9JRBuTpdKsBfJNXIhU1qZdKEFJgjbs8EJzgqzJv98RbJqTyZhtTFTPpYnRcP4BBotGrKAZgABPbR0Yz6ACa4oAggB3cxgCyFDq4SM3WPxqysMrZ53VR2ChrC6EG9MJtgm56ET7fB35uQwADsi0dAF9nc7Vpg3ZEPV6fdo9P79EGQ2GYIpS5nbk50dmKmTDnm6xSi/qOpPy/Sq+8WbXDm2F5gDwfUDO2MY7C5AvKAREotIwYU6HQIAFvZoB7ohwAVYMuGAAJxsL+2ikieyydhsOz7GgcCoDyEB8qgaQZMA8FqhqKBGriCoPoeMBMjEpCYHaVjNlq+GEVGAiBsQvDjtYlDAPYdC6JshCQF+rDTqQzwke8hQcX0bDkXIYIEZg1iBiBLhzKJTqUZg1G0bwjHMax9CkPGzg5J8zFgMIYLiYROBYECjz5PJ8GQVsezGURwD8USaJWYpM78YZywYcYRmKamfSTuYSbUP5ugbrp7wWAgrm1BJ47hfxFhwDF8hxROcZltxM58Xp5hMos4HiFJMmYFIR6iLBSk0XRaksbomkWN4ORlYVcCYIkySpMA6SZNkeGxSZahrpQ4X6KZmDGOKABy5hUGUmAALx8cxrB0DGmDOOtU10MA/hMB0VCkP66wAB78IwZApPoe7YINmDDdhlBjQgWCTTcM1UH47lcPkhCUPNS2YCta0bZQW07XtjAHToVi6Kd52XcQ13yXFQ2irCNy3N+tK6M8z2vdNs2+Jg2PipgP0QH9APLfYq3rZtmDbbt+2UIdsPw5QF1gFdN2o/d6M4/jE2E59mBPi+5O/f9i003ToPg8zUOszDx1nZziPI/ZMBozS4pC29fQfcTDhDJLlPS4DwP02DjMQyzbOqwj3NI7zd3DYL40G7oRvjCxrCORTVMy0DtMgwzTOQ9DR1w2rXM8yjbsC3rnsi8T31S9TIdy+HdtKw7MdO/HWs6waHsvcL71E34DzEMMZtB5bofWwrkfK9HHNxy7CeqPzus3Prqd+JofsBxnwdW/LtuK1H7Oxxrrs9+7yfl17PuYBxxGBxbsthzbEf2yrBfq87muKdrvel8vBOV6LqIpHXW+ZxPOfT23s+F13xcX2K/cpzfxPGAeJYeu28s67xbgfduc8T4L3PkvX+K9B6YF0HQQgwUQFPybpPfeedD4d3nt3OBScEHX0NlXZBSQKD5AwePLBL9W753wTAwhJcf7wj/mQ0WpkAD8NDG7Zz3rnGejtj5FzPqwjG7DEH/wSJMb8FAXCP1oQIiBuCoEf1PgNRexCpGkO9uQ3YHFdB8J3s3KeDC8HQLEVoohfddEV04cTQogYrAAG0AC6JiwFmJwcIo+ndNGpUTnYgwHD9Gi3wJQcwugADyxAICBkYuQbIgZbjOKsHxJR/DwHmMge/URn8UAtiAA=
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsClassNotPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            52;
#else
            37;
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
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhw40BZjgDeOAqqLoAbEQAsBPhXEcGVGAAoeDAGZ7gAQ2AMAsrZEALMFQYFxdh87ceDFAEAJJ8vk4u7p4hVJasBB7xwQAiDCKsVD48FCLArDwEIrYQEABGLgDWAJQqasrYak1FJeVVJAAStiYQDABiYAwQZgBEtjwA5iPBPvaRAZ4k4xPV0tgAxGDWKQCiAEIAqgDidc0tpRUilZ3dML0DQ6MAPBCsxRAAfACsAPo/6NNvBF/NEmH9Xu9Vqdmu82lcbj1+oNhqYRk8LOIKBBgN8/qhAbM/FFAiQ/hiscAoY0zuc4dcuoiHii0R9xH8AMwE4HExZ/cRU9ZDcQMaFNWGXem3e7I54Qkq4n74mbchZgn5yiAC4wwLaitR61Ti9oMu4McJzVFPD7oPkRLlzEEk21zKk0ggGxLWUxJVgkELifSGbUMMzAACeRlYllMAE0ZQBBADutjAdjKvRIcceSZTDh41QLHoabrFrQlCNNOdTvEtHwofwBrpLqmQAHY1m6AL4ej1bAjeuK+/2BowmEOmcOR6PIDRVvNPDw4gu1alnYvN2nlk29Oc1ln1n6cptnburgin084Re8SwuLxhFWg2LxJTQsqsVgQUIBgyjmDjgAVCMvGQABOXggKMKlLw2PtdkOE5cFQHBBQgYUcGyXJgBQ7VdWwc0iVVV8z2QdlEhoAhnQcDt9RIsj43EMMqBEGdHAYYBXFYGA9goSB/x4BcaA+SifjKXjhl4GjVGhUiCEcMNIK8ZYpPdOiCAYpiRDYjiuLYGgU08Qo/g4sApGhGSyOIQhwTeEoVJQuD9mOCzyOAETyWxey1MXESzI2XDLHMtSs2GOdbHTJgQpgXcjJ+Ox0C8ppZJnGKRLsVBErUZLZ2TasBMXYTjNsdk1hguR5MUghFHPGQkPUxjmO0ziYD0uxAkKaqytQAg0gyLJgByPICmIpLLN0bcGBi0wrIISwZQAOVsRhqgIABeYSOJ4VhEwITwdvm1hgBCThekYGgQx2AAPMQOFoTJTGPIgxoICaCIYab0EIObHkWxhgh8wQSgoBgVvWghNu23aGH2w7jo4U7jAcGArpuu6qAelTkvGqUkUeJ4AKZGAPg+r6FqWoICAJmUCEBiBgdBjbXC2na9oIA6jpOhgzqRlGGFusB7serGXpxwmSdmsm/oId9PxpoGQbWxnmahmGOfhrnEYu66+bRjGXOQbHGRlcXvuGX6KbccY5bphWwYhlnobZ2HOe5rXUYF9GheeiaxZm02YHNmZOJ4NzafpxXwaZyHWfZuGEfO5Htf5wXMe90Xjb9yWKYB+WGcj5WY+d9XXcT92U/1w3TV9z6JZ+8ngleKgJmt8O7ajh3VbjjWE955PPdTnQRaNx4Taz4IDGD0Pc4j+2VadtX455pPda9wefYzmv/cDgheIosPbaV6PHdjl3NdLnWPb1tSDaHquN9JuupaxTJm/3vPZ8Lhfu6Xsv+4r2/pQj0zo/CmlhXj2BbgffOR9O6nx7svS+q8b7ryAZvMeBAYCsAoBFSB7925zxPsXM+vcV4D2QenVBD8zb1wwekegJRcEz3wZ/LuJcSGILIZXQBKJgHUKllZAA/IwtuBdj5F0Xm7C+5dr5cNxjwtBIDUhzAAvQLwb8mGiNgUQ+Bv8r6jTXhQ+RVCA40KOLxGAwjD4d3nqw4hCDpH6PIcPIxtc+EUzKGGBwABtAAupY6B1jCESPPn3PRWU07OLMLwkxUsyAMFsDAAA8lQCAYY2J0AKGGJ4HiHDCXUSImBNi4E/ykX/bAnYgA
    // RELEASE: https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoBKArgHbBgC2AprgMID25ADpJQE4DKbAbmAMaUBnANypU8TDVQBvVJjnYEANmwAWTO0IDGlYgBMAFK0oAzdcACGwSgFlzvABZhilTAItXbDp5QA0mAJLs7jZ2js7+xMZ0mE5RfgAilLx0xG6shLzAdKyYvOYQEABGdgDWAJSy8jIo8rW5+UWluAAS5noQlABiYJQQBgBE5qwA5v1+bpYhXs64Q8NlIigAxGCm8QCiAEIAqgDilXX1BcW8JS1tuh3dvQMAPBB0eRAAfACsAPrvCGOuwZ5h1E+DyeCwOdSejVO53aXR6fX0/VuRgEhAgwDenzgPwmHlC3lwn2RqOAoJqhyOkLOrRh13hiOeAk+AGZsX88TNPgJSUtegJKGDahCTlSLlc4XdgfkMe8seM2dNAe9JRBuTpdKsBfJNXIhU1qZdKEFJgjbs8EJzgqzJv98RbJqTyZhtTFTPpYnRcP4BBotGrKAZgABPbR0Yz6ACa4oAggB3cxgCyFDq4SM3WPxqysMrZ53VR2ChrC6EG9MJtgm56ET7fB35uQwADsi0dAF9nc7Vpg3ZEPV6fdo9P79EGQ2GYIpS5nbk50dmKmTDnm6xSi/qOpPy/Sq+8WbXDm2F5gDwfUDO2MY7C5AvKAREotIwYU6HQIAFvZoB7ohwAVYMuGAAJxsL+2ikieyydhsOz7GgcCoDyEB8qgaQZMA8FqhqKBGriCoPoeMBMjEpCYHaVjNlq+GEVGAiBsQvDjtYlDAPYdC6JshCQF+rDTqQzwke8hQcX0bDkXIYIEZg1iBiBLhzKJTqUZg1G0bwjHMax9CkPGzg5J8zFgMIYLiYROBYECjz5PJ8GQVsezGURwD8USaJWYpM78YZywYcYRmKamfSTuYSbUP5ugbrp7wWAgrm1BJ47hfxFhwDF8hxROcZltxM58Xp5hMos4HiFJMmYFIR6iLBSk0XRaksbomkWN4ORlYVcCYIkySpMA6SZNkeGxSZahrpQ4X6KZmDGOKABy5hUGUmAALx8cxrB0DGmDOOtU10MA/hMB0VCkP66wAB78IwZApPoe7YINmDDdhlBjQgWCTTcM1UH47lcPkhCUPNS2YCta0bZQW07XtjAHToVi6Kd52XcQ13yXFQ2irCNy3N+tK6M8z2vdNs2+Jg2PipgP0QH9APLfYq3rZtmDbbt+2UIdsPw5QF1gFdN2o/d6M4/jE2E59mBPi+5O/f9i003ToPg8zUOszDx1nZziPI/ZMBozS4pC29fQfcTDhDJLlPS4DwP02DjMQyzbOqwj3NI7zd3DYL40G7oRvjCxrCORTVMy0DtMgwzTOQ9DR1w2rXM8yjbsC3rnsi8T31S9TIdy+HdtKw7MdO/HWs6waHsvcL71E34DzEMMZtB5bofWwrkfK9HHNxy7CeqPzus3Prqd+JofsBxnwdW/LtuK1H7Oxxrrs9+7yfl17PuYBxxGBxbsthzbEf2yrBfq87muKdrvel8vBOV6LqIpHXW+ZxPOfT23s+F13xcX2K/cpzfxPGAeJYeu28s67xbgfduc8T4L3PkvX+K9B6YF0HQQgwUQFPybpPfeedD4d3nt3OBScEHX0NlXZBSQKD5AwePLBL9W753wTAwhJcf7wj/mQ0WpkAD8NDG7Zz3rnGejtj5FzPqwjG7DEH/wSJMb8FAXCP1oQIiBuCoEf1PgNRexCpGkO9uQ3YHFdB8J3s3KeDC8HQLEVoohfddEV04cTQogYrAAG0AC6JiwFmJwcIo+ndNGpUTnYgwHD9Gi3wJQcwugADyxAICBkYuQbIgZbjOKsHxJR/DwHmMge/URn8UAtiAA=
    [Fact]
    public void GetSuspender_ShouldBuildCorrectDelegate_WhenCallbackIsClassPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            59;
#else
            42;
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
    // DEBUG: https://sharplab.io/#v2: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=
    // RELEASE: https://sharplab.io/#v2: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
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsStructNotPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            64;
#else
            52;
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
    // RELEASE: https://sharplab.io/#v2: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=
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsStructPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            64;
#else
            52;
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
            64;
#else
            52;
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
    // DEBUG: https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyAMABMugHQBKArgHbBgC2ApiQMID2dADpAwE4DKvAG5gAxgwDOAbhz4i6AKzTsM1AWY4A3jgI65ANiJ6APGBoA+AmQkVGACmQGe1iMAdQCTgGZrWVccB4KEWBWHgIRAEMICAAjCJEAawBKbV0tbF1Mgj5gCOAGAFl4gAtTBgJ/PMKSsoIAXhwAYjBvABEAUQAhAFUAcVSsrKoGAHds3PyikVLh2ySlRoYIcQYBwd0YBk8IihcFhioYFrW1zMrJmuGSAH1rmIpITbC6ggBBcQBPKhEHAoZgYqsGCdB4QJ4mcwsJxVOZKTKnXSRaJxRIkAASEUOEAYADEwEsYLYAEQRHgAcyJ7i8FQm1WmZRIpLJ8yaLQIHR6/Qy6yRsXiCXRmJg2LxBOJRggrCRZnkt3QlI8WxpVSmMyYt0lSJZ3MGvJRAoxWNx+LB4qc4l2wBlt1QCup5zpapu13Nlu163CUT5qMNwuNYqJRjM4luAGY7UqHaqGbdxNrFstVjqsnr+YKjaLTYHNVFrddbVTI7To1cNVKovGDkdPAidLXPci077sTkYYGzOhY7SI94o5d1dcHe71mzbKmfULsa8RhEwPkeOKzBQ5USkilk5l0h6zsX+87cuh6gRbABNE0waez3IxbEkM8Ey9z3hJWysGIAKwYwSSrpcDhIvT/I+86wvWAC+9b1qO44GpODDAbwi7Ltc4ZrvWW7bjofb0qW1y5KGR72HoCE8BCVovm+n7fr+rh6ABQEzk+C7DoMEEbro9bYU6tz3I8vAkK2PDALY9q7jhDAsVk9bIAA7MqFzic6vFgvxDhwrobFsaybRdH0KhNEsKw4P4gTBAZhzHNgrYKWqBAgAQACS7xfCI1mOmUmhrMgBGmMABBdlU6l1sm3lvJ83y/P8gLAqC4K+RYPGxbwQUEF5BEFB8AAqHwcOUTIpWlYUuX8AJAmwNCzsMYS3ACYBSCcIUEcQhBlkiBXYM0OmcoVvn+S6zjAO1mShb1sb7BZNbJoV95gsBEQ3kwM0Xox859QeQ26KFDgkWtESoBtOhbcRK28GRCV4REoYHUQTUACwEAUrCCAwAByDAAB7CUk9QWACPCsGMwxjC9rDAA5nDYowNAMDA7TvWIHC0L4oGNUQ90CMAbklgwthOeFrlibZXFlN9dS/cU/2A6MBAg2DEMMFD+Sw/DDCI2AyPalpuCqBl2W5QQGgEFzaBFd8JXReVuRlGEgvC6o5UmUEIQ8J5qMxKwrAQAQzbwSdC7NQQnjni9ESMKT5OUwQQM06D4McJDBxM3DCNI1QKPDQR6ua9rcFufY6CEEbBIm4whbeL1ghRBQEk/QQf0A1b1O03bDvQ8zLvs27kmhV7Ws65mMBGJlBdmP7gfG6bDBhwQxfngQkcQNH5txxTCfW8n9OMzDzus677ubZ7Gt53BBdl4bFeh4q3i5/XUcx2TLeW+3tud473cs2zHMpTnQ8+xm55j0HYIh1XU/hMUpKz4388W23Scr/bDNr+nveZ/3h2D97+cHwbR8wCf1dxCAiElfJusd45U2Bg/VOTsN592zp/Ye+8xS/wnqfakEc57NwgYnKBdNH5dxfpvLO29EF7z9KPVBwdK7V0lFQMkoCb6LzvnglOT80492Ie/G6BAZ7fxQQHce1DJ7UgoEA0IfkG5gIXjg5e+CYHrwzlvQqfCR4/0EX/ABZ8Hg0EYdg1ukCbbyPYbApRJCVG734aaKhx8aHaLoQwqRTDZH32MYQzh8DSG8MsWogR5dhHoKVJ4SUeQ9HgIMbgoxbD3FwLfgg7xX9fHWI0Wg6uMBWAUAWmEmRES5HROfh4uJXjVHIOSf42xIilSbBEPQKI2Tb6GI7gQgpsTlFqx8aUwkNj/52OpM1AA/PU5hjToEmMUa/NpHsElIIoeo8pPTKltCqJleg5QnH6KXq4/JHDWnmPaYkzph9Uln16A8GAQyXGsNXjssx3Cd4HNmX4oRFTAnTw+PkAA2gAXQubkrZ1zTETL2VMkpjyynPIWa8ywDAIgwAAPJUAgB8P4dBQgfCMDEd5DALDrPCZsq5zSblApRmBIAA===
    // RELEASE: https://sharplab.io/#v2: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
    [Fact]
    public void GetResumer_ShouldBuildCorrectDelegate_WhenCallbackIsClassPassedByRef()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            71;
#else
            57;
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
