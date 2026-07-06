namespace HowToSoftware.Core.Entities;

public class Permission
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ObjectType { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string? ObjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<PermissionsRole> PermissionsRoles { get; set; } = [];
    public ICollection<PermissionsUser> PermissionsUsers { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
