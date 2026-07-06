using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Controllers;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace HowToSoftware.Web.Tests;

public class AnalyticsControllerTests
{
    private readonly FakeAnalyticsRepository _analyticsRepo = new();
    private readonly FakeGeoIpService _geoIp = new();
    private readonly AnalyticsController _sut;

    public AnalyticsControllerTests()
    {
        _sut = new AnalyticsController(_analyticsRepo, _geoIp);
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task IngestEvent_ValidRequest_ReturnsNoContent()
    {
        var request = new AnalyticsEventRequest
        {
            SessionId = "sess-1",
            Action = "page_hit",
            PageUrl = "https://example.com/test/",
        };

        var result = await _sut.IngestEvent(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Single(_analyticsRepo.Events);
    }

    [Fact]
    public async Task IngestEvent_SetsTimestamp()
    {
        var before = DateTime.UtcNow;
        var request = new AnalyticsEventRequest
        {
            SessionId = "sess-1",
            Action = "page_hit",
            PageUrl = "https://example.com/",
        };

        await _sut.IngestEvent(request, CancellationToken.None);

        Assert.True(_analyticsRepo.Events[0].Timestamp >= before);
    }

    [Fact]
    public async Task IngestEvent_LooksUpCountry()
    {
        _geoIp.Lookups["127.0.0.1"] = "US";
        var request = new AnalyticsEventRequest
        {
            SessionId = "sess-1",
            Action = "page_hit",
            PageUrl = "https://example.com/",
        };

        await _sut.IngestEvent(request, CancellationToken.None);

        Assert.Equal("US", _analyticsRepo.Events[0].Country);
    }

    [Fact]
    public async Task IngestEvent_TooLongUrl_ReturnsBadRequest()
    {
        var request = new AnalyticsEventRequest
        {
            SessionId = "sess-1",
            Action = "page_hit",
            PageUrl = new string('x', 2001),
        };

        var result = await _sut.IngestEvent(request, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task IngestEvent_SanitizesValues()
    {
        var request = new AnalyticsEventRequest
        {
            SessionId = "  sess-1  ",
            Action = "page_hit",
            PageUrl = "https://example.com/",
            Browser = "  Chrome  ",
        };

        await _sut.IngestEvent(request, CancellationToken.None);

        Assert.Equal("sess-1", _analyticsRepo.Events[0].SessionId);
        Assert.Equal("Chrome", _analyticsRepo.Events[0].Browser);
    }

    [Fact]
    public async Task IngestEvent_WithUtmParams_SerializesToPayload()
    {
        var request = new AnalyticsEventRequest
        {
            SessionId = "sess-1",
            Action = "page_hit",
            PageUrl = "https://example.com/",
            Utm = new UtmParams
            {
                UtmSource = "twitter",
                UtmMedium = "social",
            },
        };

        await _sut.IngestEvent(request, CancellationToken.None);

        Assert.NotNull(_analyticsRepo.Events[0].Payload);
        Assert.Contains("twitter", _analyticsRepo.Events[0].Payload);
    }

    [Fact]
    public async Task IngestEvent_NoUtm_NullPayload()
    {
        var request = new AnalyticsEventRequest
        {
            SessionId = "sess-1",
            Action = "page_hit",
            PageUrl = "https://example.com/",
        };

        await _sut.IngestEvent(request, CancellationToken.None);

        Assert.Null(_analyticsRepo.Events[0].Payload);
    }

    [Fact]
    public async Task IngestEvent_XForwardedFor_UsesForwardedIp()
    {
        _geoIp.Lookups["203.0.113.1"] = "CA";
        _sut.ControllerContext.HttpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.1, 10.0.0.1";

        var request = new AnalyticsEventRequest
        {
            SessionId = "sess-1",
            Action = "page_hit",
            PageUrl = "https://example.com/",
        };

        await _sut.IngestEvent(request, CancellationToken.None);

        Assert.Equal("CA", _analyticsRepo.Events[0].Country);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
