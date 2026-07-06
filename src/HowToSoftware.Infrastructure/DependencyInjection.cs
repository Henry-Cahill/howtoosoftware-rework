using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Services;
using HowToSoftware.Infrastructure.Data;
using HowToSoftware.Infrastructure.Data.Repositories;
using HowToSoftware.Infrastructure.HealthChecks;
using HowToSoftware.Infrastructure.Services;

namespace HowToSoftware.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, Action<ImageStorageOptions>? configureImages = null)
    {
        // ASP.NET Identity: user/role stores, password hashing, token providers
        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 10;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<Role>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Ghost bcrypt → Identity PBKDF2 password migration
        services.AddScoped<IPasswordHasher<User>, GhostBcryptPasswordHasher>();

        services.AddSingleton<ILexicalRenderer, LexicalRenderer>();
        services.AddSingleton<IMobiledocRenderer, MobiledocRenderer>();
        services.AddSingleton<ISlugGenerator, SlugGenerator>();
        services.AddSingleton<IContentSanitizer, HtmlContentSanitizer>();

        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<INewsletterService, NewsletterService>();
        services.AddScoped<IAutomatedEmailService, AutomatedEmailService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IMemberImportService, MemberImportService>();
        services.AddScoped<IMemberActivityService, MemberActivityService>();
        services.AddScoped<IContentGatingService, ContentGatingService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ISuppressionService, SuppressionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsDashboardService, AnalyticsDashboardService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IGhostImportExportService, GhostImportExportService>();

        // Image storage
        var imageOptions = new ImageStorageOptions { RootPath = Directory.GetCurrentDirectory() };
        configureImages?.Invoke(imageOptions);
        services.AddSingleton(imageOptions);
        services.AddSingleton<IImageStorageService, ImageStorageService>();

        // Email
        services.Configure<MailSettings>(configuration.GetSection("Mail"));
        services.AddSingleton<IEmailService, MailgunEmailService>();

        services.AddScoped<IMagicLinkService, MagicLinkService>();
        services.AddScoped<IBruteForceService, BruteForceService>();
        services.AddScoped<IAdminAuditService, AdminAuditService>();
        services.AddScoped<IMemberImpersonationService, MemberImpersonationService>();

        // Stripe
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IDonationService, DonationService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ISnippetService, SnippetService>();
        services.AddScoped<IRedirectService, RedirectService>();
        services.AddScoped<IMentionService, MentionService>();

        // Webmention verification HTTP client
        services.AddHttpClient("Webmention", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HowToSoftware-Webmention/1.0");
            client.MaxResponseContentBufferSize = 2 * 1024 * 1024; // 2 MB limit
        });

        // Webhooks
        services.AddSingleton<WebhookDispatchChannel>();
        services.AddSingleton<IWebhookDispatchService, WebhookDispatchService>();
        services.AddHostedService<WebhookDispatchBackgroundService>();
        services.AddHttpClient("WebhookDispatch", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // IndexNow
        services.AddSingleton<IndexNowChannel>();
        services.AddSingleton<IIndexNowService, IndexNowService>();
        services.AddHostedService<IndexNowBackgroundService>();
        services.AddHttpClient("IndexNow", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HowToSoftware-IndexNow/1.0");
        });

        // GeoIP
        var geoIpOptions = new GeoIpOptions();
        var geoIpPath = configuration["GeoIp:DatabasePath"];
        if (!string.IsNullOrEmpty(geoIpPath))
            geoIpOptions.DatabasePath = geoIpPath;
        services.AddSingleton(geoIpOptions);
        services.AddSingleton<IGeoIpService, GeoIpService>();

        // Health checks
        services.AddHealthChecks()
            .AddCheck<DbHealthCheck>("database")
            .AddCheck<DiskSpaceHealthCheck>("disk_space")
            .AddCheck<StripeHealthCheck>("stripe")
            .AddCheck<MailgunHealthCheck>("mailgun");

        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IMemberSegmentRepository, MemberSegmentRepository>();
        services.AddScoped<IMemberNoteRepository, MemberNoteRepository>();
        services.AddScoped<INewsletterRepository, NewsletterRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IAutomatedEmailRepository, AutomatedEmailRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IBruteForceRepository, BruteForceRepository>();
        services.AddScoped<IInviteRepository, InviteRepository>();
        services.AddScoped<ISearchRepository, SearchRepository>();

        // ActivityPub
        services.AddScoped<IActivityPubRepository, ActivityPubRepository>();
        services.AddScoped<IActivityPubService, ActivityPubService>();
        services.AddHttpClient<IActivityPubHttpClient, ActivityPubHttpClient>();

        return services;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
