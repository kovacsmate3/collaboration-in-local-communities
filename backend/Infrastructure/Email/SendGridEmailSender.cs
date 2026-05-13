using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Backend.Infrastructure.Email;

internal sealed class SendGridEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SendGridEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken = default)
    {
        var client = new SendGridClient(options.Value);
        var from = new EmailAddress(options.Value.FromEmail, options.Value.FromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent);
        var response = await client.SendEmailAsync(msg, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "SendGrid failed to deliver email to {Email}. HTTP {Status}",
                toEmail,
                (int)response.StatusCode);
            throw new InvalidOperationException($"Email delivery failed with HTTP {(int)response.StatusCode}.");
        }
    }
}
