namespace HowToSoftware.Core.Entities;

public class PostsProduct
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public int SortOrder { get; set; }

    // Navigation properties
    public Post Post { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
