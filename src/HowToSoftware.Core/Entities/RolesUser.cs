using Microsoft.AspNetCore.Identity;

namespace HowToSoftware.Core.Entities;

public class RolesUser : IdentityUserRole<string>
{
    // IdentityUserRole<string> provides: UserId, RoleId

    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..24];

    // Navigation properties
    public Role Role { get; set; } = null!;
    public User User { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
