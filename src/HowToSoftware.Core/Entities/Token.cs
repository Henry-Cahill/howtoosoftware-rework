namespace HowToSoftware.Core.Entities;

public class Token
{
    public string Id { get; set; } = null!;
    public string TokenValue { get; set; } = null!;
    public string Uuid { get; set; } = null!;
    public string? Data { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? FirstUsedAt { get; set; }
    public int UsedCount { get; set; }
    public int OtcUsedCount { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
