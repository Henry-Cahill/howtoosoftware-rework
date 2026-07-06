namespace HowToSoftware.Core.Entities;

public class AnalyticsDailyRollup
{
    public long Id { get; set; }
    public DateTime BucketDate { get; set; }
    public int Pageviews { get; set; }
    public int UniqueVisitors { get; set; }
    public int Sessions { get; set; }
    public decimal BounceRatePercent { get; set; }
    public decimal AvgSessionDurationSeconds { get; set; }

    // Dimensional top-N rollups stored as JSON for fast dashboard loads
    public string? TopPagesJson { get; set; }
    public string? TopSourcesJson { get; set; }
    public string? DeviceBreakdownJson { get; set; }
    public string? CountryBreakdownJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
