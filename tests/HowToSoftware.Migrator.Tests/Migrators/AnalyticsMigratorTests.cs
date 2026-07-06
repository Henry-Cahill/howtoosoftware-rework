using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class AnalyticsMigratorTests
{
    #region IsAnalyticsTable

    [Theory]
    [InlineData("tinybird_analytics_backup", true)]
    [InlineData("TINYBIRD_ANALYTICS_BACKUP", true)]
    [InlineData("Tinybird_Analytics_Backup", true)]
    [InlineData("analytics_events", false)]
    [InlineData("posts", false)]
    [InlineData("members", false)]
    public void IsAnalyticsTable_ReturnsCorrectResult(string tableName, bool expected)
    {
        Assert.Equal(expected, AnalyticsMigrator.IsAnalyticsTable(tableName));
    }

    #endregion

    #region ProcessAnalytics — Empty & Non-Analytics

    [Fact]
    public void ProcessAnalytics_EmptyInserts_ReturnsZeroStats()
    {
        var result = AnalyticsMigrator.ProcessAnalytics([]);

        Assert.Equal(0, result.Stats.EventCount);
        Assert.Empty(result.TransformedInserts);
    }

    [Fact]
    public void ProcessAnalytics_NoAnalyticsTables_PassesThrough()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts", ["id", "title"], [["1", "Hello"]])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        Assert.Single(result.TransformedInserts);
        Assert.Equal("posts", result.TransformedInserts[0].TableName);
        Assert.Equal(0, result.Stats.EventCount);
    }

    #endregion

    #region ProcessAnalytics — Table Rename

    [Fact]
    public void ProcessAnalytics_RenamesTableToAnalyticsEvents()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "session_id", "action"],
                [["1", "2025-11-03 10:00:00", "sess-1", "page_hit"]])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        Assert.Single(result.TransformedInserts);
        Assert.Equal("analytics_events", result.TransformedInserts[0].TableName);
    }

    #endregion

    #region ProcessAnalytics — Column Rename

    [Fact]
    public void ProcessAnalytics_RenamesPageUrlpathColumn()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "page_urlpath", "action"],
                [["1", "2025-11-03 10:00:00", "/hello-world", "page_hit"]])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        var transformed = result.TransformedInserts[0];
        Assert.Contains("page_url_path", transformed.Columns);
        Assert.DoesNotContain("page_urlpath", transformed.Columns);
    }

    #endregion

    #region ProcessAnalytics — Id Column Stripped

    [Fact]
    public void ProcessAnalytics_StripsIdColumn()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "session_id", "action"],
                [["1", "2025-11-03 10:00:00", "sess-1", "page_hit"]])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        var transformed = result.TransformedInserts[0];
        Assert.DoesNotContain("id", transformed.Columns);
        Assert.Equal(3, transformed.Columns.Length); // timestamp, session_id, action
        Assert.Equal(3, transformed.Rows[0].Length);
        Assert.Equal("2025-11-03 10:00:00", transformed.Rows[0][0]); // timestamp is now first
    }

    #endregion

    #region ProcessAnalytics — Statistics Collection

    [Fact]
    public void ProcessAnalytics_CountsEvents()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "action"],
                [
                    ["1", "2025-11-03 10:00:00", "page_hit"],
                    ["2", "2025-11-03 10:01:00", "page_hit"],
                    ["3", "2025-11-03 10:02:00", "page_hit"],
                ])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        Assert.Equal(3, result.Stats.EventCount);
    }

    [Fact]
    public void ProcessAnalytics_CollectsActionBreakdown()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "action"],
                [
                    ["1", "2025-11-03 10:00:00", "page_hit"],
                    ["2", "2025-11-03 10:01:00", "page_hit"],
                    ["3", "2025-11-03 10:02:00", "scroll"],
                ])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        Assert.Equal(2, result.Stats.ActionCounts["page_hit"]);
        Assert.Equal(1, result.Stats.ActionCounts["scroll"]);
    }

    [Fact]
    public void ProcessAnalytics_CollectsDeviceBreakdown()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "action", "device"],
                [
                    ["1", "2025-11-03 10:00:00", "page_hit", "desktop"],
                    ["2", "2025-11-03 10:01:00", "page_hit", "mobile"],
                    ["3", "2025-11-03 10:02:00", "page_hit", "desktop"],
                ])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        Assert.Equal(2, result.Stats.DeviceCounts["desktop"]);
        Assert.Equal(1, result.Stats.DeviceCounts["mobile"]);
    }

    [Fact]
    public void ProcessAnalytics_CollectsCountryBreakdown()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "action", "country"],
                [
                    ["1", "2025-11-03 10:00:00", "page_hit", "US"],
                    ["2", "2025-11-03 10:01:00", "page_hit", "GB"],
                    ["3", "2025-11-03 10:02:00", "page_hit", "US"],
                ])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        Assert.Equal(2, result.Stats.CountryCounts["US"]);
        Assert.Equal(1, result.Stats.CountryCounts["GB"]);
    }

    [Fact]
    public void ProcessAnalytics_HandlesNullValues()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "action", "device"],
                [
                    ["1", "2025-11-03 10:00:00", null, null],
                ])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        Assert.Equal(1, result.Stats.EventCount);
        Assert.Equal(1, result.Stats.ActionCounts["(null)"]);
        Assert.Equal(1, result.Stats.DeviceCounts["(null)"]);
    }

    #endregion

    #region ProcessAnalytics — Mixed Tables

    [Fact]
    public void ProcessAnalytics_PreservesNonAnalyticsTables()
    {
        var inserts = new ParsedInsert[]
        {
            new("posts", ["id", "title"], [["1", "Hello"]]),
            new("tinybird_analytics_backup",
                ["id", "timestamp", "action"],
                [["1", "2025-11-03 10:00:00", "page_hit"]]),
            new("members", ["id", "email"], [["1", "test@example.com"]]),
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        Assert.Equal(3, result.TransformedInserts.Count);
        Assert.Equal("posts", result.TransformedInserts[0].TableName);
        Assert.Equal("analytics_events", result.TransformedInserts[1].TableName);
        Assert.Equal("members", result.TransformedInserts[2].TableName);
        Assert.Equal(1, result.Stats.EventCount);
    }

    #endregion

    #region ProcessAnalytics — Full Column Set

    [Fact]
    public void ProcessAnalytics_FullSchema_MapsAllColumns()
    {
        var inserts = new[]
        {
            new ParsedInsert("tinybird_analytics_backup",
                ["id", "timestamp", "session_id", "action", "version", "payload",
                 "site_uuid", "page_url", "page_urlpath", "referrer",
                 "device", "browser", "os", "country",
                 "member_uuid", "member_status", "post_uuid", "backed_up_at"],
                [["1", "2025-11-03 10:00:00", "sess-abc", "page_hit", "1.0", "{}",
                  "site-123", "https://howtoosoftware.com/hello", "/hello", "https://google.com",
                  "desktop", "Chrome", "Windows", "US",
                  "member-1", "free", "post-1", "2025-11-03 10:05:00"]])
        };

        var result = AnalyticsMigrator.ProcessAnalytics(inserts);

        var transformed = result.TransformedInserts[0];

        // id stripped, page_urlpath → page_url_path
        var expectedColumns = new[]
        {
            "timestamp", "session_id", "action", "version", "payload",
            "site_uuid", "page_url", "page_url_path", "referrer",
            "device", "browser", "os", "country",
            "member_uuid", "member_status", "post_uuid", "backed_up_at"
        };

        Assert.Equal(expectedColumns, transformed.Columns);
        Assert.Equal(17, transformed.Rows[0].Length);

        // Verify data values preserved
        Assert.Equal("2025-11-03 10:00:00", transformed.Rows[0][0]);  // timestamp
        Assert.Equal("sess-abc", transformed.Rows[0][1]);              // session_id
        Assert.Equal("/hello", transformed.Rows[0][7]);                // page_url_path (was page_urlpath)
    }

    #endregion

    #region Stats ToString

    [Fact]
    public void Stats_ToString_FormatsCorrectly()
    {
        var stats = new AnalyticsMigrationStats { EventCount = 5 };
        stats.ActionCounts["page_hit"] = 3;
        stats.ActionCounts["scroll"] = 2;

        var result = stats.ToString();

        Assert.Contains("Events: 5", result);
        Assert.Contains("page_hit: 3", result);
        Assert.Contains("scroll: 2", result);
    }

    #endregion
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
