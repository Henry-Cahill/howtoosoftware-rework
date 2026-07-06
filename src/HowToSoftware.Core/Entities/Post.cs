namespace HowToSoftware.Core.Entities;

public class Post
{
    public string Id { get; set; } = null!;
    public string Uuid { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Mobiledoc { get; set; }
    public string? Lexical { get; set; }
    public string? Html { get; set; }
    public string? CommentId { get; set; }
    public string? Plaintext { get; set; }
    public string? FeatureImage { get; set; }
    public bool Featured { get; set; }
    public string Type { get; set; } = "post";
    public string Status { get; set; } = "draft";
    public string? Locale { get; set; }
    public string Visibility { get; set; } = "public";
    public string EmailRecipientFilter { get; set; } = "all";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public string? CustomExcerpt { get; set; }
    public string? CodeinjectionHead { get; set; }
    public string? CodeinjectionFoot { get; set; }
    public string? CustomTemplate { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? NewsletterId { get; set; }
    public bool ShowTitleAndFeatureImage { get; set; } = true;
    public string? ParentId { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public Newsletter? Newsletter { get; set; }
    public Post? Parent { get; set; }
    public ICollection<Post> Children { get; set; } = [];
    public PostMeta? Meta { get; set; }
    public ICollection<PostRevision> Revisions { get; set; } = [];
    public ICollection<MobiledocRevision> MobiledocRevisions { get; set; } = [];
    public ICollection<Email> Emails { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<PostsTag> PostsTags { get; set; } = [];
    public ICollection<PostsAuthor> PostsAuthors { get; set; } = [];
    public ICollection<PostsProduct> PostsProducts { get; set; } = [];
    public ICollection<CollectionsPost> CollectionsPosts { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
