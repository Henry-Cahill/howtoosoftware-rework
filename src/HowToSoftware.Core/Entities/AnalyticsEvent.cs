namespace HowToSoftware.Core.Entities;

public class AnalyticsEvent
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? SessionId { get; set; }
    public string? Action { get; set; }
    public string? Version { get; set; }
    public string? Payload { get; set; }
    public string? SiteUuid { get; set; }
    public string? PageUrl { get; set; }
    public string? PageUrlPath { get; set; }
    public string? Referrer { get; set; }
    public string? Device { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Country { get; set; }
    public string? MemberUuid { get; set; }
    public string? MemberStatus { get; set; }
    public string? PostUuid { get; set; }
    public DateTime? BackedUpAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
