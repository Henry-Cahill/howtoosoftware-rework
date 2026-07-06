namespace HowToSoftware.Core.Entities;

public class ApAccountDeliveryBackoff
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int AccountId { get; set; }
    public DateTime LastFailureAt { get; set; }
    public string? LastFailureReason { get; set; }
    public DateTime BackoffUntil { get; set; }
    public int BackoffSeconds { get; set; } = 60;

    // Navigation properties
    public ApAccount Account { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
