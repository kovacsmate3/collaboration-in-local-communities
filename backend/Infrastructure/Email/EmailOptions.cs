using SendGrid;

namespace Backend.Infrastructure.Email;

public sealed class EmailOptions : SendGridClientOptions
{
    public const string SectionName = "SendGrid";

    public EmailOptions()
    {
        SetDataResidency("eu");
    }

    public string FromEmail { get; set; } = "noreply@2gather.example.com";
    public string FromName { get; set; } = "2gather";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
}
