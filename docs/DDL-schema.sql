-- =============================================================================
-- HowToSoftware T-SQL Schema
-- Translated from Ghost CMS MySQL (78 tables) + ActivityPub (17 tables)
-- Target: SQL Server 2025
-- =============================================================================
-- Type mapping:
--   MySQL varchar(N)          â†’ NVARCHAR(N)
--   MySQL text / longtext     â†’ NVARCHAR(MAX)
--   MySQL tinyint(1)          â†’ BIT
--   MySQL datetime            â†’ DATETIME2(7)
--   MySQL timestamp(6)        â†’ DATETIME2(7)
--   MySQL int unsigned AUTO_INCREMENT â†’ INT IDENTITY(1,1)
--   MySQL bigint AUTO_INCREMENT      â†’ BIGINT IDENTITY(1,1)
--   MySQL int unsigned        â†’ INT
--   MySQL json                â†’ NVARCHAR(MAX)
--   MySQL binary(32)          â†’ VARBINARY(32)
--   MySQL char(36)            â†’ NVARCHAR(36)
--   MySQL varchar(24) IDs     â†’ NVARCHAR(24) (kept for migration compatibility)
-- =============================================================================

SET NOCOUNT ON;
GO

-- =============================================================================
-- GHOST DATABASE TABLES (78 tables)
-- =============================================================================

-- =====================
-- Tier 0: No FK dependencies
-- =====================

-- -----------------------------------------------------------------------------
-- 1. Roles
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[roles] (
    [id]            NVARCHAR(24)    NOT NULL,
    [name]          NVARCHAR(50)    NOT NULL,
    [description]   NVARCHAR(2000)  NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_roles] PRIMARY KEY ([id]),
    CONSTRAINT [uq_roles_name] UNIQUE ([name])
);
GO

-- -----------------------------------------------------------------------------
-- 2. Permissions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[permissions] (
    [id]            NVARCHAR(24)    NOT NULL,
    [name]          NVARCHAR(50)    NOT NULL,
    [object_type]    NVARCHAR(50)    NOT NULL,
    [action_type]    NVARCHAR(50)    NOT NULL,
    [object_id]      NVARCHAR(24)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_permissions] PRIMARY KEY ([id]),
    CONSTRAINT [uq_permissions_name] UNIQUE ([name])
);
GO

-- -----------------------------------------------------------------------------
-- 3. Settings
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[settings] (
    [id]            NVARCHAR(24)    NOT NULL,
    [group]         NVARCHAR(50)    NOT NULL DEFAULT N'core',
    [key]           NVARCHAR(50)    NOT NULL,
    [value]         NVARCHAR(MAX)   NULL,
    [type]          NVARCHAR(50)    NOT NULL,
    [flags]         NVARCHAR(50)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_settings] PRIMARY KEY ([id]),
    CONSTRAINT [uq_settings_key] UNIQUE ([key])
);
GO

-- -----------------------------------------------------------------------------
-- 4. Labels
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[labels] (
    [id]            NVARCHAR(24)    NOT NULL,
    [name]          NVARCHAR(191)   NOT NULL,
    [slug]          NVARCHAR(191)   NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_labels] PRIMARY KEY ([id]),
    CONSTRAINT [uq_labels_name] UNIQUE ([name]),
    CONSTRAINT [uq_labels_slug] UNIQUE ([slug])
);
GO

-- -----------------------------------------------------------------------------
-- 5. Benefits
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[benefits] (
    [id]            NVARCHAR(24)    NOT NULL,
    [name]          NVARCHAR(191)   NOT NULL,
    [slug]          NVARCHAR(191)   NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_benefits] PRIMARY KEY ([id]),
    CONSTRAINT [uq_benefits_slug] UNIQUE ([slug])
);
GO

-- -----------------------------------------------------------------------------
-- 6. Integrations
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[integrations] (
    [id]            NVARCHAR(24)    NOT NULL,
    [type]          NVARCHAR(50)    NOT NULL DEFAULT N'custom',
    [name]          NVARCHAR(191)   NOT NULL,
    [slug]          NVARCHAR(191)   NOT NULL,
    [icon_image]     NVARCHAR(2000)  NULL,
    [description]   NVARCHAR(2000)  NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_integrations] PRIMARY KEY ([id]),
    CONSTRAINT [uq_integrations_slug] UNIQUE ([slug])
);
GO

-- -----------------------------------------------------------------------------
-- 7. Collections
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[collections] (
    [id]            NVARCHAR(24)    NOT NULL,
    [title]         NVARCHAR(191)   NOT NULL,
    [slug]          NVARCHAR(191)   NOT NULL,
    [description]   NVARCHAR(2000)  NULL,
    [type]          NVARCHAR(50)    NOT NULL,
    [filter]        NVARCHAR(MAX)   NULL,
    [feature_image]  NVARCHAR(2000)  NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_collections] PRIMARY KEY ([id]),
    CONSTRAINT [uq_collections_slug] UNIQUE ([slug])
);
GO

-- -----------------------------------------------------------------------------
-- 8. CustomThemeSettings
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[custom_theme_settings] (
    [id]            NVARCHAR(24)    NOT NULL,
    [theme]         NVARCHAR(191)   NOT NULL,
    [key]           NVARCHAR(191)   NOT NULL,
    [type]          NVARCHAR(50)    NOT NULL,
    [value]         NVARCHAR(MAX)   NULL,
    CONSTRAINT [pk_custom_theme_settings] PRIMARY KEY ([id])
);
GO

-- -----------------------------------------------------------------------------
-- 9. Brute
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[brute] (
    [key]           NVARCHAR(191)   NOT NULL,
    [first_request]  BIGINT          NOT NULL,
    [last_request]   BIGINT          NOT NULL,
    [lifetime]      BIGINT          NOT NULL,
    [count]         INT             NOT NULL,
    CONSTRAINT [pk_brute] PRIMARY KEY ([key])
);
GO

-- -----------------------------------------------------------------------------
-- 10. Jobs
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[jobs] (
    [id]            NVARCHAR(24)    NOT NULL,
    [name]          NVARCHAR(191)   NOT NULL,
    [status]        NVARCHAR(50)    NOT NULL DEFAULT N'queued',
    [started_at]     DATETIME2(7)    NULL,
    [finished_at]    DATETIME2(7)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    [metadata]      NVARCHAR(2000)  NULL,
    [queue_entry]    INT             NULL,
    CONSTRAINT [pk_jobs] PRIMARY KEY ([id]),
    CONSTRAINT [uq_jobs_name] UNIQUE ([name])
);
GO

-- -----------------------------------------------------------------------------
-- 11. Migrations
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ghost_migrations] (
    [id]                INT             NOT NULL IDENTITY(1,1),
    [name]              NVARCHAR(120)   NOT NULL,
    [version]           NVARCHAR(70)    NOT NULL,
    [current_version]    NVARCHAR(255)   NULL,
    CONSTRAINT [pk_ghost_migrations] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ghost_migrations_name_version] UNIQUE ([name], [version])
);
GO

-- -----------------------------------------------------------------------------
-- 12. MigrationsLock
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[migrations_lock] (
    [lock_key]       NVARCHAR(191)   NOT NULL,
    [locked]        BIT             NULL DEFAULT 0,
    [acquired_at]    DATETIME2(7)    NULL,
    [released_at]    DATETIME2(7)    NULL,
    CONSTRAINT [pk_migrations_lock] PRIMARY KEY ([lock_key])
);
GO

-- -----------------------------------------------------------------------------
-- 13. Milestones
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[milestones] (
    [id]            NVARCHAR(24)    NOT NULL,
    [type]          NVARCHAR(24)    NOT NULL,
    [value]         INT             NOT NULL,
    [currency]      NVARCHAR(24)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [email_sent_at]   DATETIME2(7)    NULL,
    CONSTRAINT [pk_milestones] PRIMARY KEY ([id])
);
GO

-- -----------------------------------------------------------------------------
-- 14. Recommendations
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[recommendations] (
    [id]                NVARCHAR(24)    NOT NULL,
    [url]               NVARCHAR(2000)  NOT NULL,
    [title]             NVARCHAR(2000)  NOT NULL,
    [excerpt]           NVARCHAR(2000)  NULL,
    [featured_image]     NVARCHAR(2000)  NULL,
    [favicon]           NVARCHAR(2000)  NULL,
    [description]       NVARCHAR(2000)  NULL,
    [one_click_subscribe] BIT             NOT NULL DEFAULT 0,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    CONSTRAINT [pk_recommendations] PRIMARY KEY ([id])
);
GO

-- -----------------------------------------------------------------------------
-- 15. Actions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[actions] (
    [id]            NVARCHAR(24)    NOT NULL,
    [resource_id]    NVARCHAR(24)    NULL,
    [resource_type]  NVARCHAR(50)    NOT NULL,
    [actor_id]       NVARCHAR(24)    NOT NULL,
    [actor_type]     NVARCHAR(50)    NOT NULL,
    [event]         NVARCHAR(50)    NOT NULL,
    [context]       NVARCHAR(MAX)   NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_actions] PRIMARY KEY ([id])
);
GO

-- -----------------------------------------------------------------------------
-- 16. Snippets
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[snippets] (
    [id]            NVARCHAR(24)    NOT NULL,
    [name]          NVARCHAR(191)   NOT NULL,
    [mobiledoc]     NVARCHAR(MAX)   NOT NULL,
    [lexical]       NVARCHAR(MAX)   NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_snippets] PRIMARY KEY ([id]),
    CONSTRAINT [uq_snippets_name] UNIQUE ([name])
);
GO

-- =====================
-- Tier 1: Independent entity tables
-- =====================

-- -----------------------------------------------------------------------------
-- 17. Users
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[users] (
    [id]                                    NVARCHAR(24)    NOT NULL,
    [name]                                  NVARCHAR(191)   NOT NULL,
    [slug]                                  NVARCHAR(191)   NOT NULL,
    [password]                              NVARCHAR(60)    NOT NULL,
    [email]                                 NVARCHAR(191)   NOT NULL,
    [profile_image]                          NVARCHAR(2000)  NULL,
    [cover_image]                            NVARCHAR(2000)  NULL,
    [bio]                                   NVARCHAR(MAX)   NULL,
    [website]                               NVARCHAR(2000)  NULL,
    [location]                              NVARCHAR(MAX)   NULL,
    [facebook]                              NVARCHAR(2000)  NULL,
    [twitter]                               NVARCHAR(2000)  NULL,
    [threads]                               NVARCHAR(191)   NULL,
    [bluesky]                               NVARCHAR(191)   NULL,
    [mastodon]                              NVARCHAR(191)   NULL,
    [tik_tok]                                NVARCHAR(191)   NULL,
    [you_tube]                               NVARCHAR(191)   NULL,
    [instagram]                             NVARCHAR(191)   NULL,
    [linked_in]                              NVARCHAR(191)   NULL,
    [accessibility]                         NVARCHAR(MAX)   NULL,
    [status]                                NVARCHAR(50)    NOT NULL DEFAULT N'active',
    [locale]                                NVARCHAR(6)     NULL,
    [visibility]                            NVARCHAR(50)    NOT NULL DEFAULT N'public',
    [meta_title]                             NVARCHAR(2000)  NULL,
    [meta_description]                       NVARCHAR(2000)  NULL,
    [tour]                                  NVARCHAR(MAX)   NULL,
    [last_seen]                              DATETIME2(7)    NULL,
    [comment_notifications]                  BIT             NOT NULL DEFAULT 1,
    [free_member_signup_notification]          BIT             NOT NULL DEFAULT 1,
    [paid_subscription_started_notification]   BIT             NOT NULL DEFAULT 1,
    [paid_subscription_canceled_notification]  BIT             NOT NULL DEFAULT 0,
    [mention_notifications]                  BIT             NOT NULL DEFAULT 1,
    [recommendation_notifications]           BIT             NOT NULL DEFAULT 1,
    [milestone_notifications]                BIT             NOT NULL DEFAULT 1,
    [donation_notifications]                 BIT             NOT NULL DEFAULT 1,
    [created_at]                             DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]                             DATETIME2(7)    NULL,
    CONSTRAINT [pk_users] PRIMARY KEY ([id]),
    CONSTRAINT [uq_users_slug] UNIQUE ([slug]),
    CONSTRAINT [uq_users_email] UNIQUE ([email])
);
GO

-- -----------------------------------------------------------------------------
-- 18. Newsletters
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[newsletters] (
    [id]                        NVARCHAR(24)    NOT NULL,
    [uuid]                      NVARCHAR(36)    NOT NULL,
    [name]                      NVARCHAR(191)   NOT NULL,
    [description]               NVARCHAR(2000)  NULL,
    [feedback_enabled]           BIT             NOT NULL DEFAULT 0,
    [slug]                      NVARCHAR(191)   NOT NULL,
    [sender_name]                NVARCHAR(191)   NULL,
    [sender_email]               NVARCHAR(191)   NULL,
    [sender_reply_to]             NVARCHAR(191)   NOT NULL DEFAULT N'newsletter',
    [status]                    NVARCHAR(50)    NOT NULL DEFAULT N'active',
    [visibility]                NVARCHAR(50)    NOT NULL DEFAULT N'members',
    [subscribe_on_signup]         BIT             NOT NULL DEFAULT 1,
    [sort_order]                 INT             NOT NULL DEFAULT 0,
    [header_image]               NVARCHAR(2000)  NULL,
    [show_header_icon]            BIT             NOT NULL DEFAULT 1,
    [show_header_title]           BIT             NOT NULL DEFAULT 1,
    [show_excerpt]               BIT             NOT NULL DEFAULT 0,
    [title_font_category]         NVARCHAR(191)   NOT NULL DEFAULT N'sans_serif',
    [title_alignment]            NVARCHAR(191)   NOT NULL DEFAULT N'center',
    [show_feature_image]          BIT             NOT NULL DEFAULT 1,
    [body_font_category]          NVARCHAR(191)   NOT NULL DEFAULT N'sans_serif',
    [footer_content]             NVARCHAR(MAX)   NULL,
    [show_badge]                 BIT             NOT NULL DEFAULT 1,
    [show_header_name]            BIT             NOT NULL DEFAULT 1,
    [show_post_title_section]      BIT             NOT NULL DEFAULT 1,
    [show_comment_cta]            BIT             NOT NULL DEFAULT 1,
    [show_subscription_details]   BIT             NOT NULL DEFAULT 0,
    [show_latest_posts]           BIT             NOT NULL DEFAULT 0,
    [background_color]           NVARCHAR(50)    NOT NULL DEFAULT N'light',
    [post_title_color]            NVARCHAR(50)    NULL,
    [created_at]                 DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]                 DATETIME2(7)    NULL,
    [button_corners]             NVARCHAR(50)    NOT NULL DEFAULT N'rounded',
    [button_style]               NVARCHAR(50)    NOT NULL DEFAULT N'fill',
    [title_font_weight]           NVARCHAR(50)    NOT NULL DEFAULT N'bold',
    [link_style]                 NVARCHAR(50)    NOT NULL DEFAULT N'underline',
    [image_corners]              NVARCHAR(50)    NOT NULL DEFAULT N'square',
    [header_background_color]     NVARCHAR(50)    NOT NULL DEFAULT N'transparent',
    [section_title_color]         NVARCHAR(50)    NULL,
    [divider_color]              NVARCHAR(50)    NULL,
    [button_color]               NVARCHAR(50)    NULL DEFAULT N'accent',
    [link_color]                 NVARCHAR(50)    NULL DEFAULT N'accent',
    CONSTRAINT [pk_newsletters] PRIMARY KEY ([id]),
    CONSTRAINT [uq_newsletters_uuid] UNIQUE ([uuid]),
    CONSTRAINT [uq_newsletters_name] UNIQUE ([name]),
    CONSTRAINT [uq_newsletters_slug] UNIQUE ([slug])
);
GO

-- -----------------------------------------------------------------------------
-- 19. Products (Tiers)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[products] (
    [id]                NVARCHAR(24)    NOT NULL,
    [name]              NVARCHAR(191)   NOT NULL,
    [slug]              NVARCHAR(191)   NOT NULL,
    [active]            BIT             NOT NULL DEFAULT 1,
    [welcome_page_url]    NVARCHAR(2000)  NULL,
    [visibility]        NVARCHAR(50)    NOT NULL DEFAULT N'none',
    [trial_days]         INT             NOT NULL DEFAULT 0,
    [description]       NVARCHAR(191)   NULL,
    [type]              NVARCHAR(50)    NOT NULL DEFAULT N'paid',
    [currency]          NVARCHAR(50)    NULL,
    [monthly_price]      INT             NULL,
    [yearly_price]       INT             NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    [monthly_price_id]    NVARCHAR(24)    NULL,
    [yearly_price_id]     NVARCHAR(24)    NULL,
    CONSTRAINT [pk_products] PRIMARY KEY ([id]),
    CONSTRAINT [uq_products_slug] UNIQUE ([slug])
);
GO

-- -----------------------------------------------------------------------------
-- 20. Tags
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[tags] (
    [id]                    NVARCHAR(24)    NOT NULL,
    [name]                  NVARCHAR(191)   NOT NULL,
    [slug]                  NVARCHAR(191)   NOT NULL,
    [description]           NVARCHAR(MAX)   NULL,
    [feature_image]          NVARCHAR(2000)  NULL,
    [parent_id]              NVARCHAR(191)   NULL,
    [visibility]            NVARCHAR(50)    NOT NULL DEFAULT N'public',
    [og_image]               NVARCHAR(2000)  NULL,
    [og_title]               NVARCHAR(300)   NULL,
    [og_description]         NVARCHAR(500)   NULL,
    [twitter_image]          NVARCHAR(2000)  NULL,
    [twitter_title]          NVARCHAR(300)   NULL,
    [twitter_description]    NVARCHAR(500)   NULL,
    [meta_title]             NVARCHAR(2000)  NULL,
    [meta_description]       NVARCHAR(2000)  NULL,
    [codeinjection_head]     NVARCHAR(MAX)   NULL,
    [codeinjection_foot]     NVARCHAR(MAX)   NULL,
    [canonical_url]          NVARCHAR(2000)  NULL,
    [accent_color]           NVARCHAR(50)    NULL,
    [created_at]             DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]             DATETIME2(7)    NULL,
    CONSTRAINT [pk_tags] PRIMARY KEY ([id]),
    CONSTRAINT [uq_tags_slug] UNIQUE ([slug])
);
GO

-- -----------------------------------------------------------------------------
-- 21. Members
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members] (
    [id]                            NVARCHAR(24)    NOT NULL,
    [uuid]                          NVARCHAR(36)    NOT NULL,
    [transient_id]                   NVARCHAR(191)   NOT NULL,
    [email]                         NVARCHAR(191)   NOT NULL,
    [status]                        NVARCHAR(50)    NOT NULL DEFAULT N'free',
    [name]                          NVARCHAR(191)   NULL,
    [expertise]                     NVARCHAR(191)   NULL,
    [note]                          NVARCHAR(2000)  NULL,
    [geolocation]                   NVARCHAR(2000)  NULL,
    [enable_comment_notifications]    BIT             NOT NULL DEFAULT 1,
    [email_count]                    INT             NOT NULL DEFAULT 0,
    [email_opened_count]              INT             NOT NULL DEFAULT 0,
    [email_open_rate]                 INT             NULL,
    [email_disabled]                 BIT             NOT NULL DEFAULT 0,
    [last_seen_at]                    DATETIME2(7)    NULL,
    [last_commented_at]               DATETIME2(7)    NULL,
    [created_at]                     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]                     DATETIME2(7)    NULL,
    [commenting]                    NVARCHAR(MAX)   NULL,
    CONSTRAINT [pk_members] PRIMARY KEY ([id]),
    CONSTRAINT [uq_members_uuid] UNIQUE ([uuid]),
    CONSTRAINT [uq_members_transient_id] UNIQUE ([transient_id]),
    CONSTRAINT [uq_members_email] UNIQUE ([email])
);
CREATE INDEX [ix_members_email_open_rate] ON [dbo].[members] ([email_open_rate]);
CREATE INDEX [ix_members_email_disabled] ON [dbo].[members] ([email_disabled]);
GO

-- -----------------------------------------------------------------------------
-- 22. ApiKeys
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[api_keys] (
    [id]                NVARCHAR(24)    NOT NULL,
    [type]              NVARCHAR(50)    NOT NULL,
    [secret]            NVARCHAR(191)   NOT NULL,
    [role_id]            NVARCHAR(24)    NULL,
    [integration_id]     NVARCHAR(24)    NULL,
    [user_id]            NVARCHAR(24)    NULL,
    [last_seen_at]        DATETIME2(7)    NULL,
    [last_seen_version]   NVARCHAR(50)    NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    CONSTRAINT [pk_api_keys] PRIMARY KEY ([id]),
    CONSTRAINT [uq_api_keys_secret] UNIQUE ([secret])
);
GO

-- -----------------------------------------------------------------------------
-- 23. Tokens
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[tokens] (
    [id]            NVARCHAR(36)    NOT NULL,
    [token]         NVARCHAR(64)    NOT NULL,
    [uuid]          NVARCHAR(36)    NOT NULL,
    [data]          NVARCHAR(2000)  NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    [first_used_at]   DATETIME2(7)    NULL,
    [used_count]     INT             NOT NULL DEFAULT 0,
    [otc_used_count]  INT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_tokens] PRIMARY KEY ([id]),
    CONSTRAINT [uq_tokens_uuid] UNIQUE ([uuid])
);
CREATE INDEX [ix_tokens_token] ON [dbo].[tokens] ([token]);
GO

-- -----------------------------------------------------------------------------
-- 24. Sessions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[sessions] (
    [id]            NVARCHAR(24)    NOT NULL,
    [session_id]     NVARCHAR(32)    NOT NULL,
    [user_id]        NVARCHAR(24)    NOT NULL,
    [session_data]   NVARCHAR(2000)  NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_sessions] PRIMARY KEY ([id]),
    CONSTRAINT [uq_sessions_session_id] UNIQUE ([session_id])
);
GO

-- -----------------------------------------------------------------------------
-- 25. Invites
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[invites] (
    [id]            NVARCHAR(24)    NOT NULL,
    [role_id]        NVARCHAR(24)    NOT NULL,
    [status]        NVARCHAR(50)    NOT NULL DEFAULT N'pending',
    [token]         NVARCHAR(191)   NOT NULL,
    [email]         NVARCHAR(191)   NOT NULL,
    [expires]       BIGINT          NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_invites] PRIMARY KEY ([id]),
    CONSTRAINT [uq_invites_token] UNIQUE ([token]),
    CONSTRAINT [uq_invites_email] UNIQUE ([email])
);
GO

-- -----------------------------------------------------------------------------
-- 26. PermissionsRoles (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[permissions_roles] (
    [id]            NVARCHAR(24)    NOT NULL,
    [role_id]        NVARCHAR(24)    NOT NULL,
    [permission_id]  NVARCHAR(24)    NOT NULL,
    CONSTRAINT [pk_permissions_roles] PRIMARY KEY ([id])
);
GO

-- -----------------------------------------------------------------------------
-- 27. PermissionsUsers (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[permissions_users] (
    [id]            NVARCHAR(24)    NOT NULL,
    [user_id]        NVARCHAR(24)    NOT NULL,
    [permission_id]  NVARCHAR(24)    NOT NULL,
    CONSTRAINT [pk_permissions_users] PRIMARY KEY ([id])
);
GO

-- -----------------------------------------------------------------------------
-- 28. RolesUsers (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[roles_users] (
    [id]            NVARCHAR(24)    NOT NULL,
    [role_id]        NVARCHAR(24)    NOT NULL,
    [user_id]        NVARCHAR(24)    NOT NULL,
    CONSTRAINT [pk_roles_users] PRIMARY KEY ([id])
);
GO

-- =====================
-- Tier 2: Tables with FK dependencies on Tier 0/1
-- =====================

-- -----------------------------------------------------------------------------
-- 29. Posts
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[posts] (
    [id]                        NVARCHAR(24)    NOT NULL,
    [uuid]                      NVARCHAR(36)    NOT NULL,
    [title]                     NVARCHAR(2000)  NOT NULL,
    [slug]                      NVARCHAR(191)   NOT NULL,
    [mobiledoc]                 NVARCHAR(MAX)   NULL,
    [lexical]                   NVARCHAR(MAX)   NULL,
    [html]                      NVARCHAR(MAX)   NULL,
    [comment_id]                 NVARCHAR(50)    NULL,
    [plaintext]                 NVARCHAR(MAX)   NULL,
    [feature_image]              NVARCHAR(2000)  NULL,
    [featured]                  BIT             NOT NULL DEFAULT 0,
    [type]                      NVARCHAR(50)    NOT NULL DEFAULT N'post',
    [status]                    NVARCHAR(50)    NOT NULL DEFAULT N'draft',
    [locale]                    NVARCHAR(6)     NULL,
    [visibility]                NVARCHAR(50)    NOT NULL DEFAULT N'public',
    [email_recipient_filter]      NVARCHAR(MAX)   NOT NULL DEFAULT N'all',
    [created_at]                 DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]                 DATETIME2(7)    NULL,
    [published_at]               DATETIME2(7)    NULL,
    [published_by]               NVARCHAR(24)    NULL,
    [custom_excerpt]             NVARCHAR(2000)  NULL,
    [codeinjection_head]         NVARCHAR(MAX)   NULL,
    [codeinjection_foot]         NVARCHAR(MAX)   NULL,
    [custom_template]            NVARCHAR(100)   NULL,
    [canonical_url]              NVARCHAR(MAX)   NULL,
    [newsletter_id]              NVARCHAR(24)    NULL,
    [show_title_and_feature_image]  BIT             NOT NULL DEFAULT 1,
    CONSTRAINT [pk_posts] PRIMARY KEY ([id]),
    CONSTRAINT [uq_posts_slug_type] UNIQUE ([slug], [type]),
    CONSTRAINT [fk_posts_newsletters] FOREIGN KEY ([newsletter_id]) REFERENCES [dbo].[newsletters]([id])
);
CREATE INDEX [ix_posts_uuid] ON [dbo].[posts] ([uuid]);
CREATE INDEX [ix_posts_updated_at] ON [dbo].[posts] ([updated_at]);
CREATE INDEX [ix_posts_published_at] ON [dbo].[posts] ([published_at]);
CREATE INDEX [ix_posts_newsletter_id] ON [dbo].[posts] ([newsletter_id]);
CREATE INDEX [ix_posts_type_status_updated_at] ON [dbo].[posts] ([type], [status], [updated_at]);
GO

-- -----------------------------------------------------------------------------
-- 30. Offers
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[offers] (
    [id]                NVARCHAR(24)    NOT NULL,
    [active]            BIT             NOT NULL DEFAULT 1,
    [name]              NVARCHAR(191)   NOT NULL,
    [code]              NVARCHAR(191)   NOT NULL,
    [product_id]         NVARCHAR(24)    NULL,
    [stripe_coupon_id]    NVARCHAR(255)   NULL,
    [interval]          NVARCHAR(50)    NOT NULL,
    [currency]          NVARCHAR(50)    NULL,
    [discount_type]      NVARCHAR(50)    NOT NULL,
    [discount_amount]    INT             NOT NULL,
    [duration]          NVARCHAR(50)    NOT NULL,
    [duration_in_months]  INT             NULL,
    [portal_title]       NVARCHAR(191)   NULL,
    [portal_description] NVARCHAR(2000)  NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    [redemption_type]    NVARCHAR(50)    NOT NULL DEFAULT N'signup',
    CONSTRAINT [pk_offers] PRIMARY KEY ([id]),
    CONSTRAINT [uq_offers_name] UNIQUE ([name]),
    CONSTRAINT [uq_offers_code] UNIQUE ([code]),
    CONSTRAINT [uq_offers_stripe_coupon_id] UNIQUE ([stripe_coupon_id]),
    CONSTRAINT [fk_offers_products] FOREIGN KEY ([product_id]) REFERENCES [dbo].[products]([id])
);
CREATE INDEX [ix_offers_product_id] ON [dbo].[offers] ([product_id]);
GO

-- -----------------------------------------------------------------------------
-- 31. StripeProducts
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[stripe_products] (
    [id]                NVARCHAR(24)    NOT NULL,
    [product_id]         NVARCHAR(24)    NULL,
    [stripe_product_id]   NVARCHAR(255)   NOT NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    CONSTRAINT [pk_stripe_products] PRIMARY KEY ([id]),
    CONSTRAINT [uq_stripe_products_stripe_product_id] UNIQUE ([stripe_product_id]),
    CONSTRAINT [fk_stripe_products_products] FOREIGN KEY ([product_id]) REFERENCES [dbo].[products]([id])
);
CREATE INDEX [ix_stripe_products_product_id] ON [dbo].[stripe_products] ([product_id]);
GO

-- -----------------------------------------------------------------------------
-- 32. AutomatedEmails
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[automated_emails] (
    [id]                NVARCHAR(24)    NOT NULL,
    [status]            NVARCHAR(50)    NOT NULL DEFAULT N'inactive',
    [slug]              NVARCHAR(191)   NOT NULL,
    [name]              NVARCHAR(191)   NOT NULL,
    [subject]           NVARCHAR(300)   NOT NULL,
    [lexical]           NVARCHAR(MAX)   NULL,
    [sender_name]        NVARCHAR(191)   NULL,
    [sender_email]       NVARCHAR(191)   NULL,
    [sender_reply_to]     NVARCHAR(191)   NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    CONSTRAINT [pk_automated_emails] PRIMARY KEY ([id]),
    CONSTRAINT [uq_automated_emails_slug] UNIQUE ([slug]),
    CONSTRAINT [uq_automated_emails_name] UNIQUE ([name])
);
CREATE INDEX [ix_automated_emails_slug] ON [dbo].[automated_emails] ([slug]);
CREATE INDEX [ix_automated_emails_status] ON [dbo].[automated_emails] ([status]);
GO

-- -----------------------------------------------------------------------------
-- 33. MembersStripeCustomers
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_stripe_customers] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [customer_id]    NVARCHAR(255)   NOT NULL,
    [name]          NVARCHAR(191)   NULL,
    [email]         NVARCHAR(191)   NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_members_stripe_customers] PRIMARY KEY ([id]),
    CONSTRAINT [uq_members_stripe_customers_customer_id] UNIQUE ([customer_id]),
    CONSTRAINT [fk_members_stripe_customers_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_stripe_customers_member_id] ON [dbo].[members_stripe_customers] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 34. Subscriptions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[subscriptions] (
    [id]                        NVARCHAR(24)    NOT NULL,
    [type]                      NVARCHAR(50)    NOT NULL,
    [status]                    NVARCHAR(50)    NOT NULL,
    [member_id]                  NVARCHAR(24)    NOT NULL,
    [tier_id]                    NVARCHAR(24)    NOT NULL,
    [cadence]                   NVARCHAR(50)    NULL,
    [currency]                  NVARCHAR(50)    NULL,
    [amount]                    INT             NULL,
    [payment_provider]           NVARCHAR(50)    NULL,
    [payment_subscription_url]    NVARCHAR(2000)  NULL,
    [payment_user_url]            NVARCHAR(2000)  NULL,
    [offer_id]                   NVARCHAR(24)    NULL,
    [expires_at]                 DATETIME2(7)    NULL,
    [created_at]                 DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]                 DATETIME2(7)    NULL,
    CONSTRAINT [pk_subscriptions] PRIMARY KEY ([id]),
    CONSTRAINT [fk_subscriptions_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_subscriptions_products] FOREIGN KEY ([tier_id]) REFERENCES [dbo].[products]([id]),
    CONSTRAINT [fk_subscriptions_offers] FOREIGN KEY ([offer_id]) REFERENCES [dbo].[offers]([id])
);
CREATE INDEX [ix_subscriptions_member_id] ON [dbo].[subscriptions] ([member_id]);
CREATE INDEX [ix_subscriptions_tier_id] ON [dbo].[subscriptions] ([tier_id]);
CREATE INDEX [ix_subscriptions_offer_id] ON [dbo].[subscriptions] ([offer_id]);
GO

-- -----------------------------------------------------------------------------
-- 35. MembersLabels (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_labels] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [label_id]       NVARCHAR(24)    NOT NULL,
    [sort_order]     INT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_members_labels] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_labels_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_members_labels_labels] FOREIGN KEY ([label_id]) REFERENCES [dbo].[labels]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_labels_member_id] ON [dbo].[members_labels] ([member_id]);
CREATE INDEX [ix_members_labels_label_id] ON [dbo].[members_labels] ([label_id]);
GO

-- -----------------------------------------------------------------------------
-- 36. MembersNewsletters (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_newsletters] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [newsletter_id]  NVARCHAR(24)    NOT NULL,
    CONSTRAINT [pk_members_newsletters] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_newsletters_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_members_newsletters_newsletters] FOREIGN KEY ([newsletter_id]) REFERENCES [dbo].[newsletters]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_newsletters_member_id] ON [dbo].[members_newsletters] ([member_id]);
CREATE INDEX [ix_members_newsletters_newsletter_id_member_id] ON [dbo].[members_newsletters] ([newsletter_id], [member_id]);
GO

-- -----------------------------------------------------------------------------
-- 37. MembersProducts (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_products] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [product_id]     NVARCHAR(24)    NOT NULL,
    [sort_order]     INT             NOT NULL DEFAULT 0,
    [expiry_at]      DATETIME2(7)    NULL,
    CONSTRAINT [pk_members_products] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_products_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_members_products_products] FOREIGN KEY ([product_id]) REFERENCES [dbo].[products]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_products_member_id] ON [dbo].[members_products] ([member_id]);
CREATE INDEX [ix_members_products_product_id] ON [dbo].[members_products] ([product_id]);
GO

-- -----------------------------------------------------------------------------
-- 38. MembersCancelEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_cancel_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [from_plan]      NVARCHAR(255)   NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_members_cancel_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_cancel_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_cancel_events_member_id] ON [dbo].[members_cancel_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 39. MembersCreatedEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_created_events] (
    [id]                NVARCHAR(24)    NOT NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [member_id]          NVARCHAR(24)    NOT NULL,
    [attribution_id]     NVARCHAR(24)    NULL,
    [attribution_type]   NVARCHAR(50)    NULL,
    [attribution_url]    NVARCHAR(2000)  NULL,
    [referrer_source]    NVARCHAR(191)   NULL,
    [referrer_medium]    NVARCHAR(191)   NULL,
    [referrer_url]       NVARCHAR(2000)  NULL,
    [utm_source]         NVARCHAR(191)   NULL,
    [utm_medium]         NVARCHAR(191)   NULL,
    [utm_campaign]       NVARCHAR(191)   NULL,
    [utm_term]           NVARCHAR(191)   NULL,
    [utm_content]        NVARCHAR(191)   NULL,
    [source]             NVARCHAR(50)    NOT NULL,
    [batch_id]           NVARCHAR(24)    NULL,
    CONSTRAINT [pk_members_created_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_created_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_created_events_member_id] ON [dbo].[members_created_events] ([member_id]);
CREATE INDEX [ix_members_created_events_attribution_id] ON [dbo].[members_created_events] ([attribution_id]);
GO

-- -----------------------------------------------------------------------------
-- 40. MembersEmailChangeEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_email_change_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [to_email]       NVARCHAR(191)   NOT NULL,
    [from_email]     NVARCHAR(191)   NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_members_email_change_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_email_change_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_email_change_events_member_id] ON [dbo].[members_email_change_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 41. MembersLoginEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_login_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_members_login_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_login_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_login_events_member_id] ON [dbo].[members_login_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 42. MembersPaymentEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_payment_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [amount]        INT             NOT NULL,
    [currency]      NVARCHAR(191)   NOT NULL,
    [source]        NVARCHAR(50)    NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_members_payment_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_payment_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_payment_events_member_id] ON [dbo].[members_payment_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 43. MembersStatusEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_status_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [from_status]    NVARCHAR(50)    NULL,
    [to_status]      NVARCHAR(50)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_members_status_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_status_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_status_events_member_id] ON [dbo].[members_status_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 44. MembersPaidSubscriptionEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_paid_subscription_events] (
    [id]                NVARCHAR(24)    NOT NULL,
    [type]              NVARCHAR(50)    NULL,
    [member_id]          NVARCHAR(24)    NOT NULL,
    [subscription_id]    NVARCHAR(24)    NULL,
    [from_plan]          NVARCHAR(255)   NULL,
    [to_plan]            NVARCHAR(255)   NULL,
    [currency]          NVARCHAR(191)   NOT NULL,
    [source]            NVARCHAR(50)    NOT NULL,
    [mrr_delta]          INT             NOT NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_members_paid_subscription_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_paid_subscription_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_paid_subscription_events_member_id] ON [dbo].[members_paid_subscription_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 45. MembersProductEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_product_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [product_id]     NVARCHAR(24)    NOT NULL,
    [action]        NVARCHAR(50)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_members_product_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_product_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_members_product_events_products] FOREIGN KEY ([product_id]) REFERENCES [dbo].[products]([id])
);
CREATE INDEX [ix_members_product_events_member_id] ON [dbo].[members_product_events] ([member_id]);
CREATE INDEX [ix_members_product_events_product_id] ON [dbo].[members_product_events] ([product_id]);
GO

-- -----------------------------------------------------------------------------
-- 46. MembersSubscribeEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_subscribe_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [subscribed]    BIT             NOT NULL DEFAULT 1,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [source]        NVARCHAR(50)    NULL,
    [newsletter_id]  NVARCHAR(24)    NULL,
    CONSTRAINT [pk_members_subscribe_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_subscribe_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_members_subscribe_events_newsletters] FOREIGN KEY ([newsletter_id]) REFERENCES [dbo].[newsletters]([id])
);
CREATE INDEX [ix_members_subscribe_events_member_id] ON [dbo].[members_subscribe_events] ([member_id]);
CREATE INDEX [ix_members_subscribe_events_newsletter_id_created_at] ON [dbo].[members_subscribe_events] ([newsletter_id], [created_at]);
GO

-- -----------------------------------------------------------------------------
-- 47. Webhooks
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[webhooks] (
    [id]                    NVARCHAR(24)    NOT NULL,
    [event]                 NVARCHAR(50)    NOT NULL,
    [target_url]             NVARCHAR(2000)  NOT NULL,
    [name]                  NVARCHAR(191)   NULL,
    [secret]                NVARCHAR(191)   NULL,
    [api_version]            NVARCHAR(50)    NOT NULL DEFAULT N'v2',
    [integration_id]         NVARCHAR(24)    NOT NULL,
    [last_triggered_at]       DATETIME2(7)    NULL,
    [last_triggered_status]   NVARCHAR(50)    NULL,
    [last_triggered_error]    NVARCHAR(50)    NULL,
    [created_at]             DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]             DATETIME2(7)    NULL,
    CONSTRAINT [pk_webhooks] PRIMARY KEY ([id]),
    CONSTRAINT [fk_webhooks_integrations] FOREIGN KEY ([integration_id]) REFERENCES [dbo].[integrations]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_webhooks_integration_id] ON [dbo].[webhooks] ([integration_id]);
GO

-- -----------------------------------------------------------------------------
-- 48. ProductsBenefits (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[products_benefits] (
    [id]            NVARCHAR(24)    NOT NULL,
    [product_id]     NVARCHAR(24)    NOT NULL,
    [benefit_id]     NVARCHAR(24)    NOT NULL,
    [sort_order]     INT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_products_benefits] PRIMARY KEY ([id]),
    CONSTRAINT [fk_products_benefits_products] FOREIGN KEY ([product_id]) REFERENCES [dbo].[products]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_products_benefits_benefits] FOREIGN KEY ([benefit_id]) REFERENCES [dbo].[benefits]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_products_benefits_product_id] ON [dbo].[products_benefits] ([product_id]);
CREATE INDEX [ix_products_benefits_benefit_id] ON [dbo].[products_benefits] ([benefit_id]);
GO

-- -----------------------------------------------------------------------------
-- 49. DonationPaymentEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[donation_payment_events] (
    [id]                NVARCHAR(24)    NOT NULL,
    [name]              NVARCHAR(191)   NULL,
    [email]             NVARCHAR(191)   NOT NULL,
    [member_id]          NVARCHAR(24)    NULL,
    [amount]            INT             NOT NULL,
    [currency]          NVARCHAR(50)    NOT NULL,
    [attribution_id]     NVARCHAR(24)    NULL,
    [attribution_type]   NVARCHAR(50)    NULL,
    [attribution_url]    NVARCHAR(2000)  NULL,
    [referrer_source]    NVARCHAR(191)   NULL,
    [referrer_medium]    NVARCHAR(191)   NULL,
    [referrer_url]       NVARCHAR(2000)  NULL,
    [utm_source]         NVARCHAR(191)   NULL,
    [utm_medium]         NVARCHAR(191)   NULL,
    [utm_campaign]       NVARCHAR(191)   NULL,
    [utm_term]           NVARCHAR(191)   NULL,
    [utm_content]        NVARCHAR(191)   NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [donation_message]   NVARCHAR(255)   NULL,
    CONSTRAINT [pk_donation_payment_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_donation_payment_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE SET NULL
);
CREATE INDEX [ix_donation_payment_events_member_id] ON [dbo].[donation_payment_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 50. Mentions (Webmention)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[mentions] (
    [id]                    NVARCHAR(24)    NOT NULL,
    [source]                NVARCHAR(2000)  NOT NULL,
    [source_title]           NVARCHAR(2000)  NULL,
    [source_site_title]       NVARCHAR(2000)  NULL,
    [source_excerpt]         NVARCHAR(2000)  NULL,
    [source_author]          NVARCHAR(2000)  NULL,
    [source_featured_image]   NVARCHAR(2000)  NULL,
    [source_favicon]         NVARCHAR(2000)  NULL,
    [target]                NVARCHAR(2000)  NOT NULL,
    [resource_id]            NVARCHAR(24)    NULL,
    [resource_type]          NVARCHAR(50)    NULL,
    [created_at]             DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [payload]               NVARCHAR(MAX)   NULL,
    [deleted]               BIT             NOT NULL DEFAULT 0,
    [verified]              BIT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_mentions] PRIMARY KEY ([id])
);
GO

-- -----------------------------------------------------------------------------
-- 51. Outbox (event outbox)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[outbox] (
    [id]            NVARCHAR(24)    NOT NULL,
    [event_type]     NVARCHAR(50)    NOT NULL,
    [status]        NVARCHAR(50)    NOT NULL DEFAULT N'pending',
    [payload]       NVARCHAR(MAX)   NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    [retry_count]    INT             NOT NULL DEFAULT 0,
    [last_retry_at]   DATETIME2(7)    NULL,
    [message]       NVARCHAR(2000)  NULL,
    CONSTRAINT [pk_outbox] PRIMARY KEY ([id])
);
CREATE INDEX [ix_outbox_event_type_status_created_at] ON [dbo].[outbox] ([event_type], [status], [created_at]);
GO

-- =====================
-- Tier 3: Tables with FK to Posts/Emails/etc.
-- =====================

-- -----------------------------------------------------------------------------
-- 52. StripePrices
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[stripe_prices] (
    [id]                NVARCHAR(24)    NOT NULL,
    [stripe_price_id]     NVARCHAR(255)   NOT NULL,
    [stripe_product_id]   NVARCHAR(255)   NOT NULL,
    [active]            BIT             NOT NULL,
    [nickname]          NVARCHAR(255)   NULL,
    [currency]          NVARCHAR(191)   NOT NULL,
    [amount]            INT             NOT NULL,
    [type]              NVARCHAR(50)    NOT NULL DEFAULT N'recurring',
    [interval]          NVARCHAR(50)    NULL,
    [description]       NVARCHAR(191)   NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    CONSTRAINT [pk_stripe_prices] PRIMARY KEY ([id]),
    CONSTRAINT [uq_stripe_prices_stripe_price_id] UNIQUE ([stripe_price_id]),
    CONSTRAINT [fk_stripe_prices_stripe_products] FOREIGN KEY ([stripe_product_id]) REFERENCES [dbo].[stripe_products]([stripe_product_id])
);
CREATE INDEX [ix_stripe_prices_stripe_product_id] ON [dbo].[stripe_prices] ([stripe_product_id]);
GO

-- -----------------------------------------------------------------------------
-- 53. PostsAuthors (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[posts_authors] (
    [id]            NVARCHAR(24)    NOT NULL,
    [post_id]        NVARCHAR(24)    NOT NULL,
    [author_id]      NVARCHAR(24)    NOT NULL,
    [sort_order]     INT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_posts_authors] PRIMARY KEY ([id]),
    CONSTRAINT [fk_posts_authors_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[posts]([id]),
    CONSTRAINT [fk_posts_authors_users] FOREIGN KEY ([author_id]) REFERENCES [dbo].[users]([id])
);
CREATE INDEX [ix_posts_authors_post_id] ON [dbo].[posts_authors] ([post_id]);
CREATE INDEX [ix_posts_authors_author_id] ON [dbo].[posts_authors] ([author_id]);
GO

-- -----------------------------------------------------------------------------
-- 54. PostsMeta
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[posts_meta] (
    [id]                    NVARCHAR(24)    NOT NULL,
    [post_id]                NVARCHAR(24)    NOT NULL,
    [og_image]               NVARCHAR(2000)  NULL,
    [og_title]               NVARCHAR(300)   NULL,
    [og_description]         NVARCHAR(500)   NULL,
    [twitter_image]          NVARCHAR(2000)  NULL,
    [twitter_title]          NVARCHAR(300)   NULL,
    [twitter_description]    NVARCHAR(500)   NULL,
    [meta_title]             NVARCHAR(2000)  NULL,
    [meta_description]       NVARCHAR(2000)  NULL,
    [email_subject]          NVARCHAR(300)   NULL,
    [frontmatter]           NVARCHAR(MAX)   NULL,
    [feature_image_alt]       NVARCHAR(2000)  NULL,
    [feature_image_caption]   NVARCHAR(MAX)   NULL,
    [email_only]             BIT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_posts_meta] PRIMARY KEY ([id]),
    CONSTRAINT [uq_posts_meta_post_id] UNIQUE ([post_id]),
    CONSTRAINT [fk_posts_meta_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[posts]([id])
);
GO

-- -----------------------------------------------------------------------------
-- 55. PostsTags (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[posts_tags] (
    [id]            NVARCHAR(24)    NOT NULL,
    [post_id]        NVARCHAR(24)    NOT NULL,
    [tag_id]         NVARCHAR(24)    NOT NULL,
    [sort_order]     INT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_posts_tags] PRIMARY KEY ([id]),
    CONSTRAINT [fk_posts_tags_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[posts]([id]),
    CONSTRAINT [fk_posts_tags_tags] FOREIGN KEY ([tag_id]) REFERENCES [dbo].[tags]([id])
);
CREATE INDEX [ix_posts_tags_tag_id] ON [dbo].[posts_tags] ([tag_id]);
CREATE INDEX [ix_posts_tags_post_id_tag_id] ON [dbo].[posts_tags] ([post_id], [tag_id]);
GO

-- -----------------------------------------------------------------------------
-- 56. PostsProducts (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[posts_products] (
    [id]            NVARCHAR(24)    NOT NULL,
    [post_id]        NVARCHAR(24)    NOT NULL,
    [product_id]     NVARCHAR(24)    NOT NULL,
    [sort_order]     INT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_posts_products] PRIMARY KEY ([id]),
    CONSTRAINT [fk_posts_products_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[posts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_posts_products_products] FOREIGN KEY ([product_id]) REFERENCES [dbo].[products]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_posts_products_post_id] ON [dbo].[posts_products] ([post_id]);
CREATE INDEX [ix_posts_products_product_id] ON [dbo].[posts_products] ([product_id]);
GO

-- -----------------------------------------------------------------------------
-- 57. PostRevisions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[post_revisions] (
    [id]                    NVARCHAR(24)    NOT NULL,
    [post_id]                NVARCHAR(24)    NOT NULL,
    [lexical]               NVARCHAR(MAX)   NULL,
    [created_at_ts]           BIGINT          NOT NULL,
    [created_at]             DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [author_id]              NVARCHAR(24)    NULL,
    [title]                 NVARCHAR(2000)  NULL,
    [post_status]            NVARCHAR(50)    NULL,
    [reason]                NVARCHAR(50)    NULL,
    [feature_image]          NVARCHAR(2000)  NULL,
    [feature_image_alt]       NVARCHAR(2000)  NULL,
    [feature_image_caption]   NVARCHAR(MAX)   NULL,
    [custom_excerpt]         NVARCHAR(2000)  NULL,
    CONSTRAINT [pk_post_revisions] PRIMARY KEY ([id]),
    CONSTRAINT [fk_post_revisions_users] FOREIGN KEY ([author_id]) REFERENCES [dbo].[users]([id])
);
CREATE INDEX [ix_post_revisions_post_id] ON [dbo].[post_revisions] ([post_id]);
CREATE INDEX [ix_post_revisions_author_id] ON [dbo].[post_revisions] ([author_id]);
GO

-- -----------------------------------------------------------------------------
-- 58. MobiledocRevisions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[mobiledoc_revisions] (
    [id]            NVARCHAR(24)    NOT NULL,
    [post_id]        NVARCHAR(24)    NOT NULL,
    [mobiledoc]     NVARCHAR(MAX)   NULL,
    [created_at_ts]   BIGINT          NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_mobiledoc_revisions] PRIMARY KEY ([id])
);
CREATE INDEX [ix_mobiledoc_revisions_post_id] ON [dbo].[mobiledoc_revisions] ([post_id]);
GO

-- -----------------------------------------------------------------------------
-- 59. CollectionsPosts (join table)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[collections_posts] (
    [id]            NVARCHAR(24)    NOT NULL,
    [collection_id]  NVARCHAR(24)    NOT NULL,
    [post_id]        NVARCHAR(24)    NOT NULL,
    [sort_order]     INT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_collections_posts] PRIMARY KEY ([id]),
    CONSTRAINT [fk_collections_posts_collections] FOREIGN KEY ([collection_id]) REFERENCES [dbo].[collections]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_collections_posts_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[posts]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_collections_posts_collection_id] ON [dbo].[collections_posts] ([collection_id]);
CREATE INDEX [ix_collections_posts_post_id] ON [dbo].[collections_posts] ([post_id]);
GO

-- -----------------------------------------------------------------------------
-- 60. Emails (campaign tracking)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[emails] (
    [id]                NVARCHAR(24)    NOT NULL,
    [post_id]            NVARCHAR(24)    NOT NULL,
    [uuid]              NVARCHAR(36)    NOT NULL,
    [status]            NVARCHAR(50)    NOT NULL DEFAULT N'pending',
    [recipient_filter]   NVARCHAR(MAX)   NOT NULL DEFAULT N'all',
    [error]             NVARCHAR(2000)  NULL,
    [error_data]         NVARCHAR(MAX)   NULL,
    [email_count]        INT             NOT NULL DEFAULT 0,
    [csd_email_count]     INT             NULL,
    [delivered_count]    INT             NOT NULL DEFAULT 0,
    [opened_count]       INT             NOT NULL DEFAULT 0,
    [failed_count]       INT             NOT NULL DEFAULT 0,
    [subject]           NVARCHAR(300)   NULL,
    [from]              NVARCHAR(2000)  NULL,
    [reply_to]           NVARCHAR(2000)  NULL,
    [html]              NVARCHAR(MAX)   NULL,
    [plaintext]         NVARCHAR(MAX)   NULL,
    [source]            NVARCHAR(MAX)   NULL,
    [source_type]        NVARCHAR(50)    NOT NULL DEFAULT N'html',
    [track_opens]        BIT             NOT NULL DEFAULT 0,
    [track_clicks]       BIT             NOT NULL DEFAULT 0,
    [feedback_enabled]   BIT             NOT NULL DEFAULT 0,
    [submitted_at]       DATETIME2(7)    NOT NULL,
    [newsletter_id]      NVARCHAR(24)    NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    CONSTRAINT [pk_emails] PRIMARY KEY ([id]),
    CONSTRAINT [uq_emails_post_id] UNIQUE ([post_id]),
    CONSTRAINT [fk_emails_newsletters] FOREIGN KEY ([newsletter_id]) REFERENCES [dbo].[newsletters]([id])
);
CREATE INDEX [ix_emails_post_id] ON [dbo].[emails] ([post_id]);
CREATE INDEX [ix_emails_newsletter_id] ON [dbo].[emails] ([newsletter_id]);
GO

-- -----------------------------------------------------------------------------
-- 61. Comments
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[comments] (
    [id]            NVARCHAR(24)    NOT NULL,
    [post_id]        NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NULL,
    [parent_id]      NVARCHAR(24)    NULL,
    [in_reply_to_id]   NVARCHAR(24)    NULL,
    [status]        NVARCHAR(50)    NOT NULL DEFAULT N'published',
    [html]          NVARCHAR(MAX)   NULL,
    [edited_at]      DATETIME2(7)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_comments] PRIMARY KEY ([id]),
    CONSTRAINT [fk_comments_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[posts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_comments_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE SET NULL,
    CONSTRAINT [fk_comments_parent] FOREIGN KEY ([parent_id]) REFERENCES [dbo].[comments]([id]),
    CONSTRAINT [fk_comments_in_reply_to] FOREIGN KEY ([in_reply_to_id]) REFERENCES [dbo].[comments]([id])
);
CREATE INDEX [ix_comments_post_id] ON [dbo].[comments] ([post_id]);
CREATE INDEX [ix_comments_member_id] ON [dbo].[comments] ([member_id]);
CREATE INDEX [ix_comments_parent_id] ON [dbo].[comments] ([parent_id]);
CREATE INDEX [ix_comments_in_reply_to_id] ON [dbo].[comments] ([in_reply_to_id]);
GO

-- -----------------------------------------------------------------------------
-- 62. Redirects
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[redirects] (
    [id]            NVARCHAR(24)    NOT NULL,
    [from]          NVARCHAR(191)   NOT NULL,
    [to]            NVARCHAR(2000)  NOT NULL,
    [post_id]        NVARCHAR(24)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_redirects] PRIMARY KEY ([id]),
    CONSTRAINT [fk_redirects_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[posts]([id]) ON DELETE SET NULL
);
CREATE INDEX [ix_redirects_from] ON [dbo].[redirects] ([from]);
CREATE INDEX [ix_redirects_post_id] ON [dbo].[redirects] ([post_id]);
GO

-- -----------------------------------------------------------------------------
-- 63. MembersFeedback
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_feedback] (
    [id]            NVARCHAR(24)    NOT NULL,
    [score]         INT             NOT NULL DEFAULT 0,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [post_id]        NVARCHAR(24)    NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL,
    CONSTRAINT [pk_members_feedback] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_feedback_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_members_feedback_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[posts]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_feedback_member_id] ON [dbo].[members_feedback] ([member_id]);
CREATE INDEX [ix_members_feedback_post_id] ON [dbo].[members_feedback] ([post_id]);
GO

-- -----------------------------------------------------------------------------
-- 64. RecommendationClickEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[recommendation_click_events] (
    [id]                NVARCHAR(24)    NOT NULL,
    [recommendation_id]  NVARCHAR(24)    NOT NULL,
    [member_id]          NVARCHAR(24)    NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_recommendation_click_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_recommendation_click_events_recommendations] FOREIGN KEY ([recommendation_id]) REFERENCES [dbo].[recommendations]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_recommendation_click_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE SET NULL
);
CREATE INDEX [ix_recommendation_click_events_recommendation_id] ON [dbo].[recommendation_click_events] ([recommendation_id]);
CREATE INDEX [ix_recommendation_click_events_member_id] ON [dbo].[recommendation_click_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 65. RecommendationSubscribeEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[recommendation_subscribe_events] (
    [id]                NVARCHAR(24)    NOT NULL,
    [recommendation_id]  NVARCHAR(24)    NOT NULL,
    [member_id]          NVARCHAR(24)    NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_recommendation_subscribe_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_recommendation_subscribe_events_recommendations] FOREIGN KEY ([recommendation_id]) REFERENCES [dbo].[recommendations]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_recommendation_subscribe_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE SET NULL
);
CREATE INDEX [ix_recommendation_subscribe_events_recommendation_id] ON [dbo].[recommendation_subscribe_events] ([recommendation_id]);
CREATE INDEX [ix_recommendation_subscribe_events_member_id] ON [dbo].[recommendation_subscribe_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 66. MembersClickEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_click_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [redirect_id]    NVARCHAR(24)    NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_members_click_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_members_click_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_members_click_events_redirects] FOREIGN KEY ([redirect_id]) REFERENCES [dbo].[redirects]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_members_click_events_member_id] ON [dbo].[members_click_events] ([member_id]);
CREATE INDEX [ix_members_click_events_redirect_id] ON [dbo].[members_click_events] ([redirect_id]);
GO

-- -----------------------------------------------------------------------------
-- 67. MembersStripeCustomersSubscriptions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_stripe_customers_subscriptions] (
    [id]                            NVARCHAR(24)    NOT NULL,
    [customer_id]                    NVARCHAR(255)   NOT NULL,
    [ghost_subscription_id]           NVARCHAR(24)    NULL,
    [subscription_id]                NVARCHAR(255)   NOT NULL,
    [stripe_price_id]                 NVARCHAR(255)   NOT NULL DEFAULT N'',
    [status]                        NVARCHAR(50)    NOT NULL,
    [cancel_at_period_end]             BIT             NOT NULL DEFAULT 0,
    [cancellation_reason]            NVARCHAR(500)   NULL,
    [current_period_end]              DATETIME2(7)    NOT NULL,
    [start_date]                     DATETIME2(7)    NOT NULL,
    [default_payment_card_last4]       NVARCHAR(4)     NULL,
    [created_at]                     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]                     DATETIME2(7)    NULL,
    [mrr]                           INT             NOT NULL DEFAULT 0,
    [offer_id]                       NVARCHAR(24)    NULL,
    [trial_start_at]                  DATETIME2(7)    NULL,
    [trial_end_at]                    DATETIME2(7)    NULL,
    [plan_id]                        NVARCHAR(255)   NOT NULL,
    [plan_nickname]                  NVARCHAR(50)    NOT NULL,
    [plan_interval]                  NVARCHAR(50)    NOT NULL,
    [plan_amount]                    INT             NOT NULL,
    [plan_currency]                  NVARCHAR(191)   NOT NULL,
    [discount_start]                 DATETIME2(7)    NULL,
    [discount_end]                   DATETIME2(7)    NULL,
    CONSTRAINT [pk_members_stripe_customers_subscriptions] PRIMARY KEY ([id]),
    CONSTRAINT [uq_mscs_subscription_id] UNIQUE ([subscription_id]),
    CONSTRAINT [fk_mscs_customer_id] FOREIGN KEY ([customer_id]) REFERENCES [dbo].[members_stripe_customers]([customer_id]) ON DELETE CASCADE,
    CONSTRAINT [fk_mscs_ghost_subscription_id] FOREIGN KEY ([ghost_subscription_id]) REFERENCES [dbo].[subscriptions]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_mscs_offer_id] FOREIGN KEY ([offer_id]) REFERENCES [dbo].[offers]([id])
);
CREATE INDEX [ix_mscs_customer_id] ON [dbo].[members_stripe_customers_subscriptions] ([customer_id]);
CREATE INDEX [ix_mscs_ghost_subscription_id] ON [dbo].[members_stripe_customers_subscriptions] ([ghost_subscription_id]);
CREATE INDEX [ix_mscs_stripe_price_id] ON [dbo].[members_stripe_customers_subscriptions] ([stripe_price_id]);
CREATE INDEX [ix_mscs_offer_id] ON [dbo].[members_stripe_customers_subscriptions] ([offer_id]);
GO

-- -----------------------------------------------------------------------------
-- 68. Suppressions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[suppressions] (
    [id]            NVARCHAR(24)    NOT NULL,
    [email]         NVARCHAR(191)   NOT NULL,
    [email_id]       NVARCHAR(24)    NULL,
    [reason]        NVARCHAR(50)    NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_suppressions] PRIMARY KEY ([id]),
    CONSTRAINT [uq_suppressions_email] UNIQUE ([email]),
    CONSTRAINT [fk_suppressions_emails] FOREIGN KEY ([email_id]) REFERENCES [dbo].[emails]([id])
);
CREATE INDEX [ix_suppressions_email_id] ON [dbo].[suppressions] ([email_id]);
GO

-- -----------------------------------------------------------------------------
-- 69. AnalyticsEvents (replaces tinybird_analytics_backup)
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[analytics_events] (
    [id]            BIGINT          NOT NULL IDENTITY(1,1),
    [timestamp]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [session_id]     NVARCHAR(255)   NULL,
    [action]        NVARCHAR(100)   NULL,
    [version]       NVARCHAR(50)    NULL,
    [payload]       NVARCHAR(MAX)   NULL,
    [site_uuid]      NVARCHAR(36)    NULL,
    [page_url]       NVARCHAR(MAX)   NULL,
    [page_url_path]   NVARCHAR(MAX)   NULL,
    [referrer]      NVARCHAR(MAX)   NULL,
    [device]        NVARCHAR(100)   NULL,
    [browser]       NVARCHAR(100)   NULL,
    [os]            NVARCHAR(100)   NULL,
    [country]       NVARCHAR(10)    NULL,
    [member_uuid]    NVARCHAR(36)    NULL,
    [member_status]  NVARCHAR(50)    NULL,
    [post_uuid]      NVARCHAR(36)    NULL,
    [backed_up_at]    DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_analytics_events] PRIMARY KEY ([id])
);
CREATE INDEX [ix_analytics_events_timestamp] ON [dbo].[analytics_events] ([timestamp]);
CREATE INDEX [ix_analytics_events_session_id] ON [dbo].[analytics_events] ([session_id]);
CREATE INDEX [ix_analytics_events_site_uuid] ON [dbo].[analytics_events] ([site_uuid]);
GO

-- =====================
-- Tier 4: Tables with FK to Tier 3 tables
-- =====================

-- -----------------------------------------------------------------------------
-- 70. OfferRedemptions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[offer_redemptions] (
    [id]                NVARCHAR(24)    NOT NULL,
    [offer_id]           NVARCHAR(24)    NOT NULL,
    [member_id]          NVARCHAR(24)    NOT NULL,
    [subscription_id]    NVARCHAR(24)    NOT NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_offer_redemptions] PRIMARY KEY ([id]),
    CONSTRAINT [fk_offer_redemptions_offers] FOREIGN KEY ([offer_id]) REFERENCES [dbo].[offers]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_offer_redemptions_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_offer_redemptions_mscs] FOREIGN KEY ([subscription_id]) REFERENCES [dbo].[members_stripe_customers_subscriptions]([id])
);
CREATE INDEX [ix_offer_redemptions_offer_id] ON [dbo].[offer_redemptions] ([offer_id]);
CREATE INDEX [ix_offer_redemptions_member_id] ON [dbo].[offer_redemptions] ([member_id]);
CREATE INDEX [ix_offer_redemptions_subscription_id] ON [dbo].[offer_redemptions] ([subscription_id]);
GO

-- -----------------------------------------------------------------------------
-- 71. MembersSubscriptionCreatedEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[members_subscription_created_events] (
    [id]                NVARCHAR(24)    NOT NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [member_id]          NVARCHAR(24)    NOT NULL,
    [subscription_id]    NVARCHAR(24)    NOT NULL,
    [attribution_id]     NVARCHAR(24)    NULL,
    [attribution_type]   NVARCHAR(50)    NULL,
    [attribution_url]    NVARCHAR(2000)  NULL,
    [referrer_source]    NVARCHAR(191)   NULL,
    [referrer_medium]    NVARCHAR(191)   NULL,
    [referrer_url]       NVARCHAR(2000)  NULL,
    [utm_source]         NVARCHAR(191)   NULL,
    [utm_medium]         NVARCHAR(191)   NULL,
    [utm_campaign]       NVARCHAR(191)   NULL,
    [utm_term]           NVARCHAR(191)   NULL,
    [utm_content]        NVARCHAR(191)   NULL,
    [batch_id]           NVARCHAR(24)    NULL,
    CONSTRAINT [pk_members_subscription_created_events] PRIMARY KEY ([id]),
    CONSTRAINT [fk_msce_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_msce_subscriptions] FOREIGN KEY ([subscription_id]) REFERENCES [dbo].[members_stripe_customers_subscriptions]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_msce_member_id] ON [dbo].[members_subscription_created_events] ([member_id]);
CREATE INDEX [ix_msce_subscription_id] ON [dbo].[members_subscription_created_events] ([subscription_id]);
CREATE INDEX [ix_msce_attribution_id] ON [dbo].[members_subscription_created_events] ([attribution_id]);
GO

-- -----------------------------------------------------------------------------
-- 72. CommentLikes
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[comment_likes] (
    [id]            NVARCHAR(24)    NOT NULL,
    [comment_id]     NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_comment_likes] PRIMARY KEY ([id]),
    CONSTRAINT [fk_comment_likes_comments] FOREIGN KEY ([comment_id]) REFERENCES [dbo].[comments]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_comment_likes_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id])
);
CREATE INDEX [ix_comment_likes_comment_id] ON [dbo].[comment_likes] ([comment_id]);
CREATE INDEX [ix_comment_likes_member_id] ON [dbo].[comment_likes] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 73. CommentReports
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[comment_reports] (
    [id]            NVARCHAR(24)    NOT NULL,
    [comment_id]     NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_comment_reports] PRIMARY KEY ([id]),
    CONSTRAINT [fk_comment_reports_comments] FOREIGN KEY ([comment_id]) REFERENCES [dbo].[comments]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_comment_reports_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE SET NULL
);
CREATE INDEX [ix_comment_reports_comment_id] ON [dbo].[comment_reports] ([comment_id]);
CREATE INDEX [ix_comment_reports_member_id] ON [dbo].[comment_reports] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 74. EmailBatches
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[email_batches] (
    [id]                        NVARCHAR(24)    NOT NULL,
    [email_id]                   NVARCHAR(24)    NOT NULL,
    [provider_id]                NVARCHAR(255)   NULL,
    [fallback_sending_domain]     BIT             NOT NULL DEFAULT 0,
    [status]                    NVARCHAR(50)    NOT NULL DEFAULT N'pending',
    [member_segment]             NVARCHAR(MAX)   NULL,
    [error_status_code]           INT             NULL,
    [error_message]              NVARCHAR(2000)  NULL,
    [error_data]                 NVARCHAR(MAX)   NULL,
    [created_at]                 DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]                 DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_email_batches] PRIMARY KEY ([id]),
    CONSTRAINT [fk_email_batches_emails] FOREIGN KEY ([email_id]) REFERENCES [dbo].[emails]([id])
);
CREATE INDEX [ix_email_batches_email_id] ON [dbo].[email_batches] ([email_id]);
GO

-- -----------------------------------------------------------------------------
-- 75. EmailSpamComplaintEvents
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[email_spam_complaint_events] (
    [id]            NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [email_id]       NVARCHAR(24)    NOT NULL,
    [email_address]  NVARCHAR(191)   NOT NULL,
    [created_at]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_email_spam_complaint_events] PRIMARY KEY ([id]),
    CONSTRAINT [uq_email_spam_complaint_events_email_id_member_id] UNIQUE ([email_id], [member_id]),
    CONSTRAINT [fk_email_spam_complaint_events_emails] FOREIGN KEY ([email_id]) REFERENCES [dbo].[emails]([id]),
    CONSTRAINT [fk_email_spam_complaint_events_members] FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_email_spam_complaint_events_member_id] ON [dbo].[email_spam_complaint_events] ([member_id]);
GO

-- -----------------------------------------------------------------------------
-- 76. AutomatedEmailRecipients
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[automated_email_recipients] (
    [id]                NVARCHAR(24)    NOT NULL,
    [automated_email_id]  NVARCHAR(24)    NOT NULL,
    [member_id]          NVARCHAR(24)    NOT NULL,
    [member_uuid]        NVARCHAR(36)    NOT NULL,
    [member_email]       NVARCHAR(191)   NOT NULL,
    [member_name]        NVARCHAR(191)   NULL,
    [created_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL,
    CONSTRAINT [pk_automated_email_recipients] PRIMARY KEY ([id]),
    CONSTRAINT [fk_automated_email_recipients_automated_emails] FOREIGN KEY ([automated_email_id]) REFERENCES [dbo].[automated_emails]([id])
);
CREATE INDEX [ix_automated_email_recipients_automated_email_id] ON [dbo].[automated_email_recipients] ([automated_email_id]);
CREATE INDEX [ix_automated_email_recipients_member_id] ON [dbo].[automated_email_recipients] ([member_id]);
GO

-- =====================
-- Tier 5: Tables with FK to Tier 4 tables
-- =====================

-- -----------------------------------------------------------------------------
-- 77. EmailRecipients
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[email_recipients] (
    [id]            NVARCHAR(24)    NOT NULL,
    [email_id]       NVARCHAR(24)    NOT NULL,
    [member_id]      NVARCHAR(24)    NOT NULL,
    [batch_id]       NVARCHAR(24)    NOT NULL,
    [processed_at]   DATETIME2(7)    NULL,
    [delivered_at]   DATETIME2(7)    NULL,
    [opened_at]      DATETIME2(7)    NULL,
    [failed_at]      DATETIME2(7)    NULL,
    [member_uuid]    NVARCHAR(36)    NOT NULL,
    [member_email]   NVARCHAR(191)   NOT NULL,
    [member_name]    NVARCHAR(191)   NULL,
    CONSTRAINT [pk_email_recipients] PRIMARY KEY ([id]),
    CONSTRAINT [fk_email_recipients_emails] FOREIGN KEY ([email_id]) REFERENCES [dbo].[emails]([id]),
    CONSTRAINT [fk_email_recipients_email_batches] FOREIGN KEY ([batch_id]) REFERENCES [dbo].[email_batches]([id])
);
CREATE INDEX [ix_email_recipients_member_id] ON [dbo].[email_recipients] ([member_id]);
CREATE INDEX [ix_email_recipients_batch_id] ON [dbo].[email_recipients] ([batch_id]);
CREATE INDEX [ix_email_recipients_email_id_member_email] ON [dbo].[email_recipients] ([email_id], [member_email]);
CREATE INDEX [ix_email_recipients_email_id_delivered_at] ON [dbo].[email_recipients] ([email_id], [delivered_at]);
CREATE INDEX [ix_email_recipients_email_id_opened_at] ON [dbo].[email_recipients] ([email_id], [opened_at]);
CREATE INDEX [ix_email_recipients_email_id_failed_at] ON [dbo].[email_recipients] ([email_id], [failed_at]);
GO

-- =====================
-- Tier 6: Tables with FK to Tier 5 tables
-- =====================

-- -----------------------------------------------------------------------------
-- 78. EmailRecipientFailures
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[email_recipient_failures] (
    [id]                NVARCHAR(24)    NOT NULL,
    [email_id]           NVARCHAR(24)    NOT NULL,
    [member_id]          NVARCHAR(24)    NULL,
    [email_recipient_id]  NVARCHAR(24)    NOT NULL,
    [code]              INT             NOT NULL,
    [enhanced_code]      NVARCHAR(50)    NULL,
    [message]           NVARCHAR(2000)  NOT NULL,
    [severity]          NVARCHAR(50)    NOT NULL DEFAULT N'permanent',
    [failed_at]          DATETIME2(7)    NOT NULL,
    [event_id]           NVARCHAR(255)   NULL,
    CONSTRAINT [pk_email_recipient_failures] PRIMARY KEY ([id]),
    CONSTRAINT [fk_email_recipient_failures_emails] FOREIGN KEY ([email_id]) REFERENCES [dbo].[emails]([id]),
    CONSTRAINT [fk_email_recipient_failures_email_recipients] FOREIGN KEY ([email_recipient_id]) REFERENCES [dbo].[email_recipients]([id])
);
CREATE INDEX [ix_email_recipient_failures_email_id] ON [dbo].[email_recipient_failures] ([email_id]);
CREATE INDEX [ix_email_recipient_failures_email_recipient_id] ON [dbo].[email_recipient_failures] ([email_recipient_id]);
GO


-- =============================================================================
-- ACTIVITYPUB DATABASE TABLES (17 tables)
-- Prefixed with AP_ to avoid name collisions with Ghost tables
-- =============================================================================

-- =====================
-- AP Tier 0: No FK dependencies
-- =====================

-- -----------------------------------------------------------------------------
-- AP-1. AP_Sites
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_sites] (
    [id]                INT             NOT NULL IDENTITY(1,1),
    [created_at]         DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [host]              NVARCHAR(255)   NOT NULL,
    [webhook_secret]     NVARCHAR(64)    NOT NULL,
    [ghost_pro]          BIT             NOT NULL DEFAULT 1,
    CONSTRAINT [pk_ap_sites] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_sites_host] UNIQUE ([host]),
    CONSTRAINT [uq_ap_sites_webhook_secret] UNIQUE ([webhook_secret])
);
GO

-- -----------------------------------------------------------------------------
-- AP-2. AP_Accounts
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_accounts] (
    [id]                    INT             NOT NULL IDENTITY(1,1),
    [created_at]             DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]             DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [username]              NVARCHAR(255)   NOT NULL,
    [name]                  NVARCHAR(255)   NULL,
    [bio]                   NVARCHAR(MAX)   NULL,
    [avatar_url]             NVARCHAR(1024)  NULL,
    [banner_image_url]        NVARCHAR(1024)  NULL,
    [url]                   NVARCHAR(1024)  NULL,
    [custom_fields]          NVARCHAR(MAX)   NULL,
    [ap_id]                  NVARCHAR(1024)  NOT NULL,
    [ap_inbox_url]            NVARCHAR(1024)  NOT NULL,
    [ap_shared_inbox_url]      NVARCHAR(1024)  NULL,
    [ap_public_key]           NVARCHAR(MAX)   NULL,
    [ap_private_key]          NVARCHAR(MAX)   NULL,
    [ap_outbox_url]           NVARCHAR(1024)  NULL,
    [ap_following_url]        NVARCHAR(1024)  NULL,
    [ap_followers_url]        NVARCHAR(1024)  NULL,
    [ap_liked_url]            NVARCHAR(1024)  NULL,
    [uuid]                  NVARCHAR(36)    NULL,
    [ap_id_hash]              AS HASHBYTES('SHA2_256', [ap_id]) PERSISTED,
    [domain]                NVARCHAR(255)   NOT NULL,
    [domain_hash]            AS HASHBYTES('SHA2_256', LOWER([domain])) PERSISTED,
    [ap_inbox_url_hash]        AS HASHBYTES('SHA2_256', LOWER([ap_inbox_url])) PERSISTED,
    CONSTRAINT [pk_ap_accounts] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_accounts_uuid] UNIQUE ([uuid]),
    CONSTRAINT [uq_ap_accounts_ap_id_hash] UNIQUE ([ap_id_hash])
);
CREATE INDEX [ix_ap_accounts_username] ON [dbo].[ap_accounts] ([username]);
CREATE INDEX [ix_ap_accounts_domain_hash] ON [dbo].[ap_accounts] ([domain_hash]);
CREATE INDEX [ix_ap_accounts_ap_inbox_url_hash] ON [dbo].[ap_accounts] ([ap_inbox_url_hash]);
GO

-- -----------------------------------------------------------------------------
-- AP-3. AP_SchemaMigrations
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_schema_migrations] (
    [version]       BIGINT          NOT NULL,
    [dirty]         BIT             NOT NULL,
    CONSTRAINT [pk_ap_schema_migrations] PRIMARY KEY ([version])
);
GO

-- -----------------------------------------------------------------------------
-- AP-4. AP_KeyValue
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_key_value] (
    [id]                INT             NOT NULL IDENTITY(1,1),
    [key]               NVARCHAR(768)   NULL,
    [value]             NVARCHAR(MAX)   NOT NULL,
    [expires]           DATETIME2(7)    NULL,
    [object_id]          AS CAST(LEFT(JSON_VALUE([value], '$.object.id'), 255) AS NVARCHAR(255)),
    [object_in_reply_to]   AS CAST(LEFT(JSON_VALUE([value], '$.object.inReplyTo'), 255) AS NVARCHAR(255)),
    [created_at]         DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]         DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [pk_ap_key_value] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_key_value_key] UNIQUE ([key])
);
CREATE INDEX [ix_ap_key_value_object_id] ON [dbo].[ap_key_value] ([object_id]);
CREATE INDEX [ix_ap_key_value_object_in_reply_to] ON [dbo].[ap_key_value] ([object_in_reply_to]);
CREATE INDEX [ix_ap_key_value_expires] ON [dbo].[ap_key_value] ([expires]);
GO

-- -----------------------------------------------------------------------------
-- AP-5. AP_GhostApPostMappings
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_ghost_ap_post_mappings] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [ghost_uuid]     NVARCHAR(36)    NOT NULL,
    [ap_id]          NVARCHAR(1024)  NOT NULL,
    [ap_id_hash]      AS HASHBYTES('SHA2_256', [ap_id]) PERSISTED,
    CONSTRAINT [pk_ap_ghost_ap_post_mappings] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_ghost_ap_post_mappings_ghost_uuid] UNIQUE ([ghost_uuid]),
    CONSTRAINT [uq_ap_ghost_ap_post_mappings_ap_id_hash] UNIQUE ([ap_id_hash])
);
GO

-- =====================
-- AP Tier 1: Depends on AP_Accounts, AP_Sites
-- =====================

-- -----------------------------------------------------------------------------
-- AP-6. AP_Users
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_users] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [account_id]     INT             NOT NULL,
    [site_id]        INT             NOT NULL,
    CONSTRAINT [pk_ap_users] PRIMARY KEY ([id]),
    CONSTRAINT [fk_ap_users_accounts] FOREIGN KEY ([account_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_users_sites] FOREIGN KEY ([site_id]) REFERENCES [dbo].[ap_sites]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_ap_users_account_id] ON [dbo].[ap_users] ([account_id]);
CREATE INDEX [ix_ap_users_site_id] ON [dbo].[ap_users] ([site_id]);
GO

-- -----------------------------------------------------------------------------
-- AP-7. AP_AccountDeliveryBackoffs
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_account_delivery_backoffs] (
    [id]                    INT             NOT NULL IDENTITY(1,1),
    [created_at]             DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]             DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [account_id]             INT             NOT NULL,
    [last_failure_at]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [last_failure_reason]     NVARCHAR(MAX)   NULL,
    [backoff_until]          DATETIME2(7)    NOT NULL,
    [backoff_seconds]        INT             NOT NULL DEFAULT 60,
    CONSTRAINT [pk_ap_account_delivery_backoffs] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_account_delivery_backoffs_account_id] UNIQUE ([account_id]),
    CONSTRAINT [fk_ap_account_delivery_backoffs_accounts] FOREIGN KEY ([account_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_ap_account_delivery_backoffs_backoff_until] ON [dbo].[ap_account_delivery_backoffs] ([backoff_until]);
GO

-- -----------------------------------------------------------------------------
-- AP-8. AP_Blocks
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_blocks] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [blocker_id]     INT             NOT NULL,
    [blocked_id]     INT             NOT NULL,
    CONSTRAINT [pk_ap_blocks] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_blocks_blocker_blocked] UNIQUE ([blocker_id], [blocked_id]),
    CONSTRAINT [fk_ap_blocks_blocker] FOREIGN KEY ([blocker_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_blocks_blocked] FOREIGN KEY ([blocked_id]) REFERENCES [dbo].[ap_accounts]([id])
);
CREATE INDEX [ix_ap_blocks_blocked_id_blocker_id] ON [dbo].[ap_blocks] ([blocked_id], [blocker_id]);
GO

-- -----------------------------------------------------------------------------
-- AP-9. AP_DomainBlocks
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_domain_blocks] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [blocker_id]     INT             NOT NULL,
    [domain]        NVARCHAR(255)   NOT NULL,
    [domain_hash]    AS HASHBYTES('SHA2_256', LOWER([domain])) PERSISTED,
    CONSTRAINT [pk_ap_domain_blocks] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_domain_blocks_blocker_domain] UNIQUE ([blocker_id], [domain]),
    CONSTRAINT [fk_ap_domain_blocks_blocker] FOREIGN KEY ([blocker_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_ap_domain_blocks_domain_hash] ON [dbo].[ap_domain_blocks] ([domain_hash]);
GO

-- -----------------------------------------------------------------------------
-- AP-10. AP_Follows
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_follows] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [follower_id]    INT             NOT NULL,
    [following_id]   INT             NOT NULL,
    CONSTRAINT [pk_ap_follows] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_follows_follower_following] UNIQUE ([follower_id], [following_id]),
    CONSTRAINT [fk_ap_follows_follower] FOREIGN KEY ([follower_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_follows_following] FOREIGN KEY ([following_id]) REFERENCES [dbo].[ap_accounts]([id])
);
CREATE INDEX [ix_ap_follows_follower_id] ON [dbo].[ap_follows] ([follower_id]);
CREATE INDEX [ix_ap_follows_following_id] ON [dbo].[ap_follows] ([following_id]);
GO

-- -----------------------------------------------------------------------------
-- AP-11. AP_Posts
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_posts] (
    [id]                    INT             NOT NULL IDENTITY(1,1),
    [uuid]                  NVARCHAR(36)    NOT NULL DEFAULT CONVERT(NVARCHAR(36), NEWID()),
    [created_at]             DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [updated_at]             DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [type]                  TINYINT         NOT NULL,
    [audience]              TINYINT         NOT NULL,
    [author_id]              INT             NOT NULL,
    [title]                 NVARCHAR(256)   NULL,
    [excerpt]               NVARCHAR(500)   NULL,
    [content]               NVARCHAR(MAX)   NULL,
    [url]                   NVARCHAR(1024)  NOT NULL,
    [image_url]              NVARCHAR(1024)  NULL,
    [published_at]           DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [like_count]             INT             NOT NULL DEFAULT 0,
    [repost_count]           INT             NOT NULL DEFAULT 0,
    [reply_count]            INT             NOT NULL DEFAULT 0,
    [reading_time_minutes]    INT             NOT NULL DEFAULT 0,
    [ap_id]                  NVARCHAR(1024)  NOT NULL,
    [ap_id_hash]              AS HASHBYTES('SHA2_256', [ap_id]) PERSISTED,
    [in_reply_to]             INT             NULL,
    [thread_root]            INT             NULL,
    [attachments]           NVARCHAR(MAX)   NULL,
    [deleted_at]             DATETIME2(7)    NULL,
    [metadata]              NVARCHAR(MAX)   NULL,
    [summary]               NVARCHAR(500)   NULL,
    CONSTRAINT [pk_ap_posts] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_posts_uuid] UNIQUE ([uuid]),
    CONSTRAINT [uq_ap_posts_ap_id_hash] UNIQUE ([ap_id_hash]),
    CONSTRAINT [fk_ap_posts_author] FOREIGN KEY ([author_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_posts_in_reply_to] FOREIGN KEY ([in_reply_to]) REFERENCES [dbo].[ap_posts]([id]),
    CONSTRAINT [fk_ap_posts_thread_root] FOREIGN KEY ([thread_root]) REFERENCES [dbo].[ap_posts]([id])
);
CREATE INDEX [ix_ap_posts_author_id] ON [dbo].[ap_posts] ([author_id]);
CREATE INDEX [ix_ap_posts_in_reply_to] ON [dbo].[ap_posts] ([in_reply_to]);
CREATE INDEX [ix_ap_posts_thread_root] ON [dbo].[ap_posts] ([thread_root]);
CREATE INDEX [ix_ap_posts_type_author] ON [dbo].[ap_posts] ([type], [author_id], [id]);
GO

-- =====================
-- AP Tier 2: Depends on AP_Posts, AP_Users, AP_Accounts
-- =====================

-- -----------------------------------------------------------------------------
-- AP-12. AP_Feeds
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_feeds] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [post_type]      TINYINT         NULL,
    [audience]      TINYINT         NULL,
    [user_id]        INT             NOT NULL,
    [post_id]        INT             NOT NULL,
    [author_id]      INT             NOT NULL,
    [reposted_by_id]  INT             NULL,
    [published_at]   DATETIME2(7)    NOT NULL,
    CONSTRAINT [pk_ap_feeds] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_feeds_user_post_repost] UNIQUE ([user_id], [post_id], [reposted_by_id]),
    CONSTRAINT [fk_ap_feeds_users] FOREIGN KEY ([user_id]) REFERENCES [dbo].[ap_users]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_feeds_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[ap_posts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_feeds_author] FOREIGN KEY ([author_id]) REFERENCES [dbo].[ap_accounts]([id]),
    CONSTRAINT [fk_ap_feeds_reposted_by] FOREIGN KEY ([reposted_by_id]) REFERENCES [dbo].[ap_accounts]([id])
);
CREATE INDEX [ix_ap_feeds_post_id] ON [dbo].[ap_feeds] ([post_id]);
CREATE INDEX [ix_ap_feeds_author_id] ON [dbo].[ap_feeds] ([author_id]);
CREATE INDEX [ix_ap_feeds_reposted_by_id] ON [dbo].[ap_feeds] ([reposted_by_id]);
CREATE INDEX [ix_ap_feeds_user_id] ON [dbo].[ap_feeds] ([user_id]);
CREATE INDEX [ix_ap_feeds_post_type] ON [dbo].[ap_feeds] ([post_type]);
CREATE INDEX [ix_ap_feeds_audience] ON [dbo].[ap_feeds] ([audience]);
CREATE INDEX [ix_ap_feeds_published_at] ON [dbo].[ap_feeds] ([published_at]);
CREATE INDEX [ix_ap_feeds_user_id_post_type_published_at] ON [dbo].[ap_feeds] ([user_id], [post_type], [published_at] DESC);
GO

-- -----------------------------------------------------------------------------
-- AP-13. AP_Likes
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_likes] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [account_id]     INT             NOT NULL,
    [post_id]        INT             NOT NULL,
    CONSTRAINT [pk_ap_likes] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_likes_account_post] UNIQUE ([account_id], [post_id]),
    CONSTRAINT [fk_ap_likes_accounts] FOREIGN KEY ([account_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_likes_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[ap_posts]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_ap_likes_post_id] ON [dbo].[ap_likes] ([post_id]);
GO

-- -----------------------------------------------------------------------------
-- AP-14. AP_Mentions
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_mentions] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [post_id]        INT             NOT NULL,
    [account_id]     INT             NOT NULL,
    CONSTRAINT [pk_ap_mentions] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_mentions_account_post] UNIQUE ([account_id], [post_id]),
    CONSTRAINT [fk_ap_mentions_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[ap_posts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_mentions_accounts] FOREIGN KEY ([account_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_ap_mentions_post_id] ON [dbo].[ap_mentions] ([post_id]);
GO

-- -----------------------------------------------------------------------------
-- AP-15. AP_Notifications
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_notifications] (
    [id]                INT             NOT NULL IDENTITY(1,1),
    [created_at]         DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [event_type]         TINYINT         NOT NULL,
    [user_id]            INT             NOT NULL,
    [account_id]         INT             NOT NULL,
    [post_id]            INT             NULL,
    [in_reply_to_post_id]   INT             NULL,
    [read]              BIT             NOT NULL DEFAULT 0,
    CONSTRAINT [pk_ap_notifications] PRIMARY KEY ([id]),
    CONSTRAINT [fk_ap_notifications_users] FOREIGN KEY ([user_id]) REFERENCES [dbo].[ap_users]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_notifications_accounts] FOREIGN KEY ([account_id]) REFERENCES [dbo].[ap_accounts]([id]),
    CONSTRAINT [fk_ap_notifications_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[ap_posts]([id]),
    CONSTRAINT [fk_ap_notifications_in_reply_to_post] FOREIGN KEY ([in_reply_to_post_id]) REFERENCES [dbo].[ap_posts]([id])
);
CREATE INDEX [ix_ap_notifications_account_id] ON [dbo].[ap_notifications] ([account_id]);
CREATE INDEX [ix_ap_notifications_post_id] ON [dbo].[ap_notifications] ([post_id]);
CREATE INDEX [ix_ap_notifications_in_reply_to_post_id] ON [dbo].[ap_notifications] ([in_reply_to_post_id]);
CREATE INDEX [ix_ap_notifications_user_id_id] ON [dbo].[ap_notifications] ([user_id], [id] DESC);
CREATE INDEX [ix_ap_notifications_user_id_read] ON [dbo].[ap_notifications] ([user_id], [read]);
GO

-- -----------------------------------------------------------------------------
-- AP-16. AP_Outboxes
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_outboxes] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [uuid]          NVARCHAR(36)    NOT NULL DEFAULT CONVERT(NVARCHAR(36), NEWID()),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [published_at]   DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [post_type]      TINYINT         NOT NULL,
    [outbox_type]    TINYINT         NOT NULL,
    [account_id]     INT             NOT NULL,
    [post_id]        INT             NOT NULL,
    [author_id]      INT             NOT NULL,
    CONSTRAINT [pk_ap_outboxes] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_outboxes_uuid] UNIQUE ([uuid]),
    CONSTRAINT [uq_ap_outboxes_account_post_outbox_type] UNIQUE ([account_id], [post_id], [outbox_type]),
    CONSTRAINT [fk_ap_outboxes_accounts] FOREIGN KEY ([account_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_outboxes_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[ap_posts]([id]),
    CONSTRAINT [fk_ap_outboxes_author] FOREIGN KEY ([author_id]) REFERENCES [dbo].[ap_accounts]([id])
);
CREATE INDEX [ix_ap_outboxes_post_id] ON [dbo].[ap_outboxes] ([post_id]);
CREATE INDEX [ix_ap_outboxes_author_id] ON [dbo].[ap_outboxes] ([author_id]);
CREATE INDEX [ix_ap_outboxes_account_id_outbox_type_published_at] ON [dbo].[ap_outboxes] ([account_id], [outbox_type], [published_at] DESC);
GO

-- -----------------------------------------------------------------------------
-- AP-17. AP_Reposts
-- -----------------------------------------------------------------------------
CREATE TABLE [dbo].[ap_reposts] (
    [id]            INT             NOT NULL IDENTITY(1,1),
    [created_at]     DATETIME2(7)    NULL DEFAULT SYSUTCDATETIME(),
    [account_id]     INT             NOT NULL,
    [post_id]        INT             NOT NULL,
    CONSTRAINT [pk_ap_reposts] PRIMARY KEY ([id]),
    CONSTRAINT [uq_ap_reposts_account_post] UNIQUE ([account_id], [post_id]),
    CONSTRAINT [fk_ap_reposts_accounts] FOREIGN KEY ([account_id]) REFERENCES [dbo].[ap_accounts]([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_ap_reposts_posts] FOREIGN KEY ([post_id]) REFERENCES [dbo].[ap_posts]([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_ap_reposts_post_id] ON [dbo].[ap_reposts] ([post_id]);
GO

-- =============================================================================
-- END OF SCHEMA
-- Total: 78 Ghost tables + 17 ActivityPub tables = 95 tables
-- =============================================================================
