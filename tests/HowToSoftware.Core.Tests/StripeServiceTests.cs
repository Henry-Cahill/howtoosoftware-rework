using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Tests;

/// <summary>
/// Tests for IStripeService interface contract compliance.
/// Uses a fake in-memory implementation to validate business rules
/// without requiring Stripe API calls or a real database.
/// </summary>
public class StripeServiceTests
{
    private readonly FakeStripeService _sut = new();

    // ── CreateProductAsync ──────────────────────────────────────

    [Fact]
    public async Task CreateProduct_SetsCorrectDefaults()
    {
        var product = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Premium",
            Currency = "usd",
            MonthlyPrice = 500,
            YearlyPrice = 5000,
        });

        Assert.NotNull(product);
        Assert.Equal("Premium", product.Name);
        Assert.Equal("usd", product.Currency);
        Assert.Equal(500, product.MonthlyPrice);
        Assert.Equal(5000, product.YearlyPrice);
        Assert.True(product.Active);
        Assert.Equal("paid", product.Type);
        Assert.Equal("public", product.Visibility);
    }

    [Fact]
    public async Task CreateProduct_WithBenefits_CreatesLinkedBenefits()
    {
        var product = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Pro",
            Currency = "usd",
            MonthlyPrice = 1000,
            YearlyPrice = 10000,
            BenefitNames = ["Full access", "Priority support"],
        });

        Assert.Equal(2, product.ProductsBenefits.Count);
        var names = product.ProductsBenefits.OrderBy(b => b.SortOrder).Select(b => b.Benefit.Name).ToList();
        Assert.Equal("Full access", names[0]);
        Assert.Equal("Priority support", names[1]);
    }

    [Fact]
    public async Task CreateProduct_WithTrial_SetsTrialDays()
    {
        var product = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Trial Tier",
            Currency = "usd",
            MonthlyPrice = 500,
            YearlyPrice = 5000,
            TrialDays = 14,
        });

        Assert.Equal(14, product.TrialDays);
    }

    [Fact]
    public async Task CreateProduct_GeneratesSlug()
    {
        var product = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "My Premium Tier",
            Currency = "usd",
            MonthlyPrice = 500,
            YearlyPrice = 5000,
        });

        Assert.False(string.IsNullOrEmpty(product.Slug));
    }

    // ── GetProductsAsync ────────────────────────────────────────

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Free",
            Currency = "usd",
            MonthlyPrice = 0,
            YearlyPrice = 0,
        });
        await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Premium",
            Currency = "usd",
            MonthlyPrice = 500,
            YearlyPrice = 5000,
        });

        var all = await _sut.GetProductsAsync();
        Assert.Equal(2, all.Count);
    }

    // ── UpdateProductAsync ──────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_ChangesFields()
    {
        var product = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Basic",
            Currency = "usd",
            MonthlyPrice = 500,
            YearlyPrice = 5000,
        });

        await _sut.UpdateProductAsync(product.Id, new UpdateProductRequest
        {
            Name = "Basic Plus",
            MonthlyPrice = 700,
        });

        var updated = await _sut.GetProductAsync(product.Id);
        Assert.NotNull(updated);
        Assert.Equal("Basic Plus", updated.Name);
        Assert.Equal(700, updated.MonthlyPrice);
        Assert.Equal(5000, updated.YearlyPrice); // Unchanged
    }

    [Fact]
    public async Task UpdateProduct_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateProductAsync("nonexistent", new UpdateProductRequest { Name = "X" }));
    }

    // ── ArchiveProductAsync ─────────────────────────────────────

    [Fact]
    public async Task ArchiveProduct_SetsInactive()
    {
        var product = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Will Archive",
            Currency = "usd",
            MonthlyPrice = 500,
            YearlyPrice = 5000,
        });

        await _sut.ArchiveProductAsync(product.Id);

        var archived = await _sut.GetProductAsync(product.Id);
        Assert.NotNull(archived);
        Assert.False(archived.Active);
    }

    [Fact]
    public async Task ArchiveProduct_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ArchiveProductAsync("nonexistent"));
    }

    // ── GetSubscriberCountsAsync ────────────────────────────────

    [Fact]
    public async Task GetSubscriberCounts_ReturnsCountsByProduct()
    {
        var basic = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Basic", Currency = "usd", MonthlyPrice = 500, YearlyPrice = 5000,
        });
        var pro = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Pro", Currency = "usd", MonthlyPrice = 1000, YearlyPrice = 10000,
        });
        _sut.AddSubscriber(basic.Id, "m1");
        _sut.AddSubscriber(basic.Id, "m2");
        _sut.AddSubscriber(pro.Id, "m3");

        var counts = await _sut.GetSubscriberCountsAsync();

        Assert.Equal(2, counts[basic.Id]);
        Assert.Equal(1, counts[pro.Id]);
    }

    [Fact]
    public async Task GetSubscriberCounts_ExcludesExpiredEntitlements()
    {
        var tier = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Lifetime", Currency = "usd", MonthlyPrice = 0, YearlyPrice = 0,
        });
        _sut.AddSubscriber(tier.Id, "m-active");
        _sut.AddSubscriber(tier.Id, "m-future", DateTime.UtcNow.AddDays(30));
        _sut.AddSubscriber(tier.Id, "m-expired", DateTime.UtcNow.AddDays(-1));

        var counts = await _sut.GetSubscriberCountsAsync();

        // Only the perpetual + future-dated entitlements count.
        Assert.Equal(2, counts[tier.Id]);
    }

    [Fact]
    public async Task GetSubscriberCounts_OmitsProductsWithNoSubscribers()
    {
        var tier = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "Empty", Currency = "usd", MonthlyPrice = 0, YearlyPrice = 0,
        });

        var counts = await _sut.GetSubscriberCountsAsync();

        Assert.False(counts.ContainsKey(tier.Id));
    }

    [Fact]
    public async Task GetSubscriberCounts_FiltersByProductIds()
    {
        var a = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "A", Currency = "usd", MonthlyPrice = 0, YearlyPrice = 0,
        });
        var b = await _sut.CreateProductAsync(new CreateProductRequest
        {
            Name = "B", Currency = "usd", MonthlyPrice = 0, YearlyPrice = 0,
        });
        _sut.AddSubscriber(a.Id, "m1");
        _sut.AddSubscriber(b.Id, "m2");

        var counts = await _sut.GetSubscriberCountsAsync([a.Id]);

        Assert.Single(counts);
        Assert.Equal(1, counts[a.Id]);
    }

    // ── MRR Calculation ─────────────────────────────────────────

    [Theory]
    [InlineData(1000, "month", 1000)]
    [InlineData(12000, "year", 1000)]
    [InlineData(100, "week", 400)]
    public void CalculateMrr_CorrectValues(int amount, string interval, int expectedMrr)
    {
        var result = FakeStripeService.TestCalculateMrr(amount, interval);
        Assert.Equal(expectedMrr, result);
    }

    // ── Fake Implementation ─────────────────────────────────────

    private sealed class FakeStripeService : IStripeService
    {
        private readonly List<Product> _products = [];
        private readonly List<Benefit> _benefits = [];
        private readonly List<(string ProductId, string MemberId, DateTime? ExpiryAt)> _subscribers = [];

        public Task<List<Product>> GetProductsAsync(CancellationToken ct = default)
            => Task.FromResult(_products.ToList());

        public Task<Product?> GetProductAsync(string productId, CancellationToken ct = default)
            => Task.FromResult(_products.FirstOrDefault(p => p.Id == productId));

        public Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var product = new Product
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = request.Name,
                Slug = request.Name.ToLowerInvariant().Replace(" ", "-"),
                Description = request.Description,
                Currency = request.Currency,
                MonthlyPrice = request.MonthlyPrice,
                YearlyPrice = request.YearlyPrice,
                TrialDays = request.TrialDays,
                WelcomePageUrl = request.WelcomePageUrl,
                Type = "paid",
                Active = true,
                Visibility = "public",
                CreatedAt = now,
                UpdatedAt = now,
            };

            if (request.BenefitNames is { Count: > 0 })
            {
                for (var i = 0; i < request.BenefitNames.Count; i++)
                {
                    var benefit = _benefits.FirstOrDefault(b => b.Name == request.BenefitNames[i])
                        ?? new Benefit
                        {
                            Id = Guid.NewGuid().ToString("D"),
                            Name = request.BenefitNames[i],
                            Slug = request.BenefitNames[i].ToLowerInvariant().Replace(" ", "-"),
                            CreatedAt = now,
                        };
                    if (!_benefits.Contains(benefit))
                        _benefits.Add(benefit);

                    product.ProductsBenefits.Add(new ProductsBenefit
                    {
                        Id = Guid.NewGuid().ToString("D"),
                        ProductId = product.Id,
                        BenefitId = benefit.Id,
                        Benefit = benefit,
                        SortOrder = i,
                    });
                }
            }

            _products.Add(product);
            return Task.FromResult(product);
        }

        public Task UpdateProductAsync(string productId, UpdateProductRequest request, CancellationToken ct = default)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId)
                ?? throw new InvalidOperationException($"Product {productId} not found");

            if (request.Name is not null) product.Name = request.Name;
            if (request.Description is not null) product.Description = request.Description;
            if (request.Currency is not null) product.Currency = request.Currency;
            if (request.MonthlyPrice.HasValue) product.MonthlyPrice = request.MonthlyPrice.Value;
            if (request.YearlyPrice.HasValue) product.YearlyPrice = request.YearlyPrice.Value;
            if (request.TrialDays.HasValue) product.TrialDays = request.TrialDays.Value;
            if (request.SortOrder.HasValue) product.SortOrder = request.SortOrder.Value;
            if (request.Active.HasValue) product.Active = request.Active.Value;
            product.UpdatedAt = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        public Task ArchiveProductAsync(string productId, CancellationToken ct = default)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId)
                ?? throw new InvalidOperationException($"Product {productId} not found");

            product.Active = false;
            product.UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task ReorderProductsAsync(IReadOnlyList<string> orderedProductIds, CancellationToken ct = default)
        {
            for (var i = 0; i < orderedProductIds.Count; i++)
            {
                var product = _products.FirstOrDefault(p => p.Id == orderedProductIds[i]);
                if (product is null) continue;
                product.SortOrder = i;
                product.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, int>> GetSubscriberCountsAsync(
            IReadOnlyCollection<string>? productIds = null, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var query = _subscribers.Where(s => s.ExpiryAt == null || s.ExpiryAt > now);
            if (productIds is { Count: > 0 })
            {
                var ids = productIds.Where(s => !string.IsNullOrWhiteSpace(s)).ToHashSet();
                if (ids.Count == 0) return Task.FromResult(new Dictionary<string, int>());
                query = query.Where(s => ids.Contains(s.ProductId));
            }
            return Task.FromResult(
                query.GroupBy(s => s.ProductId)
                     .ToDictionary(g => g.Key, g => g.Count()));
        }

        public void AddSubscriber(string productId, string memberId, DateTime? expiryAt = null)
            => _subscribers.Add((productId, memberId, expiryAt));

        public Task SyncProductToStripeAsync(string productId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<string> CreateCheckoutSessionAsync(
            string memberId, string productId, string cadence,
            string successUrl, string cancelUrl,
            string? offerId = null, CancellationToken ct = default)
            => Task.FromResult($"https://checkout.stripe.com/test/{Guid.NewGuid()}");

        public Task<string> CreatePortalSessionAsync(string memberId, string returnUrl, CancellationToken ct = default)
            => Task.FromResult($"https://billing.stripe.com/test/{Guid.NewGuid()}");

        public Task HandleWebhookEventAsync(string eventType, string eventJson, CancellationToken ct = default)
            => Task.CompletedTask;

        public static int TestCalculateMrr(int amount, string interval)
        {
            return interval switch
            {
                "year" => amount / 12,
                "month" => amount,
                "week" => amount * 4,
                "day" => amount * 30,
                _ => amount,
            };
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
