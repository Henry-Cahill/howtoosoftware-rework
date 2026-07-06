namespace HowToSoftware.Core.Entities;

public class CollectionsPost
{
    public string Id { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public int SortOrder { get; set; }

    // Navigation properties
    public Collection Collection { get; set; } = null!;
    public Post Post { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
