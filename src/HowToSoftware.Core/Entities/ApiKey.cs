namespace HowToSoftware.Core.Entities;

public class ApiKey
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Secret { get; set; } = null!;
    public string? RoleId { get; set; }
    public string? IntegrationId { get; set; }
    public string? UserId { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? LastSeenVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Integration? Integration { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
