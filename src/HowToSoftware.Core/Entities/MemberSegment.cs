namespace HowToSoftware.Core.Entities;

/// <summary>
/// A saved combination of member-list filters (status / label / engagement /
/// search) displayed as a quick-access chip above the member table.
/// </summary>
public class MemberSegment
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? StatusFilter { get; set; }
    public string? LabelId { get; set; }
    public string? EngagementFilter { get; set; }
    public string? SearchQuery { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Label? Label { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
