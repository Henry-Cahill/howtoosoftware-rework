namespace HowToSoftware.Core.Entities;

public class EmailBatch
{
    public string Id { get; set; } = null!;
    public string EmailId { get; set; } = null!;
    public string? ProviderId { get; set; }
    public bool FallbackSendingDomain { get; set; }
    public string Status { get; set; } = "pending";
    public string? MemberSegment { get; set; }
    public int? ErrorStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorData { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Email Email { get; set; } = null!;
    public ICollection<EmailRecipient> Recipients { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
