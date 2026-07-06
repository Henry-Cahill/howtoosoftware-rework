using HowToSoftware.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Data;

internal static class SeedData
{
    // Fixed timestamp for all seed records (deterministic migrations)
    private static readonly DateTime SeedDate = new(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        SeedRoles(modelBuilder);
        SeedSettings(modelBuilder);
        SeedNewsletter(modelBuilder);
        SeedFreeProduct(modelBuilder);
        SeedAutomatedEmails(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = "000000000000000000000001", Name = "Owner", NormalizedName = "OWNER", ConcurrencyStamp = "00000000-0000-0000-0000-000000000001", Description = "Blog Owner", CreatedAt = SeedDate },
            new Role { Id = "000000000000000000000002", Name = "Administrator", NormalizedName = "ADMINISTRATOR", ConcurrencyStamp = "00000000-0000-0000-0000-000000000002", Description = "Administrators", CreatedAt = SeedDate },
            new Role { Id = "000000000000000000000003", Name = "Editor", NormalizedName = "EDITOR", ConcurrencyStamp = "00000000-0000-0000-0000-000000000003", Description = "Editors", CreatedAt = SeedDate },
            new Role { Id = "000000000000000000000004", Name = "Author", NormalizedName = "AUTHOR", ConcurrencyStamp = "00000000-0000-0000-0000-000000000004", Description = "Authors", CreatedAt = SeedDate },
            new Role { Id = "000000000000000000000005", Name = "Contributor", NormalizedName = "CONTRIBUTOR", ConcurrencyStamp = "00000000-0000-0000-0000-000000000005", Description = "Contributors", CreatedAt = SeedDate }
        );
    }

    private static void SeedSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Setting>().HasData(
            // --- Site ---
            Setting("000000000000000000001001", "site", "title", "", "string"),
            Setting("000000000000000000001002", "site", "description", "Thoughts, stories and ideas.", "string"),
            Setting("000000000000000000001003", "site", "logo", null, "string"),
            Setting("000000000000000000001004", "site", "icon", null, "string"),
            Setting("000000000000000000001005", "site", "cover_image", "https://static.ghost.org/v5.0.0/images/publication-cover.jpg", "string"),
            Setting("000000000000000000001006", "site", "accent_color", "#FF1A75", "string"),
            Setting("000000000000000000001007", "site", "locale", "en", "string"),
            Setting("000000000000000000001008", "site", "timezone", "Etc/UTC", "string"),
            Setting("000000000000000000001009", "site", "facebook", null, "string"),
            Setting("000000000000000000001010", "site", "twitter", null, "string"),
            Setting("000000000000000000001011", "site", "meta_title", null, "string"),
            Setting("000000000000000000001012", "site", "meta_description", null, "string"),
            Setting("000000000000000000001013", "site", "og_image", null, "string"),
            Setting("000000000000000000001014", "site", "og_title", null, "string"),
            Setting("000000000000000000001015", "site", "og_description", null, "string"),
            Setting("000000000000000000001016", "site", "twitter_image", null, "string"),
            Setting("000000000000000000001017", "site", "twitter_title", null, "string"),
            Setting("000000000000000000001018", "site", "twitter_description", null, "string"),
            Setting("000000000000000000001019", "site", "codeinjection_head", null, "string"),
            Setting("000000000000000000001020", "site", "codeinjection_foot", null, "string"),

            // --- Navigation ---
            Setting("000000000000000000001021", "site", "navigation", "[{\"label\":\"Home\",\"url\":\"/\"}]", "string"),
            Setting("000000000000000000001022", "site", "secondary_navigation", "[]", "string"),

            // --- Members ---
            Setting("000000000000000000001030", "members", "members_signup_access", "all", "string"),
            Setting("000000000000000000001031", "members", "default_content_visibility", "public", "string"),
            Setting("000000000000000000001032", "members", "members_track_sources", "true", "boolean"),

            // --- Portal ---
            Setting("000000000000000000001040", "portal", "portal_button", "true", "boolean"),
            Setting("000000000000000000001041", "portal", "portal_plans", "[\"free\"]", "string"),
            Setting("000000000000000000001042", "portal", "portal_default_plan", "yearly", "string"),
            Setting("000000000000000000001043", "portal", "portal_name", "true", "boolean"),

            // --- Email ---
            Setting("000000000000000000001050", "email", "email_track_opens", "true", "boolean"),
            Setting("000000000000000000001051", "email", "email_track_clicks", "true", "boolean"),
            Setting("000000000000000000001052", "email", "email_verification_required", "false", "boolean"),
            Setting("000000000000000000001053", "email", "mailgun_domain", null, "string"),
            Setting("000000000000000000001054", "email", "mailgun_api_key", null, "string"),
            Setting("000000000000000000001055", "email", "mailgun_base_url", "https://api.mailgun.net/v3", "string"),

            // --- Donations ---
            Setting("000000000000000000001060", "donations", "donations_currency", "USD", "string"),
            Setting("000000000000000000001061", "donations", "donations_suggested_amount", "500", "string"),

            // --- Labs / Features ---
            Setting("000000000000000000001070", "labs", "comments_enabled", "all", "string"),
            Setting("000000000000000000001071", "labs", "email_analytics_enabled", "true", "boolean"),
            Setting("000000000000000000001072", "labs", "outbound_link_tagging", "true", "boolean"),
            Setting("000000000000000000001073", "labs", "members_enabled", "true", "boolean"),

            // --- Core ---
            Setting("000000000000000000001080", "core", "db_hash", Guid.Empty.ToString("N")[..24], "string"),
            Setting("000000000000000000001081", "core", "active_theme", "howtoosoftware-custom", "string"),

            // --- Theme ---
            Setting("000000000000000000001090", "theme", "posts_per_page", "15", "number")
        );
    }

    private static void SeedNewsletter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Newsletter>().HasData(
            new Newsletter
            {
                Id = "000000000000000000002001",
                Uuid = "00000000-0000-0000-0000-000000000001",
                Name = "Default newsletter",
                Slug = "default-newsletter",
                Status = "active",
                Visibility = "members",
                SubscribeOnSignup = true,
                SortOrder = 0,
                SenderReplyTo = "newsletter",
                ShowHeaderIcon = true,
                ShowHeaderTitle = true,
                ShowHeaderName = true,
                ShowPostTitleSection = true,
                ShowCommentCta = true,
                ShowFeatureImage = true,
                ShowBadge = true,
                FeedbackEnabled = true,
                TitleFontCategory = "sans_serif",
                TitleAlignment = "center",
                TitleFontWeight = "bold",
                BodyFontCategory = "sans_serif",
                BackgroundColor = "light",
                ButtonCorners = "rounded",
                ButtonStyle = "fill",
                LinkStyle = "underline",
                ImageCorners = "square",
                HeaderBackgroundColor = "transparent",
                ButtonColor = "accent",
                LinkColor = "accent",
                CreatedAt = SeedDate
            }
        );
    }

    private static void SeedFreeProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = "000000000000000000003001",
                Name = "Free",
                Slug = "free",
                Active = true,
                Visibility = "public",
                Type = "free",
                TrialDays = 0,
                CreatedAt = SeedDate
            }
        );
    }

    private static Setting Setting(string id, string group, string key, string? value, string type) =>
        new() { Id = id, Group = group, Key = key, Value = value, Type = type, CreatedAt = SeedDate };

    private static void SeedAutomatedEmails(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutomatedEmail>().HasData(
            new AutomatedEmail
            {
                Id = "000000000000000000004001",
                Slug = "welcome",
                Name = "Welcome aboard!",
                Subject = "\uD83D\uDC4B Welcome to {{site_title}}!",
                Status = "active",
                Lexical = "{\"root\":{\"children\":[{\"children\":[{\"type\":\"text\",\"text\":\"You've successfully signed up and you'll receive new posts straight to your inbox.\"}],\"type\":\"paragraph\"},{\"children\":[{\"type\":\"text\",\"text\":\"If you didn't sign up for this site, you can simply ignore this email.\"}],\"type\":\"paragraph\"}],\"type\":\"root\"}}",
                CreatedAt = SeedDate
            }
        );
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
