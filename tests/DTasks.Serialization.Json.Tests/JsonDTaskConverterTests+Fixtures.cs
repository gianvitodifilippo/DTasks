using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

public partial class JsonDTaskConverterTests
{
    private static Dictionary<string, object> GetIdsToReferences(DTaskReferenceResolver resolver)
    {
        return new(Impl(resolver));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_idsToReferences")]
        static extern ref Dictionary<string, object> Impl(DTaskReferenceResolver resolver);
    }

    private static Dictionary<object, string> GetReferencesToIds(DTaskReferenceResolver resolver)
    {
        return new(Impl(resolver));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_referencesToIds")]
        static extern ref Dictionary<object, string> Impl(DTaskReferenceResolver resolver);
    }

    private static readonly Type s_stateMachineType;

    static JsonDTaskConverterTests()
    {
        MethodInfo method = typeof(AsyncMethodContainer).GetRequiredMethod(
            name: nameof(AsyncMethodContainer.Method),
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
        public const string TypeId = "StateMachine1";

        public Service1? __this;
        public SerializableType1? local1;
        public string? local2;
        public string? local3;
    }

    public struct StateMachine2
    {
        public const string TypeId = "StateMachine2";

        public Service2? __this;
        public SerializableType1? local1;
        public SerializableType2? local2;
        public SerializableType2? local3;
        public int local4;
    }

    internal sealed record JsonDTaskConverterFixture(
        Values Values,
        References References,
        Services Services,
        Tokens Tokens,
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

            object token1 = nameof(Service1);
            object token2 = nameof(Service2);

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
                  "$type": "{{StateMachine1.TypeId}}",
                  "__this": {
                    "$ref": "0"
                  },
                  "local1": {
                    "$ref": "1"
                  },
                  "local2": "{{string1}}"
                }
                """;

            string stateMachine2Json = $$"""
                {
                  "$type": "{{StateMachine2.TypeId}}",
                  "__this": {
                    "$ref": "2"
                  },
                  "local1": {
                    "$ref": "1"
                  },
                  "local2": {
                    "$ref": "3"
                  },
                  "local3": {
                    "$ref": "5"
                  },
                  "local4": {{int1}}
                }
                """;

            string heapJson = $$"""
                {
                  "s_count": 2,
                  "heap": [
                    {
                      "id": "0",
                      "type": "{{typeof(string).AssemblyQualifiedName}}",
                      "value": "{{token1}}"
                    },
                    {
                      "type": "{{typeof(SerializableType1).AssemblyQualifiedName}}",
                      "value": {
                        "$id": "1",
                        "Value1": "{{string1}}",
                        "Value2": {{int1}}
                      }
                    },
                    {
                      "id": "2",
                      "type": "{{typeof(string).AssemblyQualifiedName}}",
                      "value": "{{token2}}"
                    },
                    {
                      "type": "{{typeof(SerializableType2).AssemblyQualifiedName}}",
                      "value": {
                        "$id": "3",
                        "Reference": {
                          "$ref": "1"
                        },
                        "Value1": "{{string2}}",
                        "PolymorphicReference": {
                          "$id": "4",
                          "$type": "{{PolymorphicType2.TypeDiscriminator}}",
                          "Value": {{int2}}
                        }
                      }
                    },
                    {
                      "type": "{{typeof(SerializableType2).AssemblyQualifiedName}}",
                      "value": {
                        "$id": "5",
                        "PolymorphicReference": {
                          "$ref": "4"
                        }
                      }
                    }
                  ]
                }
                """;

            return new(
                new Values(string1, string2, int1, int2),
                new References(polymorphicReference, reference1, reference2, reference3),
                new Services(service1, service2),
                new Tokens(token1, token2),
                new StateMachines(stateMachine1, stateMachine2),
                new Jsons(stateMachine1Json, stateMachine2Json, heapJson));
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

    internal sealed record Tokens(
        object Token1,
        object Token2);

    internal sealed record Values(
        string String1,
        string String2,
        int Int1,
        int Int2);

    internal sealed record Jsons(
        string StateMachine1Json,
        string StateMachine2Json,
        string HeapJson);
}
