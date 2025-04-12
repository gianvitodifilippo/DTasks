using DTasks.Infrastructure;
using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

// TODO: We should rewrite all of those tests since they are only adapted from a previous version
public partial class JsonDAsyncSerializerTests
{
    private readonly JsonDTaskConverterFixture _fixture;
    private readonly IStateMachineInspector _inspector;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly IDAsyncMarshaler _marshaler;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly JsonDAsyncSerializer _sut;

    public JsonDAsyncSerializerTests()
    {
        _fixture = JsonDTaskConverterFixture.Create();
        _inspector = Substitute.For<IStateMachineInspector>();
        _typeResolver = Substitute.For<IDAsyncTypeResolver>();
        _marshaler = new MockDAsyncMarshaler(_fixture);
        _jsonOptions = new()
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _sut = new(_inspector, _typeResolver, _jsonOptions);

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
    }

    [Fact]
    public void SerializationOnFirstSuspension_ShouldCorrectlySerializeState()
    {
        // Arrange
        ISuspensionContext context = Substitute.For<ISuspensionContext>();
        (StateMachine1 stateMachine1, StateMachine2 stateMachine2) = _fixture.StateMachines;
        StateMachine1Suspender suspender1 = new();
        StateMachine2Suspender suspender2 = new();
        ArrayBufferWriter<byte> buffer1 = new();
        ArrayBufferWriter<byte> buffer2 = new();
        DAsyncId parentId1 = _fixture.ParentIds.ParentId1;
        DAsyncId parentId2 = _fixture.ParentIds.ParentId2;

        _inspector
            .GetSuspender(typeof(StateMachine1))
            .Returns(suspender1);
        _inspector
            .GetSuspender(typeof(StateMachine2))
            .Returns(suspender2);

        context.Marshaler.Returns(_marshaler);

        // Act
        _sut.SerializeStateMachine(buffer1, context, parentId1, ref stateMachine1);
        _sut.SerializeStateMachine(buffer2, context, parentId2, ref stateMachine2);

        // Assert
        string stateMachine1Json = Encoding.UTF8.GetString(buffer1.WrittenSpan);
        string stateMachine2Json = Encoding.UTF8.GetString(buffer2.WrittenSpan);

        stateMachine1Json.Should().Be(_fixture.Jsons.StateMachine1Json);
        stateMachine2Json.Should().Be(_fixture.Jsons.StateMachine2Json);
    }

    [Fact]
    public void Deserialization_ShouldCorrectlyDeserializeState()
    {
        // Arrange
        IResumptionContext context = Substitute.For<IResumptionContext>();
        byte[] stateMachine1Bytes = Encoding.UTF8.GetBytes(_fixture.Jsons.StateMachine1Json);
        byte[] stateMachine2Bytes = Encoding.UTF8.GetBytes(_fixture.Jsons.StateMachine2Json);
        StateMachine1Resumer resumer1 = new();
        StateMachine2Resumer resumer2 = new();

        ref StateMachine1 stateMachine1 = ref resumer1.StateMachine;
        ref StateMachine2 stateMachine2 = ref resumer2.StateMachine;

        context.Marshaler.Returns(_marshaler);

        _inspector
            .GetResumer(typeof(StateMachine1))
            .Returns(resumer1);
        _inspector
            .GetResumer(typeof(StateMachine2))
            .Returns(resumer2);

        // Act
        _ = _sut.DeserializeStateMachine(context, stateMachine1Bytes);
        _ = _sut.DeserializeStateMachine(context, stateMachine2Bytes, new object());

        // Assert
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
        stateMachine2.local1.Should().BeSameAs(stateMachine2.local2!.Reference);
        stateMachine2.local2.PolymorphicReference.Should().BeSameAs(stateMachine2.local3!.PolymorphicReference);
    }

    [Fact]
    public void CreateInspector_ShouldCreateWorkingInspector()
    {
        // Act
        DynamicStateMachineInspector inspector = DynamicStateMachineInspector.Create(typeof(IStateMachineSuspender<>), typeof(IStateMachineResumer), _typeResolver);
        object suspender = inspector.GetSuspender(s_stateMachineType);
        object resumer = inspector.GetResumer(s_stateMachineType);

        // Assert
        suspender.Should().BeAssignableTo(typeof(IStateMachineSuspender<>).MakeGenericType(s_stateMachineType));
        resumer.Should().BeAssignableTo<IStateMachineResumer>();
    }
}
