namespace HowToSoftware.Core.Entities;

public class AnalyticsHourlyRollup
{
    public long Id { get; set; }
    public DateTime BucketHour { get; set; }
    public int Pageviews { get; set; }
    public int UniqueVisitors { get; set; }
    public int Sessions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
