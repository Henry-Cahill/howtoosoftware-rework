using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;
using Product = HowToSoftware.Core.Entities.Product;
using Subscription = HowToSoftware.Core.Entities.Subscription;
using CheckoutSession = Stripe.Checkout.Session;

namespace HowToSoftware.Infrastructure.Services;

public sealed class StripeService : IStripeService
{
    private readonly AppDbContext _db;
    private readonly StripeSettings _settings;
    private readonly ISlugGenerator _slugs;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        AppDbContext db,
        IOptions<StripeSettings> settings,
        ISlugGenerator slugs,
        ILogger<StripeService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _slugs = slugs;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    // ================================================================
    // Products / Tiers
    // ================================================================

    public async Task<List<Product>> GetProductsAsync(CancellationToken ct = default)
    {
        return await _db.Products
            .Include(p => p.StripeProducts)
            .Include(p => p.ProductsBenefits).ThenInclude(pb => pb.Benefit)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Product?> GetProductAsync(string productId, CancellationToken ct = default)
    {
        return await _db.Products
            .Include(p => p.StripeProducts).ThenInclude(sp => sp.Prices)
            .Include(p => p.ProductsBenefits).ThenInclude(pb => pb.Benefit)
            .Include(p => p.Offers)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var id = ObjectIdGenerator.New();
        var slug = _slugs.GenerateSlug(request.Name);

        // New tiers append to the end of the public pricing display.
        var maxSortOrder = await _db.Products
            .Select(p => (int?)p.SortOrder)
            .MaxAsync(ct) ?? -1;

        var product = new Product
        {
            Id = id,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            Currency = request.Currency,
            MonthlyPrice = request.MonthlyPrice,
            YearlyPrice = request.YearlyPrice,
            TrialDays = request.TrialDays,
            WelcomePageUrl = request.WelcomePageUrl,
            Type = "paid",
            Active = true,
            Visibility = "public",
            SortOrder = maxSortOrder + 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Products.Add(product);

        // Create benefits
        if (request.BenefitNames is { Count: > 0 })
        {
            for (var i = 0; i < request.BenefitNames.Count; i++)
            {
                var benefitName = request.BenefitNames[i];
                var benefit = await _db.Benefits.FirstOrDefaultAsync(b => b.Name == benefitName, ct)
                    ?? new Benefit
                    {
                        Id = ObjectIdGenerator.New(),
                        Name = benefitName,
                        Slug = _slugs.GenerateSlug(benefitName),
                        CreatedAt = now,
                    };

                if (string.IsNullOrEmpty(benefit.Id) || !await _db.Benefits.AnyAsync(b => b.Id == benefit.Id, ct))
                    _db.Benefits.Add(benefit);

                _db.ProductsBenefits.Add(new ProductsBenefit
                {
                    Id = ObjectIdGenerator.New(),
                    ProductId = product.Id,
                    BenefitId = benefit.Id,
                    SortOrder = i,
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        await SyncProductToStripeAsync(product.Id, ct);

        _logger.LogInformation("Created product {ProductId} ({Name})", product.Id, product.Name);
        return product;
    }

    public async Task UpdateProductAsync(string productId, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.ProductsBenefits)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new InvalidOperationException($"Product {productId} not found");

        var now = DateTime.UtcNow;

        if (request.Name is not null) product.Name = request.Name;
        if (request.Description is not null) product.Description = request.Description;
        if (request.Currency is not null) product.Currency = request.Currency;
        if (request.MonthlyPrice.HasValue) product.MonthlyPrice = request.MonthlyPrice.Value;
        if (request.YearlyPrice.HasValue) product.YearlyPrice = request.YearlyPrice.Value;
        if (request.TrialDays.HasValue) product.TrialDays = request.TrialDays.Value;
        if (request.WelcomePageUrl is not null) product.WelcomePageUrl = request.WelcomePageUrl;
        if (request.Active.HasValue) product.Active = request.Active.Value;
        if (request.SortOrder.HasValue) product.SortOrder = request.SortOrder.Value;
        product.UpdatedAt = now;

        // Update benefits if provided
        if (request.BenefitNames is not null)
        {
            _db.ProductsBenefits.RemoveRange(product.ProductsBenefits);

            for (var i = 0; i < request.BenefitNames.Count; i++)
            {
                var benefitName = request.BenefitNames[i];
                var benefit = await _db.Benefits.FirstOrDefaultAsync(b => b.Name == benefitName, ct)
                    ?? new Benefit
                    {
                        Id = ObjectIdGenerator.New(),
                        Name = benefitName,
                        Slug = _slugs.GenerateSlug(benefitName),
                        CreatedAt = now,
                    };

                if (!await _db.Benefits.AnyAsync(b => b.Id == benefit.Id, ct))
                    _db.Benefits.Add(benefit);

                _db.ProductsBenefits.Add(new ProductsBenefit
                {
                    Id = ObjectIdGenerator.New(),
                    ProductId = product.Id,
                    BenefitId = benefit.Id,
                    SortOrder = i,
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        await SyncProductToStripeAsync(product.Id, ct);

        _logger.LogInformation("Updated product {ProductId}", productId);
    }

    public async Task ArchiveProductAsync(string productId, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.StripeProducts)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new InvalidOperationException($"Product {productId} not found");

        product.Active = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Archive in Stripe
        var productService = new ProductService();
        foreach (var sp in product.StripeProducts)
        {
            try
            {
                await productService.UpdateAsync(sp.StripeProductId, new ProductUpdateOptions { Active = false },
                    cancellationToken: ct);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Failed to archive Stripe product {StripeProductId}", sp.StripeProductId);
            }
        }

        _logger.LogInformation("Archived product {ProductId}", productId);
    }

    public async Task ReorderProductsAsync(IReadOnlyList<string> orderedProductIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(orderedProductIds);
        if (orderedProductIds.Count == 0) return;

        var ids = orderedProductIds.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var products = await _db.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        for (var i = 0; i < ids.Count; i++)
        {
            var product = products.FirstOrDefault(p => p.Id == ids[i]);
            if (product is null) continue;
            if (product.SortOrder == i) continue;
            product.SortOrder = i;
            product.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Reordered {Count} products", products.Count);
    }

    public async Task<Dictionary<string, int>> GetSubscriberCountsAsync(
        IReadOnlyCollection<string>? productIds = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var query = _db.MembersProducts
            .Where(mp => mp.ExpiryAt == null || mp.ExpiryAt > now);

        if (productIds is { Count: > 0 })
        {
            var ids = productIds.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (ids.Count == 0) return [];
            query = query.Where(mp => ids.Contains(mp.ProductId));
        }

        return await query
            .GroupBy(mp => mp.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count, ct);
    }

    public async Task SyncProductToStripeAsync(string productId, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.StripeProducts).ThenInclude(sp => sp.Prices)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new InvalidOperationException($"Product {productId} not found");

        if (string.IsNullOrEmpty(_settings.SecretKey))
        {
            _logger.LogWarning("Stripe secret key not configured — skipping sync for product {ProductId}", productId);
            return;
        }

        var productService = new ProductService();
        var priceService = new PriceService();
        var now = DateTime.UtcNow;

        // 1. Ensure Stripe Product exists
        var stripeProduct = product.StripeProducts.FirstOrDefault();
        string stripeProductId;

        if (stripeProduct is null)
        {
            var created = await productService.CreateAsync(new ProductCreateOptions
            {
                Name = product.Name,
                Description = product.Description,
                Active = product.Active,
                Metadata = new Dictionary<string, string> { ["ghost_product_id"] = product.Id },
            }, cancellationToken: ct);

            stripeProductId = created.Id;

            stripeProduct = new StripeProduct
            {
                Id = ObjectIdGenerator.New(),
                ProductId = product.Id,
                StripeProductId = stripeProductId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.StripeProducts.Add(stripeProduct);
        }
        else
        {
            stripeProductId = stripeProduct.StripeProductId;
            await productService.UpdateAsync(stripeProductId, new ProductUpdateOptions
            {
                Name = product.Name,
                Description = product.Description,
                Active = product.Active,
            }, cancellationToken: ct);
            stripeProduct.UpdatedAt = now;
        }

        // 2. Ensure monthly price exists
        await EnsurePriceAsync(stripeProduct, product, priceService, "month",
            product.MonthlyPrice ?? 0, product.Currency ?? "usd", ct);

        // 3. Ensure yearly price exists
        await EnsurePriceAsync(stripeProduct, product, priceService, "year",
            product.YearlyPrice ?? 0, product.Currency ?? "usd", ct);

        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsurePriceAsync(
        StripeProduct stripeProduct, Product product,
        PriceService priceService, string interval,
        int amount, string currency, CancellationToken ct)
    {
        var existing = stripeProduct.Prices.FirstOrDefault(p => p.Interval == interval && p.Active);

        if (existing is not null && existing.Amount == amount && existing.Currency == currency)
            return; // No change needed

        // Archive old price if amount changed
        if (existing is not null)
        {
            await priceService.UpdateAsync(existing.StripePriceId,
                new PriceUpdateOptions { Active = false }, cancellationToken: ct);
            existing.Active = false;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        // Create new price
        var created = await priceService.CreateAsync(new PriceCreateOptions
        {
            Product = stripeProduct.StripeProductId,
            UnitAmount = amount,
            Currency = currency,
            Recurring = new PriceRecurringOptions { Interval = interval },
            Metadata = new Dictionary<string, string>
            {
                ["ghost_product_id"] = product.Id,
                ["cadence"] = interval == "month" ? "monthly" : "yearly",
            },
        }, cancellationToken: ct);

        var now = DateTime.UtcNow;
        var newPrice = new StripePrice
        {
            Id = ObjectIdGenerator.New(),
            StripePriceId = created.Id,
            StripeProductId = stripeProduct.StripeProductId,
            Active = true,
            Nickname = $"{product.Name} ({interval}ly)",
            Currency = currency,
            Amount = amount,
            Type = "recurring",
            Interval = interval,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.StripePrices.Add(newPrice);

        // Update product's price ID references
        if (interval == "month")
            product.MonthlyPriceId = newPrice.Id;
        else
            product.YearlyPriceId = newPrice.Id;
    }

    // ================================================================
    // Checkout
    // ================================================================

    public async Task<string> CreateCheckoutSessionAsync(
        string memberId, string productId, string cadence,
        string successUrl, string cancelUrl,
        string? offerId = null, CancellationToken ct = default)
    {
        var member = await _db.Members.FindAsync([memberId], ct)
            ?? throw new InvalidOperationException($"Member {memberId} not found");

        var product = await _db.Products
            .Include(p => p.StripeProducts).ThenInclude(sp => sp.Prices)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new InvalidOperationException($"Product {productId} not found");

        var stripeProduct = product.StripeProducts.FirstOrDefault()
            ?? throw new InvalidOperationException($"Product {productId} not synced to Stripe");

        var interval = cadence == "yearly" ? "year" : "month";
        var price = stripeProduct.Prices.FirstOrDefault(p => p.Interval == interval && p.Active)
            ?? throw new InvalidOperationException($"No active {interval}ly price for product {productId}");

        // Get or create Stripe customer
        var customerId = await GetOrCreateStripeCustomerAsync(member, ct);

        var sessionOptions = new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = price.StripePriceId,
                    Quantity = 1,
                },
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["member_id"] = memberId,
                ["product_id"] = productId,
                ["cadence"] = cadence,
                ["offer_id"] = offerId ?? "",
            },
            AllowPromotionCodes = offerId is null, // Don't allow promo codes if an offer is already applied
        };

        // Apply trial if configured
        if (product.TrialDays > 0)
        {
            sessionOptions.SubscriptionData = new SessionSubscriptionDataOptions
            {
                TrialPeriodDays = product.TrialDays,
            };
        }

        // Apply specific offer/coupon
        if (offerId is not null)
        {
            var offer = await _db.Offers.FindAsync([offerId], ct);
            if (offer?.StripeCouponId is not null)
            {
                sessionOptions.Discounts = new List<SessionDiscountOptions>
                {
                    new SessionDiscountOptions { Coupon = offer.StripeCouponId },
                };
            }
        }

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: ct);

        _logger.LogInformation(
            "Created checkout session {SessionId} for member {MemberId} → product {ProductId} ({Cadence})",
            session.Id, memberId, productId, cadence);

        return session.Url;
    }

    // ================================================================
    // Customer Portal
    // ================================================================

    public async Task<string> CreatePortalSessionAsync(string memberId, string returnUrl, CancellationToken ct = default)
    {
        var stripeCustomer = await _db.MembersStripeCustomers
            .FirstOrDefaultAsync(c => c.MemberId == memberId, ct)
            ?? throw new InvalidOperationException($"No Stripe customer for member {memberId}");

        var portalService = new Stripe.BillingPortal.SessionService();
        var session = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomer.CustomerId,
            ReturnUrl = returnUrl,
        }, cancellationToken: ct);

        return session.Url;
    }

    // ================================================================
    // Webhook Event Processing
    // ================================================================

    public async Task HandleWebhookEventAsync(string eventType, string eventJson, CancellationToken ct = default)
    {
        switch (eventType)
        {
            case "checkout.session.completed":
                // Check if this is a donation checkout — if so, skip subscription handling
                if (IsDonationCheckout(eventJson))
                    break;
                await HandleCheckoutCompleted(eventJson, ct);
                break;

            case "customer.subscription.updated":
                await HandleSubscriptionUpdated(eventJson, ct);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeleted(eventJson, ct);
                break;

            case "invoice.payment_succeeded":
                await HandlePaymentSucceeded(eventJson, ct);
                break;

            case "invoice.payment_failed":
                _logger.LogWarning("Invoice payment failed — event logged for review");
                break;

            default:
                _logger.LogDebug("Ignoring Stripe webhook event: {EventType}", eventType);
                break;
        }
    }

    private async Task HandleCheckoutCompleted(string eventJson, CancellationToken ct)
    {
        var stripeEvent = EventUtility.ParseEvent(eventJson);
        if (stripeEvent.Data.Object is not CheckoutSession session) return;

        var memberId = session.Metadata.GetValueOrDefault("member_id");
        var productId = session.Metadata.GetValueOrDefault("product_id");
        var cadence = session.Metadata.GetValueOrDefault("cadence");
        var offerId = session.Metadata.GetValueOrDefault("offer_id");
        if (string.IsNullOrEmpty(offerId)) offerId = null;

        if (string.IsNullOrEmpty(memberId) || string.IsNullOrEmpty(productId)) return;

        var member = await _db.Members.FindAsync([memberId], ct);
        if (member is null) return;

        var now = DateTime.UtcNow;
        var subscriptionId = session.SubscriptionId;

        if (string.IsNullOrEmpty(subscriptionId)) return;

        // Fetch full subscription from Stripe
        var subService = new SubscriptionService();
        var stripeSub = await subService.GetAsync(subscriptionId, cancellationToken: ct);

        // Create local subscription record
        var localSub = new Subscription
        {
            Id = ObjectIdGenerator.New(),
            Type = "paid",
            Status = stripeSub.Status,
            MemberId = memberId,
            TierId = productId,
            Cadence = cadence,
            Currency = stripeSub.Currency,
            Amount = (int)(stripeSub.Items.Data.FirstOrDefault()?.Price.UnitAmount ?? 0L),
            PaymentProvider = "stripe",
            PaymentSubscriptionUrl = $"https://dashboard.stripe.com/subscriptions/{subscriptionId}",
            OfferId = offerId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Subscriptions.Add(localSub);

        // Ensure MembersStripeCustomer exists
        var stripeCustomer = await _db.MembersStripeCustomers
            .FirstOrDefaultAsync(c => c.MemberId == memberId, ct);

        if (stripeCustomer is null)
        {
            stripeCustomer = new MembersStripeCustomer
            {
                Id = ObjectIdGenerator.New(),
                MemberId = memberId,
                CustomerId = session.CustomerId,
                Name = member.Name,
                Email = member.Email,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.MembersStripeCustomers.Add(stripeCustomer);
            await _db.SaveChangesAsync(ct); // Save so FK works
        }

        // Create stripe subscription record
        var priceItem = stripeSub.Items.Data.FirstOrDefault();
        var mscSub = new MembersStripeCustomerSubscription
        {
            Id = ObjectIdGenerator.New(),
            CustomerId = stripeCustomer.CustomerId,
            GhostSubscriptionId = localSub.Id,
            SubscriptionId = subscriptionId,
            StripePriceId = priceItem?.Price.Id ?? "",
            Status = stripeSub.Status,
            CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd,
            CurrentPeriodEnd = stripeSub.CurrentPeriodEnd,
            StartDate = stripeSub.StartDate,
            CreatedAt = now,
            UpdatedAt = now,
            PlanId = priceItem?.Price.Id ?? "",
            PlanNickname = priceItem?.Price.Nickname ?? "",
            PlanInterval = priceItem?.Price.Recurring?.Interval ?? "month",
            PlanAmount = (int)(priceItem?.Price.UnitAmount ?? 0L),
            PlanCurrency = stripeSub.Currency ?? "usd",
            Mrr = CalculateMrr(
                (int)(priceItem?.Price.UnitAmount ?? 0L),
                priceItem?.Price.Recurring?.Interval ?? "month"),
        };

        if (stripeSub.TrialStart.HasValue)
        {
            mscSub.TrialStartAt = stripeSub.TrialStart.Value;
            mscSub.TrialEndAt = stripeSub.TrialEnd;
        }

        // Track offer on Stripe subscription record
        if (offerId is not null)
            mscSub.OfferId = offerId;

        _db.MembersStripeCustomersSubscriptions.Add(mscSub);

        // Record offer redemption
        if (offerId is not null)
        {
            _db.OfferRedemptions.Add(new OfferRedemption
            {
                Id = ObjectIdGenerator.New(),
                OfferId = offerId,
                MemberId = memberId,
                SubscriptionId = localSub.Id,
                CreatedAt = now,
            });
        }

        // Grant product access
        var hasAccess = await _db.MembersProducts
            .AnyAsync(mp => mp.MemberId == memberId && mp.ProductId == productId, ct);
        if (!hasAccess)
        {
            _db.MembersProducts.Add(new MembersProduct
            {
                Id = ObjectIdGenerator.New(),
                MemberId = memberId,
                ProductId = productId,
                SortOrder = 0,
            });
        }

        // Upgrade member status
        if (member.Status == "free")
        {
            var oldStatus = member.Status;
            member.Status = "paid";
            member.UpdatedAt = now;

            _db.MembersStatusEvents.Add(new MembersStatusEvent
            {
                Id = ObjectIdGenerator.New(),
                MemberId = memberId,
                FromStatus = oldStatus,
                ToStatus = "paid",
                CreatedAt = now,
            });
        }

        // Record events
        _db.MembersPaidSubscriptionEvents.Add(new MembersPaidSubscriptionEvent
        {
            Id = ObjectIdGenerator.New(),
            Type = "created",
            MemberId = memberId,
            SubscriptionId = subscriptionId,
            FromPlan = null,
            ToPlan = priceItem?.Price.Id,
            Currency = stripeSub.Currency ?? "usd",
            Source = "stripe",
            MrrDelta = mscSub.Mrr,
            CreatedAt = now,
        });

        _db.MembersSubscriptionCreatedEvents.Add(new MembersSubscriptionCreatedEvent
        {
            Id = ObjectIdGenerator.New(),
            MemberId = memberId,
            SubscriptionId = mscSub.Id,
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Checkout completed: member {MemberId} subscribed to product {ProductId} via {SubscriptionId}",
            memberId, productId, subscriptionId);
    }

    private async Task HandleSubscriptionUpdated(string eventJson, CancellationToken ct)
    {
        var stripeEvent = EventUtility.ParseEvent(eventJson);
        if (stripeEvent.Data.Object is not Stripe.Subscription stripeSub) return;

        var mscSub = await _db.MembersStripeCustomersSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == stripeSub.Id, ct);
        if (mscSub is null) return;

        var now = DateTime.UtcNow;
        var priceItem = stripeSub.Items.Data.FirstOrDefault();
        var oldMrr = mscSub.Mrr;

        mscSub.Status = stripeSub.Status;
        mscSub.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
        mscSub.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd;
        mscSub.StripePriceId = priceItem?.Price.Id ?? mscSub.StripePriceId;
        mscSub.PlanId = priceItem?.Price.Id ?? mscSub.PlanId;
        mscSub.PlanNickname = priceItem?.Price.Nickname ?? mscSub.PlanNickname;
        mscSub.PlanInterval = priceItem?.Price.Recurring?.Interval ?? mscSub.PlanInterval;
        mscSub.PlanAmount = (int)(priceItem?.Price.UnitAmount ?? mscSub.PlanAmount);
        mscSub.PlanCurrency = stripeSub.Currency ?? mscSub.PlanCurrency;
        mscSub.Mrr = CalculateMrr(mscSub.PlanAmount, mscSub.PlanInterval);
        mscSub.UpdatedAt = now;

        if (stripeSub.CancelAtPeriodEnd && string.IsNullOrEmpty(mscSub.CancellationReason))
            mscSub.CancellationReason = "member";

        // Update card info if available
        if (stripeSub.DefaultPaymentMethodId is not null)
        {
            var pmService = new PaymentMethodService();
            try
            {
                var pm = await pmService.GetAsync(stripeSub.DefaultPaymentMethodId, cancellationToken: ct);
                mscSub.DefaultPaymentCardLast4 = pm.Card?.Last4;
            }
            catch (StripeException)
            {
                // Ignore — card info is nice-to-have
            }
        }

        // Discount tracking
        if (stripeSub.Discount is not null)
        {
            mscSub.DiscountStart = stripeSub.Discount.Start;
            mscSub.DiscountEnd = stripeSub.Discount.End;
        }

        // Update local subscription
        var localSub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == mscSub.GhostSubscriptionId, ct);
        if (localSub is not null)
        {
            localSub.Status = stripeSub.Status;
            localSub.Amount = mscSub.PlanAmount;
            localSub.Currency = mscSub.PlanCurrency;
            localSub.UpdatedAt = now;
        }

        // Record MRR change if prices changed
        var mrrDelta = mscSub.Mrr - oldMrr;
        if (mrrDelta != 0)
        {
            // Find member via customer
            var stripeCustomer = await _db.MembersStripeCustomers
                .FirstOrDefaultAsync(c => c.CustomerId == mscSub.CustomerId, ct);

            if (stripeCustomer is not null)
            {
                _db.MembersPaidSubscriptionEvents.Add(new MembersPaidSubscriptionEvent
                {
                    Id = ObjectIdGenerator.New(),
                    Type = "updated",
                    MemberId = stripeCustomer.MemberId,
                    SubscriptionId = stripeSub.Id,
                    Currency = mscSub.PlanCurrency,
                    Source = "stripe",
                    MrrDelta = mrrDelta,
                    CreatedAt = now,
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Subscription updated: {SubscriptionId} → {Status}", stripeSub.Id, stripeSub.Status);
    }

    private async Task HandleSubscriptionDeleted(string eventJson, CancellationToken ct)
    {
        var stripeEvent = EventUtility.ParseEvent(eventJson);
        if (stripeEvent.Data.Object is not Stripe.Subscription stripeSub) return;

        var mscSub = await _db.MembersStripeCustomersSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == stripeSub.Id, ct);
        if (mscSub is null) return;

        var now = DateTime.UtcNow;
        var oldMrr = mscSub.Mrr;

        mscSub.Status = "canceled";
        mscSub.Mrr = 0;
        mscSub.UpdatedAt = now;

        // Update local subscription
        var localSub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == mscSub.GhostSubscriptionId, ct);
        if (localSub is not null)
        {
            localSub.Status = "canceled";
            localSub.UpdatedAt = now;
        }

        // Find member and check if they still have active subscriptions
        var stripeCustomer = await _db.MembersStripeCustomers
            .FirstOrDefaultAsync(c => c.CustomerId == mscSub.CustomerId, ct);

        if (stripeCustomer is not null)
        {
            var memberId = stripeCustomer.MemberId;

            // Remove product access
            if (localSub is not null)
            {
                var access = await _db.MembersProducts
                    .FirstOrDefaultAsync(mp => mp.MemberId == memberId && mp.ProductId == localSub.TierId, ct);
                if (access is not null)
                    _db.MembersProducts.Remove(access);
            }

            // Check if member has any remaining active subscriptions
            var hasActiveSubscriptions = await _db.MembersStripeCustomersSubscriptions
                .AnyAsync(s => s.CustomerId == stripeCustomer.CustomerId
                    && s.SubscriptionId != stripeSub.Id
                    && s.Status == "active", ct);

            if (!hasActiveSubscriptions)
            {
                var member = await _db.Members.FindAsync([memberId], ct);
                if (member is not null && member.Status == "paid")
                {
                    member.Status = "free";
                    member.UpdatedAt = now;

                    _db.MembersStatusEvents.Add(new MembersStatusEvent
                    {
                        Id = ObjectIdGenerator.New(),
                        MemberId = memberId,
                        FromStatus = "paid",
                        ToStatus = "free",
                        CreatedAt = now,
                    });
                }
            }

            // Record cancellation event
            _db.MembersPaidSubscriptionEvents.Add(new MembersPaidSubscriptionEvent
            {
                Id = ObjectIdGenerator.New(),
                Type = "canceled",
                MemberId = memberId,
                SubscriptionId = stripeSub.Id,
                Currency = mscSub.PlanCurrency,
                Source = "stripe",
                MrrDelta = -oldMrr,
                CreatedAt = now,
            });

            _db.MembersCancelEvents.Add(new MembersCancelEvent
            {
                Id = ObjectIdGenerator.New(),
                MemberId = memberId,
                FromPlan = mscSub.PlanId,
                CreatedAt = now,
            });
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Subscription canceled: {SubscriptionId}", stripeSub.Id);
    }

    private async Task HandlePaymentSucceeded(string eventJson, CancellationToken ct)
    {
        var stripeEvent = EventUtility.ParseEvent(eventJson);
        if (stripeEvent.Data.Object is not Invoice invoice) return;

        // Skip the first invoice (already handled by checkout.session.completed)
        if (invoice.BillingReason == "subscription_create") return;

        var subscriptionId = invoice.SubscriptionId;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var mscSub = await _db.MembersStripeCustomersSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, ct);
        if (mscSub is null) return;

        var stripeCustomer = await _db.MembersStripeCustomers
            .FirstOrDefaultAsync(c => c.CustomerId == mscSub.CustomerId, ct);
        if (stripeCustomer is null) return;

        var now = DateTime.UtcNow;

        _db.MembersPaymentEvents.Add(new MembersPaymentEvent
        {
            Id = ObjectIdGenerator.New(),
            MemberId = stripeCustomer.MemberId,
            Amount = (int)invoice.AmountPaid,
            Currency = invoice.Currency ?? "usd",
            Source = "stripe",
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Payment recorded for subscription {SubscriptionId}", subscriptionId);
    }

    // ================================================================
    // Helpers
    // ================================================================

    private async Task<string> GetOrCreateStripeCustomerAsync(Member member, CancellationToken ct)
    {
        var existing = await _db.MembersStripeCustomers
            .FirstOrDefaultAsync(c => c.MemberId == member.Id, ct);

        if (existing is not null)
            return existing.CustomerId;

        var customerService = new CustomerService();
        var stripeCustomer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = member.Email,
            Name = member.Name,
            Metadata = new Dictionary<string, string> { ["member_id"] = member.Id },
        }, cancellationToken: ct);

        var now = DateTime.UtcNow;
        var record = new MembersStripeCustomer
        {
            Id = ObjectIdGenerator.New(),
            MemberId = member.Id,
            CustomerId = stripeCustomer.Id,
            Name = member.Name,
            Email = member.Email,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.MembersStripeCustomers.Add(record);
        await _db.SaveChangesAsync(ct);

        return stripeCustomer.Id;
    }

    private static int CalculateMrr(int amount, string interval)
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

    private static bool IsDonationCheckout(string eventJson)
    {
        var stripeEvent = EventUtility.ParseEvent(eventJson);
        if (stripeEvent.Data.Object is not CheckoutSession session) return false;
        return session.Metadata.TryGetValue("donation", out var val) && val == "true";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
