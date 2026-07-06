namespace HowToSoftware.Core.Entities;

public class EmailRecipientFailure
{
    public string Id { get; set; } = null!;
    public string EmailId { get; set; } = null!;
    public string? MemberId { get; set; }
    public string EmailRecipientId { get; set; } = null!;
    public int Code { get; set; }
    public string? EnhancedCode { get; set; }
    public string Message { get; set; } = null!;
    public string Severity { get; set; } = "permanent";
    public DateTime FailedAt { get; set; }
    public string? EventId { get; set; }

    // Navigation properties
    public Email Email { get; set; } = null!;
    public EmailRecipient EmailRecipient { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
