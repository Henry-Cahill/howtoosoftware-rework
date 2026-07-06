namespace HowToSoftware.Infrastructure.Services;

public sealed class MailSettings
{
    /// <summary>Mailgun SMTP domain (e.g. "mg.example.com").</summary>
    public string MailgunDomain { get; set; } = string.Empty;

    /// <summary>Mailgun API key, used as the SMTP password.</summary>
    public string MailgunApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional SMTP login username. When empty, defaults to
    /// "postmaster@{MailgunDomain}". Set this if you created a custom
    /// SMTP credential in Mailgun (e.g. "howtoosoftware@mg.example.com").
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>SMTP host. Defaults to Mailgun's SMTP endpoint.</summary>
    public string SmtpHost { get; set; } = "smtp.mailgun.org";

    /// <summary>SMTP port. 587 = STARTTLS (recommended).</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>Default "From" address when none is specified.</summary>
    public string DefaultFrom { get; set; } = string.Empty;

    /// <summary>Max emails per batch chunk sent in a single SMTP session.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Delay in milliseconds between batch chunks for rate limiting.</summary>
    public int BatchDelayMs { get; set; } = 1000;

    /// <summary>Max send attempts per message before marking as failed.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>SMTP connection timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
