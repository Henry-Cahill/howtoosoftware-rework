namespace HowToSoftware.Core.Entities;

public class Job
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Status { get; set; } = "queued";
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Metadata { get; set; }
    public int? QueueEntry { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
