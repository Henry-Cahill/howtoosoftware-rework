namespace HowToSoftware.Core.Entities;

public class Setting
{
    public string Id { get; set; } = null!;
    public string Group { get; set; } = "core";
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public string Type { get; set; } = null!;
    public string? Flags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
