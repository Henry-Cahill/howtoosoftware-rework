namespace HowToSoftware.Core.Entities;

public class Webhook
{
    public string Id { get; set; } = null!;
    public string Event { get; set; } = null!;
    public string TargetUrl { get; set; } = null!;
    public string? Name { get; set; }
    public string? Secret { get; set; }
    public string ApiVersion { get; set; } = "v2";
    public string IntegrationId { get; set; } = null!;
    public DateTime? LastTriggeredAt { get; set; }
    public string? LastTriggeredStatus { get; set; }
    public string? LastTriggeredError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Integration Integration { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
