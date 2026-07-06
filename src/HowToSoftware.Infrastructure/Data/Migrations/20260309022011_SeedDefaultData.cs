using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "newsletters",
                columns: new[] { "id", "background_color", "body_font_category", "button_color", "button_corners", "button_style", "created_at", "description", "divider_color", "feedback_enabled", "footer_content", "header_background_color", "header_image", "image_corners", "link_color", "link_style", "name", "post_title_color", "section_title_color", "sender_email", "sender_name", "sender_reply_to", "show_badge", "show_comment_cta", "show_excerpt", "show_feature_image", "show_header_icon", "show_header_name", "show_header_title", "show_latest_posts", "show_post_title_section", "show_subscription_details", "slug", "sort_order", "status", "subscribe_on_signup", "title_alignment", "title_font_category", "title_font_weight", "updated_at", "uuid", "visibility" },
                values: new object[] { "000000000000000000002001", "light", "sans_serif", "accent", "rounded", "fill", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, null, "transparent", null, "square", "accent", "underline", "Default newsletter", null, null, null, null, "newsletter", true, true, false, true, true, true, true, false, true, false, "default-newsletter", 0, "active", true, "center", "sans_serif", "bold", null, "00000000-0000-0000-0000-000000000001", "members" });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "id", "active", "created_at", "currency", "description", "monthly_price", "monthly_price_id", "name", "slug", "trial_days", "type", "updated_at", "visibility", "welcome_page_url", "yearly_price", "yearly_price_id" },
                values: new object[] { "000000000000000000003001", true, new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Free", "free", 0, "free", null, "public", null, null, null });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "description", "name", "updated_at" },
                values: new object[,]
                {
                    { "000000000000000000000001", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Blog Owner", "Owner", null },
                    { "000000000000000000000002", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Administrators", "Administrator", null },
                    { "000000000000000000000003", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Editors", "Editor", null },
                    { "000000000000000000000004", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Authors", "Author", null },
                    { "000000000000000000000005", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Contributors", "Contributor", null }
                });

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "created_at", "flags", "group", "key", "type", "updated_at", "value" },
                values: new object[,]
                {
                    { "000000000000000000001001", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "title", "string", null, "" },
                    { "000000000000000000001002", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "description", "string", null, "Thoughts, stories and ideas." },
                    { "000000000000000000001003", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "logo", "string", null, null },
                    { "000000000000000000001004", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "icon", "string", null, null },
                    { "000000000000000000001005", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "cover_image", "string", null, "https://static.ghost.org/v5.0.0/images/publication-cover.jpg" },
                    { "000000000000000000001006", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "accent_color", "string", null, "#FF1A75" },
                    { "000000000000000000001007", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "locale", "string", null, "en" },
                    { "000000000000000000001008", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "timezone", "string", null, "Etc/UTC" },
                    { "000000000000000000001009", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "facebook", "string", null, null },
                    { "000000000000000000001010", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "twitter", "string", null, null },
                    { "000000000000000000001011", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "meta_title", "string", null, null },
                    { "000000000000000000001012", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "meta_description", "string", null, null },
                    { "000000000000000000001013", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "og_image", "string", null, null },
                    { "000000000000000000001014", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "og_title", "string", null, null },
                    { "000000000000000000001015", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "og_description", "string", null, null },
                    { "000000000000000000001016", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "twitter_image", "string", null, null },
                    { "000000000000000000001017", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "twitter_title", "string", null, null },
                    { "000000000000000000001018", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "twitter_description", "string", null, null },
                    { "000000000000000000001019", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "codeinjection_head", "string", null, null },
                    { "000000000000000000001020", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "codeinjection_foot", "string", null, null },
                    { "000000000000000000001021", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "navigation", "string", null, "[{\"label\":\"Home\",\"url\":\"/\"}]" },
                    { "000000000000000000001022", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "site", "secondary_navigation", "string", null, "[]" },
                    { "000000000000000000001030", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "members", "members_signup_access", "string", null, "all" },
                    { "000000000000000000001031", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "members", "default_content_visibility", "string", null, "public" },
                    { "000000000000000000001032", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "members", "members_track_sources", "boolean", null, "true" },
                    { "000000000000000000001040", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "portal", "portal_button", "boolean", null, "true" },
                    { "000000000000000000001041", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "portal", "portal_plans", "string", null, "[\"free\"]" },
                    { "000000000000000000001042", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "portal", "portal_default_plan", "string", null, "yearly" },
                    { "000000000000000000001043", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "portal", "portal_name", "boolean", null, "true" },
                    { "000000000000000000001050", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "email", "email_track_opens", "boolean", null, "true" },
                    { "000000000000000000001051", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "email", "email_track_clicks", "boolean", null, "true" },
                    { "000000000000000000001052", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "email", "email_verification_required", "boolean", null, "false" },
                    { "000000000000000000001053", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "email", "mailgun_domain", "string", null, null },
                    { "000000000000000000001054", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "email", "mailgun_api_key", "string", null, null },
                    { "000000000000000000001055", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "email", "mailgun_base_url", "string", null, "https://api.mailgun.net/v3" },
                    { "000000000000000000001060", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "donations", "donations_currency", "string", null, "USD" },
                    { "000000000000000000001061", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "donations", "donations_suggested_amount", "string", null, "500" },
                    { "000000000000000000001070", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "labs", "comments_enabled", "string", null, "all" },
                    { "000000000000000000001071", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "labs", "email_analytics_enabled", "boolean", null, "true" },
                    { "000000000000000000001072", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "labs", "outbound_link_tagging", "boolean", null, "true" },
                    { "000000000000000000001073", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "labs", "members_enabled", "boolean", null, "true" },
                    { "000000000000000000001080", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "core", "db_hash", "string", null, "000000000000000000000000" },
                    { "000000000000000000001081", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "core", "active_theme", "string", null, "howtoosoftware-custom" },
                    { "000000000000000000001090", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "theme", "posts_per_page", "number", null, "15" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "newsletters",
                keyColumn: "id",
                keyValue: "000000000000000000002001");

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: "000000000000000000003001");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: "000000000000000000000001");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: "000000000000000000000002");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: "000000000000000000000003");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: "000000000000000000000004");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: "000000000000000000000005");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001001");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001002");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001003");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001004");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001005");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001006");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001007");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001008");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001009");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001010");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001011");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001012");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001013");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001014");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001015");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001016");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001017");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001018");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001019");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001020");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001021");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001022");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001030");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001031");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001032");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001040");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001041");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001042");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001043");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001050");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001051");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001052");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001053");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001054");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001055");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001060");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001061");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001070");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001071");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001072");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001073");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001080");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001081");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: "000000000000000000001090");
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
