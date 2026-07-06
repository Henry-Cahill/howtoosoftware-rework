namespace HowToSoftware.Core.Entities;

public class EmailRecipient
{
    public string Id { get; set; } = null!;
    public string EmailId { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string BatchId { get; set; } = null!;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string MemberUuid { get; set; } = null!;
    public string MemberEmail { get; set; } = null!;
    public string? MemberName { get; set; }

    /// <summary>For A/B subject-line testing: "a", "b", "holdout", or null when no A/B test is active for the parent Email.</summary>
    public string? AbVariant { get; set; }

    // Navigation properties
    public Email Email { get; set; } = null!;
    public EmailBatch Batch { get; set; } = null!;
    public ICollection<EmailRecipientFailure> Failures { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
