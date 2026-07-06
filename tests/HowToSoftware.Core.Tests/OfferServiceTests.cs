using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Tests;

/// <summary>
/// Tests for IOfferService interface contract compliance.
/// Uses a fake in-memory implementation to validate business rules
/// without requiring Stripe API calls or a real database.
/// </summary>
public class OfferServiceTests
{
    private readonly FakeOfferService _sut;

    public OfferServiceTests()
    {
        _sut = new FakeOfferService();
        // Seed a product for offers to reference
        _sut.SeedProduct(new Product
        {
            Id = "prod-1",
            Name = "Premium",
            Slug = "premium",
            Currency = "usd",
            MonthlyPrice = 500,
            YearlyPrice = 5000,
            Active = true,
            CreatedAt = DateTime.UtcNow,
        });
    }

    // ── CreateOfferAsync ────────────────────────────────────────

    [Fact]
    public async Task CreateOffer_SetsCorrectDefaults()
    {
        var offer = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Launch Discount",
            Code = "launch20",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 20,
            Interval = "month",
            Duration = "once",
        });

        Assert.NotNull(offer);
        Assert.Equal("Launch Discount", offer.Name);
        Assert.Equal("launch20", offer.Code);
        Assert.Equal("prod-1", offer.ProductId);
        Assert.Equal("percent", offer.DiscountType);
        Assert.Equal(20, offer.DiscountAmount);
        Assert.Equal("once", offer.Duration);
        Assert.True(offer.Active);
    }

    [Fact]
    public async Task CreateOffer_NormalizesCodeToLowercase()
    {
        var offer = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Summer Deal",
            Code = "SUMMER2025",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 15,
            Interval = "year",
            Duration = "forever",
        });

        Assert.Equal("summer2025", offer.Code);
    }

    [Fact]
    public async Task CreateOffer_FixedDiscount_SetsCurrency()
    {
        var offer = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Five Off",
            Code = "five-off",
            ProductId = "prod-1",
            DiscountType = "fixed",
            DiscountAmount = 500,
            Interval = "month",
            Duration = "once",
            Currency = "eur",
        });

        Assert.Equal("fixed", offer.DiscountType);
        Assert.Equal(500, offer.DiscountAmount);
        Assert.Equal("eur", offer.Currency);
    }

    [Fact]
    public async Task CreateOffer_RepeatingDuration_SetsDurationInMonths()
    {
        var offer = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Three Months",
            Code = "three-mo",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 50,
            Interval = "month",
            Duration = "repeating",
            DurationInMonths = 3,
        });

        Assert.Equal("repeating", offer.Duration);
        Assert.Equal(3, offer.DurationInMonths);
    }

    [Fact]
    public async Task CreateOffer_DuplicateCode_Throws()
    {
        await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "First",
            Code = "dupe",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 10,
            Interval = "month",
            Duration = "once",
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateOfferAsync(new CreateOfferRequest
            {
                Name = "Second",
                Code = "dupe",
                ProductId = "prod-1",
                DiscountType = "percent",
                DiscountAmount = 20,
                Interval = "month",
                Duration = "once",
            }));
    }

    [Fact]
    public async Task CreateOffer_InvalidProduct_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateOfferAsync(new CreateOfferRequest
            {
                Name = "Bad",
                Code = "bad",
                ProductId = "nonexistent",
                DiscountType = "percent",
                DiscountAmount = 10,
                Interval = "month",
                Duration = "once",
            }));
    }

    // ── GetOfferAsync / GetOffersAsync ──────────────────────────

    [Fact]
    public async Task GetOffers_ReturnsAllOffers()
    {
        await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "A",
            Code = "a",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 10,
            Interval = "month",
            Duration = "once",
        });
        await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "B",
            Code = "b",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 20,
            Interval = "month",
            Duration = "once",
        });

        var all = await _sut.GetOffersAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetOffer_ById_ReturnsCorrectOffer()
    {
        var created = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Find Me",
            Code = "findme",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 10,
            Interval = "month",
            Duration = "once",
        });

        var found = await _sut.GetOfferAsync(created.Id);
        Assert.NotNull(found);
        Assert.Equal("Find Me", found.Name);
    }

    [Fact]
    public async Task GetOffer_NotFound_ReturnsNull()
    {
        var found = await _sut.GetOfferAsync("nonexistent");
        Assert.Null(found);
    }

    // ── GetOfferByCodeAsync ─────────────────────────────────────

    [Fact]
    public async Task GetOfferByCode_ReturnsActiveOffer()
    {
        await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Code Lookup",
            Code = "lookup",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 10,
            Interval = "month",
            Duration = "once",
        });

        var found = await _sut.GetOfferByCodeAsync("lookup");
        Assert.NotNull(found);
        Assert.Equal("Code Lookup", found.Name);
    }

    [Fact]
    public async Task GetOfferByCode_ArchivedOffer_ReturnsNull()
    {
        var offer = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Will Archive",
            Code = "archived",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 10,
            Interval = "month",
            Duration = "once",
        });
        await _sut.ArchiveOfferAsync(offer.Id);

        var found = await _sut.GetOfferByCodeAsync("archived");
        Assert.Null(found);
    }

    // ── UpdateOfferAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdateOffer_ChangesDisplayFields()
    {
        var offer = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Original",
            Code = "update-test",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 10,
            Interval = "month",
            Duration = "once",
        });

        await _sut.UpdateOfferAsync(offer.Id, new UpdateOfferRequest
        {
            Name = "Updated Name",
            PortalTitle = "New Title",
            PortalDescription = "New Desc",
        });

        var updated = await _sut.GetOfferAsync(offer.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("New Title", updated.PortalTitle);
        Assert.Equal("New Desc", updated.PortalDescription);
        // Discount fields should not change
        Assert.Equal(10, updated.DiscountAmount);
    }

    [Fact]
    public async Task UpdateOffer_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateOfferAsync("nonexistent", new UpdateOfferRequest { Name = "X" }));
    }

    // ── ArchiveOfferAsync ───────────────────────────────────────

    [Fact]
    public async Task ArchiveOffer_SetsInactive()
    {
        var offer = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Will Archive",
            Code = "archive",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 10,
            Interval = "month",
            Duration = "once",
        });

        await _sut.ArchiveOfferAsync(offer.Id);

        var archived = await _sut.GetOfferAsync(offer.Id);
        Assert.NotNull(archived);
        Assert.False(archived.Active);
    }

    [Fact]
    public async Task ArchiveOffer_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ArchiveOfferAsync("nonexistent"));
    }

    // ── Redemptions ─────────────────────────────────────────────

    [Fact]
    public async Task RecordRedemption_CreatesRecord()
    {
        var offer = await _sut.CreateOfferAsync(new CreateOfferRequest
        {
            Name = "Redeemable",
            Code = "redeem",
            ProductId = "prod-1",
            DiscountType = "percent",
            DiscountAmount = 10,
            Interval = "month",
            Duration = "once",
        });

        await _sut.RecordRedemptionAsync(offer.Id, "member-1", "sub-1");
        await _sut.RecordRedemptionAsync(offer.Id, "member-2", "sub-2");

        var redemptions = await _sut.GetRedemptionsAsync(offer.Id);
        Assert.Equal(2, redemptions.Count);
    }

    // ── Fake Implementation ─────────────────────────────────────

    private sealed class FakeOfferService : IOfferService
    {
        private readonly List<Offer> _offers = [];
        private readonly List<OfferRedemption> _redemptions = [];
        private readonly List<Product> _products = [];

        public void SeedProduct(Product product) => _products.Add(product);

        public Task<List<Offer>> GetOffersAsync(CancellationToken ct = default)
            => Task.FromResult(_offers.ToList());

        public Task<Offer?> GetOfferAsync(string offerId, CancellationToken ct = default)
            => Task.FromResult(_offers.FirstOrDefault(o => o.Id == offerId));

        public Task<Offer?> GetOfferByCodeAsync(string code, CancellationToken ct = default)
            => Task.FromResult(_offers.FirstOrDefault(o => o.Code == code && o.Active));

        public Task<Offer> CreateOfferAsync(CreateOfferRequest request, CancellationToken ct = default)
        {
            var product = _products.FirstOrDefault(p => p.Id == request.ProductId)
                ?? throw new InvalidOperationException($"Product {request.ProductId} not found");

            if (_offers.Any(o => o.Code == request.Code.Trim().ToLowerInvariant()))
                throw new InvalidOperationException($"Offer code '{request.Code}' is already in use");

            var now = DateTime.UtcNow;
            var offer = new Offer
            {
                Id = Guid.NewGuid().ToString("D"),
                Active = true,
                Name = request.Name.Trim(),
                Code = request.Code.Trim().ToLowerInvariant(),
                ProductId = request.ProductId,
                Interval = request.Interval,
                Currency = request.Currency ?? product.Currency,
                DiscountType = request.DiscountType,
                DiscountAmount = request.DiscountAmount,
                Duration = request.Duration,
                DurationInMonths = request.Duration == "repeating" ? request.DurationInMonths : null,
                PortalTitle = request.PortalTitle,
                PortalDescription = request.PortalDescription,
                CreatedAt = now,
                UpdatedAt = now,
                Product = product,
            };

            _offers.Add(offer);
            return Task.FromResult(offer);
        }

        public Task UpdateOfferAsync(string offerId, UpdateOfferRequest request, CancellationToken ct = default)
        {
            var offer = _offers.FirstOrDefault(o => o.Id == offerId)
                ?? throw new InvalidOperationException($"Offer {offerId} not found");

            if (request.Name is not null) offer.Name = request.Name.Trim();
            if (request.PortalTitle is not null) offer.PortalTitle = request.PortalTitle;
            if (request.PortalDescription is not null) offer.PortalDescription = request.PortalDescription;
            offer.UpdatedAt = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        public Task ArchiveOfferAsync(string offerId, CancellationToken ct = default)
        {
            var offer = _offers.FirstOrDefault(o => o.Id == offerId)
                ?? throw new InvalidOperationException($"Offer {offerId} not found");

            offer.Active = false;
            offer.UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task RecordRedemptionAsync(string offerId, string memberId, string subscriptionId, CancellationToken ct = default)
        {
            _redemptions.Add(new OfferRedemption
            {
                Id = Guid.NewGuid().ToString("D"),
                OfferId = offerId,
                MemberId = memberId,
                SubscriptionId = subscriptionId,
                CreatedAt = DateTime.UtcNow,
            });
            return Task.CompletedTask;
        }

        public Task<List<OfferRedemption>> GetRedemptionsAsync(string offerId, CancellationToken ct = default)
            => Task.FromResult(_redemptions.Where(r => r.OfferId == offerId).ToList());
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
