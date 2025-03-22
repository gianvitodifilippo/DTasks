using DTasks;
using System.Net;
using System.Net.Mail;

namespace Approvals;

public class ApprovalService(IConfiguration configuration) : IDisposable
{
    private readonly SmtpClient _smtp = new SmtpClient(configuration["MailHog:Host"]!)
    {
        Port = int.Parse(configuration["MailHog:Port"]!)
    };

    // We should be able to generate the callback with an attribute, something like
    // [DAsyncCallback(route: "approvals/{$id}/{$result}", Method = "get")]
    public DTask<ApprovalResult> SendApprovalRequestDAsync(ApprovalRequestDetails details, string approverEmail)
    {
        return DTask<ApprovalResult>.Factory.Callback(async (id, cancellationToken) =>
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress("noreply@approvals.com"),
                Subject = "Approval Request",
                Body = $"""
                    <html>
                        <body>
                            <p>Hello,</p>
                            <p>You have a new {details.ApprovalCategory} approval request from <strong>{details.Requestor}</strong>.</p>
                            <p>Please review the approval request by visiting the following link:</p>
                            <p><a href='{details.ResourceUri}'>Review request</a></p>
                            <p>
                                <a href='http://localhost:5033/approvals/{WebUtility.UrlEncode(id.ToString())}/Approve' style='padding: 10px 20px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin-right: 10px;'>Approve</a>
                                <a href='http://localhost:5033/approvals/{WebUtility.UrlEncode(id.ToString())}/Reject' style='padding: 10px 20px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px;'>Reject</a>
                            </p>
                            <p>Best regards,<br>The Approval System</p>
                        </body>
                    </html>
                    """,
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
