namespace HowToSoftware.Core.Entities;

public class ApOutbox
{
    public int Id { get; set; }
    public string Uuid { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public byte PostType { get; set; }
    public byte OutboxType { get; set; }
    public int AccountId { get; set; }
    public int PostId { get; set; }
    public int AuthorId { get; set; }

    // Navigation properties
    public ApAccount Account { get; set; } = null!;
    public ApPost Post { get; set; } = null!;
    public ApAccount Author { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
