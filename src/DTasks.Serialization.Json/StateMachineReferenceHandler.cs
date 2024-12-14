using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

internal sealed class StateMachineReferenceHandler(ReferenceResolver resolver) : ReferenceHandler
{
    public override ReferenceResolver CreateResolver() => resolver;
}
