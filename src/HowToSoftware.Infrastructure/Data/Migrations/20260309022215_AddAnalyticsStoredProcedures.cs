using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── usp_KPI_Summary ──────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_KPI_Summary]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        COUNT(DISTINCT ae.[session_id])   AS Visitors,
                        COUNT(DISTINCT ae.[session_id])   AS Visits,
                        COUNT(*)                          AS Pageviews,
                        -- Bounce rate: % of sessions with exactly 1 pageview
                        CAST(
                            CASE WHEN COUNT(DISTINCT ae.[session_id]) = 0 THEN 0
                            ELSE 100.0
                                * SUM(CASE WHEN s.cnt = 1 THEN 1 ELSE 0 END)
                                / COUNT(DISTINCT ae.[session_id])
                            END AS DECIMAL(5,2))          AS BounceRatePercent,
                        -- Avg session duration in seconds
                        CAST(
                            ISNULL(AVG(
                                CASE WHEN s.duration_sec > 0 THEN s.duration_sec END
                            ), 0) AS DECIMAL(10,2))       AS AvgSessionDurationSeconds
                    FROM [dbo].[analytics_events] ae
                    CROSS APPLY (
                        SELECT
                            COUNT(*)                                               AS cnt,
                            DATEDIFF(SECOND, MIN(ae2.[timestamp]), MAX(ae2.[timestamp])) AS duration_sec
                        FROM [dbo].[analytics_events] ae2
                        WHERE ae2.[session_id] = ae.[session_id]
                          AND ae2.[timestamp] >= @StartDate
                          AND ae2.[timestamp] <  @EndDate
                    ) s
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate;
                END;
                """);

            // ── usp_TopPages ─────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_TopPages]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2,
                    @Top       INT = 20
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@Top)
                        ae.[page_url_path]                    AS PagePath,
                        COUNT(*)                              AS Pageviews,
                        COUNT(DISTINCT ae.[session_id])       AS UniqueVisitors
                    FROM [dbo].[analytics_events] ae
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                      AND ae.[page_url_path] IS NOT NULL
                    GROUP BY ae.[page_url_path]
                    ORDER BY Pageviews DESC;
                END;
                """);

            // ── usp_TopSources ───────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_TopSources]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2,
                    @Top       INT = 20
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@Top)
                        COALESCE(ae.[referrer], '(direct)')   AS Source,
                        COUNT(*)                              AS Visits,
                        COUNT(DISTINCT ae.[session_id])       AS UniqueVisitors
                    FROM [dbo].[analytics_events] ae
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                    GROUP BY ae.[referrer]
                    ORDER BY Visits DESC;
                END;
                """);

            // ── usp_DeviceBreakdown ──────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_DeviceBreakdown]
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
                        COALESCE(ae.[device], 'Unknown')         AS Device,
                        COUNT(DISTINCT ae.[session_id])           AS Sessions,
                        CAST(
                            CASE WHEN t.TotalSessions = 0 THEN 0
                            ELSE 100.0 * COUNT(DISTINCT ae.[session_id]) / t.TotalSessions
                            END AS DECIMAL(5,2))                  AS Percentage
                    FROM [dbo].[analytics_events] ae
                    CROSS JOIN totals t
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                    GROUP BY ae.[device], t.TotalSessions
                    ORDER BY Sessions DESC;
                END;
                """);

            // ── usp_CountryBreakdown ─────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_CountryBreakdown]
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
                        COALESCE(ae.[country], 'Unknown')        AS Country,
                        COUNT(DISTINCT ae.[session_id])           AS Visitors,
                        CAST(
                            CASE WHEN t.TotalSessions = 0 THEN 0
                            ELSE 100.0 * COUNT(DISTINCT ae.[session_id]) / t.TotalSessions
                            END AS DECIMAL(5,2))                  AS Percentage
                    FROM [dbo].[analytics_events] ae
                    CROSS JOIN totals t
                    WHERE ae.[timestamp] >= @StartDate
                      AND ae.[timestamp] <  @EndDate
                    GROUP BY ae.[country], t.TotalSessions
                    ORDER BY Visitors DESC;
                END;
                """);

            // ── usp_HourlyTraffic ────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE PROCEDURE [dbo].[usp_HourlyTraffic]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    ;WITH hours AS (
                        SELECT 0 AS HourOfDay
                        UNION ALL SELECT 1  UNION ALL SELECT 2  UNION ALL SELECT 3
                        UNION ALL SELECT 4  UNION ALL SELECT 5  UNION ALL SELECT 6
                        UNION ALL SELECT 7  UNION ALL SELECT 8  UNION ALL SELECT 9
                        UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
                        UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
                        UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18
                        UNION ALL SELECT 19 UNION ALL SELECT 20 UNION ALL SELECT 21
                        UNION ALL SELECT 22 UNION ALL SELECT 23
                    )
                    SELECT
                        h.HourOfDay,
                        ISNULL(COUNT(ae.[id]), 0)                 AS Pageviews,
                        ISNULL(COUNT(DISTINCT ae.[session_id]), 0) AS UniqueVisitors
                    FROM hours h
                    LEFT JOIN [dbo].[analytics_events] ae
                        ON DATEPART(HOUR, ae.[timestamp]) = h.HourOfDay
                       AND ae.[timestamp] >= @StartDate
                       AND ae.[timestamp] <  @EndDate
                    GROUP BY h.HourOfDay
                    ORDER BY h.HourOfDay;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_KPI_Summary];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_TopPages];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_TopSources];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_DeviceBreakdown];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_CountryBreakdown];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_HourlyTraffic];");
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
