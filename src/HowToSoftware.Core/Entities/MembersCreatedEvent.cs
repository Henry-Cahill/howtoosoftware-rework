namespace HowToSoftware.Core.Entities;

public class MembersCreatedEvent
{
    public string Id { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string MemberId { get; set; } = null!;
    public string? AttributionId { get; set; }
    public string? AttributionType { get; set; }
    public string? AttributionUrl { get; set; }
    public string? ReferrerSource { get; set; }
    public string? ReferrerMedium { get; set; }
    public string? ReferrerUrl { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmTerm { get; set; }
    public string? UtmContent { get; set; }
    public string Source { get; set; } = null!;
    public string? BatchId { get; set; }

    public Member Member { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
