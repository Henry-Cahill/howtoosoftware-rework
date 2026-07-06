using HowToSoftware.Core.Entities;

namespace HowToSoftware.Infrastructure.Tests;

public class AnalyticsRollupServiceTests
{
    // ── Entity model tests ──────────────────────────────────────

    [Fact]
    public void AnalyticsHourlyRollup_DefaultValues()
    {
        var rollup = new AnalyticsHourlyRollup();

        Assert.Equal(0, rollup.Id);
        Assert.Equal(0, rollup.Pageviews);
        Assert.Equal(0, rollup.UniqueVisitors);
        Assert.Equal(0, rollup.Sessions);
        Assert.Equal(default, rollup.BucketHour);
    }

    [Fact]
    public void AnalyticsHourlyRollup_HoldsValues()
    {
        var bucket = new DateTime(2026, 3, 11, 14, 0, 0, DateTimeKind.Utc);
        var rollup = new AnalyticsHourlyRollup
        {
            Id = 1,
            BucketHour = bucket,
            Pageviews = 150,
            UniqueVisitors = 42,
            Sessions = 45,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        Assert.Equal(1, rollup.Id);
        Assert.Equal(bucket, rollup.BucketHour);
        Assert.Equal(150, rollup.Pageviews);
        Assert.Equal(42, rollup.UniqueVisitors);
        Assert.Equal(45, rollup.Sessions);
    }

    [Fact]
    public void AnalyticsDailyRollup_DefaultValues()
    {
        var rollup = new AnalyticsDailyRollup();

        Assert.Equal(0, rollup.Id);
        Assert.Equal(0, rollup.Pageviews);
        Assert.Equal(0, rollup.UniqueVisitors);
        Assert.Equal(0, rollup.Sessions);
        Assert.Equal(0, rollup.BounceRatePercent);
        Assert.Equal(0, rollup.AvgSessionDurationSeconds);
        Assert.Null(rollup.TopPagesJson);
        Assert.Null(rollup.TopSourcesJson);
        Assert.Null(rollup.DeviceBreakdownJson);
        Assert.Null(rollup.CountryBreakdownJson);
    }

    [Fact]
    public void AnalyticsDailyRollup_HoldsValues()
    {
        var bucket = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var rollup = new AnalyticsDailyRollup
        {
            Id = 1,
            BucketDate = bucket,
            Pageviews = 1200,
            UniqueVisitors = 300,
            Sessions = 350,
            BounceRatePercent = 45.50m,
            AvgSessionDurationSeconds = 127.30m,
            TopPagesJson = "[{\"path\":\"/getting-started\",\"views\":100,\"visitors\":50}]",
            TopSourcesJson = "[{\"source\":\"google.com\",\"visits\":80,\"visitors\":60}]",
            DeviceBreakdownJson = "[{\"device\":\"Desktop\",\"sessions\":200}]",
            CountryBreakdownJson = "[{\"country\":\"US\",\"visitors\":150}]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        Assert.Equal(bucket, rollup.BucketDate);
        Assert.Equal(1200, rollup.Pageviews);
        Assert.Equal(300, rollup.UniqueVisitors);
        Assert.Equal(350, rollup.Sessions);
        Assert.Equal(45.50m, rollup.BounceRatePercent);
        Assert.Equal(127.30m, rollup.AvgSessionDurationSeconds);
        Assert.Contains("getting-started", rollup.TopPagesJson);
        Assert.Contains("google.com", rollup.TopSourcesJson);
        Assert.Contains("Desktop", rollup.DeviceBreakdownJson);
        Assert.Contains("US", rollup.CountryBreakdownJson);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
