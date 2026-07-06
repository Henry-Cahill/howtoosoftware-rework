using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IDonationService
{
    /// <summary>
    /// Creates a Stripe Checkout session for a one-time donation.
    /// Returns the checkout URL to redirect the donor to.
    /// </summary>
    Task<string> CreateDonationCheckoutSessionAsync(
        CreateDonationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Records a completed donation payment event from a Stripe webhook.
    /// </summary>
    Task RecordDonationAsync(string eventJson, CancellationToken ct = default);

    /// <summary>Gets all donation payment events, most recent first.</summary>
    Task<List<DonationPaymentEvent>> GetDonationsAsync(CancellationToken ct = default);

    /// <summary>Gets donation configuration (currency, suggested amount).</summary>
    Task<DonationSettings> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>Updates donation configuration.</summary>
    Task UpdateSettingsAsync(DonationSettings settings, CancellationToken ct = default);
}

public record CreateDonationRequest
{
    public required string Email { get; init; }
    public required int AmountInCents { get; init; }
    public required string Currency { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public string? Name { get; init; }
    public string? MemberId { get; init; }
    public string? DonationMessage { get; init; }
}

public record DonationSettings
{
    public string Currency { get; init; } = "USD";
    public int SuggestedAmountInCents { get; init; } = 500;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
