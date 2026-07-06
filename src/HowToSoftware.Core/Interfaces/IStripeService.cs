using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IStripeService
{
    // ── Products / Tiers ────────────────────────────────────────

    /// <summary>
    /// Ensures the local Product has a corresponding Stripe product + prices.
    /// Creates them in Stripe if they don't exist, or updates if they've changed.
    /// </summary>
    Task SyncProductToStripeAsync(string productId, CancellationToken ct = default);

    /// <summary>Gets all local products with their Stripe mapping status.</summary>
    Task<List<Product>> GetProductsAsync(CancellationToken ct = default);

    /// <summary>Gets a single product with Stripe and benefit details.</summary>
    Task<Product?> GetProductAsync(string productId, CancellationToken ct = default);

    /// <summary>Creates a new product/tier locally and syncs to Stripe.</summary>
    Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);

    /// <summary>Updates a product/tier and syncs changes to Stripe.</summary>
    Task UpdateProductAsync(string productId, UpdateProductRequest request, CancellationToken ct = default);

    /// <summary>Archives a product (sets active=false) and archives in Stripe.</summary>
    Task ArchiveProductAsync(string productId, CancellationToken ct = default);

    /// <summary>
    /// Replaces the SortOrder of every product in <paramref name="orderedProductIds"/>
    /// with its index in the list. Products not present in the list keep their
    /// existing SortOrder. Determines the display order on the public pricing page.
    /// </summary>
    Task ReorderProductsAsync(IReadOnlyList<string> orderedProductIds, CancellationToken ct = default);

    /// <summary>
    /// Returns the count of currently-entitled members per product (active subscribers).
    /// "Active" means a row in members_products whose ExpiryAt is null (perpetual access)
    /// or in the future. Products with zero subscribers are omitted from the result.
    /// If <paramref name="productIds"/> is non-null, only those products are considered.
    /// </summary>
    Task<Dictionary<string, int>> GetSubscriberCountsAsync(
        IReadOnlyCollection<string>? productIds = null, CancellationToken ct = default);

    // ── Checkout ────────────────────────────────────────────────

    /// <summary>
    /// Creates a Stripe Checkout session for a member subscribing to a product tier.
    /// Returns the session URL the member should be redirected to.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        string memberId, string productId, string cadence,
        string successUrl, string cancelUrl,
        string? offerId = null, CancellationToken ct = default);

    // ── Customer Portal ─────────────────────────────────────────

    /// <summary>
    /// Creates a Stripe Billing Portal session so members can manage their subscription.
    /// Returns the portal URL.
    /// </summary>
    Task<string> CreatePortalSessionAsync(string memberId, string returnUrl, CancellationToken ct = default);

    // ── Webhook Processing ──────────────────────────────────────

    /// <summary>
    /// Processes an incoming Stripe webhook event. Called by the webhook controller
    /// after signature verification.
    /// </summary>
    Task HandleWebhookEventAsync(string eventType, string eventJson, CancellationToken ct = default);
}

public record CreateProductRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Currency { get; init; }
    public required int MonthlyPrice { get; init; }
    public required int YearlyPrice { get; init; }
    public int TrialDays { get; init; }
    public string? WelcomePageUrl { get; init; }
    public List<string>? BenefitNames { get; init; }
}

public record UpdateProductRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Currency { get; init; }
    public int? MonthlyPrice { get; init; }
    public int? YearlyPrice { get; init; }
    public int? TrialDays { get; init; }
    public string? WelcomePageUrl { get; init; }
    public List<string>? BenefitNames { get; init; }
    public bool? Active { get; init; }
    public int? SortOrder { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
