using DTasks.Infrastructure;
using DTasks.Utils;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

public partial class JsonStateMachineSerializerTests
{
    private static readonly Type s_stateMachineType;

    static JsonStateMachineSerializerTests()
    {
        MethodInfo method = typeof(AsyncMethodContainer).GetRequiredMethod(
            name: nameof(AsyncMethodContainer.Method),
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Static | BindingFlags.Public,
            parameterTypes: [typeof(string)]);

        StateMachineAttribute? attribute = method.GetCustomAttribute<StateMachineAttribute>();
        Debug.Assert(attribute is not null);

        s_stateMachineType = attribute.StateMachineType;
    }

    public static class AsyncMethodContainer
    {
        public static async DTask<int> Method(string arg)
        {
            await DTask.Yield();
            return arg.Length;
        }
    }

    public class SerializableType1
    {
        public string? Value1 { get; set; }

        public int Value2 { get; set; }
    }

    public class SerializableType2
    {
        public SerializableType1? Reference { get; set; }

        public string? Value1 { get; set; }

        public IPolymorphicType? PolymorphicReference { get; set; }
    }

    [JsonDerivedType(typeof(PolymorphicType1), typeDiscriminator: PolymorphicType1.TypeDiscriminator)]
    [JsonDerivedType(typeof(PolymorphicType2), typeDiscriminator: PolymorphicType2.TypeDiscriminator)]
    public interface IPolymorphicType;

    public class PolymorphicType1 : IPolymorphicType
    {
        public const string TypeDiscriminator = nameof(PolymorphicType1);

        public string? Value { get; set; }
    }

    public class PolymorphicType2 : IPolymorphicType
    {
        public const string TypeDiscriminator = nameof(PolymorphicType2);

        public int Value { get; set; }
    }

    public class Service1
    {
        public Service2? Service2;
    }

    public class Service2;

    public struct StateMachine1
    {
        public static readonly TypeId TypeId = new(nameof(StateMachine1));

        public Service1? __this;
        public SerializableType1? local1;
        public string? local2;
        public string? local3;
    }

    public struct StateMachine2
    {
        public static readonly TypeId TypeId = new(nameof(StateMachine2));

        public Service2? __this;
        public SerializableType1? local1;
        public SerializableType2? local2;
        public SerializableType2? local3;
        public int local4;
    }

    internal class StateMachine1Suspender : IStateMachineSuspender<StateMachine1>
    {
        public void Suspend(ref StateMachine1 stateMachine, ISuspensionContext suspensionContext, ref readonly JsonStateMachineWriter writer)
        {
            writer.WriteField(nameof(StateMachine1.__this), stateMachine.__this);
            writer.WriteField(nameof(StateMachine1.local1), stateMachine.local1);
            writer.WriteField(nameof(StateMachine1.local2), stateMachine.local2);
            writer.WriteField(nameof(StateMachine1.local3), stateMachine.local3);
        }
    }

    internal class StateMachine1Resumer : IStateMachineResumer
    {
        public StateMachine1 StateMachine;

        public IDAsyncRunnable Resume(ref JsonStateMachineReader reader)
        {
            reader.ReadField(nameof(StateMachine1.__this), ref StateMachine.__this);
            reader.ReadField(nameof(StateMachine1.local1), ref StateMachine.local1);
            reader.ReadField(nameof(StateMachine1.local2), ref StateMachine.local2);
            reader.ReadField(nameof(StateMachine1.local3), ref StateMachine.local3);
            return Substitute.For<IDAsyncRunnable>();
        }

        public IDAsyncRunnable Resume<TResult>(ref JsonStateMachineReader reader, TResult result)
        {
            reader.ReadField(nameof(StateMachine1.__this), ref StateMachine.__this);
            reader.ReadField(nameof(StateMachine1.local1), ref StateMachine.local1);
            reader.ReadField(nameof(StateMachine1.local2), ref StateMachine.local2);
            reader.ReadField(nameof(StateMachine1.local3), ref StateMachine.local3);
            return Substitute.For<IDAsyncRunnable>();
        }
    }

    internal class StateMachine2Suspender : IStateMachineSuspender<StateMachine2>
    {
        public void Suspend(ref StateMachine2 stateMachine, ISuspensionContext suspensionContext, ref readonly JsonStateMachineWriter writer)
        {
            writer.WriteField(nameof(StateMachine2.__this), stateMachine.__this);
            writer.WriteField(nameof(StateMachine2.local1), stateMachine.local1);
            writer.WriteField(nameof(StateMachine2.local2), stateMachine.local2);
            writer.WriteField(nameof(StateMachine2.local3), stateMachine.local3);
            writer.WriteField(nameof(StateMachine2.local4), stateMachine.local4);
        }
    }

    internal class StateMachine2Resumer : IStateMachineResumer
    {
        public StateMachine2 StateMachine;

        public IDAsyncRunnable Resume(ref JsonStateMachineReader reader)
        {
            reader.ReadField(nameof(StateMachine2.__this), ref StateMachine.__this);
            reader.ReadField(nameof(StateMachine2.local1), ref StateMachine.local1);
            reader.ReadField(nameof(StateMachine2.local2), ref StateMachine.local2);
            reader.ReadField(nameof(StateMachine2.local3), ref StateMachine.local3);
            reader.ReadField(nameof(StateMachine2.local4), ref StateMachine.local4);
            return Substitute.For<IDAsyncRunnable>();
        }

        public IDAsyncRunnable Resume<TResult>(ref JsonStateMachineReader reader, TResult result)
        {
            reader.ReadField(nameof(StateMachine2.__this), ref StateMachine.__this);
            reader.ReadField(nameof(StateMachine2.local1), ref StateMachine.local1);
            reader.ReadField(nameof(StateMachine2.local2), ref StateMachine.local2);
            reader.ReadField(nameof(StateMachine2.local3), ref StateMachine.local3);
            reader.ReadField(nameof(StateMachine2.local4), ref StateMachine.local4);
            return Substitute.For<IDAsyncRunnable>();
        }
    }

    internal sealed record JsonDTaskConverterFixture(
        Values Values,
        References References,
        Services Services,
        ParentIds ParentIds,
        StateMachines StateMachines,
        Jsons Jsons)
    {
        public static JsonDTaskConverterFixture Create()
        {
            string string1 = nameof(string1);
            string string2 = nameof(string2);
            int int1 = 42;
            int int2 = 420;

            var polymorphicReference = new PolymorphicType2()
            {
                Value = int2
            };
            var reference1 = new SerializableType1()
            {
                Value2 = int1,
                Value1 = string1
            };
            var reference2 = new SerializableType2()
            {
                Reference = reference1,
                Value1 = string2,
                PolymorphicReference = polymorphicReference
            };
            var reference3 = new SerializableType2()
            {
                PolymorphicReference = polymorphicReference
            };

            var service2 = new Service2();
            var service1 = new Service1()
            {
                Service2 = service2
            };

            DAsyncId parentId1 = DAsyncId.New();
            DAsyncId parentId2 = DAsyncId.New();

            object surrogate1 = nameof(Service1);
            object surrogate2 = nameof(Service2);

            StateMachine1 stateMachine1 = new()
            {
                __this = service1,
                local1 = reference1,
                local2 = string1,
                local3 = null
            };
            StateMachine2 stateMachine2 = new()
            {
                __this = service2,
                local1 = reference1,
                local2 = reference2,
                local3 = reference3,
                local4 = int1
            };

            string stateMachine1Json = $$"""
                {
                  "$typeId": "{{StateMachine1.TypeId}}",
                  "$parentId": "{{parentId1}}",
                  "__this": {
                    "@dtasks.tid": null,
                    "surrogate": "{{surrogate1}}"
                  },
                  "local1": {
                    "$id": "1",
                    "Value1": "{{string1}}",
                    "Value2": {{int1}}
                  },
                  "local2": "{{string1}}"
                }
                """;

            string stateMachine2Json = $$"""
                {
                  "$typeId": "{{StateMachine2.TypeId}}",
                  "$parentId": "{{parentId2}}",
                  "__this": {
                    "@dtasks.tid": null,
                    "surrogate": "{{surrogate2}}"
                  },
                  "local1": {
                    "$id": "1",
                    "Value1": "{{string1}}",
                    "Value2": {{int1}}
                  },
                  "local2": {
                    "$id": "2",
                    "Reference": {
                      "$ref": "1"
                    },
                    "Value1": "{{string2}}",
                    "PolymorphicReference": {
                      "$id": "3",
                      "$type": "{{PolymorphicType2.TypeDiscriminator}}",
                      "Value": {{int2}}
                    }
                  },
                  "local3": {
                    "$id": "4",
                    "PolymorphicReference": {
                      "$ref": "3"
                    }
                  },
                  "local4": {{int1}}
                }
                """;

            return new(
                new Values(string1, string2, int1, int2),
                new References(polymorphicReference, reference1, reference2, reference3),
                new Services(service1, service2),
                new ParentIds(parentId1, parentId2),
                new StateMachines(stateMachine1, stateMachine2),
                new Jsons(stateMachine1Json, stateMachine2Json));
        }
    }

    internal sealed record StateMachines(
        StateMachine1 StateMachine1,
        StateMachine2 StateMachine2);

    internal sealed record References(
        IPolymorphicType PolymorphicReference,
        SerializableType1 Reference1,
        SerializableType2 Reference2,
        SerializableType2 Reference3);

    internal sealed record Services(
        Service1 Service1,
        Service2 Service2);

    internal sealed record ParentIds(
        DAsyncId ParentId1,
        DAsyncId ParentId2);

    internal sealed record Values(
        string String1,
        string String2,
        int Int1,
        int Int2);

    internal sealed record Jsons(
        string StateMachine1Json,
        string StateMachine2Json);

    private class MockDAsyncSurrogator(JsonDTaskConverterFixture fixture) : IDAsyncSurrogator, ISurrogateConverter
    {
        public bool TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
            where TAction : struct, ISurrogationAction
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            if (ReferenceEquals(value, fixture.Services.Service1))
            {
                action.SurrogateAs(default, nameof(Service1));
                return true;
            }

            if (ReferenceEquals(value, fixture.Services.Service2))
            {
                action.SurrogateAs(default, nameof(Service2));
                return true;
            }

            return false;
        }

        public bool TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
            where TAction : struct, IRestorationAction
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            if (typeof(T) == typeof(Service1))
            {
                action.RestoreAs(typeof(string), this);
                return true;
            }

            if (typeof(T) == typeof(Service2))
            {
                action.RestoreAs(typeof(string), this);
                return true;
            }

            return false;
        }

        public T Convert<TSurrogate, T>(TSurrogate surrogate)
        {
            return surrogate switch
            {
                nameof(Service1) => (T)(object)fixture.Services.Service1,
                nameof(Service2) => (T)(object)fixture.Services.Service2,
                _ => throw new ArgumentException("Invalid surrogate", nameof(surrogate))
            };
        }
    }
}
