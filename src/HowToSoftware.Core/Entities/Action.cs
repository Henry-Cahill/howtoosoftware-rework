namespace HowToSoftware.Core.Entities;

public class Action
{
    public string Id { get; set; } = null!;
    public string? ResourceId { get; set; }
    public string ResourceType { get; set; } = null!;
    public string ActorId { get; set; } = null!;
    public string ActorType { get; set; } = null!;
    public string Event { get; set; } = null!;
    public string? Context { get; set; }
    public DateTime CreatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
