using DTasks.Utils;
using System.Diagnostics;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private void SetBranchResult()
    {
        switch (_state)
        {
            case FlowState.WhenAll:
                _whenAllBranchCount--;
                break;

            case FlowState.WhenAllResult:
                Debug.Fail("Expected a result from completed WhenAll branch.");
                break;

            default:
                Debug.Fail("Invalid parent flow state.");
                break;
        }
    }

    private void SetBranchResult<TResult>(TResult result)
    {
        switch (_state)
        {
            case FlowState.WhenAll:
                _whenAllBranchCount--;
                break;

            case FlowState.WhenAllResult:
                Assert.Is<Dictionary<int, TResult>>(_whenAllBranchResults);
                _whenAllBranchResults.Add(_whenAllBranchCount, result);
                break;

            default:
                Debug.Fail("Invalid parent flow state.");
                break;
        }
    }

    private void SetBranchException(Exception exception)
    {
        _aggregateExceptions ??= new(1);
        _aggregateExceptions.Add(exception);

        switch (_state)
        {
            case FlowState.WhenAll:
            case FlowState.WhenAllResult:
                _whenAllBranchCount--;
                break;

            default:
                Debug.Fail("Invalid parent flow state.");
                break;
        }
    }
}
