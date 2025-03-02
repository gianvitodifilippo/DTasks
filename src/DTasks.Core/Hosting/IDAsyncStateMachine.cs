using System.ComponentModel;

namespace DTasks.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncStateMachine
{
    void Start(IDAsyncMethodBuilder builder);

    void MoveNext();

    void Suspend();
}
