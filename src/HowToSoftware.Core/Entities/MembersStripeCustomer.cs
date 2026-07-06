namespace HowToSoftware.Core.Entities;

public class MembersStripeCustomer
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Member Member { get; set; } = null!;
    public ICollection<MembersStripeCustomerSubscription> Subscriptions { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
