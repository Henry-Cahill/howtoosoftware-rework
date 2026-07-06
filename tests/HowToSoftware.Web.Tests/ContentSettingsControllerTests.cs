using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Controllers;
using HowToSoftware.Web.Models.Api;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace HowToSoftware.Web.Tests;

public class ContentSettingsControllerTests
{
    private readonly FakeSettingsService _settings = new();
    private readonly ContentSettingsController _sut;

    public ContentSettingsControllerTests()
    {
        _sut = new ContentSettingsController(_settings);
    }

    [Fact]
    public async Task GetSettings_ReturnsOkWithEnvelope()
    {
        _settings.Settings["title"] = "My Site";
        _settings.Settings["description"] = "A test description";

        var result = await _sut.GetSettings();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostSettingsEnvelope>(ok.Value);
        Assert.Equal("My Site", envelope.Settings.Title);
        Assert.Equal("A test description", envelope.Settings.Description);
    }

    [Fact]
    public async Task GetSettings_MapsAllKnownKeys()
    {
        _settings.Settings["title"] = "Title";
        _settings.Settings["description"] = "Desc";
        _settings.Settings["logo"] = "/logo.png";
        _settings.Settings["icon"] = "/icon.png";
        _settings.Settings["accent_color"] = "#ff0000";
        _settings.Settings["cover_image"] = "/cover.jpg";
        _settings.Settings["facebook"] = "fb";
        _settings.Settings["twitter"] = "@tw";
        _settings.Settings["locale"] = "en";
        _settings.Settings["timezone"] = "UTC";
        _settings.Settings["url"] = "https://example.com";
        _settings.Settings["members_support_address"] = "support@example.com";

        var result = await _sut.GetSettings();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostSettingsEnvelope>(ok.Value);
        var s = envelope.Settings;
        Assert.Equal("Title", s.Title);
        Assert.Equal("Desc", s.Description);
        Assert.Equal("/logo.png", s.Logo);
        Assert.Equal("/icon.png", s.Icon);
        Assert.Equal("#ff0000", s.AccentColor);
        Assert.Equal("/cover.jpg", s.CoverImage);
        Assert.Equal("fb", s.Facebook);
        Assert.Equal("@tw", s.Twitter);
        Assert.Equal("en", s.Lang);
        Assert.Equal("UTC", s.Timezone);
        Assert.Equal("https://example.com", s.Url);
        Assert.Equal("support@example.com", s.MembersSupportAddress);
    }

    [Fact]
    public async Task GetSettings_MissingKeys_ReturnsNulls()
    {
        var result = await _sut.GetSettings();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostSettingsEnvelope>(ok.Value);
        Assert.Null(envelope.Settings.Title);
        Assert.Null(envelope.Settings.Description);
    }

    [Fact]
    public async Task GetSettings_NavigationJson_Parsed()
    {
        _settings.Settings["navigation"] = "[{\"label\":\"Home\",\"url\":\"/\"}]";

        var result = await _sut.GetSettings();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostSettingsEnvelope>(ok.Value);
        Assert.NotNull(envelope.Settings.Navigation);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
