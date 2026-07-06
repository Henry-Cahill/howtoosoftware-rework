using System.Text.Json.Serialization;

namespace HowToSoftware.Web.Models;

public record NavItem(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("url")] string Url
);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
