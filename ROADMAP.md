# DTasks roadmap

## Current status (pre-alpha)

The core functionality has been implemented and demonstrated through samples.
Some key features are still missing, and important design decisions have yet to be finalized.

### DTasks.Core

This library defines the core types and APIs and is agnostic of the persistence model being used.

**API**

✅ Core types (`DTask`, `DTask<TResult>`) and APIs (`DTask.Delay`, `DTask.WhenAll`, `DTask.Run`, etc.) defined and mostly stable.

❓ Infrastructure types (`IDAsyncFlow`, `IDAsyncRunnable`, etc.) defined, but design must be confirmed.

❌ Distributed equivalent of `CancellationToken` not defined.

**Implementation**

✅ Integration with async/await.

❓ Exception/cancellation handling to be validated.

### DTasks

This package implements a snapshot-based persistence model, where state machines are dehydrated and hydrated at yield points.

**API**

✅ Support for executing callbacks on suspension defined (`DTask.Factory.Callback`) but location (`DTasks` or `DTasks.Core`) must be confirmed.

✅ State machine inspection model defined and mostly stable.

❓ Support for type identifiers defined but no support for generics.

❓ Infrastructure types (`DAsyncHost`, `DAsyncId`, etc.) defined, but design must be confirmed.

❌ A way of persisting data outside of the state machine fields not defined.

❌ Distributed locking not yet defined.

**Implementation**

✅ Support for `DTask.Yield`, `DTask.Delay`, `DTask.Factory.Callback` and completed DTasks.

❓ Limited support for `DTask.WhenAll` and `DTask.WhenAny`.

❌ No support for `DTask.Run`.

❌ No support for exceptions and cancellation.

❌ Limited marshaling support: failures when marshaling/unmarshaling arrays of `DTask`.

### DTasks.Inspection.Dynamic

This package allows accessing the fields of state machines (inspection) through generation of dynamic code.

**API**

✅ `DynamicStateMachineInspector` defined and mostly stable.

**Implementation**

✅ Basic inspection support.

❌ Does not recognize state machine fields that are no longer or not yet needed.

### DTasks.Serialization

Implements the hydration/dehydration pattern using binary serialization.

**API**

❓ Support for multiple serialization formats (`IDAsyncSerializer`) and storage (`IDAsyncStorage`) defined but design must be confirmed.

**Implementation**

✅ Basic hydration/dehydration support.

❌ Cleanup of stale data not implemented.

### DTasks.Serialization.Json

Implements serialization with JSON format.

**API**

✅ Basic support for JSON serialization.

**Implementation**

❓ Marshaling strategy to be revised.

### DTasks.Serialization.StackExchangeRedis

Adds support for using Redis as storage for serialized state.

**API**

No contracts defined.

**Implementation**

✅ Simple support for Redis storage.

### DTasks.Extensions.DependencyInjection

Adds support for marshaling services registered in a DI container (IServiceCollection).

**API**

✅ Integration pattern (`AddDTasks`) defined and mostly stable.

✅ Validation pattern (`IsDAsyncService`, `GetDAsyncService`) defined.

**Implementation**

✅ Marshaling of services implemented.

❓ Limited configuration inference.

❌ No open generics support.

### DTasks.Extensions.Hosting

Integrates DTasks with Microsoft.Extensions.Hosting.

**API**

✅ Integration pattern (`UseDTasks`) defined and mostly stable.

**Implementation**

✅ Basic implementation completed.

### DTasks.AspNetCore

Provides integration of DTasks with ASP.NET Core, enabling workflow orchestration and async endpoints within web applications.

**API**

✅ Integration with the `IResult` type.

❓ Infrastructure types (`AspNetCoreDAsyncHost`, `IDAsyncCallback`) defined but in need of revision.

❌ Code generation patterns not defined.

❌ Generated endpoint routing convention not defined.

❌ Integration with existing WebSockets (non-SignalR) code not defined.

**Implementation**

❓ Sketched `DAsyncHost` implementation, needs to be revised.

❓ Very basic support for `DTask.Delay` and `DTask.Yield` implementation, supports only Redis.

❌ WebSockets backplane not supported.

❌ Missing crucial code generation support.

❌ Missing integration with SignalR.


## Next steps (alpha)

- Definition of endpoint routing conventions.
- Implementation of ASP.NET Core source generators.
- Exception and cancellation handling.
- Marshaling revision.
- Integration with Azure Functions.
- Support for `DTask.Run`.
- Generic types and method support.

## Future plans (beta & first release)

- State versioning investigation.
- Definition of distributed locking API.
- Revision of infrastructure (hosting) API.
- Efficient state machine inspection strategy.
- Integration with SignalR.
- Performance enhancement.
- Clear definition of constraints (locals must be marshalable, etc.)
