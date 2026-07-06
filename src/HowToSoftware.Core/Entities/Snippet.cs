namespace HowToSoftware.Core.Entities;

public class Snippet
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Mobiledoc { get; set; } = null!;
    public string? Lexical { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
