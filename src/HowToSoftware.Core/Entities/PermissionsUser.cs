namespace HowToSoftware.Core.Entities;

public class PermissionsUser
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string PermissionId { get; set; } = null!;

    // Navigation properties
    public User User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
