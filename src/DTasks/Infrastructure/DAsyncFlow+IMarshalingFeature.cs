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
        Dictionary<DTask, DTaskSurrogate> surrogates = Surrogates;
        if (surrogates.ContainsKey(task))
            return;

        DTaskSurrogate surrogate = DTaskSurrogate.Create(task, _idFactory);
        surrogates.Add(task, surrogate);

        Assign(ref _marshalingId, surrogate.Id);
        ((IDAsyncRunnable)task).Run(this);
    }

    private void EnsureNotMarshaling()
    {
        if (IsMarshaling)
            throw new MarshalingException("Only DTask objects representing d-async methods can be explicitly marshaled.");
    }
}