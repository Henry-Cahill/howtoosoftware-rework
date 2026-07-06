using HowToSoftware.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, string, IdentityUserClaim<string>, RolesUser,
        IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>(options)
{
    // ===== Content =====
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostMeta> PostsMeta => Set<PostMeta>();
    public DbSet<PostRevision> PostRevisions => Set<PostRevision>();
    public DbSet<MobiledocRevision> MobiledocRevisions => Set<MobiledocRevision>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PostsTag> PostsTags => Set<PostsTag>();
    public DbSet<PostsAuthor> PostsAuthors => Set<PostsAuthor>();
    public DbSet<PostsProduct> PostsProducts => Set<PostsProduct>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionsPost> CollectionsPosts => Set<CollectionsPost>();

    // ===== Users & Roles (Users, Roles, UserRoles provided by IdentityDbContext) =====
    public DbSet<RolesUser> RolesUsers => Set<RolesUser>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionsRole> PermissionsRoles => Set<PermissionsRole>();
    public DbSet<PermissionsUser> PermissionsUsers => Set<PermissionsUser>();

    // ===== Members =====
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<MembersLabel> MembersLabels => Set<MembersLabel>();
    public DbSet<MemberSegment> MemberSegments => Set<MemberSegment>();
    public DbSet<MemberNote> MemberNotes => Set<MemberNote>();
    public DbSet<MembersNewsletter> MembersNewsletters => Set<MembersNewsletter>();
    public DbSet<MembersProduct> MembersProducts => Set<MembersProduct>();
    public DbSet<MembersStripeCustomer> MembersStripeCustomers => Set<MembersStripeCustomer>();
    public DbSet<MembersStripeCustomerSubscription> MembersStripeCustomersSubscriptions => Set<MembersStripeCustomerSubscription>();
    public DbSet<MembersCancelEvent> MembersCancelEvents => Set<MembersCancelEvent>();
    public DbSet<MembersClickEvent> MembersClickEvents => Set<MembersClickEvent>();
    public DbSet<MembersCreatedEvent> MembersCreatedEvents => Set<MembersCreatedEvent>();
    public DbSet<MembersEmailChangeEvent> MembersEmailChangeEvents => Set<MembersEmailChangeEvent>();
    public DbSet<MembersFeedback> MembersFeedback => Set<MembersFeedback>();
    public DbSet<MembersLoginEvent> MembersLoginEvents => Set<MembersLoginEvent>();
    public DbSet<MembersPaidSubscriptionEvent> MembersPaidSubscriptionEvents => Set<MembersPaidSubscriptionEvent>();
    public DbSet<MembersPaymentEvent> MembersPaymentEvents => Set<MembersPaymentEvent>();
    public DbSet<MembersProductEvent> MembersProductEvents => Set<MembersProductEvent>();
    public DbSet<MembersStatusEvent> MembersStatusEvents => Set<MembersStatusEvent>();
    public DbSet<MembersSubscribeEvent> MembersSubscribeEvents => Set<MembersSubscribeEvent>();
    public DbSet<MembersSubscriptionCreatedEvent> MembersSubscriptionCreatedEvents => Set<MembersSubscriptionCreatedEvent>();

    // ===== Email =====
    public DbSet<Email> Emails => Set<Email>();
    public DbSet<EmailBatch> EmailBatches => Set<EmailBatch>();
    public DbSet<EmailRecipient> EmailRecipients => Set<EmailRecipient>();
    public DbSet<EmailRecipientFailure> EmailRecipientFailures => Set<EmailRecipientFailure>();
    public DbSet<EmailSpamComplaintEvent> EmailSpamComplaintEvents => Set<EmailSpamComplaintEvent>();
    public DbSet<AutomatedEmail> AutomatedEmails => Set<AutomatedEmail>();
    public DbSet<AutomatedEmailRecipient> AutomatedEmailRecipients => Set<AutomatedEmailRecipient>();
    public DbSet<AutomatedEmailSchedule> AutomatedEmailSchedules => Set<AutomatedEmailSchedule>();

    // ===== Commerce =====
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Benefit> Benefits => Set<Benefit>();
    public DbSet<ProductsBenefit> ProductsBenefits => Set<ProductsBenefit>();
    public DbSet<StripeProduct> StripeProducts => Set<StripeProduct>();
    public DbSet<StripePrice> StripePrices => Set<StripePrice>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferRedemption> OfferRedemptions => Set<OfferRedemption>();
    public DbSet<DonationPaymentEvent> DonationPaymentEvents => Set<DonationPaymentEvent>();

    // ===== Comments =====
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<CommentReport> CommentReports => Set<CommentReport>();

    // ===== Newsletter =====
    public DbSet<Newsletter> Newsletters => Set<Newsletter>();

    // ===== Settings & Config =====
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<CustomThemeSetting> CustomThemeSettings => Set<CustomThemeSetting>();
    public DbSet<Snippet> Snippets => Set<Snippet>();

    // ===== Analytics =====
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<AnalyticsHourlyRollup> AnalyticsHourlyRollups => Set<AnalyticsHourlyRollup>();
    public DbSet<AnalyticsDailyRollup> AnalyticsDailyRollups => Set<AnalyticsDailyRollup>();

    // ===== Infrastructure =====
    public DbSet<Core.Entities.Action> Actions => Set<Core.Entities.Action>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<Brute> Brute => Set<Brute>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    // ===== Feeds & Discovery =====
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<Redirect> Redirects => Set<Redirect>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationClickEvent> RecommendationClickEvents => Set<RecommendationClickEvent>();
    public DbSet<RecommendationSubscribeEvent> RecommendationSubscribeEvents => Set<RecommendationSubscribeEvent>();
    public DbSet<Mention> Mentions => Set<Mention>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Outbox> Outbox => Set<Outbox>();
    public DbSet<Suppression> Suppressions => Set<Suppression>();

    // ===== ActivityPub =====
    public DbSet<ApSite> ApSites => Set<ApSite>();
    public DbSet<ApAccount> ApAccounts => Set<ApAccount>();
    public DbSet<ApSchemaMigration> ApSchemaMigrations => Set<ApSchemaMigration>();
    public DbSet<ApKeyValue> ApKeyValues => Set<ApKeyValue>();
    public DbSet<ApGhostApPostMapping> ApGhostApPostMappings => Set<ApGhostApPostMapping>();
    public DbSet<ApUser> ApUsers => Set<ApUser>();
    public DbSet<ApAccountDeliveryBackoff> ApAccountDeliveryBackoffs => Set<ApAccountDeliveryBackoff>();
    public DbSet<ApBlock> ApBlocks => Set<ApBlock>();
    public DbSet<ApDomainBlock> ApDomainBlocks => Set<ApDomainBlock>();
    public DbSet<ApFollow> ApFollows => Set<ApFollow>();
    public DbSet<ApPost> ApPosts => Set<ApPost>();
    public DbSet<ApFeed> ApFeeds => Set<ApFeed>();
    public DbSet<ApLike> ApLikes => Set<ApLike>();
    public DbSet<ApMention> ApMentions => Set<ApMention>();
    public DbSet<ApNotification> ApNotifications => Set<ApNotification>();
    public DbSet<ApOutbox> ApOutboxes => Set<ApOutbox>();
    public DbSet<ApRepost> ApReposts => Set<ApRepost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Override Identity default table names (AspNet*) to match our schema.
        // The snake_case convention applied at the end converts these to snake_case.
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<RolesUser>().ToTable("RolesUsers");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

        // Configure string key lengths for Identity support tables
        modelBuilder.Entity<IdentityUserClaim<string>>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(24);
            e.Property(x => x.ClaimType).HasMaxLength(256);
            e.Property(x => x.ClaimValue).HasMaxLength(1024);
        });
        modelBuilder.Entity<IdentityUserLogin<string>>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(24);
            e.Property(x => x.LoginProvider).HasMaxLength(128);
            e.Property(x => x.ProviderKey).HasMaxLength(128);
            e.Property(x => x.ProviderDisplayName).HasMaxLength(256);
        });
        modelBuilder.Entity<IdentityUserToken<string>>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(24);
            e.Property(x => x.LoginProvider).HasMaxLength(128);
            e.Property(x => x.Name).HasMaxLength(128);
        });
        modelBuilder.Entity<IdentityRoleClaim<string>>(e =>
        {
            e.Property(x => x.RoleId).HasMaxLength(24);
            e.Property(x => x.ClaimType).HasMaxLength(256);
            e.Property(x => x.ClaimValue).HasMaxLength(1024);
        });

        // =================================================================
        // Tier 0: No FK dependencies
        // =================================================================

        // --- Roles ---
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        // --- Permissions ---
        modelBuilder.Entity<Permission>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        // --- Settings ---
        modelBuilder.Entity<Setting>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Group).HasDefaultValue("core");
            e.HasIndex(x => x.Key).IsUnique();
        });

        // --- Labels ---
        modelBuilder.Entity<Label>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // --- MemberSegments ---
        modelBuilder.Entity<MemberSegment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.SortOrder);
            e.HasOne(x => x.Label)
                .WithMany()
                .HasForeignKey(x => x.LabelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- AdminAuditLogs ---
        modelBuilder.Entity<AdminAuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AdminUserId);
            e.HasIndex(x => x.Action);
            e.HasIndex(x => new { x.TargetType, x.TargetId });
            e.HasIndex(x => x.CreatedAt);
        });

        // --- MemberNotes (append-only thread, MEM.7) ---
        modelBuilder.Entity<MemberNote>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MemberId, x.CreatedAt });
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Benefits ---
        modelBuilder.Entity<Benefit>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // --- Integrations ---
        modelBuilder.Entity<Integration>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasDefaultValue("custom");
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // --- Collections ---
        modelBuilder.Entity<Collection>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // --- CustomThemeSettings ---
        modelBuilder.Entity<CustomThemeSetting>(e =>
        {
            e.HasKey(x => x.Id);
        });

        // --- Brute ---
        modelBuilder.Entity<Brute>(e =>
        {
            e.HasKey(x => x.Key);
        });

        // --- Jobs ---
        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("queued");
            e.HasIndex(x => x.Name).IsUnique();
        });

        // --- Milestones ---
        modelBuilder.Entity<Milestone>(e =>
        {
            e.HasKey(x => x.Id);
        });

        // --- Recommendations ---
        modelBuilder.Entity<Recommendation>(e =>
        {
            e.HasKey(x => x.Id);
        });

        // --- Actions ---
        modelBuilder.Entity<Core.Entities.Action>(e =>
        {
            e.HasKey(x => x.Id);
        });

        // --- Snippets ---
        modelBuilder.Entity<Snippet>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        // --- Mentions ---
        modelBuilder.Entity<Mention>(e =>
        {
            e.HasKey(x => x.Id);
        });

        // --- Outbox ---
        modelBuilder.Entity<Outbox>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("pending");
            e.HasIndex(x => new { x.EventType, x.Status, x.CreatedAt });
        });

        // =================================================================
        // Tier 1: Independent entity tables
        // =================================================================

        // --- Users ---
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("active");
            e.Property(x => x.Visibility).HasDefaultValue("public");
            e.Property(x => x.CommentNotifications).HasDefaultValue(true);
            e.Property(x => x.FreeMemberSignupNotification).HasDefaultValue(true);
            e.Property(x => x.PaidSubscriptionStartedNotification).HasDefaultValue(true);
            e.Property(x => x.PaidSubscriptionCanceledNotification).HasDefaultValue(false);
            e.Property(x => x.MentionNotifications).HasDefaultValue(true);
            e.Property(x => x.RecommendationNotifications).HasDefaultValue(true);
            e.Property(x => x.MilestoneNotifications).HasDefaultValue(true);
            e.Property(x => x.DonationNotifications).HasDefaultValue(true);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        // --- Newsletters ---
        modelBuilder.Entity<Newsletter>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SenderReplyTo).HasDefaultValue("newsletter");
            e.Property(x => x.Status).HasDefaultValue("active");
            e.Property(x => x.Visibility).HasDefaultValue("members");
            e.Property(x => x.SubscribeOnSignup).HasDefaultValue(true);
            e.Property(x => x.ArchiveEnabled).HasDefaultValue(false);
            e.Property(x => x.ShowHeaderIcon).HasDefaultValue(true);
            e.Property(x => x.ShowHeaderTitle).HasDefaultValue(true);
            e.Property(x => x.TitleFontCategory).HasDefaultValue("sans_serif");
            e.Property(x => x.TitleAlignment).HasDefaultValue("center");
            e.Property(x => x.ShowFeatureImage).HasDefaultValue(true);
            e.Property(x => x.BodyFontCategory).HasDefaultValue("sans_serif");
            e.Property(x => x.ShowBadge).HasDefaultValue(true);
            e.Property(x => x.ShowHeaderName).HasDefaultValue(true);
            e.Property(x => x.ShowPostTitleSection).HasDefaultValue(true);
            e.Property(x => x.ShowCommentCta).HasDefaultValue(true);
            e.Property(x => x.BackgroundColor).HasDefaultValue("light");
            e.Property(x => x.ButtonCorners).HasDefaultValue("rounded");
            e.Property(x => x.ButtonStyle).HasDefaultValue("fill");
            e.Property(x => x.TitleFontWeight).HasDefaultValue("bold");
            e.Property(x => x.LinkStyle).HasDefaultValue("underline");
            e.Property(x => x.ImageCorners).HasDefaultValue("square");
            e.Property(x => x.HeaderBackgroundColor).HasDefaultValue("transparent");
            e.Property(x => x.ButtonColor).HasDefaultValue("accent");
            e.Property(x => x.LinkColor).HasDefaultValue("accent");
            e.HasIndex(x => x.Uuid).IsUnique();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // --- Products (Tiers) ---
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Active).HasDefaultValue(true);
            e.Property(x => x.Visibility).HasDefaultValue("none");
            e.Property(x => x.Type).HasDefaultValue("paid");
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.SortOrder);
        });

        // --- Tags ---
        modelBuilder.Entity<Tag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Visibility).HasDefaultValue("public");
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // --- Members ---
        modelBuilder.Entity<Member>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("free");
            e.Property(x => x.EnableCommentNotifications).HasDefaultValue(true);
            e.Property(x => x.AvatarImage).HasMaxLength(2000);
            e.HasIndex(x => x.Uuid).IsUnique();
            e.HasIndex(x => x.TransientId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique()
                .IncludeProperties(x => new { x.Name, x.Status, x.Uuid });
            e.HasIndex(x => x.EmailOpenRate);
            e.HasIndex(x => x.EmailDisabled);
        });

        // --- ApiKeys ---
        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Secret).IsUnique();
            e.HasOne(x => x.Integration)
                .WithMany(x => x.ApiKeys)
                .HasForeignKey(x => x.IntegrationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Tokens ---
        modelBuilder.Entity<Token>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenValue).HasColumnName("Token");
            e.HasIndex(x => x.TokenValue);
            e.HasIndex(x => x.Uuid).IsUnique();
        });

        // --- Sessions ---
        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SessionId).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Invites ---
        modelBuilder.Entity<Invite>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("pending");
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PermissionsRoles (join) ---
        modelBuilder.Entity<PermissionsRole>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Role)
                .WithMany(x => x.PermissionsRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission)
                .WithMany(x => x.PermissionsRoles)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PermissionsUsers (join) ---
        modelBuilder.Entity<PermissionsUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User)
                .WithMany(x => x.PermissionsUsers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission)
                .WithMany(x => x.PermissionsUsers)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- RolesUsers (join) ---
        modelBuilder.Entity<RolesUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Role)
                .WithMany(x => x.RolesUsers)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
                .WithMany(x => x.RolesUsers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =================================================================
        // Tier 2: Tables with FK dependencies on Tier 0/1
        // =================================================================

        // --- Posts ---
        modelBuilder.Entity<Post>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Featured).HasDefaultValue(false);
            e.Property(x => x.Type).HasDefaultValue("post");
            e.Property(x => x.Status).HasDefaultValue("draft");
            e.Property(x => x.Visibility).HasDefaultValue("public");
            e.Property(x => x.EmailRecipientFilter).HasDefaultValue("all");
            e.Property(x => x.ShowTitleAndFeatureImage).HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasIndex(x => new { x.Slug, x.Type }).IsUnique()
                .IncludeProperties(x => new { x.Status, x.Visibility, x.PublishedAt, x.Title });
            e.HasIndex(x => x.Uuid);
            e.HasIndex(x => x.UpdatedAt);
            e.HasIndex(x => x.PublishedAt);
            e.HasIndex(x => x.NewsletterId);
            e.HasIndex(x => new { x.Type, x.Status, x.UpdatedAt });
            e.HasIndex(x => new { x.Status, x.Type, x.PublishedAt })
                .IncludeProperties(x => new { x.Id, x.Title, x.Slug, x.FeatureImage, x.CustomExcerpt, x.Featured, x.Visibility });
            e.HasIndex(x => new { x.Type, x.ParentId, x.SortOrder });
            e.HasOne(x => x.Newsletter)
                .WithMany(x => x.Posts)
                .HasForeignKey(x => x.NewsletterId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Offers ---
        modelBuilder.Entity<Offer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Active).HasDefaultValue(true);
            e.Property(x => x.RedemptionType).HasDefaultValue("signup");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.StripeCouponId).IsUnique();
            e.HasIndex(x => x.ProductId);
            e.HasOne(x => x.Product)
                .WithMany(x => x.Offers)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- StripeProducts ---
        modelBuilder.Entity<StripeProduct>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.StripeProductId).IsUnique();
            e.HasIndex(x => x.ProductId);
            e.HasOne(x => x.Product)
                .WithMany(x => x.StripeProducts)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- AutomatedEmails ---
        modelBuilder.Entity<AutomatedEmail>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("inactive");
            e.Property(x => x.DelayMinutes).HasDefaultValue(0);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.TriggerEvent);
        });

        // --- MembersStripeCustomers ---
        modelBuilder.Entity<MembersStripeCustomer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CustomerId).IsUnique();
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Subscriptions ---
        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.TierId);
            e.HasIndex(x => x.OfferId);
            e.HasOne(x => x.Member)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tier)
                .WithMany()
                .HasForeignKey(x => x.TierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Offer)
                .WithMany()
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- MembersLabels (join) ---
        modelBuilder.Entity<MembersLabel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.LabelId);
            e.HasOne(x => x.Member)
                .WithMany(x => x.MembersLabels)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Label)
                .WithMany(x => x.MembersLabels)
                .HasForeignKey(x => x.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersNewsletters (join) ---
        modelBuilder.Entity<MembersNewsletter>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => new { x.NewsletterId, x.MemberId });
            e.HasOne(x => x.Member)
                .WithMany(x => x.MembersNewsletters)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Newsletter)
                .WithMany(x => x.MembersNewsletters)
                .HasForeignKey(x => x.NewsletterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersProducts (join) ---
        modelBuilder.Entity<MembersProduct>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.ProductId);
            e.HasOne(x => x.Member)
                .WithMany(x => x.MembersProducts)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
                .WithMany(x => x.MembersProducts)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersCancelEvents ---
        modelBuilder.Entity<MembersCancelEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersCreatedEvents ---
        modelBuilder.Entity<MembersCreatedEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.AttributionId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersEmailChangeEvents ---
        modelBuilder.Entity<MembersEmailChangeEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersLoginEvents ---
        modelBuilder.Entity<MembersLoginEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersPaymentEvents ---
        modelBuilder.Entity<MembersPaymentEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersStatusEvents ---
        modelBuilder.Entity<MembersStatusEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersPaidSubscriptionEvents ---
        modelBuilder.Entity<MembersPaidSubscriptionEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersProductEvents ---
        modelBuilder.Entity<MembersProductEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.ProductId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- MembersSubscribeEvents ---
        modelBuilder.Entity<MembersSubscribeEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Subscribed).HasDefaultValue(true);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => new { x.NewsletterId, x.CreatedAt });
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Newsletter)
                .WithMany()
                .HasForeignKey(x => x.NewsletterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Webhooks ---
        modelBuilder.Entity<Webhook>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ApiVersion).HasDefaultValue("v2");
            e.HasIndex(x => x.IntegrationId);
            e.HasOne(x => x.Integration)
                .WithMany(x => x.Webhooks)
                .HasForeignKey(x => x.IntegrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ProductsBenefits (join) ---
        modelBuilder.Entity<ProductsBenefit>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.BenefitId);
            e.HasOne(x => x.Product)
                .WithMany(x => x.ProductsBenefits)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Benefit)
                .WithMany(x => x.ProductsBenefits)
                .HasForeignKey(x => x.BenefitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- DonationPaymentEvents ---
        modelBuilder.Entity<DonationPaymentEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- RecommendationClickEvents ---
        modelBuilder.Entity<RecommendationClickEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RecommendationId);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Recommendation)
                .WithMany()
                .HasForeignKey(x => x.RecommendationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- RecommendationSubscribeEvents ---
        modelBuilder.Entity<RecommendationSubscribeEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RecommendationId);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Recommendation)
                .WithMany()
                .HasForeignKey(x => x.RecommendationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =================================================================
        // Tier 3: Tables with FK to Posts/Emails/etc.
        // =================================================================

        // --- StripePrices ---
        modelBuilder.Entity<StripePrice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasDefaultValue("recurring");
            e.HasIndex(x => x.StripePriceId).IsUnique();
            e.HasIndex(x => x.StripeProductId);
            e.HasOne(x => x.StripeProductEntity)
                .WithMany(x => x.Prices)
                .HasForeignKey(x => x.StripeProductId)
                .HasPrincipalKey(x => x.StripeProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- PostsAuthors (join) ---
        modelBuilder.Entity<PostsAuthor>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.AuthorId);
            e.HasOne(x => x.Post)
                .WithMany(x => x.PostsAuthors)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author)
                .WithMany(x => x.PostsAuthors)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PostsMeta ---
        modelBuilder.Entity<PostMeta>(e =>
        {
            e.ToTable("PostsMeta");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PostId).IsUnique();
            e.Property(x => x.EmailOnly).HasDefaultValue(false);
            e.HasOne(x => x.Post)
                .WithOne(x => x.Meta)
                .HasForeignKey<PostMeta>(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PostsTags (join) ---
        modelBuilder.Entity<PostsTag>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TagId);
            e.HasIndex(x => new { x.PostId, x.TagId });
            e.HasOne(x => x.Post)
                .WithMany(x => x.PostsTags)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag)
                .WithMany(x => x.PostsTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PostsProducts (join) ---
        modelBuilder.Entity<PostsProduct>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.ProductId);
            e.HasOne(x => x.Post)
                .WithMany(x => x.PostsProducts)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
                .WithMany(x => x.PostsProducts)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PostRevisions ---
        modelBuilder.Entity<PostRevision>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.AuthorId);
            e.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);
            // No FK to Posts in DDL (intentional — revisions can outlive posts)
        });

        // --- MobiledocRevisions ---
        modelBuilder.Entity<MobiledocRevision>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PostId);
            // No FK to Posts in DDL
        });

        // --- CollectionsPosts (join) ---
        modelBuilder.Entity<CollectionsPost>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CollectionId);
            e.HasIndex(x => x.PostId);
            e.HasOne(x => x.Collection)
                .WithMany(x => x.CollectionsPosts)
                .HasForeignKey(x => x.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Post)
                .WithMany(x => x.CollectionsPosts)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Emails ---
        modelBuilder.Entity<Email>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("pending");
            e.Property(x => x.RecipientFilter).HasDefaultValue("all");
            e.Property(x => x.SourceType).HasDefaultValue("html");
            e.HasIndex(x => x.PostId).IsUnique();
            e.HasIndex(x => x.NewsletterId);
            e.HasIndex(x => new { x.AbTestPhase, x.AbTestStartedAt });
            e.HasOne(x => x.Post)
                .WithMany(x => x.Emails)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Newsletter)
                .WithMany(x => x.Emails)
                .HasForeignKey(x => x.NewsletterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Comments ---
        modelBuilder.Entity<Comment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("published");
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.ParentId);
            e.HasIndex(x => x.InReplyToId);
            e.HasOne(x => x.Post)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.InReplyTo)
                .WithMany()
                .HasForeignKey(x => x.InReplyToId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // --- Redirects ---
        modelBuilder.Entity<Redirect>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.From);
            e.HasIndex(x => x.PostId);
            e.HasOne(x => x.Post)
                .WithMany()
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- MembersFeedback ---
        modelBuilder.Entity<MembersFeedback>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.PostId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Post)
                .WithMany()
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersClickEvents ---
        modelBuilder.Entity<MembersClickEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.RedirectId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Redirect)
                .WithMany()
                .HasForeignKey(x => x.RedirectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersStripeCustomersSubscriptions ---
        modelBuilder.Entity<MembersStripeCustomerSubscription>(e =>
        {
            e.ToTable("MembersStripeCustomersSubscriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.StripePriceId).HasDefaultValue("");
            e.Property(x => x.CancelAtPeriodEnd).HasDefaultValue(false);
            e.Property(x => x.Mrr).HasDefaultValue(0);
            e.HasIndex(x => x.SubscriptionId).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.GhostSubscriptionId);
            e.HasIndex(x => x.StripePriceId);
            e.HasIndex(x => x.OfferId);
            e.HasOne(x => x.Customer)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.CustomerId)
                .HasPrincipalKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GhostSubscription)
                .WithMany()
                .HasForeignKey(x => x.GhostSubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Offer)
                .WithMany()
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Suppressions ---
        modelBuilder.Entity<Suppression>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.EmailId);
            e.HasOne(x => x.EmailEntity)
                .WithMany()
                .HasForeignKey(x => x.EmailId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- AnalyticsEvents ---
        modelBuilder.Entity<AnalyticsEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.Timestamp)
                .IncludeProperties(x => new { x.SessionId, x.PageUrlPath, x.Referrer, x.Device, x.Country });
            e.HasIndex(x => new { x.SessionId, x.Timestamp });
            e.HasIndex(x => x.SiteUuid);
        });

        // --- AnalyticsHourlyRollups ---
        modelBuilder.Entity<AnalyticsHourlyRollup>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.BucketHour).IsUnique();
        });

        // --- AnalyticsDailyRollups ---
        modelBuilder.Entity<AnalyticsDailyRollup>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.BounceRatePercent).HasPrecision(5, 2);
            e.Property(x => x.AvgSessionDurationSeconds).HasPrecision(10, 2);
            e.HasIndex(x => x.BucketDate).IsUnique();
        });

        // =================================================================
        // Tier 4+: Tables with FK to Tier 3+ tables
        // =================================================================

        // --- OfferRedemptions ---
        modelBuilder.Entity<OfferRedemption>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OfferId);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.SubscriptionId);
            e.HasOne(x => x.Offer)
                .WithMany(x => x.Redemptions)
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MembersSubscriptionCreatedEvents ---
        modelBuilder.Entity<MembersSubscriptionCreatedEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.SubscriptionId);
            e.HasIndex(x => x.AttributionId);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Subscription)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // --- CommentLikes ---
        modelBuilder.Entity<CommentLike>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CommentId);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Comment)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- CommentReports ---
        modelBuilder.Entity<CommentReport>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CommentId);
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Comment)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- EmailBatches ---
        modelBuilder.Entity<EmailBatch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasDefaultValue("pending");
            e.HasIndex(x => x.EmailId);
            e.HasOne(x => x.Email)
                .WithMany(x => x.Batches)
                .HasForeignKey(x => x.EmailId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- EmailSpamComplaintEvents ---
        modelBuilder.Entity<EmailSpamComplaintEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EmailId, x.MemberId }).IsUnique();
            e.HasIndex(x => x.MemberId);
            e.HasOne(x => x.Email)
                .WithMany()
                .HasForeignKey(x => x.EmailId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- AutomatedEmailRecipients ---
        modelBuilder.Entity<AutomatedEmailRecipient>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AutomatedEmailId);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.MemberEmail);
            e.HasOne(x => x.AutomatedEmail)
                .WithMany(x => x.Recipients)
                .HasForeignKey(x => x.AutomatedEmailId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- AutomatedEmailSchedules ---
        modelBuilder.Entity<AutomatedEmailSchedule>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AutomatedEmailId);
            e.HasIndex(x => x.MemberId);
            // Pending-work index for the background dispatcher.
            e.HasIndex(x => new { x.ProcessedAt, x.ScheduledFor });
            e.HasOne(x => x.AutomatedEmail)
                .WithMany()
                .HasForeignKey(x => x.AutomatedEmailId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- EmailRecipients ---
        modelBuilder.Entity<EmailRecipient>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MemberId);
            e.HasIndex(x => x.BatchId);
            e.HasIndex(x => new { x.EmailId, x.MemberEmail })
                .IncludeProperties(x => new { x.MemberId, x.MemberName, x.ProcessedAt });
            e.HasIndex(x => new { x.EmailId, x.DeliveredAt });
            e.HasIndex(x => new { x.EmailId, x.OpenedAt });
            e.HasIndex(x => new { x.EmailId, x.FailedAt });
            e.HasIndex(x => new { x.EmailId, x.ProcessedAt })
                .IncludeProperties(x => new { x.DeliveredAt, x.OpenedAt, x.FailedAt });
            e.HasOne(x => x.Email)
                .WithMany()
                .HasForeignKey(x => x.EmailId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Batch)
                .WithMany(x => x.Recipients)
                .HasForeignKey(x => x.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- EmailRecipientFailures ---
        modelBuilder.Entity<EmailRecipientFailure>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Severity).HasDefaultValue("permanent");
            e.HasIndex(x => x.EmailId);
            e.HasIndex(x => x.EmailRecipientId);
            e.HasOne(x => x.Email)
                .WithMany()
                .HasForeignKey(x => x.EmailId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EmailRecipient)
                .WithMany(x => x.Failures)
                .HasForeignKey(x => x.EmailRecipientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =================================================================
        // ActivityPub Tier 0: No FK dependencies
        // =================================================================

        // --- ApSites ---
        modelBuilder.Entity<ApSite>(e =>
        {
            e.ToTable("ApSites");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Host).HasMaxLength(255);
            e.Property(x => x.WebhookSecret).HasMaxLength(64);
            e.HasIndex(x => x.Host).IsUnique();
            e.HasIndex(x => x.WebhookSecret).IsUnique();
        });

        // --- ApAccounts ---
        modelBuilder.Entity<ApAccount>(e =>
        {
            e.ToTable("ApAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Username).HasMaxLength(255);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.AvatarUrl).HasMaxLength(1024);
            e.Property(x => x.BannerImageUrl).HasMaxLength(1024);
            e.Property(x => x.Url).HasMaxLength(1024);
            e.Property(x => x.ApId).HasMaxLength(1024);
            e.Property(x => x.ApInboxUrl).HasMaxLength(1024);
            e.Property(x => x.ApSharedInboxUrl).HasMaxLength(1024);
            e.Property(x => x.ApOutboxUrl).HasMaxLength(1024);
            e.Property(x => x.ApFollowingUrl).HasMaxLength(1024);
            e.Property(x => x.ApFollowersUrl).HasMaxLength(1024);
            e.Property(x => x.ApLikedUrl).HasMaxLength(1024);
            e.Property(x => x.Uuid).HasMaxLength(36);
            e.Property(x => x.Domain).HasMaxLength(255);
            e.Property(x => x.ApIdHash).HasComputedColumnSql("HASHBYTES('SHA2_256', [ap_id])", stored: true);
            e.Property(x => x.DomainHash).HasComputedColumnSql("HASHBYTES('SHA2_256', LOWER([domain]))", stored: true);
            e.Property(x => x.ApInboxUrlHash).HasComputedColumnSql("HASHBYTES('SHA2_256', LOWER([ap_inbox_url]))", stored: true);
            e.HasIndex(x => x.Uuid).IsUnique().HasFilter("[uuid] IS NOT NULL");
            e.HasIndex(x => x.ApIdHash).IsUnique();
            e.HasIndex(x => x.Username);
            e.HasIndex(x => x.DomainHash);
            e.HasIndex(x => x.ApInboxUrlHash);
        });

        // --- ApSchemaMigrations ---
        modelBuilder.Entity<ApSchemaMigration>(e =>
        {
            e.ToTable("ApSchemaMigrations");
            e.HasKey(x => x.Version);
        });

        // --- ApKeyValue ---
        modelBuilder.Entity<ApKeyValue>(e =>
        {
            e.ToTable("ApKeyValues");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Key).HasMaxLength(768);
            e.Property(x => x.ObjectId).HasComputedColumnSql("CAST(LEFT(JSON_VALUE([value], '$.object.id'), 255) AS NVARCHAR(255))");
            e.Property(x => x.ObjectInReplyTo).HasComputedColumnSql("CAST(LEFT(JSON_VALUE([value], '$.object.inReplyTo'), 255) AS NVARCHAR(255))");
            e.HasIndex(x => x.Key).IsUnique().HasFilter("[key] IS NOT NULL");
            e.HasIndex(x => x.ObjectId);
            e.HasIndex(x => x.ObjectInReplyTo);
            e.HasIndex(x => x.Expires);
        });

        // --- ApGhostApPostMappings ---
        modelBuilder.Entity<ApGhostApPostMapping>(e =>
        {
            e.ToTable("ApGhostApPostMappings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.GhostUuid).HasMaxLength(36);
            e.Property(x => x.ApId).HasMaxLength(1024);
            e.Property(x => x.ApIdHash).HasComputedColumnSql("HASHBYTES('SHA2_256', [ap_id])", stored: true);
            e.HasIndex(x => x.GhostUuid).IsUnique();
            e.HasIndex(x => x.ApIdHash).IsUnique();
        });

        // =================================================================
        // ActivityPub Tier 1: Depends on ApAccounts, ApSites
        // =================================================================

        // --- ApUsers ---
        modelBuilder.Entity<ApUser>(e =>
        {
            e.ToTable("ApUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.SiteId);
            e.HasOne(x => x.Account)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Site)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ApAccountDeliveryBackoffs ---
        modelBuilder.Entity<ApAccountDeliveryBackoff>(e =>
        {
            e.ToTable("ApAccountDeliveryBackoffs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.BackoffSeconds).HasDefaultValue(60);
            e.HasIndex(x => x.AccountId).IsUnique();
            e.HasIndex(x => x.BackoffUntil);
            e.HasOne(x => x.Account)
                .WithOne(x => x.DeliveryBackoff)
                .HasForeignKey<ApAccountDeliveryBackoff>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ApBlocks ---
        modelBuilder.Entity<ApBlock>(e =>
        {
            e.ToTable("ApBlocks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.BlockerId, x.BlockedId }).IsUnique();
            e.HasIndex(x => new { x.BlockedId, x.BlockerId });
            e.HasOne(x => x.Blocker)
                .WithMany(x => x.Blocking)
                .HasForeignKey(x => x.BlockerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Blocked)
                .WithMany(x => x.BlockedBy)
                .HasForeignKey(x => x.BlockedId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ApDomainBlocks ---
        modelBuilder.Entity<ApDomainBlock>(e =>
        {
            e.ToTable("ApDomainBlocks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Domain).HasMaxLength(255);
            e.Property(x => x.DomainHash).HasComputedColumnSql("HASHBYTES('SHA2_256', LOWER([domain]))", stored: true);
            e.HasIndex(x => new { x.BlockerId, x.Domain }).IsUnique();
            e.HasIndex(x => x.DomainHash);
            e.HasOne(x => x.Blocker)
                .WithMany(x => x.DomainBlocks)
                .HasForeignKey(x => x.BlockerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ApFollows ---
        modelBuilder.Entity<ApFollow>(e =>
        {
            e.ToTable("ApFollows");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.FollowerId, x.FollowingId }).IsUnique();
            e.HasIndex(x => x.FollowerId);
            e.HasIndex(x => x.FollowingId);
            e.HasOne(x => x.Follower)
                .WithMany(x => x.Following)
                .HasForeignKey(x => x.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Following)
                .WithMany(x => x.Followers)
                .HasForeignKey(x => x.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =================================================================
        // ActivityPub Tier 2: Depends on ApPosts, ApUsers, ApAccounts
        // =================================================================

        // --- ApPosts ---
        modelBuilder.Entity<ApPost>(e =>
        {
            e.ToTable("ApPosts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Uuid).HasMaxLength(36).HasDefaultValueSql("CONVERT(NVARCHAR(36), NEWID())");
            e.Property(x => x.Title).HasMaxLength(256);
            e.Property(x => x.Excerpt).HasMaxLength(500);
            e.Property(x => x.Summary).HasMaxLength(500);
            e.Property(x => x.Url).HasMaxLength(1024);
            e.Property(x => x.ImageUrl).HasMaxLength(1024);
            e.Property(x => x.ApId).HasMaxLength(1024);
            e.Property(x => x.ApIdHash).HasComputedColumnSql("HASHBYTES('SHA2_256', [ap_id])", stored: true);
            e.Property(x => x.PublishedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Uuid).IsUnique();
            e.HasIndex(x => x.ApIdHash).IsUnique();
            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => x.InReplyToId);
            e.HasIndex(x => x.ThreadRootId);
            e.HasIndex(x => new { x.Type, x.AuthorId, x.Id });
            e.HasOne(x => x.Author)
                .WithMany(x => x.Posts)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InReplyTo)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.InReplyToId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ThreadRoot)
                .WithMany()
                .HasForeignKey(x => x.ThreadRootId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ApFeeds ---
        modelBuilder.Entity<ApFeed>(e =>
        {
            e.ToTable("ApFeeds");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.UserId, x.PostId, x.RepostedById }).IsUnique().HasFilter(null);
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => x.RepostedById);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.PostType);
            e.HasIndex(x => x.Audience);
            e.HasIndex(x => x.PublishedAt);
            e.HasIndex(x => new { x.UserId, x.PostType, x.PublishedAt }).IsDescending(false, false, true);
            e.HasOne(x => x.User)
                .WithMany(x => x.Feeds)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Post)
                .WithMany(x => x.Feeds)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RepostedBy)
                .WithMany()
                .HasForeignKey(x => x.RepostedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ApLikes ---
        modelBuilder.Entity<ApLike>(e =>
        {
            e.ToTable("ApLikes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.AccountId, x.PostId }).IsUnique();
            e.HasIndex(x => x.PostId);
            e.HasOne(x => x.Account)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Post)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ApMentions ---
        modelBuilder.Entity<ApMention>(e =>
        {
            e.ToTable("ApMentions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.AccountId, x.PostId }).IsUnique();
            e.HasIndex(x => x.PostId);
            e.HasOne(x => x.Post)
                .WithMany(x => x.Mentions)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Account)
                .WithMany(x => x.Mentions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ApNotifications ---
        modelBuilder.Entity<ApNotification>(e =>
        {
            e.ToTable("ApNotifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Read).HasDefaultValue(false);
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.InReplyToPostId);
            e.HasIndex(x => new { x.UserId, x.Id }).IsDescending(false, true);
            e.HasIndex(x => new { x.UserId, x.Read });
            e.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Post)
                .WithMany()
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InReplyToPost)
                .WithMany()
                .HasForeignKey(x => x.InReplyToPostId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ApOutboxes ---
        modelBuilder.Entity<ApOutbox>(e =>
        {
            e.ToTable("ApOutboxes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Uuid).HasMaxLength(36).HasDefaultValueSql("CONVERT(NVARCHAR(36), NEWID())");
            e.HasIndex(x => x.Uuid).IsUnique();
            e.HasIndex(x => new { x.AccountId, x.PostId, x.OutboxType }).IsUnique();
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => new { x.AccountId, x.OutboxType, x.PublishedAt }).IsDescending(false, false, true);
            e.HasOne(x => x.Account)
                .WithMany(x => x.Outboxes)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Post)
                .WithMany(x => x.Outboxes)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ApReposts ---
        modelBuilder.Entity<ApRepost>(e =>
        {
            e.ToTable("ApReposts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.AccountId, x.PostId }).IsUnique();
            e.HasIndex(x => x.PostId);
            e.HasOne(x => x.Account)
                .WithMany(x => x.Reposts)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Post)
                .WithMany(x => x.Reposts)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Apply column lengths from T-SQL schema
        ConfigureColumnLengths(modelBuilder);

        // Apply server-side defaults from T-SQL schema
        ConfigureDefaults(modelBuilder);

        // Seed default data (roles, settings, newsletter, free product)
        SeedData.Apply(modelBuilder);

        // Apply snake_case naming to all tables, columns, keys, indexes, and FKs
        modelBuilder.ApplySnakeCaseNamingConvention();
    }

    private static void ConfigureColumnLengths(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(50);
            e.Property(x => x.NormalizedName).HasMaxLength(50);
            e.Property(x => x.ConcurrencyStamp).HasMaxLength(36);
            e.Property(x => x.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(50);
            e.Property(x => x.ObjectType).HasMaxLength(50);
            e.Property(x => x.ActionType).HasMaxLength(50);
            e.Property(x => x.ObjectId).HasMaxLength(24);
        });

        modelBuilder.Entity<Setting>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Group).HasMaxLength(50);
            e.Property(x => x.Key).HasMaxLength(50);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Flags).HasMaxLength(50);
        });

        modelBuilder.Entity<Label>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Slug).HasMaxLength(191);
        });

        modelBuilder.Entity<Benefit>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Slug).HasMaxLength(191);
        });

        modelBuilder.Entity<Integration>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Slug).HasMaxLength(191);
            e.Property(x => x.IconImage).HasMaxLength(2000);
            e.Property(x => x.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Collection>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Title).HasMaxLength(191);
            e.Property(x => x.Slug).HasMaxLength(191);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.FeatureImage).HasMaxLength(2000);
        });

        modelBuilder.Entity<CustomThemeSetting>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Theme).HasMaxLength(191);
            e.Property(x => x.Key).HasMaxLength(191);
            e.Property(x => x.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<Brute>(e =>
        {
            e.Property(x => x.Key).HasMaxLength(191);
        });

        modelBuilder.Entity<Job>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Metadata).HasMaxLength(2000);
        });

        modelBuilder.Entity<Milestone>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Type).HasMaxLength(24);
            e.Property(x => x.Currency).HasMaxLength(24);
        });

        modelBuilder.Entity<Recommendation>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Url).HasMaxLength(2000);
            e.Property(x => x.Title).HasMaxLength(2000);
            e.Property(x => x.Excerpt).HasMaxLength(2000);
            e.Property(x => x.FeaturedImage).HasMaxLength(2000);
            e.Property(x => x.Favicon).HasMaxLength(2000);
            e.Property(x => x.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Core.Entities.Action>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.ResourceId).HasMaxLength(24);
            e.Property(x => x.ResourceType).HasMaxLength(50);
            e.Property(x => x.ActorId).HasMaxLength(24);
            e.Property(x => x.ActorType).HasMaxLength(50);
            e.Property(x => x.Event).HasMaxLength(50);
        });

        modelBuilder.Entity<Snippet>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.UserName).HasMaxLength(191);
            e.Property(x => x.NormalizedUserName).HasMaxLength(191);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Slug).HasMaxLength(191);
            e.Property(x => x.GhostPassword).HasColumnName("password").HasMaxLength(60);
            e.Property(x => x.PasswordHash).HasMaxLength(256);
            e.Property(x => x.Email).HasMaxLength(191);
            e.Property(x => x.NormalizedEmail).HasMaxLength(191);
            e.Property(x => x.SecurityStamp).HasMaxLength(256);
            e.Property(x => x.ConcurrencyStamp).HasMaxLength(36);
            e.Property(x => x.ProfileImage).HasMaxLength(2000);
            e.Property(x => x.CoverImage).HasMaxLength(2000);
            e.Property(x => x.Website).HasMaxLength(2000);
            e.Property(x => x.Facebook).HasMaxLength(2000);
            e.Property(x => x.Twitter).HasMaxLength(2000);
            e.Property(x => x.Threads).HasMaxLength(191);
            e.Property(x => x.Bluesky).HasMaxLength(191);
            e.Property(x => x.Mastodon).HasMaxLength(191);
            e.Property(x => x.TikTok).HasMaxLength(191);
            e.Property(x => x.YouTube).HasMaxLength(191);
            e.Property(x => x.Instagram).HasMaxLength(191);
            e.Property(x => x.LinkedIn).HasMaxLength(191);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Locale).HasMaxLength(6);
            e.Property(x => x.Visibility).HasMaxLength(50);
            e.Property(x => x.MetaTitle).HasMaxLength(2000);
            e.Property(x => x.MetaDescription).HasMaxLength(2000);
        });

        modelBuilder.Entity<Newsletter>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Uuid).HasMaxLength(36);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Slug).HasMaxLength(191);
            e.Property(x => x.SenderName).HasMaxLength(191);
            e.Property(x => x.SenderEmail).HasMaxLength(191);
            e.Property(x => x.SenderReplyTo).HasMaxLength(191);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Visibility).HasMaxLength(50);
            e.Property(x => x.HeaderImage).HasMaxLength(2000);
            e.Property(x => x.TitleFontCategory).HasMaxLength(191);
            e.Property(x => x.TitleAlignment).HasMaxLength(191);
            e.Property(x => x.BodyFontCategory).HasMaxLength(191);
            e.Property(x => x.BackgroundColor).HasMaxLength(50);
            e.Property(x => x.PostTitleColor).HasMaxLength(50);
            e.Property(x => x.ButtonCorners).HasMaxLength(50);
            e.Property(x => x.ButtonStyle).HasMaxLength(50);
            e.Property(x => x.TitleFontWeight).HasMaxLength(50);
            e.Property(x => x.LinkStyle).HasMaxLength(50);
            e.Property(x => x.ImageCorners).HasMaxLength(50);
            e.Property(x => x.HeaderBackgroundColor).HasMaxLength(50);
            e.Property(x => x.SectionTitleColor).HasMaxLength(50);
            e.Property(x => x.DividerColor).HasMaxLength(50);
            e.Property(x => x.ButtonColor).HasMaxLength(50);
            e.Property(x => x.LinkColor).HasMaxLength(50);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Slug).HasMaxLength(191);
            e.Property(x => x.WelcomePageUrl).HasMaxLength(2000);
            e.Property(x => x.Visibility).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(191);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Currency).HasMaxLength(50);
            e.Property(x => x.MonthlyPriceId).HasMaxLength(24);
            e.Property(x => x.YearlyPriceId).HasMaxLength(24);
        });

        modelBuilder.Entity<Tag>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Slug).HasMaxLength(191);
            e.Property(x => x.FeatureImage).HasMaxLength(2000);
            e.Property(x => x.ParentId).HasMaxLength(191);
            e.Property(x => x.Visibility).HasMaxLength(50);
            e.Property(x => x.OgImage).HasMaxLength(2000);
            e.Property(x => x.OgTitle).HasMaxLength(300);
            e.Property(x => x.OgDescription).HasMaxLength(500);
            e.Property(x => x.TwitterImage).HasMaxLength(2000);
            e.Property(x => x.TwitterTitle).HasMaxLength(300);
            e.Property(x => x.TwitterDescription).HasMaxLength(500);
            e.Property(x => x.MetaTitle).HasMaxLength(2000);
            e.Property(x => x.MetaDescription).HasMaxLength(2000);
            e.Property(x => x.CanonicalUrl).HasMaxLength(2000);
            e.Property(x => x.AccentColor).HasMaxLength(50);
        });

        modelBuilder.Entity<Member>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Uuid).HasMaxLength(36);
            e.Property(x => x.TransientId).HasMaxLength(191);
            e.Property(x => x.Email).HasMaxLength(191);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Expertise).HasMaxLength(191);
            e.Property(x => x.Note).HasMaxLength(2000);
            e.Property(x => x.Geolocation).HasMaxLength(2000);
        });

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Secret).HasMaxLength(191);
            e.Property(x => x.RoleId).HasMaxLength(24);
            e.Property(x => x.IntegrationId).HasMaxLength(24);
            e.Property(x => x.UserId).HasMaxLength(24);
            e.Property(x => x.LastSeenVersion).HasMaxLength(50);
        });

        modelBuilder.Entity<Token>(e =>
        {
            // Widened from Ghost's nvarchar(24)/nvarchar(32) to fit our
            // GUID ids (36 chars) and SHA-256 hex token hashes (64 chars).
            // See widen-tokens-columns.sql for the prod migration.
            e.Property(x => x.Id).HasMaxLength(36);
            e.Property(x => x.TokenValue).HasMaxLength(64);
            e.Property(x => x.Uuid).HasMaxLength(36);
            e.Property(x => x.Data).HasMaxLength(2000);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.SessionId).HasMaxLength(32);
            e.Property(x => x.UserId).HasMaxLength(24);
            e.Property(x => x.SessionData).HasMaxLength(2000);
        });

        modelBuilder.Entity<Invite>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.RoleId).HasMaxLength(24);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Token).HasMaxLength(191);
            e.Property(x => x.Email).HasMaxLength(191);
        });

        modelBuilder.Entity<PermissionsRole>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.RoleId).HasMaxLength(24);
            e.Property(x => x.PermissionId).HasMaxLength(24);
        });

        modelBuilder.Entity<PermissionsUser>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.UserId).HasMaxLength(24);
            e.Property(x => x.PermissionId).HasMaxLength(24);
        });

        modelBuilder.Entity<RolesUser>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.RoleId).HasMaxLength(24);
            e.Property(x => x.UserId).HasMaxLength(24);
        });

        modelBuilder.Entity<Post>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Uuid).HasMaxLength(36);
            e.Property(x => x.Title).HasMaxLength(2000);
            e.Property(x => x.Slug).HasMaxLength(191);
            e.Property(x => x.CommentId).HasMaxLength(50);
            e.Property(x => x.FeatureImage).HasMaxLength(2000);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Locale).HasMaxLength(6);
            e.Property(x => x.Visibility).HasMaxLength(50);
            e.Property(x => x.PublishedBy).HasMaxLength(24);
            e.Property(x => x.CustomExcerpt).HasMaxLength(2000);
            e.Property(x => x.CustomTemplate).HasMaxLength(100);
            e.Property(x => x.NewsletterId).HasMaxLength(24);
            e.Property(x => x.ParentId).HasMaxLength(24);
        });

        modelBuilder.Entity<Offer>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Code).HasMaxLength(191);
            e.Property(x => x.ProductId).HasMaxLength(24);
            e.Property(x => x.StripeCouponId).HasMaxLength(255);
            e.Property(x => x.Interval).HasMaxLength(50);
            e.Property(x => x.Currency).HasMaxLength(50);
            e.Property(x => x.DiscountType).HasMaxLength(50);
            e.Property(x => x.Duration).HasMaxLength(50);
            e.Property(x => x.PortalTitle).HasMaxLength(191);
            e.Property(x => x.PortalDescription).HasMaxLength(2000);
            e.Property(x => x.RedemptionType).HasMaxLength(50);
        });

        modelBuilder.Entity<StripeProduct>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.ProductId).HasMaxLength(24);
            e.Property(x => x.StripeProductId).HasMaxLength(255);
        });

        modelBuilder.Entity<AutomatedEmail>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Slug).HasMaxLength(191);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Subject).HasMaxLength(300);
            e.Property(x => x.SenderName).HasMaxLength(191);
            e.Property(x => x.SenderEmail).HasMaxLength(191);
            e.Property(x => x.SenderReplyTo).HasMaxLength(191);
            e.Property(x => x.TriggerEvent).HasMaxLength(100);
        });

        modelBuilder.Entity<AutomatedEmailSchedule>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.AutomatedEmailId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.MemberUuid).HasMaxLength(36);
            e.Property(x => x.MemberEmail).HasMaxLength(191);
            e.Property(x => x.MemberName).HasMaxLength(191);
            e.Property(x => x.SiteUrl).HasMaxLength(500);
            e.Property(x => x.FailureReason).HasMaxLength(2000);
        });

        modelBuilder.Entity<MembersStripeCustomer>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.CustomerId).HasMaxLength(255);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Email).HasMaxLength(191);
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.TierId).HasMaxLength(24);
            e.Property(x => x.Cadence).HasMaxLength(50);
            e.Property(x => x.Currency).HasMaxLength(50);
            e.Property(x => x.PaymentProvider).HasMaxLength(50);
            e.Property(x => x.PaymentSubscriptionUrl).HasMaxLength(2000);
            e.Property(x => x.PaymentUserUrl).HasMaxLength(2000);
            e.Property(x => x.OfferId).HasMaxLength(24);
        });

        modelBuilder.Entity<MembersLabel>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.LabelId).HasMaxLength(24);
        });

        modelBuilder.Entity<MemberSegment>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.StatusFilter).HasMaxLength(20);
            e.Property(x => x.LabelId).HasMaxLength(24);
            e.Property(x => x.EngagementFilter).HasMaxLength(40);
            e.Property(x => x.SearchQuery).HasMaxLength(191);
        });

        modelBuilder.Entity<AdminAuditLog>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(36);
            e.Property(x => x.AdminUserId).HasMaxLength(24);
            e.Property(x => x.AdminUserEmail).HasMaxLength(191);
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.TargetType).HasMaxLength(50);
            e.Property(x => x.TargetId).HasMaxLength(36);
            e.Property(x => x.Metadata).HasMaxLength(2000);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
        });

        modelBuilder.Entity<MembersNewsletter>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.NewsletterId).HasMaxLength(24);
        });

        modelBuilder.Entity<MembersProduct>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.ProductId).HasMaxLength(24);
        });

        modelBuilder.Entity<MembersCancelEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.FromPlan).HasMaxLength(255);
        });

        modelBuilder.Entity<MembersCreatedEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.AttributionId).HasMaxLength(24);
            e.Property(x => x.AttributionType).HasMaxLength(50);
            e.Property(x => x.AttributionUrl).HasMaxLength(2000);
            e.Property(x => x.ReferrerSource).HasMaxLength(191);
            e.Property(x => x.ReferrerMedium).HasMaxLength(191);
            e.Property(x => x.ReferrerUrl).HasMaxLength(2000);
            e.Property(x => x.UtmSource).HasMaxLength(191);
            e.Property(x => x.UtmMedium).HasMaxLength(191);
            e.Property(x => x.UtmCampaign).HasMaxLength(191);
            e.Property(x => x.UtmTerm).HasMaxLength(191);
            e.Property(x => x.UtmContent).HasMaxLength(191);
            e.Property(x => x.Source).HasMaxLength(50);
            e.Property(x => x.BatchId).HasMaxLength(24);
        });

        modelBuilder.Entity<MembersEmailChangeEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.ToEmail).HasMaxLength(191);
            e.Property(x => x.FromEmail).HasMaxLength(191);
        });

        modelBuilder.Entity<MembersLoginEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
        });

        modelBuilder.Entity<MembersPaymentEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.Currency).HasMaxLength(191);
            e.Property(x => x.Source).HasMaxLength(50);
        });

        modelBuilder.Entity<MembersStatusEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.FromStatus).HasMaxLength(50);
            e.Property(x => x.ToStatus).HasMaxLength(50);
        });

        modelBuilder.Entity<MembersPaidSubscriptionEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.SubscriptionId).HasMaxLength(24);
            e.Property(x => x.FromPlan).HasMaxLength(255);
            e.Property(x => x.ToPlan).HasMaxLength(255);
            e.Property(x => x.Currency).HasMaxLength(191);
            e.Property(x => x.Source).HasMaxLength(50);
        });

        modelBuilder.Entity<MembersProductEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.ProductId).HasMaxLength(24);
            e.Property(x => x.Action).HasMaxLength(50);
        });

        modelBuilder.Entity<MembersSubscribeEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.Source).HasMaxLength(50);
            e.Property(x => x.NewsletterId).HasMaxLength(24);
        });

        modelBuilder.Entity<Webhook>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Event).HasMaxLength(50);
            e.Property(x => x.TargetUrl).HasMaxLength(2000);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Secret).HasMaxLength(191);
            e.Property(x => x.ApiVersion).HasMaxLength(50);
            e.Property(x => x.IntegrationId).HasMaxLength(24);
            e.Property(x => x.LastTriggeredStatus).HasMaxLength(50);
            e.Property(x => x.LastTriggeredError).HasMaxLength(50);
        });

        modelBuilder.Entity<ProductsBenefit>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.ProductId).HasMaxLength(24);
            e.Property(x => x.BenefitId).HasMaxLength(24);
        });

        modelBuilder.Entity<DonationPaymentEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Name).HasMaxLength(191);
            e.Property(x => x.Email).HasMaxLength(191);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.Currency).HasMaxLength(50);
            e.Property(x => x.AttributionId).HasMaxLength(24);
            e.Property(x => x.AttributionType).HasMaxLength(50);
            e.Property(x => x.AttributionUrl).HasMaxLength(2000);
            e.Property(x => x.ReferrerSource).HasMaxLength(191);
            e.Property(x => x.ReferrerMedium).HasMaxLength(191);
            e.Property(x => x.ReferrerUrl).HasMaxLength(2000);
            e.Property(x => x.UtmSource).HasMaxLength(191);
            e.Property(x => x.UtmMedium).HasMaxLength(191);
            e.Property(x => x.UtmCampaign).HasMaxLength(191);
            e.Property(x => x.UtmTerm).HasMaxLength(191);
            e.Property(x => x.UtmContent).HasMaxLength(191);
            e.Property(x => x.DonationMessage).HasMaxLength(255);
        });

        modelBuilder.Entity<Mention>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Source).HasMaxLength(2000);
            e.Property(x => x.SourceTitle).HasMaxLength(2000);
            e.Property(x => x.SourceSiteTitle).HasMaxLength(2000);
            e.Property(x => x.SourceExcerpt).HasMaxLength(2000);
            e.Property(x => x.SourceAuthor).HasMaxLength(2000);
            e.Property(x => x.SourceFeaturedImage).HasMaxLength(2000);
            e.Property(x => x.SourceFavicon).HasMaxLength(2000);
            e.Property(x => x.Target).HasMaxLength(2000);
            e.Property(x => x.ResourceId).HasMaxLength(24);
            e.Property(x => x.ResourceType).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue(MentionStatus.Approved);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<Outbox>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.EventType).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Message).HasMaxLength(2000);
        });

        modelBuilder.Entity<StripePrice>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.StripePriceId).HasMaxLength(255);
            e.Property(x => x.StripeProductId).HasMaxLength(255);
            e.Property(x => x.Nickname).HasMaxLength(255);
            e.Property(x => x.Currency).HasMaxLength(191);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Interval).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(191);
        });

        modelBuilder.Entity<PostsAuthor>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
            e.Property(x => x.AuthorId).HasMaxLength(24);
        });

        modelBuilder.Entity<PostMeta>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
            e.Property(x => x.OgImage).HasMaxLength(2000);
            e.Property(x => x.OgTitle).HasMaxLength(300);
            e.Property(x => x.OgDescription).HasMaxLength(500);
            e.Property(x => x.TwitterImage).HasMaxLength(2000);
            e.Property(x => x.TwitterTitle).HasMaxLength(300);
            e.Property(x => x.TwitterDescription).HasMaxLength(500);
            e.Property(x => x.MetaTitle).HasMaxLength(2000);
            e.Property(x => x.MetaDescription).HasMaxLength(2000);
            e.Property(x => x.EmailSubject).HasMaxLength(300);
            e.Property(x => x.EmailSubjectB).HasMaxLength(300);
            e.Property(x => x.FeatureImageAlt).HasMaxLength(2000);
        });

        modelBuilder.Entity<PostsTag>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
            e.Property(x => x.TagId).HasMaxLength(24);
        });

        modelBuilder.Entity<PostsProduct>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
            e.Property(x => x.ProductId).HasMaxLength(24);
        });

        modelBuilder.Entity<PostRevision>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
            e.Property(x => x.AuthorId).HasMaxLength(24);
            e.Property(x => x.Title).HasMaxLength(2000);
            e.Property(x => x.PostStatus).HasMaxLength(50);
            e.Property(x => x.Reason).HasMaxLength(50);
            e.Property(x => x.FeatureImage).HasMaxLength(2000);
            e.Property(x => x.FeatureImageAlt).HasMaxLength(2000);
            e.Property(x => x.CustomExcerpt).HasMaxLength(2000);
        });

        modelBuilder.Entity<MobiledocRevision>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
        });

        modelBuilder.Entity<CollectionsPost>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.CollectionId).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
        });

        modelBuilder.Entity<Email>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
            e.Property(x => x.Uuid).HasMaxLength(36);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Error).HasMaxLength(2000);
            e.Property(x => x.Subject).HasMaxLength(300);
            e.Property(x => x.From).HasMaxLength(2000);
            e.Property(x => x.ReplyTo).HasMaxLength(2000);
            e.Property(x => x.SourceType).HasMaxLength(50);
            e.Property(x => x.NewsletterId).HasMaxLength(24);
            e.Property(x => x.SubjectB).HasMaxLength(300);
            e.Property(x => x.AbTestPhase).HasMaxLength(20);
            e.Property(x => x.AbTestWinnerVariant).HasMaxLength(1);
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.ParentId).HasMaxLength(24);
            e.Property(x => x.InReplyToId).HasMaxLength(24);
            e.Property(x => x.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<Redirect>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.From).HasMaxLength(191);
            e.Property(x => x.To).HasMaxLength(2000);
            e.Property(x => x.PostId).HasMaxLength(24);
            e.Property(x => x.HitCount).HasDefaultValue(0L);
            e.Property(x => x.IsRegex).HasDefaultValue(false);
        });

        modelBuilder.Entity<MembersFeedback>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.PostId).HasMaxLength(24);
        });

        modelBuilder.Entity<RecommendationClickEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.RecommendationId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
        });

        modelBuilder.Entity<RecommendationSubscribeEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.RecommendationId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
        });

        modelBuilder.Entity<MembersClickEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.RedirectId).HasMaxLength(24);
        });

        modelBuilder.Entity<MembersStripeCustomerSubscription>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.CustomerId).HasMaxLength(255);
            e.Property(x => x.GhostSubscriptionId).HasMaxLength(24);
            e.Property(x => x.SubscriptionId).HasMaxLength(255);
            e.Property(x => x.StripePriceId).HasMaxLength(255);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.Property(x => x.DefaultPaymentCardLast4).HasMaxLength(4);
            e.Property(x => x.OfferId).HasMaxLength(24);
            e.Property(x => x.PlanId).HasMaxLength(255);
            e.Property(x => x.PlanNickname).HasMaxLength(50);
            e.Property(x => x.PlanInterval).HasMaxLength(50);
            e.Property(x => x.PlanCurrency).HasMaxLength(191);
        });

        modelBuilder.Entity<Suppression>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.Email).HasMaxLength(191);
            e.Property(x => x.EmailId).HasMaxLength(24);
            e.Property(x => x.Reason).HasMaxLength(50);
        });

        modelBuilder.Entity<AnalyticsEvent>(e =>
        {
            e.Property(x => x.SessionId).HasMaxLength(255);
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.Version).HasMaxLength(50);
            e.Property(x => x.SiteUuid).HasMaxLength(36);
            e.Property(x => x.Device).HasMaxLength(100);
            e.Property(x => x.Browser).HasMaxLength(100);
            e.Property(x => x.Os).HasMaxLength(100);
            e.Property(x => x.Country).HasMaxLength(10);
            e.Property(x => x.MemberUuid).HasMaxLength(36);
            e.Property(x => x.MemberStatus).HasMaxLength(50);
            e.Property(x => x.PostUuid).HasMaxLength(36);
        });

        modelBuilder.Entity<OfferRedemption>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.OfferId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.SubscriptionId).HasMaxLength(24);
        });

        modelBuilder.Entity<MembersSubscriptionCreatedEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.SubscriptionId).HasMaxLength(24);
            e.Property(x => x.AttributionId).HasMaxLength(24);
            e.Property(x => x.AttributionType).HasMaxLength(50);
            e.Property(x => x.AttributionUrl).HasMaxLength(2000);
            e.Property(x => x.ReferrerSource).HasMaxLength(191);
            e.Property(x => x.ReferrerMedium).HasMaxLength(191);
            e.Property(x => x.ReferrerUrl).HasMaxLength(2000);
            e.Property(x => x.UtmSource).HasMaxLength(191);
            e.Property(x => x.UtmMedium).HasMaxLength(191);
            e.Property(x => x.UtmCampaign).HasMaxLength(191);
            e.Property(x => x.UtmTerm).HasMaxLength(191);
            e.Property(x => x.UtmContent).HasMaxLength(191);
            e.Property(x => x.BatchId).HasMaxLength(24);
        });

        modelBuilder.Entity<CommentLike>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.CommentId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
        });

        modelBuilder.Entity<CommentReport>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.CommentId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
        });

        modelBuilder.Entity<EmailBatch>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.EmailId).HasMaxLength(24);
            e.Property(x => x.ProviderId).HasMaxLength(255);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
        });

        modelBuilder.Entity<EmailSpamComplaintEvent>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.EmailId).HasMaxLength(24);
            e.Property(x => x.EmailAddress).HasMaxLength(191);
        });

        modelBuilder.Entity<AutomatedEmailRecipient>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.AutomatedEmailId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.MemberUuid).HasMaxLength(36);
            e.Property(x => x.MemberEmail).HasMaxLength(191);
            e.Property(x => x.MemberName).HasMaxLength(191);
            e.Property(x => x.FailureReason).HasMaxLength(2000);
        });

        modelBuilder.Entity<EmailRecipient>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.EmailId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.BatchId).HasMaxLength(24);
            e.Property(x => x.MemberUuid).HasMaxLength(36);
            e.Property(x => x.MemberEmail).HasMaxLength(191);
            e.Property(x => x.MemberName).HasMaxLength(191);
            e.Property(x => x.AbVariant).HasMaxLength(10);
        });

        modelBuilder.Entity<EmailRecipientFailure>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(24);
            e.Property(x => x.EmailId).HasMaxLength(24);
            e.Property(x => x.MemberId).HasMaxLength(24);
            e.Property(x => x.EmailRecipientId).HasMaxLength(24);
            e.Property(x => x.EnhancedCode).HasMaxLength(50);
            e.Property(x => x.Message).HasMaxLength(2000);
            e.Property(x => x.Severity).HasMaxLength(50);
            e.Property(x => x.EventId).HasMaxLength(255);
        });
    }

    private static void ConfigureDefaults(ModelBuilder modelBuilder)
    {
        // CreatedAt → DEFAULT SYSUTCDATETIME() for all entities with created_at
        modelBuilder.Entity<Role>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Permission>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Setting>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Label>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MemberSegment>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<AdminAuditLog>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MemberNote>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Benefit>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Integration>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Collection>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Job>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Milestone>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Recommendation>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Core.Entities.Action>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Snippet>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<User>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Newsletter>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Product>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Tag>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Member>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApiKey>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Token>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Session>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Invite>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Post>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Offer>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<StripeProduct>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<AutomatedEmail>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersStripeCustomer>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Subscription>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersCancelEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersCreatedEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersEmailChangeEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersLoginEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersPaymentEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersStatusEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersPaidSubscriptionEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersProductEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersSubscribeEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Webhook>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<DonationPaymentEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Mention>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Outbox>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<StripePrice>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<PostRevision>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MobiledocRevision>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Email>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Comment>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Redirect>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersFeedback>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<RecommendationClickEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<RecommendationSubscribeEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersClickEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersStripeCustomerSubscription>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<Suppression>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<OfferRedemption>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<MembersSubscriptionCreatedEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<CommentLike>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<CommentReport>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<EmailBatch>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<EmailSpamComplaintEvent>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<AutomatedEmailRecipient>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<AutomatedEmailSchedule>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // AnalyticsEvent uses Timestamp instead of CreatedAt
        modelBuilder.Entity<AnalyticsEvent>().Property(x => x.Timestamp).HasDefaultValueSql("SYSUTCDATETIME()");

        // Analytics rollup tables
        modelBuilder.Entity<AnalyticsHourlyRollup>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<AnalyticsHourlyRollup>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<AnalyticsDailyRollup>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<AnalyticsDailyRollup>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // UpdatedAt → DEFAULT SYSUTCDATETIME() (only tables that have this in the DDL)
        modelBuilder.Entity<Comment>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<CommentLike>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<CommentReport>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<EmailBatch>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // ActivityPub CreatedAt defaults
        modelBuilder.Entity<ApSite>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApAccount>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApKeyValue>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApGhostApPostMapping>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApUser>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApAccountDeliveryBackoff>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApBlock>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApDomainBlock>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApFollow>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApPost>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApFeed>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApLike>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApMention>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApNotification>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApOutbox>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApRepost>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // ActivityPub UpdatedAt defaults
        modelBuilder.Entity<ApSite>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApAccount>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApKeyValue>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApUser>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApAccountDeliveryBackoff>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApFollow>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<ApPost>().Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
