namespace HowToSoftware.Core.Entities;

public class ApFollow
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }

    // Navigation properties
    public ApAccount Follower { get; set; } = null!;
    public ApAccount Following { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
