namespace HowToSoftware.Core.Entities;

public class MembersStripeCustomerSubscription
{
    public string Id { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string? GhostSubscriptionId { get; set; }
    public string SubscriptionId { get; set; } = null!;
    public string StripePriceId { get; set; } = "";
    public string Status { get; set; } = null!;
    public bool CancelAtPeriodEnd { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime StartDate { get; set; }
    public string? DefaultPaymentCardLast4 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int Mrr { get; set; }
    public string? OfferId { get; set; }
    public DateTime? TrialStartAt { get; set; }
    public DateTime? TrialEndAt { get; set; }
    public string PlanId { get; set; } = null!;
    public string PlanNickname { get; set; } = null!;
    public string PlanInterval { get; set; } = null!;
    public int PlanAmount { get; set; }
    public string PlanCurrency { get; set; } = null!;
    public DateTime? DiscountStart { get; set; }
    public DateTime? DiscountEnd { get; set; }

    // Navigation properties
    public MembersStripeCustomer Customer { get; set; } = null!;
    public Subscription? GhostSubscription { get; set; }
    public Offer? Offer { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
