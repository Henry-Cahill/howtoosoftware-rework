namespace HowToSoftware.Core.Entities;

public class Redirect
{
    public string Id { get; set; } = null!;
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
    public string? PostId { get; set; }
    public long HitCount { get; set; }
    public bool IsRegex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Post? Post { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
