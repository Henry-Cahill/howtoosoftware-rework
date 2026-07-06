namespace HowToSoftware.Core.Entities;

public class Integration
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = "custom";
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? IconImage { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<Webhook> Webhooks { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
