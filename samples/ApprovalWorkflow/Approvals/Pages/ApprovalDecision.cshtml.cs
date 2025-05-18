using DTasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Approvals.Pages;

public class ApprovalPage : PageModel
{
    [BindProperty(Name = "operationId", SupportsGet = true)]
    public DAsyncId OperationId { get; set; }
    
    [BindProperty(Name = "result", SupportsGet = true)]
    public int Result { get; set; }
    
    public string? CallbackPath { get; set; }

    public void OnGet()
    {
        CallbackPath = ApprovalService.ResumptionEndpoint.MakeCallbackPath(OperationId);
    }
}