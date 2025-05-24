using DTasks;
using System.Net;
using System.Net.Mail;
using DTasks.AspNetCore.Execution;

namespace Approvals;

public interface IApprovalService
{
    DTask<ApprovalResult> SendApprovalRequestDAsync(ApprovalRequestDetails details, string approverEmail);
}

public class ApprovalService(IConfiguration configuration) : IApprovalService, IDisposable
{
    public static readonly ResumptionEndpoint<ApprovalResult> ResumptionEndpoint = new("approvals/resume/{operationId}");
    
    private readonly SmtpClient _smtp = new SmtpClient(configuration["MailHog:Host"]!)
    {
        Port = int.Parse(configuration["MailHog:Port"]!)
    };

    public DTask<ApprovalResult> SendApprovalRequestDAsync(ApprovalRequestDetails details, string approverEmail)
    {
        return DTask<ApprovalResult>.Factory.Suspend(async (operationId, cancellationToken) =>
        {
            string approveLink = $"http://localhost:5033/ApprovalDecision?operationId={WebUtility.UrlEncode(operationId.ToString())}&result={(int)ApprovalResult.Approve}";
            string rejectLink = $"http://localhost:5033/ApprovalDecision?operationId={WebUtility.UrlEncode(operationId.ToString())}&result={(int)ApprovalResult.Reject}";
            var mailMessage = new MailMessage
            {
                From = new MailAddress("noreply@approvals.com"),
                Body = $"""
                        <html>
                            <body>
                                <p>Hello,</p>
                                <p>You have a new {details.ApprovalCategory} approval request from <strong>{details.Requestor}</strong>.</p>
                                <p>Please review the approval request by visiting the following link:</p>
                                <p><a href='{details.ResourceUri}'>Review request</a></p>
                                <p>
                                    <a href='{approveLink}' style='padding: 10px 20px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin-right: 10px;'>Approve</a>
                                    <a href='{rejectLink}' style='padding: 10px 20px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px;'>Reject</a>
                                </p>
                                <p>Best regards,<br>The Approval System</p>
                            </body>
                        </html>
                        """,
                Subject = "Approval Request",
                IsBodyHtml = true
            };
            mailMessage.To.Add(approverEmail);

            await _smtp.SendMailAsync(mailMessage, cancellationToken);
        });
    }

    public void Dispose()
    {
        _smtp.Dispose();
    }
}
