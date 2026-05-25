namespace Backend.Infrastructure.Email;

/// <summary>
/// Abstraction for sending emails, allowing for different implementations (e.g., SendGrid, SMTP) and easier testing.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="toEmail">To email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="htmlContent">HTML content of the email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken = default);
}
