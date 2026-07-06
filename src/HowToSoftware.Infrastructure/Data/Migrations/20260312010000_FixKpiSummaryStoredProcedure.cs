using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Fixes usp_KPI_Summary: replaces CROSS APPLY (O(n²) per-row) with a CTE
    /// that pre-aggregates per session, yielding correct avg session duration and
    /// accurate bounce rate at O(n) cost.
    /// </summary>
    public partial class FixKpiSummaryStoredProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER PROCEDURE [dbo].[usp_KPI_Summary]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    ;WITH session_stats AS (
                        SELECT
                            [session_id],
                            COUNT(*)                                                     AS event_count,
                            DATEDIFF(SECOND, MIN([timestamp]), MAX([timestamp]))          AS duration_sec
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @StartDate
                          AND [timestamp] <  @EndDate
                          AND [session_id] IS NOT NULL
                        GROUP BY [session_id]
                    )
                    SELECT
                        COUNT(*)                              AS Visitors,
                        COUNT(*)                              AS Visits,
                        SUM(ss.event_count)                   AS Pageviews,
                        CAST(
                            CASE WHEN COUNT(*) = 0 THEN 0
                            ELSE 100.0
                                * SUM(CASE WHEN ss.event_count = 1 THEN 1 ELSE 0 END)
                                / COUNT(*)
                            END AS DECIMAL(5,2))              AS BounceRatePercent,
                        CAST(
                            ISNULL(AVG(
                                CASE WHEN ss.duration_sec > 0 THEN CAST(ss.duration_sec AS DECIMAL(10,2)) END
                            ), 0) AS DECIMAL(10,2))           AS AvgSessionDurationSeconds
                    FROM session_stats ss;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore original CROSS APPLY version
            migrationBuilder.Sql("""
                ALTER PROCEDURE [dbo].[usp_KPI_Summary]
                    @StartDate DATETIME2,
                    @EndDate   DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        COUNT(DISTINCT ae.[session_id])   AS Visitors,
                        COUNT(DISTINCT ae.[session_id])   AS Visits,
                        COUNT(*)                          AS Pageviews,
                        CAST(
                            CASE WHEN COUNT(DISTINCT ae.[session_id]) = 0 THEN 0
                            ELSE 100.0
                                * SUM(CASE WHEN s.cnt = 1 THEN 1 ELSE 0 END)
                                / COUNT(DISTINCT ae.[session_id])
                            END AS DECIMAL(5,2))          AS BounceRatePercent,
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
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
