namespace HowToSoftware.Core.Entities;

public class Outbox
{
    public string Id { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public string Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public string? Message { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
