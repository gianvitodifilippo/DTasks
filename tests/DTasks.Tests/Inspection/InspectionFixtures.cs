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
        private int _field = 0;

        // Generated state machine reference: https://sharplab.io/#v2:D4AQTAjAsAUCAMACEECsBuWtyIIIGcBPAOwGMBZAUwBcALAewBMBhe46gQwEtjKAnWAG9YiRAAc+XAG4dqlRD2qIA+gDMulADaNEAXkTxMMEcgDMyABzIAbAB5FAPkRU6TABTlCAFUJj5HPgBzAEoTYRhRURAAThsAOgBNDW03YKNI5AgkTXpSDk09RADAuK96AGVqSWJA1IBCdMiY+IARLQ5CNyz4eDSTUUVEPkp8AFdNJX1m5loubRcGRlSjfuQAdiGR8aUAahV1LR09nLzNOIAZShq6RoBfLAjxSRk5G3t2Jxm5xgX3YL0nHQ+PQAO6IXhggBy9GoAEkALZiTSUeFXOSMACiAA9SJQxNQuGxlrB7sY4GBnN5fJQjEA===
        public async DTask<int> Method(MyType arg)
        {
            await DTask.Yield();
            string local = arg.ToString()!;
            await Task.Delay(10000);
            int result = await ChildMethod();

            return result + _field + local.Length;
        }

        private DTask<int> ChildMethod() => throw new NotImplementedException();
    }

    public class MyType;
}
