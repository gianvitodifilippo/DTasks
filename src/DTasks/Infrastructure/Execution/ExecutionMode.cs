using System.ComponentModel;

namespace DTasks.Infrastructure.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class ExecutionMode
{
    public static readonly ExecutionMode Replay = new ReplayMode();

    public static readonly ExecutionMode Snapshot = new SnapshotMode();

    private sealed class ReplayMode : ExecutionMode
    {
        
    }

    private sealed class SnapshotMode : ExecutionMode
    {
        
    }
}
