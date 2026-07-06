namespace HowToSoftware.Core.Entities;

public class Invite
{
    public string Id { get; set; } = null!;
    public string RoleId { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public string Token { get; set; } = null!;
    public string Email { get; set; } = null!;
    public long Expires { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
