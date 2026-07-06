namespace HowToSoftware.Core.Entities;

public class PostsAuthor
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
    public int SortOrder { get; set; }

    // Navigation properties
    public Post Post { get; set; } = null!;
    public User Author { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
