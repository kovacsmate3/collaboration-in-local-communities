using SendGrid;

namespace Backend.Infrastructure.Email;

public sealed class EmailOptions : SendGridClientOptions
{
    public const string SectionName = "SendGrid";

    public string FromEmail { get; set; } = "noreply@2gather.hu";
    public string FromName { get; set; } = "2gather";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
}
