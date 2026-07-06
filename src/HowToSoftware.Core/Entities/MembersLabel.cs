namespace HowToSoftware.Core.Entities;

public class MembersLabel
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string LabelId { get; set; } = null!;
    public int SortOrder { get; set; }

    // Navigation properties
    public Member Member { get; set; } = null!;
    public Label Label { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
