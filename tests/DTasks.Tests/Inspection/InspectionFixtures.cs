using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTasks.Inspection;

public static class InspectionFixtures
{
    public const string LocalFieldName =
#if DEBUG
        "<local>5__1";
#else
        "<local>5__2";
#endif

    public static readonly Type StateMachineType;

#if DEBUG
    public static readonly ConstructorInfo StateMachineConstructor;
#endif

    static InspectionFixtures()
    {
        MethodInfo method = typeof(AsyncMethodContainer).GetRequiredMethod(
            name: nameof(AsyncMethodContainer.Method),
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(MyType)]);

        StateMachineAttribute? attribute = method.GetCustomAttribute<StateMachineAttribute>();
        Debug.Assert(attribute is not null);

        StateMachineType = attribute.StateMachineType;

#if DEBUG
        StateMachineConstructor = StateMachineType.GetRequiredConstructor(
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: []);
#endif
    }

    public class AsyncMethodContainer
    {
        private int _field;

        // Generated state machine reference
        // https://sharplab.io/#v2:D4AQTAjAsAUCAMACEECsBuWtyIIIGcBPAOwGMBZAUwBcALAewBMBhe46gQwEtjKAnWAG9YiRD2qIA+gDMulADaNMMEcgAcyAGwAecQD5EVOkwAU5QgBVCAB0qIOfAOYBKVcJijRIAJxaAdACacoomzsqeyBBI8vSkHPKIALz2Tn4W9ADK1Hw8jqEAhOGePv4AIgochCZR8PBhqqLiiHyU+ACu8hLJJcy0XIpGDIyhyg3IAOzNrR0SANRSsgqMiPMxcfJ+ADKUxI50RQC+WB5auuwGvf2Mg6bOSQZ0fPQA7oi8rwBy9NQAkgC21nklD+O2olEYAFEAB6kSjWahcNgjWBHFRwMCGSw2OyCRCooA===
        public async DTask<int> Method(MyType arg)
        {
            await DTask.Yield();
            string local = arg.ToString()!;
            await Task.Delay(10000);
            int result = await ChildMethod();

            return result + _field + local.Length;
        }

        public DTask<int> ChildMethod() => throw new NotImplementedException();
    }

    public class MyType;
}
