namespace HowToSoftware.Core.Entities;

public class Product
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool Active { get; set; } = true;
    public string? WelcomePageUrl { get; set; }
    public string Visibility { get; set; } = "none";
    public int TrialDays { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = "paid";
    public string? Currency { get; set; }
    public int? MonthlyPrice { get; set; }
    public int? YearlyPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? MonthlyPriceId { get; set; }
    public string? YearlyPriceId { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public ICollection<Offer> Offers { get; set; } = [];
    public ICollection<StripeProduct> StripeProducts { get; set; } = [];
    public ICollection<PostsProduct> PostsProducts { get; set; } = [];
    public ICollection<MembersProduct> MembersProducts { get; set; } = [];
    public ICollection<ProductsBenefit> ProductsBenefits { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
