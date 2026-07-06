using Microsoft.AspNetCore.Identity;

namespace HowToSoftware.Core.Entities;

public class Role : IdentityRole<string>
{
    // IdentityRole<string> provides: Id, Name, NormalizedName, ConcurrencyStamp

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<RolesUser> RolesUsers { get; set; } = [];
    public ICollection<PermissionsRole> PermissionsRoles { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
