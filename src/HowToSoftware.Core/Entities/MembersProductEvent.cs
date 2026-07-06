namespace HowToSoftware.Core.Entities;

public class MembersProductEvent
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public string? Action { get; set; }
    public DateTime CreatedAt { get; set; }

    public Member Member { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
