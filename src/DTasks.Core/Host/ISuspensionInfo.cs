namespace DTasks.Host;

public interface ISuspensionInfo
{
    bool IsSuspended<TAwaiter>(ref TAwaiter awaiter);
}
