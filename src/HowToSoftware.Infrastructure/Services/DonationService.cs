using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;
using CheckoutSession = Stripe.Checkout.Session;

namespace HowToSoftware.Infrastructure.Services;

public sealed class DonationService : IDonationService
{
    private readonly AppDbContext _db;
    private readonly StripeSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<DonationService> _logger;

    public DonationService(
        AppDbContext db,
        IOptions<StripeSettings> settings,
        ISettingsService settingsService,
        ILogger<DonationService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _settingsService = settingsService;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<string> CreateDonationCheckoutSessionAsync(
        CreateDonationRequest request, CancellationToken ct = default)
    {
        if (request.AmountInCents <= 0)
            throw new ArgumentException("Donation amount must be positive");

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = request.Email,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = request.AmountInCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Donation",
                            Description = "One-time donation to HowToSoftware",
                        },
                    },
                    Quantity = 1,
                },
            },
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["donation"] = "true",
                ["donor_email"] = request.Email,
                ["donor_name"] = request.Name ?? "",
                ["member_id"] = request.MemberId ?? "",
                ["donation_message"] = request.DonationMessage ?? "",
            },
            SubmitType = "donate",
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: ct);

        _logger.LogInformation(
            "Created donation checkout session {SessionId} for {Email} — {Amount} {Currency}",
            session.Id, LogSanitizer.SanitizeForLog(request.Email), request.AmountInCents,
            LogSanitizer.SanitizeForLog(request.Currency));

        return session.Url;
    }

    public async Task RecordDonationAsync(string eventJson, CancellationToken ct = default)
    {
        var stripeEvent = EventUtility.ParseEvent(eventJson);
        if (stripeEvent.Data.Object is not CheckoutSession session) return;

        // Only process donation checkout sessions
        if (!session.Metadata.TryGetValue("donation", out var isDonation) || isDonation != "true")
            return;

        var email = session.Metadata.GetValueOrDefault("donor_email") ?? session.CustomerEmail ?? "";
        var name = session.Metadata.GetValueOrDefault("donor_name");
        var memberId = session.Metadata.GetValueOrDefault("member_id");
        var donationMessage = session.Metadata.GetValueOrDefault("donation_message");
        if (string.IsNullOrEmpty(memberId)) memberId = null;
        if (string.IsNullOrEmpty(name)) name = null;
        if (string.IsNullOrEmpty(donationMessage)) donationMessage = null;

        var donation = new DonationPaymentEvent
        {
            Id = ObjectIdGenerator.New(),
            Email = email,
            Name = name,
            MemberId = memberId,
            Amount = (int)(session.AmountTotal ?? 0),
            Currency = session.Currency ?? "usd",
            DonationMessage = donationMessage,
            CreatedAt = DateTime.UtcNow,
        };

        _db.DonationPaymentEvents.Add(donation);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Recorded donation {DonationId}: {Amount} {Currency}",
            donation.Id, donation.Amount, donation.Currency);
    }

    public async Task<List<DonationPaymentEvent>> GetDonationsAsync(CancellationToken ct = default)
    {
        return await _db.DonationPaymentEvents
            .Include(d => d.Member)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<DonationSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var currency = await _settingsService.GetStringAsync("donations_currency", ct) ?? "USD";
        var suggestedStr = await _settingsService.GetStringAsync("donations_suggested_amount", ct) ?? "500";
        _ = int.TryParse(suggestedStr, out var suggestedAmount);

        return new DonationSettings
        {
            Currency = currency,
            SuggestedAmountInCents = suggestedAmount > 0 ? suggestedAmount : 500,
        };
    }

    public async Task UpdateSettingsAsync(DonationSettings settings, CancellationToken ct = default)
    {
        await _settingsService.SetAsync("donations_currency", settings.Currency, ct);
        await _settingsService.SetAsync("donations_suggested_amount", settings.SuggestedAmountInCents.ToString(), ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
