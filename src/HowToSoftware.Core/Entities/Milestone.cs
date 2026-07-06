namespace HowToSoftware.Core.Entities;

public class Milestone
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int Value { get; set; }
    public string? Currency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
