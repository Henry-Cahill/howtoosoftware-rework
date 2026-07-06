using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Tests;

/// <summary>
/// Tests for IDonationService interface contract compliance.
/// Uses a fake in-memory implementation to validate business rules
/// without requiring Stripe API calls or a real database.
/// </summary>
public class DonationServiceTests
{
    private readonly FakeDonationService _sut = new();

    // ── CreateDonationCheckoutSessionAsync ───────────────────────

    [Fact]
    public async Task CreateCheckout_ValidRequest_ReturnsUrl()
    {
        var url = await _sut.CreateDonationCheckoutSessionAsync(new CreateDonationRequest
        {
            Email = "donor@example.com",
            AmountInCents = 500,
            Currency = "USD",
            SuccessUrl = "https://example.com/thanks",
            CancelUrl = "https://example.com/cancel",
        });

        Assert.StartsWith("https://", url);
    }

    [Fact]
    public async Task CreateCheckout_ZeroAmount_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateDonationCheckoutSessionAsync(new CreateDonationRequest
            {
                Email = "donor@example.com",
                AmountInCents = 0,
                Currency = "USD",
                SuccessUrl = "https://example.com/thanks",
                CancelUrl = "https://example.com/cancel",
            }));
    }

    [Fact]
    public async Task CreateCheckout_NegativeAmount_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateDonationCheckoutSessionAsync(new CreateDonationRequest
            {
                Email = "donor@example.com",
                AmountInCents = -100,
                Currency = "USD",
                SuccessUrl = "https://example.com/thanks",
                CancelUrl = "https://example.com/cancel",
            }));
    }

    [Fact]
    public async Task CreateCheckout_WithOptionalFields_ReturnsUrl()
    {
        var url = await _sut.CreateDonationCheckoutSessionAsync(new CreateDonationRequest
        {
            Email = "donor@example.com",
            AmountInCents = 2000,
            Currency = "EUR",
            SuccessUrl = "https://example.com/thanks",
            CancelUrl = "https://example.com/cancel",
            Name = "Jane Doe",
            MemberId = "member-123",
            DonationMessage = "Keep up the great work!",
        });

        Assert.StartsWith("https://", url);
    }

    // ── GetDonationsAsync ───────────────────────────────────────

    [Fact]
    public async Task GetDonations_Empty_ReturnsEmptyList()
    {
        var donations = await _sut.GetDonationsAsync();
        Assert.Empty(donations);
    }

    [Fact]
    public async Task GetDonations_AfterRecording_ReturnsDonations()
    {
        _sut.AddDonation("donor@test.com", 1000, "usd");
        _sut.AddDonation("other@test.com", 2500, "eur");

        var donations = await _sut.GetDonationsAsync();

        Assert.Equal(2, donations.Count);
        // Most recent first
        Assert.Equal("other@test.com", donations[0].Email);
        Assert.Equal("donor@test.com", donations[1].Email);
    }

    // ── GetSettingsAsync / UpdateSettingsAsync ───────────────────

    [Fact]
    public async Task GetSettings_ReturnsDefaults()
    {
        var settings = await _sut.GetSettingsAsync();

        Assert.Equal("USD", settings.Currency);
        Assert.Equal(500, settings.SuggestedAmountInCents);
    }

    [Fact]
    public async Task UpdateSettings_PersistsChanges()
    {
        await _sut.UpdateSettingsAsync(new DonationSettings
        {
            Currency = "EUR",
            SuggestedAmountInCents = 1000,
        });

        var settings = await _sut.GetSettingsAsync();

        Assert.Equal("EUR", settings.Currency);
        Assert.Equal(1000, settings.SuggestedAmountInCents);
    }

    [Fact]
    public async Task GetDonations_OrderedByMostRecent()
    {
        _sut.AddDonation("first@test.com", 100, "usd", DateTime.UtcNow.AddHours(-2));
        _sut.AddDonation("second@test.com", 200, "usd", DateTime.UtcNow.AddHours(-1));
        _sut.AddDonation("third@test.com", 300, "usd", DateTime.UtcNow);

        var donations = await _sut.GetDonationsAsync();

        Assert.Equal("third@test.com", donations[0].Email);
        Assert.Equal("second@test.com", donations[1].Email);
        Assert.Equal("first@test.com", donations[2].Email);
    }

    // ════════════════════════════════════════════════════════════
    // Fake implementation
    // ════════════════════════════════════════════════════════════

    private sealed class FakeDonationService : IDonationService
    {
        private readonly List<DonationPaymentEvent> _donations = [];
        private DonationSettings _settings = new();

        public Task<string> CreateDonationCheckoutSessionAsync(
            CreateDonationRequest request, CancellationToken ct = default)
        {
            if (request.AmountInCents <= 0)
                throw new ArgumentException("Donation amount must be positive");

            return Task.FromResult($"https://checkout.stripe.com/donate/{Guid.NewGuid()}");
        }

        public Task RecordDonationAsync(string eventJson, CancellationToken ct = default)
        {
            // In tests, use AddDonation helper instead
            return Task.CompletedTask;
        }

        public Task<List<DonationPaymentEvent>> GetDonationsAsync(CancellationToken ct = default)
        {
            var sorted = _donations.OrderByDescending(d => d.CreatedAt).ToList();
            return Task.FromResult(sorted);
        }

        public Task<DonationSettings> GetSettingsAsync(CancellationToken ct = default)
            => Task.FromResult(_settings);

        public Task UpdateSettingsAsync(DonationSettings settings, CancellationToken ct = default)
        {
            _settings = settings;
            return Task.CompletedTask;
        }

        public void AddDonation(string email, int amount, string currency, DateTime? createdAt = null)
        {
            _donations.Add(new DonationPaymentEvent
            {
                Id = Guid.NewGuid().ToString("D"),
                Email = email,
                Amount = amount,
                Currency = currency,
                CreatedAt = createdAt ?? DateTime.UtcNow,
            });
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
