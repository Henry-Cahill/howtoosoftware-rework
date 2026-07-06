using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "actions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    resource_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    resource_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    actor_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    actor_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    @event = table.Column<string>(name: "event", type: "nvarchar(50)", maxLength: 50, nullable: false),
                    context = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "analytics_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    session_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    site_uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    page_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    page_url_path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    referrer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    device = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    browser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    os = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    member_uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    member_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    post_uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    backed_up_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analytics_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automated_emails",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "inactive"),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    lexical = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sender_name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    sender_email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    sender_reply_to = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automated_emails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "benefits",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benefits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "brute",
                columns: table => new
                {
                    key = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    first_request = table.Column<long>(type: "bigint", nullable: false),
                    last_request = table.Column<long>(type: "bigint", nullable: false),
                    lifetime = table.Column<long>(type: "bigint", nullable: false),
                    count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brute", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "collections",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    title = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    filter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    feature_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custom_theme_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    theme = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    key = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_theme_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integrations",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "custom"),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    icon_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "queued"),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    finished_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    queue_entry = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "labels",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_labels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    transient_id = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "free"),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    expertise = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    geolocation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    enable_comment_notifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    email_count = table.Column<int>(type: "int", nullable: false),
                    email_opened_count = table.Column<int>(type: "int", nullable: false),
                    email_open_rate = table.Column<int>(type: "int", nullable: true),
                    email_disabled = table.Column<bool>(type: "bit", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_commented_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    commenting = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mentions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    source = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    source_title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    source_site_title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    source_excerpt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    source_author = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    source_featured_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    source_favicon = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    target = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    resource_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    resource_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    deleted = table.Column<bool>(type: "bit", nullable: false),
                    verified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestones",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    type = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    value = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    email_sent_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_milestones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "newsletters",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    feedback_enabled = table.Column<bool>(type: "bit", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    sender_name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    sender_email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    sender_reply_to = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false, defaultValue: "newsletter"),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "active"),
                    visibility = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "members"),
                    subscribe_on_signup = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    header_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    show_header_icon = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    show_header_title = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    show_excerpt = table.Column<bool>(type: "bit", nullable: false),
                    title_font_category = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false, defaultValue: "sans_serif"),
                    title_alignment = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false, defaultValue: "center"),
                    show_feature_image = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    body_font_category = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false, defaultValue: "sans_serif"),
                    footer_content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    show_badge = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    show_header_name = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    show_post_title_section = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    show_comment_cta = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    show_subscription_details = table.Column<bool>(type: "bit", nullable: false),
                    show_latest_posts = table.Column<bool>(type: "bit", nullable: false),
                    background_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "light"),
                    post_title_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    button_corners = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "rounded"),
                    button_style = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "fill"),
                    title_font_weight = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "bold"),
                    link_style = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "underline"),
                    image_corners = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "square"),
                    header_background_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "transparent"),
                    section_title_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    divider_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    button_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "accent"),
                    link_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "accent")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_newsletters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    event_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    last_retry_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    object_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    action_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    object_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    welcome_page_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    visibility = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "none"),
                    trial_days = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "paid"),
                    currency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    monthly_price = table.Column<int>(type: "int", nullable: true),
                    yearly_price = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    monthly_price_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    yearly_price_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recommendations",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    excerpt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    featured_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    favicon = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    one_click_subscribe = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recommendations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    group = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "core"),
                    key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    flags = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snippets",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    mobiledoc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lexical = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_snippets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    feature_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    parent_id = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    visibility = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "public"),
                    og_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    og_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    og_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    twitter_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    twitter_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    twitter_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    meta_title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    meta_description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    codeinjection_head = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    codeinjection_foot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    canonical_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    accent_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tokens",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    token = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    data = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    first_used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    used_count = table.Column<int>(type: "int", nullable: false),
                    otc_used_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    password = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    profile_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    cover_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    website = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    facebook = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    twitter = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    threads = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    bluesky = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    mastodon = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    tik_tok = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    you_tube = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    instagram = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    linked_in = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    accessibility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "active"),
                    locale = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    visibility = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "public"),
                    meta_title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    meta_description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    tour = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    last_seen = table.Column<DateTime>(type: "datetime2", nullable: true),
                    comment_notifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    free_member_signup_notification = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    paid_subscription_started_notification = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    paid_subscription_canceled_notification = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    mention_notifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    recommendation_notifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    milestone_notifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    donation_notifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automated_email_recipients",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    automated_email_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    member_email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    member_name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automated_email_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_automated_email_recipients_automated_emails_automated_email_id",
                        column: x => x.automated_email_id,
                        principalTable: "automated_emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    secret = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    role_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    integration_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    user_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_seen_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_keys_integrations_integration_id",
                        column: x => x.integration_id,
                        principalTable: "integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "webhooks",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    @event = table.Column<string>(name: "event", type: "nvarchar(50)", maxLength: 50, nullable: false),
                    target_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    secret = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    api_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "v2"),
                    integration_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    last_triggered_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_triggered_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    last_triggered_error = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhooks", x => x.id);
                    table.ForeignKey(
                        name: "fk_webhooks_integrations_integration_id",
                        column: x => x.integration_id,
                        principalTable: "integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "donation_payment_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    amount = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    attribution_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    attribution_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    attribution_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    referrer_source = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    referrer_medium = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    referrer_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    utm_source = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_medium = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_campaign = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_term = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_content = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    donation_message = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_donation_payment_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_donation_payment_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "members_cancel_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    from_plan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_cancel_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_cancel_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_created_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    attribution_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    attribution_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    attribution_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    referrer_source = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    referrer_medium = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    referrer_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    utm_source = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_medium = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_campaign = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_term = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_content = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    batch_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_created_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_created_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_email_change_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    to_email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    from_email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_email_change_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_email_change_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_labels",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    label_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_labels", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_labels_labels_label_id",
                        column: x => x.label_id,
                        principalTable: "labels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_labels_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_login_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_login_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_login_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_paid_subscription_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    subscription_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    from_plan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    to_plan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    mrr_delta = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_paid_subscription_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_paid_subscription_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_payment_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_payment_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_payment_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_status_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    from_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    to_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_status_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_status_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_stripe_customers",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    customer_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_stripe_customers", x => x.id);
                    table.UniqueConstraint("ak_members_stripe_customers_customer_id", x => x.customer_id);
                    table.ForeignKey(
                        name: "fk_members_stripe_customers_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_newsletters",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    newsletter_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_newsletters", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_newsletters_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_newsletters_newsletters_newsletter_id",
                        column: x => x.newsletter_id,
                        principalTable: "newsletters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_subscribe_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    subscribed = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    newsletter_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_subscribe_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_subscribe_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_subscribe_events_newsletters_newsletter_id",
                        column: x => x.newsletter_id,
                        principalTable: "newsletters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    mobiledoc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lexical = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    html = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    comment_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    plaintext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    feature_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    featured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "post"),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "draft"),
                    locale = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    visibility = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "public"),
                    email_recipient_filter = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "all"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    published_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    published_by = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    custom_excerpt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    codeinjection_head = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    codeinjection_foot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    custom_template = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    canonical_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    newsletter_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    show_title_and_feature_image = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.id);
                    table.ForeignKey(
                        name: "fk_posts_newsletters_newsletter_id",
                        column: x => x.newsletter_id,
                        principalTable: "newsletters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "members_product_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    product_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_product_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_product_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_product_events_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "members_products",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    product_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    expiry_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_products_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "offers",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    code = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    product_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    stripe_coupon_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    interval = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    discount_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    discount_amount = table.Column<int>(type: "int", nullable: false),
                    duration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    duration_in_months = table.Column<int>(type: "int", nullable: true),
                    portal_title = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    portal_description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    redemption_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "signup")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_offers_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "products_benefits",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    product_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    benefit_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products_benefits", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_benefits_benefits_benefit_id",
                        column: x => x.benefit_id,
                        principalTable: "benefits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_products_benefits_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stripe_products",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    product_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    stripe_product_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stripe_products", x => x.id);
                    table.UniqueConstraint("ak_stripe_products_stripe_product_id", x => x.stripe_product_id);
                    table.ForeignKey(
                        name: "fk_stripe_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_click_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    recommendation_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recommendation_click_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_recommendation_click_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_recommendation_click_events_recommendations_recommendation_id",
                        column: x => x.recommendation_id,
                        principalTable: "recommendations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_subscribe_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    recommendation_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recommendation_subscribe_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_recommendation_subscribe_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_recommendation_subscribe_events_recommendations_recommendation_id",
                        column: x => x.recommendation_id,
                        principalTable: "recommendations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invites",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    role_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    token = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    expires = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invites", x => x.id);
                    table.ForeignKey(
                        name: "fk_invites_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permissions_roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    role_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    permission_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_permissions_roles_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_permissions_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permissions_users",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    permission_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_permissions_users_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_permissions_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles_users",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    role_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_roles_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_roles_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    session_id = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    session_data = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "collections_posts",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    collection_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collections_posts", x => x.id);
                    table.ForeignKey(
                        name: "fk_collections_posts_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_collections_posts_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    parent_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    in_reply_to_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "published"),
                    html = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    edited_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_comments_comments_in_reply_to_id",
                        column: x => x.in_reply_to_id,
                        principalTable: "comments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_comments_comments_parent_id",
                        column: x => x.parent_id,
                        principalTable: "comments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_comments_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_comments_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emails",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    recipient_filter = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "all"),
                    error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    error_data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email_count = table.Column<int>(type: "int", nullable: false),
                    csd_email_count = table.Column<int>(type: "int", nullable: true),
                    delivered_count = table.Column<int>(type: "int", nullable: false),
                    opened_count = table.Column<int>(type: "int", nullable: false),
                    failed_count = table.Column<int>(type: "int", nullable: false),
                    subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    from = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    reply_to = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    html = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    plaintext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    source_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "html"),
                    track_opens = table.Column<bool>(type: "bit", nullable: false),
                    track_clicks = table.Column<bool>(type: "bit", nullable: false),
                    feedback_enabled = table.Column<bool>(type: "bit", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    newsletter_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emails", x => x.id);
                    table.ForeignKey(
                        name: "fk_emails_newsletters_newsletter_id",
                        column: x => x.newsletter_id,
                        principalTable: "newsletters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_emails_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_feedback",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    score = table.Column<int>(type: "int", nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_feedback", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_feedback_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_feedback_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobiledoc_revisions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    mobiledoc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at_ts = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobiledoc_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_mobiledoc_revisions_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_revisions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    lexical = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at_ts = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    author_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    post_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    feature_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    feature_image_alt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    feature_image_caption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    custom_excerpt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_post_revisions_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_revisions_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "posts_authors",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    author_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts_authors", x => x.id);
                    table.ForeignKey(
                        name: "fk_posts_authors_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_posts_authors_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "posts_meta",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    og_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    og_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    og_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    twitter_image = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    twitter_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    twitter_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    meta_title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    meta_description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    email_subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    frontmatter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    feature_image_alt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    feature_image_caption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email_only = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts_meta", x => x.id);
                    table.ForeignKey(
                        name: "fk_posts_meta_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "posts_products",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    product_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_posts_products_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_posts_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "posts_tags",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    tag_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_posts_tags_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_posts_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "redirects",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    from = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    to = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    post_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_redirects", x => x.id);
                    table.ForeignKey(
                        name: "fk_redirects_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "offer_redemptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    offer_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    subscription_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_offer_redemptions_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_offer_redemptions_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    tier_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    cadence = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    amount = table.Column<int>(type: "int", nullable: true),
                    payment_provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    payment_subscription_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    payment_user_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    offer_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscriptions_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_subscriptions_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_subscriptions_products_tier_id",
                        column: x => x.tier_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stripe_prices",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    stripe_price_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    stripe_product_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    nickname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "recurring"),
                    interval = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stripe_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_stripe_prices_stripe_products_stripe_product_id",
                        column: x => x.stripe_product_id,
                        principalTable: "stripe_products",
                        principalColumn: "stripe_product_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comment_likes",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    comment_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comment_likes", x => x.id);
                    table.ForeignKey(
                        name: "fk_comment_likes_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_comment_likes_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comment_reports",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    comment_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comment_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_comment_reports_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_comment_reports_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "email_batches",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    email_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    provider_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    fallback_sending_domain = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    member_segment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    error_status_code = table.Column<int>(type: "int", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    error_data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_batches_emails_email_id",
                        column: x => x.email_id,
                        principalTable: "emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_spam_complaint_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    email_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    email_address = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_spam_complaint_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_spam_complaint_events_emails_email_id",
                        column: x => x.email_id,
                        principalTable: "emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_spam_complaint_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suppressions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    email_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppressions", x => x.id);
                    table.ForeignKey(
                        name: "fk_suppressions_emails_email_id",
                        column: x => x.email_id,
                        principalTable: "emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "members_click_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    redirect_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_click_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_click_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_click_events_redirects_redirect_id",
                        column: x => x.redirect_id,
                        principalTable: "redirects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members_stripe_customers_subscriptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    customer_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ghost_subscription_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    subscription_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    stripe_price_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: ""),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    cancel_at_period_end = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    cancellation_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    current_period_end = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    default_payment_card_last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    mrr = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    offer_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    trial_start_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    trial_end_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    plan_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    plan_nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    plan_interval = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    plan_amount = table.Column<int>(type: "int", nullable: false),
                    plan_currency = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    discount_start = table.Column<DateTime>(type: "datetime2", nullable: true),
                    discount_end = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_stripe_customers_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_stripe_customers_subscriptions_members_stripe_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "members_stripe_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_stripe_customers_subscriptions_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_members_stripe_customers_subscriptions_subscriptions_ghost_subscription_id",
                        column: x => x.ghost_subscription_id,
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "email_recipients",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    email_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    batch_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    processed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    opened_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    member_uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    member_email = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: false),
                    member_name = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_recipients_email_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "email_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_recipients_emails_email_id",
                        column: x => x.email_id,
                        principalTable: "emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "members_subscription_created_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    subscription_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    attribution_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    attribution_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    attribution_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    referrer_source = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    referrer_medium = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    referrer_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    utm_source = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_medium = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_campaign = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_term = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    utm_content = table.Column<string>(type: "nvarchar(191)", maxLength: 191, nullable: true),
                    batch_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members_subscription_created_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_subscription_created_events_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_members_subscription_created_events_members_stripe_customers_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "members_stripe_customers_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "email_recipient_failures",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    email_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    member_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    email_recipient_id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    code = table.Column<int>(type: "int", nullable: false),
                    enhanced_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "permanent"),
                    failed_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    event_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_recipient_failures", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_recipient_failures_email_recipients_email_recipient_id",
                        column: x => x.email_recipient_id,
                        principalTable: "email_recipients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_recipient_failures_emails_email_id",
                        column: x => x.email_id,
                        principalTable: "emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analytics_events_session_id",
                table: "analytics_events",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_analytics_events_site_uuid",
                table: "analytics_events",
                column: "site_uuid");

            migrationBuilder.CreateIndex(
                name: "ix_analytics_events_timestamp",
                table: "analytics_events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_integration_id",
                table: "api_keys",
                column: "integration_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_secret",
                table: "api_keys",
                column: "secret",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_automated_email_recipients_automated_email_id",
                table: "automated_email_recipients",
                column: "automated_email_id");

            migrationBuilder.CreateIndex(
                name: "ix_automated_email_recipients_member_id",
                table: "automated_email_recipients",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_automated_emails_name",
                table: "automated_emails",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_automated_emails_slug",
                table: "automated_emails",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_automated_emails_status",
                table: "automated_emails",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_benefits_slug",
                table: "benefits",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_collections_slug",
                table: "collections",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_collections_posts_collection_id",
                table: "collections_posts",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_collections_posts_post_id",
                table: "collections_posts",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_likes_comment_id",
                table: "comment_likes",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_likes_member_id",
                table: "comment_likes",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_reports_comment_id",
                table: "comment_reports",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_reports_member_id",
                table: "comment_reports",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_in_reply_to_id",
                table: "comments",
                column: "in_reply_to_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_member_id",
                table: "comments",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_parent_id",
                table: "comments",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_post_id",
                table: "comments",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_donation_payment_events_member_id",
                table: "donation_payment_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_batches_email_id",
                table: "email_batches",
                column: "email_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_recipient_failures_email_id",
                table: "email_recipient_failures",
                column: "email_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_recipient_failures_email_recipient_id",
                table: "email_recipient_failures",
                column: "email_recipient_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_batch_id",
                table: "email_recipients",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_email_id_delivered_at",
                table: "email_recipients",
                columns: new[] { "email_id", "delivered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_email_id_failed_at",
                table: "email_recipients",
                columns: new[] { "email_id", "failed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_email_id_member_email",
                table: "email_recipients",
                columns: new[] { "email_id", "member_email" });

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_email_id_opened_at",
                table: "email_recipients",
                columns: new[] { "email_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_member_id",
                table: "email_recipients",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_spam_complaint_events_email_id_member_id",
                table: "email_spam_complaint_events",
                columns: new[] { "email_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_spam_complaint_events_member_id",
                table: "email_spam_complaint_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_emails_newsletter_id",
                table: "emails",
                column: "newsletter_id");

            migrationBuilder.CreateIndex(
                name: "ix_emails_post_id",
                table: "emails",
                column: "post_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integrations_slug",
                table: "integrations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invites_email",
                table: "invites",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invites_role_id",
                table: "invites",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_invites_token",
                table: "invites",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_jobs_name",
                table: "jobs",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_labels_name",
                table: "labels",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_labels_slug",
                table: "labels",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_email",
                table: "members",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_email_disabled",
                table: "members",
                column: "email_disabled");

            migrationBuilder.CreateIndex(
                name: "ix_members_email_open_rate",
                table: "members",
                column: "email_open_rate");

            migrationBuilder.CreateIndex(
                name: "ix_members_transient_id",
                table: "members",
                column: "transient_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_uuid",
                table: "members",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_cancel_events_member_id",
                table: "members_cancel_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_click_events_member_id",
                table: "members_click_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_click_events_redirect_id",
                table: "members_click_events",
                column: "redirect_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_created_events_attribution_id",
                table: "members_created_events",
                column: "attribution_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_created_events_member_id",
                table: "members_created_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_email_change_events_member_id",
                table: "members_email_change_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_feedback_member_id",
                table: "members_feedback",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_feedback_post_id",
                table: "members_feedback",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_labels_label_id",
                table: "members_labels",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_labels_member_id",
                table: "members_labels",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_login_events_member_id",
                table: "members_login_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_newsletters_member_id",
                table: "members_newsletters",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_newsletters_newsletter_id_member_id",
                table: "members_newsletters",
                columns: new[] { "newsletter_id", "member_id" });

            migrationBuilder.CreateIndex(
                name: "ix_members_paid_subscription_events_member_id",
                table: "members_paid_subscription_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_payment_events_member_id",
                table: "members_payment_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_product_events_member_id",
                table: "members_product_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_product_events_product_id",
                table: "members_product_events",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_products_member_id",
                table: "members_products",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_products_product_id",
                table: "members_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_status_events_member_id",
                table: "members_status_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_stripe_customers_customer_id",
                table: "members_stripe_customers",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_stripe_customers_member_id",
                table: "members_stripe_customers",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_stripe_customers_subscriptions_customer_id",
                table: "members_stripe_customers_subscriptions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_stripe_customers_subscriptions_ghost_subscription_id",
                table: "members_stripe_customers_subscriptions",
                column: "ghost_subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_stripe_customers_subscriptions_offer_id",
                table: "members_stripe_customers_subscriptions",
                column: "offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_stripe_customers_subscriptions_stripe_price_id",
                table: "members_stripe_customers_subscriptions",
                column: "stripe_price_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_stripe_customers_subscriptions_subscription_id",
                table: "members_stripe_customers_subscriptions",
                column: "subscription_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_subscribe_events_member_id",
                table: "members_subscribe_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_subscribe_events_newsletter_id_created_at",
                table: "members_subscribe_events",
                columns: new[] { "newsletter_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_members_subscription_created_events_attribution_id",
                table: "members_subscription_created_events",
                column: "attribution_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_subscription_created_events_member_id",
                table: "members_subscription_created_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_subscription_created_events_subscription_id",
                table: "members_subscription_created_events",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobiledoc_revisions_post_id",
                table: "mobiledoc_revisions",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_name",
                table: "newsletters",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_slug",
                table: "newsletters",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_uuid",
                table: "newsletters",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offer_redemptions_member_id",
                table: "offer_redemptions",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_redemptions_offer_id",
                table: "offer_redemptions",
                column: "offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_redemptions_subscription_id",
                table: "offer_redemptions",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_offers_code",
                table: "offers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offers_name",
                table: "offers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offers_product_id",
                table: "offers",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_offers_stripe_coupon_id",
                table: "offers",
                column: "stripe_coupon_id",
                unique: true,
                filter: "[stripe_coupon_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_event_type_status_created_at",
                table: "outbox",
                columns: new[] { "event_type", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_name",
                table: "permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_roles_permission_id",
                table: "permissions_roles",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_roles_role_id",
                table: "permissions_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_users_permission_id",
                table: "permissions_users",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_users_user_id",
                table: "permissions_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_revisions_author_id",
                table: "post_revisions",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_revisions_post_id",
                table: "post_revisions",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_newsletter_id",
                table: "posts",
                column: "newsletter_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_published_at",
                table: "posts",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "ix_posts_slug_type",
                table: "posts",
                columns: new[] { "slug", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_type_status_updated_at",
                table: "posts",
                columns: new[] { "type", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_posts_updated_at",
                table: "posts",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_posts_uuid",
                table: "posts",
                column: "uuid");

            migrationBuilder.CreateIndex(
                name: "ix_posts_authors_author_id",
                table: "posts_authors",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_authors_post_id",
                table: "posts_authors",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_meta_post_id",
                table: "posts_meta",
                column: "post_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_products_post_id",
                table: "posts_products",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_products_product_id",
                table: "posts_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_tags_post_id_tag_id",
                table: "posts_tags",
                columns: new[] { "post_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "ix_posts_tags_tag_id",
                table: "posts_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_slug",
                table: "products",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_benefits_benefit_id",
                table: "products_benefits",
                column: "benefit_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_benefits_product_id",
                table: "products_benefits",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_recommendation_click_events_member_id",
                table: "recommendation_click_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_recommendation_click_events_recommendation_id",
                table: "recommendation_click_events",
                column: "recommendation_id");

            migrationBuilder.CreateIndex(
                name: "ix_recommendation_subscribe_events_member_id",
                table: "recommendation_subscribe_events",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_recommendation_subscribe_events_recommendation_id",
                table: "recommendation_subscribe_events",
                column: "recommendation_id");

            migrationBuilder.CreateIndex(
                name: "ix_redirects_from",
                table: "redirects",
                column: "from");

            migrationBuilder.CreateIndex(
                name: "ix_redirects_post_id",
                table: "redirects",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_users_role_id",
                table: "roles_users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_users_user_id",
                table: "roles_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_session_id",
                table: "sessions",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id",
                table: "sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_settings_key",
                table: "settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_snippets_name",
                table: "snippets",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stripe_prices_stripe_price_id",
                table: "stripe_prices",
                column: "stripe_price_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stripe_prices_stripe_product_id",
                table: "stripe_prices",
                column: "stripe_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stripe_products_product_id",
                table: "stripe_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stripe_products_stripe_product_id",
                table: "stripe_products",
                column: "stripe_product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_member_id",
                table: "subscriptions",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_offer_id",
                table: "subscriptions",
                column: "offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_tier_id",
                table: "subscriptions",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppressions_email",
                table: "suppressions",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppressions_email_id",
                table: "suppressions",
                column: "email_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_slug",
                table: "tags",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tokens_token",
                table: "tokens",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "ix_tokens_uuid",
                table: "tokens",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_slug",
                table: "users",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhooks_integration_id",
                table: "webhooks",
                column: "integration_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actions");

            migrationBuilder.DropTable(
                name: "analytics_events");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "automated_email_recipients");

            migrationBuilder.DropTable(
                name: "brute");

            migrationBuilder.DropTable(
                name: "collections_posts");

            migrationBuilder.DropTable(
                name: "comment_likes");

            migrationBuilder.DropTable(
                name: "comment_reports");

            migrationBuilder.DropTable(
                name: "custom_theme_settings");

            migrationBuilder.DropTable(
                name: "donation_payment_events");

            migrationBuilder.DropTable(
                name: "email_recipient_failures");

            migrationBuilder.DropTable(
                name: "email_spam_complaint_events");

            migrationBuilder.DropTable(
                name: "invites");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "members_cancel_events");

            migrationBuilder.DropTable(
                name: "members_click_events");

            migrationBuilder.DropTable(
                name: "members_created_events");

            migrationBuilder.DropTable(
                name: "members_email_change_events");

            migrationBuilder.DropTable(
                name: "members_feedback");

            migrationBuilder.DropTable(
                name: "members_labels");

            migrationBuilder.DropTable(
                name: "members_login_events");

            migrationBuilder.DropTable(
                name: "members_newsletters");

            migrationBuilder.DropTable(
                name: "members_paid_subscription_events");

            migrationBuilder.DropTable(
                name: "members_payment_events");

            migrationBuilder.DropTable(
                name: "members_product_events");

            migrationBuilder.DropTable(
                name: "members_products");

            migrationBuilder.DropTable(
                name: "members_status_events");

            migrationBuilder.DropTable(
                name: "members_subscribe_events");

            migrationBuilder.DropTable(
                name: "members_subscription_created_events");

            migrationBuilder.DropTable(
                name: "mentions");

            migrationBuilder.DropTable(
                name: "milestones");

            migrationBuilder.DropTable(
                name: "mobiledoc_revisions");

            migrationBuilder.DropTable(
                name: "offer_redemptions");

            migrationBuilder.DropTable(
                name: "outbox");

            migrationBuilder.DropTable(
                name: "permissions_roles");

            migrationBuilder.DropTable(
                name: "permissions_users");

            migrationBuilder.DropTable(
                name: "post_revisions");

            migrationBuilder.DropTable(
                name: "posts_authors");

            migrationBuilder.DropTable(
                name: "posts_meta");

            migrationBuilder.DropTable(
                name: "posts_products");

            migrationBuilder.DropTable(
                name: "posts_tags");

            migrationBuilder.DropTable(
                name: "products_benefits");

            migrationBuilder.DropTable(
                name: "recommendation_click_events");

            migrationBuilder.DropTable(
                name: "recommendation_subscribe_events");

            migrationBuilder.DropTable(
                name: "roles_users");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "snippets");

            migrationBuilder.DropTable(
                name: "stripe_prices");

            migrationBuilder.DropTable(
                name: "suppressions");

            migrationBuilder.DropTable(
                name: "tokens");

            migrationBuilder.DropTable(
                name: "webhooks");

            migrationBuilder.DropTable(
                name: "automated_emails");

            migrationBuilder.DropTable(
                name: "collections");

            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "email_recipients");

            migrationBuilder.DropTable(
                name: "redirects");

            migrationBuilder.DropTable(
                name: "labels");

            migrationBuilder.DropTable(
                name: "members_stripe_customers_subscriptions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "benefits");

            migrationBuilder.DropTable(
                name: "recommendations");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "stripe_products");

            migrationBuilder.DropTable(
                name: "integrations");

            migrationBuilder.DropTable(
                name: "email_batches");

            migrationBuilder.DropTable(
                name: "members_stripe_customers");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "emails");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DropTable(
                name: "offers");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "newsletters");
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
