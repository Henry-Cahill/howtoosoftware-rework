using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoveringIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_posts_slug_type",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_members_email",
                table: "members");

            migrationBuilder.DropIndex(
                name: "ix_email_recipients_email_id_member_email",
                table: "email_recipients");

            migrationBuilder.DropIndex(
                name: "ix_analytics_events_session_id",
                table: "analytics_events");

            migrationBuilder.DropIndex(
                name: "ix_analytics_events_timestamp",
                table: "analytics_events");

            migrationBuilder.CreateIndex(
                name: "ix_posts_slug_type",
                table: "posts",
                columns: new[] { "slug", "type" },
                unique: true)
                .Annotation("SqlServer:Include", new[] { "status", "visibility", "published_at", "title" });

            migrationBuilder.CreateIndex(
                name: "ix_posts_status_type_published_at",
                table: "posts",
                columns: new[] { "status", "type", "published_at" })
                .Annotation("SqlServer:Include", new[] { "id", "title", "slug", "feature_image", "custom_excerpt", "featured", "visibility" });

            migrationBuilder.CreateIndex(
                name: "ix_members_email",
                table: "members",
                column: "email",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "name", "status", "uuid" });

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_email_id_member_email",
                table: "email_recipients",
                columns: new[] { "email_id", "member_email" })
                .Annotation("SqlServer:Include", new[] { "member_id", "member_name", "processed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_email_id_processed_at",
                table: "email_recipients",
                columns: new[] { "email_id", "processed_at" })
                .Annotation("SqlServer:Include", new[] { "delivered_at", "opened_at", "failed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_analytics_events_session_id_timestamp",
                table: "analytics_events",
                columns: new[] { "session_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_analytics_events_timestamp",
                table: "analytics_events",
                column: "timestamp")
                .Annotation("SqlServer:Include", new[] { "session_id", "page_url_path", "referrer", "device", "country" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_posts_slug_type",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_posts_status_type_published_at",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_members_email",
                table: "members");

            migrationBuilder.DropIndex(
                name: "ix_email_recipients_email_id_member_email",
                table: "email_recipients");

            migrationBuilder.DropIndex(
                name: "ix_email_recipients_email_id_processed_at",
                table: "email_recipients");

            migrationBuilder.DropIndex(
                name: "ix_analytics_events_session_id_timestamp",
                table: "analytics_events");

            migrationBuilder.DropIndex(
                name: "ix_analytics_events_timestamp",
                table: "analytics_events");

            migrationBuilder.CreateIndex(
                name: "ix_posts_slug_type",
                table: "posts",
                columns: new[] { "slug", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_email",
                table: "members",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_email_id_member_email",
                table: "email_recipients",
                columns: new[] { "email_id", "member_email" });

            migrationBuilder.CreateIndex(
                name: "ix_analytics_events_session_id",
                table: "analytics_events",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_analytics_events_timestamp",
                table: "analytics_events",
                column: "timestamp");
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
