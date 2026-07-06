namespace HowToSoftware.Core.Entities;

public class RecommendationClickEvent
{
    public string Id { get; set; } = null!;
    public string RecommendationId { get; set; } = null!;
    public string? MemberId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Recommendation Recommendation { get; set; } = null!;
    public Member? Member { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
