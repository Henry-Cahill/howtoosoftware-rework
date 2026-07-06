namespace HowToSoftware.Core.Entities;

public class PermissionsRole
{
    public string Id { get; set; } = null!;
    public string RoleId { get; set; } = null!;
    public string PermissionId { get; set; } = null!;

    // Navigation properties
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
