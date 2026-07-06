using HowToSoftware.Admin.Services;

namespace HowToSoftware.Admin.Tests;

public class LiveAnalyticsServiceTests
{
    // ── DTO tests ───────────────────────────────────────────────

    [Fact]
    public void RecentPageviewDto_DefaultValues()
    {
        var dto = new RecentPageviewDto();

        Assert.Equal("", dto.PagePath);
        Assert.Equal(default, dto.Timestamp);
        Assert.Null(dto.Country);
        Assert.Null(dto.Device);
        Assert.Null(dto.Browser);
        Assert.Null(dto.Referrer);
        Assert.Null(dto.MemberUuid);
    }

    [Fact]
    public void RecentPageviewDto_HoldsValues()
    {
        var now = DateTime.UtcNow;
        var dto = new RecentPageviewDto
        {
            PagePath = "/getting-started/",
            Timestamp = now,
            Country = "US",
            Device = "Desktop",
            Browser = "Chrome",
            Referrer = "https://google.com",
            MemberUuid = "member-123",
        };

        Assert.Equal("/getting-started/", dto.PagePath);
        Assert.Equal(now, dto.Timestamp);
        Assert.Equal("US", dto.Country);
        Assert.Equal("Desktop", dto.Device);
        Assert.Equal("Chrome", dto.Browser);
        Assert.Equal("https://google.com", dto.Referrer);
        Assert.Equal("member-123", dto.MemberUuid);
    }

    [Fact]
    public void RecentPageviewDto_NullableFieldsAcceptNull()
    {
        var dto = new RecentPageviewDto
        {
            PagePath = "/test/",
            Timestamp = DateTime.UtcNow,
            Country = null,
            Device = null,
            Browser = null,
            Referrer = null,
            MemberUuid = null,
        };

        Assert.Null(dto.Country);
        Assert.Null(dto.Device);
        Assert.Null(dto.Browser);
        Assert.Null(dto.Referrer);
        Assert.Null(dto.MemberUuid);
    }

    [Fact]
    public void RecentPageviewDto_CanCreateMultiple()
    {
        var list = new List<RecentPageviewDto>
        {
            new() { PagePath = "/page-1/", Timestamp = DateTime.UtcNow },
            new() { PagePath = "/page-2/", Timestamp = DateTime.UtcNow.AddSeconds(-5) },
            new() { PagePath = "/page-3/", Timestamp = DateTime.UtcNow.AddSeconds(-10), MemberUuid = "m-1" },
        };

        Assert.Equal(3, list.Count);
        Assert.Single(list, p => p.MemberUuid is not null);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
