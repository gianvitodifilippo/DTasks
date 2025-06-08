using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure.Fakes;

internal sealed record FakeSurrogatedArray(TypeId TypeId, object?[] Items);
