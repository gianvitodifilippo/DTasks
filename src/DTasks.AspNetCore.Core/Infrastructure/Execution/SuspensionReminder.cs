using System.ComponentModel;

namespace DTasks.AspNetCore.Infrastructure.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct SuspensionReminder(DAsyncId Id, DateTimeOffset DueDateTime);
