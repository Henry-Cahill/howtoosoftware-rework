namespace HowToSoftware.Core.Entities;

public class Session
{
    public string Id { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string SessionData { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
