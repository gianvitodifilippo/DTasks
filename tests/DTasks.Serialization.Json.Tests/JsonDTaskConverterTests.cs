using DTasks.Hosting;
using DTasks.Inspection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DTasks.Serialization.Json;

public partial class JsonDTaskConverterTests
{
    private static readonly string _flowId = "flowId";

    private readonly JsonDTaskConverterFixture _fixture;
    private readonly IStateMachineInspector _inspector;
    private readonly IStateMachineTypeResolver _typeResolver;
    private readonly JsonSerializerOptions _options;
    private readonly IDTaskScope _scope;
    private readonly JsonDTaskConverter _sut;

    public JsonDTaskConverterTests()
    {
        _fixture = JsonDTaskConverterFixture.Create();
        _inspector = Substitute.For<IStateMachineInspector>();
        _typeResolver = Substitute.For<IStateMachineTypeResolver>();
        _options = new()
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _scope = Substitute.For<IDTaskScope>();
        _sut = new(_inspector, _typeResolver, _options);


        _typeResolver
            .GetTypeId(typeof(StateMachine1))
            .Returns(StateMachine1.TypeId);
        _typeResolver
            .GetTypeId(typeof(StateMachine2))
            .Returns(StateMachine2.TypeId);
        _typeResolver
            .GetType(StateMachine1.TypeId)
            .Returns(typeof(StateMachine1));
        _typeResolver
            .GetType(StateMachine2.TypeId)
            .Returns(typeof(StateMachine2));

        _scope
            .TryGetReferenceToken(_fixture.Services.Service1, out Arg.Any<object?>())
            .Returns(call =>
            {
                call[1] = _fixture.Tokens.Token1;
                return true;
            });
        _scope
            .TryGetReferenceToken(_fixture.Services.Service2, out Arg.Any<object?>())
            .Returns(call =>
            {
                call[1] = _fixture.Tokens.Token2;
                return true;
            });
        _scope
            .TryGetReference(_fixture.Tokens.Token1, out Arg.Any<object?>())
            .Returns(call =>
            {
                call[1] = _fixture.Services.Service1;
                return true;
            });
        _scope
            .TryGetReference(_fixture.Tokens.Token2, out Arg.Any<object?>())
            .Returns(call =>
            {
                call[1] = _fixture.Services.Service2;
                return true;
            });
    }

    [Fact]
    public void SerializationOnFirstSuspension_ShouldCorrectlySerializeState()
    {
        // Arrange
        IStateMachineInfo info = Substitute.For<IStateMachineInfo>();
        (StateMachine1 stateMachine1, StateMachine2 stateMachine2) = _fixture.StateMachines;

        DTaskSuspender<StateMachine1> suspender1 = delegate (ref StateMachine1 stateMachine, IStateMachineInfo info, ref readonly StateMachineDeconstructor deconstructor)
        {
            deconstructor.HandleField(nameof(StateMachine1.__this), stateMachine.__this);
            deconstructor.HandleField(nameof(StateMachine1.local1), stateMachine.local1);
            deconstructor.HandleField(nameof(StateMachine1.local2), stateMachine.local2);
            deconstructor.HandleField(nameof(StateMachine1.local3), stateMachine.local3);
        };

        DTaskSuspender<StateMachine2> suspender2 = delegate (ref StateMachine2 stateMachine, IStateMachineInfo info, ref readonly StateMachineDeconstructor deconstructor)
        {
            deconstructor.HandleField(nameof(StateMachine2.__this), stateMachine.__this);
            deconstructor.HandleField(nameof(StateMachine2.local1), stateMachine.local1);
            deconstructor.HandleField(nameof(StateMachine2.local2), stateMachine.local2);
            deconstructor.HandleField(nameof(StateMachine2.local3), stateMachine.local3);
            deconstructor.HandleField(nameof(StateMachine2.local4), stateMachine.local4);
        };

        _inspector
            .GetSuspender(typeof(StateMachine1))
            .Returns(suspender1);
        _inspector
            .GetSuspender(typeof(StateMachine2))
            .Returns(suspender2);

        // Act
        JsonFlowHeap heap = _sut.CreateHeap(_scope);
        ReadOnlyMemory<byte> stateMachine1Bytes = _sut.SerializeStateMachine(ref heap, ref stateMachine1, info);
        ReadOnlyMemory<byte> stateMachine2Bytes = _sut.SerializeStateMachine(ref heap, ref stateMachine2, info);

        Dictionary<string, object> idsToReferences = GetIdsToReferences(heap.ReferenceResolver);
        Dictionary<object, string> referencesToIds = GetReferencesToIds(heap.ReferenceResolver);

        ReadOnlyMemory<byte> heapBytes = _sut.SerializeHeap(ref heap);

        // Assert
        VerifyHeapState(idsToReferences, referencesToIds);

        string stateMachine1Json = Encoding.UTF8.GetString(stateMachine1Bytes.Span);
        string stateMachine2Json = Encoding.UTF8.GetString(stateMachine2Bytes.Span);
        string heapJson = Encoding.UTF8.GetString(heapBytes.Span);

        stateMachine1Json.Should().Be(_fixture.Jsons.StateMachine1Json);
        stateMachine2Json.Should().Be(_fixture.Jsons.StateMachine2Json);
        heapJson.Should().Be(_fixture.Jsons.HeapJson);
    }

    [Fact]
    public void Deserialization_ShouldCorrectlyDeserializeState()
    {
        // Arrange
        byte[] stateMachine1Bytes = Encoding.UTF8.GetBytes(_fixture.Jsons.StateMachine1Json);
        byte[] stateMachine2Bytes = Encoding.UTF8.GetBytes(_fixture.Jsons.StateMachine2Json);
        byte[] heapBytes = Encoding.UTF8.GetBytes(_fixture.Jsons.HeapJson);

        StateMachine1 stateMachine1 = new();
        StateMachine2 stateMachine2 = new();

        DTaskResumer resumer1 = delegate (DTask resultTask, ref StateMachineConstructor constructor)
        {
            constructor.HandleField(nameof(StateMachine1.__this), ref stateMachine1.__this);
            constructor.HandleField(nameof(StateMachine1.local1), ref stateMachine1.local1);
            constructor.HandleField(nameof(StateMachine1.local2), ref stateMachine1.local2);
            constructor.HandleField(nameof(StateMachine1.local3), ref stateMachine1.local3);
            return resultTask;
        };

        DTaskResumer resumer2 = delegate (DTask resultTask, ref StateMachineConstructor constructor)
        {
            constructor.HandleField(nameof(StateMachine2.__this), ref stateMachine2.__this);
            constructor.HandleField(nameof(StateMachine2.local1), ref stateMachine2.local1);
            constructor.HandleField(nameof(StateMachine2.local2), ref stateMachine2.local2);
            constructor.HandleField(nameof(StateMachine2.local3), ref stateMachine2.local3);
            constructor.HandleField(nameof(StateMachine2.local4), ref stateMachine2.local4);
            return resultTask;
        };

        _inspector
            .GetResumer(typeof(StateMachine1))
            .Returns(resumer1);
        _inspector
            .GetResumer(typeof(StateMachine2))
            .Returns(resumer2);

        // Act
        JsonFlowHeap heap = _sut.DeserializeHeap(_flowId, _scope, heapBytes);

        Dictionary<string, object> idsToReferences = GetIdsToReferences(heap.ReferenceResolver);
        Dictionary<object, string> referencesToIds = GetReferencesToIds(heap.ReferenceResolver);

        _ = _sut.DeserializeStateMachine(_flowId, ref heap, stateMachine2Bytes, Substitute.For<DTask>());
        _ = _sut.DeserializeStateMachine(_flowId, ref heap, stateMachine1Bytes, Substitute.For<DTask>());

        // Assert
        VerifyHeapState(idsToReferences, referencesToIds);

        stateMachine1.__this.Should().Be(_fixture.Services.Service1);
        stateMachine1.local1.Should().BeEquivalentTo(_fixture.References.Reference1);
        stateMachine1.local2.Should().Be(_fixture.Values.String1);
        stateMachine1.local3.Should().BeNull();
        stateMachine2.__this.Should().Be(_fixture.Services.Service2);
        stateMachine2.local1.Should().BeEquivalentTo(_fixture.References.Reference1);
        stateMachine2.local2.Should().BeEquivalentTo(_fixture.References.Reference2);
        stateMachine2.local3.Should().BeEquivalentTo(_fixture.References.Reference3);
        stateMachine2.local4.Should().Be(_fixture.Values.Int1);

        stateMachine1.__this!.Service2.Should().BeSameAs(stateMachine2.__this);
        stateMachine1.local1.Should().BeSameAs(stateMachine2.local1);
        stateMachine2.local1.Should().BeSameAs(stateMachine2.local2!.Reference);
        stateMachine2.local2.PolymorphicReference.Should().BeSameAs(stateMachine2.local3!.PolymorphicReference);
    }

    private void VerifyHeapState(Dictionary<string, object> idsToReferences, Dictionary<object, string> referencesToIds)
    {
        idsToReferences.Should().ContainKey("0").WhoseValue.Should().Be(_fixture.Services.Service1);
        idsToReferences.Should().ContainKey("1").WhoseValue.Should().BeOfType<SerializableType1>();
        idsToReferences.Should().ContainKey("2").WhoseValue.Should().Be(_fixture.Services.Service2);
        idsToReferences.Should().ContainKey("3").WhoseValue.Should().BeOfType<SerializableType2>();
        idsToReferences.Should().ContainKey("4").WhoseValue.Should().BeOfType<PolymorphicType2>();
        idsToReferences.Should().ContainKey("5").WhoseValue.Should().BeOfType<SerializableType2>();

        SerializableType1 reference1 = (SerializableType1)idsToReferences["1"];
        SerializableType2 reference2 = (SerializableType2)idsToReferences["3"];
        PolymorphicType2 polymorphicReference = (PolymorphicType2)idsToReferences["4"];
        SerializableType2 reference3 = (SerializableType2)idsToReferences["5"];

        referencesToIds[_fixture.Services.Service1].Should().Be("0");
        referencesToIds[reference1].Should().Be("1");
        referencesToIds[_fixture.Services.Service2].Should().Be("2");
        referencesToIds[reference2].Should().Be("3");
        referencesToIds[polymorphicReference].Should().Be("4");
        referencesToIds[reference3].Should().Be("5");

        reference1.Should().BeSameAs(reference2.Reference);
        polymorphicReference.Should().BeSameAs(reference2.PolymorphicReference);
        reference2.PolymorphicReference.Should().BeSameAs(reference3.PolymorphicReference);
    }

    [Fact]
    public void CreateInspector_ShouldCreateWorkingInspector()
    {
        // Act
        StateMachineInspector inspector = JsonDTaskConverter.CreateInspector();
        Delegate suspender = inspector.GetSuspender(_stateMachineType);
        Delegate resumer = inspector.GetResumer(_stateMachineType);

        // Assert
        suspender.Should().BeOfType(typeof(DTaskSuspender<>).MakeGenericType(_stateMachineType));
        resumer.Should().BeOfType<DTaskResumer>();
    }
}
