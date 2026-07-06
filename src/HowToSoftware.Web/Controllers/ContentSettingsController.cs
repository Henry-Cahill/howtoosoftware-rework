using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/content/settings")]
[Authorize(Policy = "ContentApi")]
[EnableRateLimiting("content-api")]
public class ContentSettingsController(ISettingsService settingsService) : ControllerBase
{
    // Setting keys that map to Ghost Content API /settings/ response
    private static readonly string[] PublicSettingKeys =
    [
        "title", "description", "logo", "icon", "accent_color", "cover_image",
        "facebook", "twitter", "locale", "timezone",
        "codeinjection_head", "codeinjection_foot",
        "navigation", "secondary_navigation",
        "meta_title", "meta_description",
        "og_image", "og_title", "og_description",
        "twitter_image", "twitter_title", "twitter_description",
        "members_support_address"
    ];

    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken ct = default)
    {
        var allSettings = await settingsService.GetAllAsync(ct);
        var lookup = allSettings.ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);

        string? Get(string key) => lookup.GetValueOrDefault(key);

        object? GetJson(string key)
        {
            var raw = Get(key);
            if (raw is null) return null;
            try { return JsonSerializer.Deserialize<object>(raw); }
            catch { return raw; }
        }

        var resource = new SettingsResource
        {
            Title = Get("title"),
            Description = Get("description"),
            Logo = Get("logo"),
            Icon = Get("icon"),
            AccentColor = Get("accent_color"),
            CoverImage = Get("cover_image"),
            Facebook = Get("facebook"),
            Twitter = Get("twitter"),
            Lang = Get("locale"),
            Timezone = Get("timezone"),
            CodeinjectionHead = Get("codeinjection_head"),
            CodeinjectionFoot = Get("codeinjection_foot"),
            Navigation = GetJson("navigation"),
            SecondaryNavigation = GetJson("secondary_navigation"),
            MetaTitle = Get("meta_title"),
            MetaDescription = Get("meta_description"),
            OgImage = Get("og_image"),
            OgTitle = Get("og_title"),
            OgDescription = Get("og_description"),
            TwitterImage = Get("twitter_image"),
            TwitterTitle = Get("twitter_title"),
            TwitterDescription = Get("twitter_description"),
            MembersSupportAddress = Get("members_support_address"),
            Url = Get("url"),
        };

        return Ok(new GhostSettingsEnvelope
        {
            Settings = resource,
            Meta = new { },
        });
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
