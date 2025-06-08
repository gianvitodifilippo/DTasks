using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Marshaling;
using DTasks.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IMarshalingFeature
{
    [MemberNotNullWhen(true, nameof(_marshalingId))]
    private bool IsMarshaling => _marshalingId.HasValue;
    
    void IMarshalingFeature.Marshal(DTask task)
    {
        if (task.Status is not DTaskStatus.Pending)
            throw new ArgumentException("Only pending DTask objects can be explicitly marshaled.", nameof(task));
        
        Dictionary<DTask, DAsyncId> handleIds = HandleIds;
        if (handleIds.ContainsKey(task))
            return;

        DAsyncId id = _idFactory.NewId();
        handleIds.Add(task, id);

        Assign(ref _marshalingId, id);
        ((IDAsyncRunnable)task).Run(this);
    }

    private void EnsureNotMarshaling()
    {
        if (IsMarshaling)
            throw new MarshalingException("Only DTask objects representing d-async methods can be explicitly marshaled.");
    }
}