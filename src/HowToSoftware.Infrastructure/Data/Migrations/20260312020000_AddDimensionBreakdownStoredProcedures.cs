using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDimensionBreakdownStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── usp_BrowserBreakdown ─────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_BrowserBreakdown]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    ;WITH totals AS (
                        SELECT COUNT(DISTINCT [session_id]) AS TotalSessions
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @StartDate
                          AND [timestamp] <  @EndDate
                    )
                    SELECT
                        COALESCE(ae.[browser], 'Unknown')        AS Browser,
                        COUNT(DISTINCT ae.[session_id])           AS Sessions,
                        CAST(
                            CASE WHEN t.TotalSessions = 0 THEN 0
                            ELSE 100.0 * COUNT(DISTINCT ae.[session_id]) / t.TotalSessions
                            END AS DECIMAL(5,2))                  AS Percentage
                    FROM [dbo].[analytics_events] ae
                    CROSS JOIN totals t
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                    GROUP BY ae.[browser], t.TotalSessions
                    ORDER BY Sessions DESC;
                END;
                """);

            // ── usp_OsBreakdown ──────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_OsBreakdown]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    ;WITH totals AS (
                        SELECT COUNT(DISTINCT [session_id]) AS TotalSessions
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @StartDate
                          AND [timestamp] <  @EndDate
                    )
                    SELECT
                        COALESCE(ae.[os], 'Unknown')             AS Os,
                        COUNT(DISTINCT ae.[session_id])           AS Sessions,
                        CAST(
                            CASE WHEN t.TotalSessions = 0 THEN 0
                            ELSE 100.0 * COUNT(DISTINCT ae.[session_id]) / t.TotalSessions
                            END AS DECIMAL(5,2))                  AS Percentage
                    FROM [dbo].[analytics_events] ae
                    CROSS JOIN totals t
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                    GROUP BY ae.[os], t.TotalSessions
                    ORDER BY Sessions DESC;
                END;
                """);

            // ── usp_ReferrerBreakdown ────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_ReferrerBreakdown]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2,
                    @Top       INT = 20
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@Top)
                        COALESCE(ae.[referrer], '(direct)')       AS Referrer,
                        COUNT(*)                                  AS Visits,
                        COUNT(DISTINCT ae.[session_id])           AS UniqueVisitors
                    FROM [dbo].[analytics_events] ae
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                    GROUP BY ae.[referrer]
                    ORDER BY Visits DESC;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_BrowserBreakdown];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_OsBreakdown];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_ReferrerBreakdown];");
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
