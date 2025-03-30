using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncStateMachine
{
    void Start(IDAsyncMethodBuilder builder);

    void MoveNext();

    void Suspend();
}
