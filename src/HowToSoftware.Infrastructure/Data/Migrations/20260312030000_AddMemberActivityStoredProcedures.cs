using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberActivityStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index to accelerate member-uuid joins on analytics_events
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'ix_analytics_events_member_uuid'
                      AND object_id = OBJECT_ID('dbo.analytics_events'))
                CREATE INDEX [ix_analytics_events_member_uuid]
                    ON [dbo].[analytics_events] ([member_uuid])
                    WHERE [member_uuid] IS NOT NULL;
                """);

            // Index to accelerate post-uuid joins on analytics_events
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'ix_analytics_events_post_uuid'
                      AND object_id = OBJECT_ID('dbo.analytics_events'))
                CREATE INDEX [ix_analytics_events_post_uuid]
                    ON [dbo].[analytics_events] ([post_uuid])
                    WHERE [post_uuid] IS NOT NULL;
                """);

            // ── usp_PostEngagement ───────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_PostEngagement]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2,
                    @Top       INT = 20
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@Top)
                        p.[title]                                            AS PostTitle,
                        p.[slug]                                             AS PostSlug,
                        COUNT(*)                                             AS TotalViews,
                        SUM(CASE WHEN ae.[member_uuid] IS NOT NULL THEN 1 ELSE 0 END) AS MemberViews,
                        SUM(CASE WHEN ae.[member_uuid] IS NULL     THEN 1 ELSE 0 END) AS AnonymousViews,
                        COUNT(DISTINCT ae.[member_uuid])                     AS UniqueMemberViewers
                    FROM [dbo].[analytics_events] ae
                    INNER JOIN [dbo].[posts] p ON p.[uuid] = ae.[post_uuid]
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                      AND ae.[post_uuid] IS NOT NULL
                      AND p.[type] = 'post'
                      AND p.[status] = 'published'
                    GROUP BY p.[title], p.[slug]
                    ORDER BY TotalViews DESC;
                END;
                """);

            // ── usp_MemberEngagement ─────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_MemberEngagement]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2,
                    @Top       INT = 20
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@Top)
                        ae.[member_uuid]                          AS MemberUuid,
                        m.[name]                                  AS MemberName,
                        m.[email]                                 AS MemberEmail,
                        COALESCE(m.[status], ae.[member_status])  AS MemberStatus,
                        COUNT(*)                                  AS Pageviews,
                        COUNT(DISTINCT ae.[session_id])           AS Sessions,
                        MAX(ae.[timestamp])                       AS LastSeenAt,
                        COUNT(DISTINCT ae.[post_uuid])            AS PostsRead
                    FROM [dbo].[analytics_events] ae
                    LEFT JOIN [dbo].[members] m ON m.[uuid] = ae.[member_uuid]
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                      AND ae.[member_uuid] IS NOT NULL
                    GROUP BY ae.[member_uuid], m.[name], m.[email],
                             COALESCE(m.[status], ae.[member_status])
                    ORDER BY Pageviews DESC;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_PostEngagement];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_MemberEngagement];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [ix_analytics_events_member_uuid] ON [dbo].[analytics_events];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [ix_analytics_events_post_uuid] ON [dbo].[analytics_events];");
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
