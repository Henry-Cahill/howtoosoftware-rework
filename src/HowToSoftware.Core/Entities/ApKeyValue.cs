namespace HowToSoftware.Core.Entities;

public class ApKeyValue
{
    public int Id { get; set; }
    public string? Key { get; set; }
    public string Value { get; set; } = null!;
    public DateTime? Expires { get; set; }
    public string? ObjectId { get; set; }
    public string? ObjectInReplyTo { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
