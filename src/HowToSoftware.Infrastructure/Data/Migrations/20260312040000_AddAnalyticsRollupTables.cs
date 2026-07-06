using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsRollupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create analytics_hourly_rollups table
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.analytics_hourly_rollups') AND type = N'U')
                CREATE TABLE [dbo].[analytics_hourly_rollups] (
                    [id]               BIGINT         IDENTITY(1,1) NOT NULL,
                    [bucket_hour]      DATETIME2(7)   NOT NULL,
                    [pageviews]        INT            NOT NULL DEFAULT 0,
                    [unique_visitors]  INT            NOT NULL DEFAULT 0,
                    [sessions]         INT            NOT NULL DEFAULT 0,
                    [created_at]       DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME(),
                    [updated_at]       DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [pk_analytics_hourly_rollups] PRIMARY KEY CLUSTERED ([id])
                );
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'ix_analytics_hourly_rollups_bucket_hour'
                      AND object_id = OBJECT_ID('dbo.analytics_hourly_rollups'))
                CREATE UNIQUE INDEX [ix_analytics_hourly_rollups_bucket_hour]
                    ON [dbo].[analytics_hourly_rollups] ([bucket_hour]);
                """);

            // Create analytics_daily_rollups table
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.analytics_daily_rollups') AND type = N'U')
                CREATE TABLE [dbo].[analytics_daily_rollups] (
                    [id]                           BIGINT          IDENTITY(1,1) NOT NULL,
                    [bucket_date]                  DATETIME2(7)    NOT NULL,
                    [pageviews]                    INT             NOT NULL DEFAULT 0,
                    [unique_visitors]              INT             NOT NULL DEFAULT 0,
                    [sessions]                     INT             NOT NULL DEFAULT 0,
                    [bounce_rate_percent]          DECIMAL(5,2)    NOT NULL DEFAULT 0,
                    [avg_session_duration_seconds] DECIMAL(10,2)   NOT NULL DEFAULT 0,
                    [top_pages_json]               NVARCHAR(MAX)   NULL,
                    [top_sources_json]             NVARCHAR(MAX)   NULL,
                    [device_breakdown_json]        NVARCHAR(MAX)   NULL,
                    [country_breakdown_json]       NVARCHAR(MAX)   NULL,
                    [created_at]                   DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
                    [updated_at]                   DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [pk_analytics_daily_rollups] PRIMARY KEY CLUSTERED ([id])
                );
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'ix_analytics_daily_rollups_bucket_date'
                      AND object_id = OBJECT_ID('dbo.analytics_daily_rollups'))
                CREATE UNIQUE INDEX [ix_analytics_daily_rollups_bucket_date]
                    ON [dbo].[analytics_daily_rollups] ([bucket_date]);
                """);

            // Stored procedure: usp_RollupHourly
            // Aggregates raw analytics_events into hourly summary rows using MERGE (upsert)
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[usp_RollupHourly]
                    @BucketHour DATETIME2(7)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @BucketEnd DATETIME2(7) = DATEADD(HOUR, 1, @BucketHour);

                    MERGE [dbo].[analytics_hourly_rollups] AS target
                    USING (
                        SELECT
                            @BucketHour AS [bucket_hour],
                            COUNT(*) AS [pageviews],
                            COUNT(DISTINCT [session_id]) AS [unique_visitors],
                            COUNT(DISTINCT [session_id]) AS [sessions]
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @BucketHour AND [timestamp] < @BucketEnd
                    ) AS source
                    ON target.[bucket_hour] = source.[bucket_hour]
                    WHEN MATCHED THEN
                        UPDATE SET
                            target.[pageviews] = source.[pageviews],
                            target.[unique_visitors] = source.[unique_visitors],
                            target.[sessions] = source.[sessions],
                            target.[updated_at] = SYSUTCDATETIME()
                    WHEN NOT MATCHED THEN
                        INSERT ([bucket_hour], [pageviews], [unique_visitors], [sessions])
                        VALUES (source.[bucket_hour], source.[pageviews], source.[unique_visitors], source.[sessions]);
                END;
                """);

            // Stored procedure: usp_RollupDaily
            // Aggregates raw analytics_events into daily summary rows with dimension breakdowns as JSON
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[usp_RollupDaily]
                    @BucketDate DATETIME2(7)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @BucketEnd DATETIME2(7) = DATEADD(DAY, 1, @BucketDate);

                    -- Core KPIs
                    DECLARE @Pageviews INT, @UniqueVisitors INT, @Sessions INT;
                    DECLARE @BounceRate DECIMAL(5,2), @AvgDuration DECIMAL(10,2);

                    SELECT
                        @Pageviews = COUNT(*),
                        @UniqueVisitors = COUNT(DISTINCT [session_id]),
                        @Sessions = COUNT(DISTINCT [session_id])
                    FROM [dbo].[analytics_events]
                    WHERE [timestamp] >= @BucketDate AND [timestamp] < @BucketEnd;

                    -- Bounce rate: % of sessions with only 1 event
                    ;WITH session_stats AS (
                        SELECT [session_id], COUNT(*) AS event_count
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @BucketDate AND [timestamp] < @BucketEnd
                          AND [session_id] IS NOT NULL
                        GROUP BY [session_id]
                    )
                    SELECT @BounceRate = CAST(
                        CASE WHEN COUNT(*) = 0 THEN 0
                        ELSE 100.0 * SUM(CASE WHEN event_count = 1 THEN 1 ELSE 0 END) / COUNT(*)
                        END AS DECIMAL(5,2))
                    FROM session_stats;

                    -- Average session duration (seconds)
                    ;WITH session_durations AS (
                        SELECT [session_id],
                               DATEDIFF(SECOND, MIN([timestamp]), MAX([timestamp])) AS duration_sec
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @BucketDate AND [timestamp] < @BucketEnd
                          AND [session_id] IS NOT NULL
                        GROUP BY [session_id]
                    )
                    SELECT @AvgDuration = CAST(ISNULL(AVG(
                        CASE WHEN duration_sec > 0 THEN CAST(duration_sec AS DECIMAL(10,2)) END
                    ), 0) AS DECIMAL(10,2))
                    FROM session_durations;

                    -- Top pages JSON (top 50)
                    DECLARE @TopPages NVARCHAR(MAX);
                    SELECT @TopPages = (
                        SELECT TOP 50
                            [page_url_path] AS [path],
                            COUNT(*) AS [views],
                            COUNT(DISTINCT [session_id]) AS [visitors]
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @BucketDate AND [timestamp] < @BucketEnd
                          AND [page_url_path] IS NOT NULL
                        GROUP BY [page_url_path]
                        ORDER BY COUNT(*) DESC
                        FOR JSON PATH
                    );

                    -- Top sources JSON (top 50)
                    DECLARE @TopSources NVARCHAR(MAX);
                    SELECT @TopSources = (
                        SELECT TOP 50
                            COALESCE([referrer], '(direct)') AS [source],
                            COUNT(DISTINCT [session_id]) AS [visits],
                            COUNT(DISTINCT [session_id]) AS [visitors]
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @BucketDate AND [timestamp] < @BucketEnd
                        GROUP BY [referrer]
                        ORDER BY COUNT(DISTINCT [session_id]) DESC
                        FOR JSON PATH
                    );

                    -- Device breakdown JSON
                    DECLARE @Devices NVARCHAR(MAX);
                    SELECT @Devices = (
                        SELECT
                            COALESCE([device], 'Unknown') AS [device],
                            COUNT(DISTINCT [session_id]) AS [sessions]
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @BucketDate AND [timestamp] < @BucketEnd
                        GROUP BY [device]
                        ORDER BY COUNT(DISTINCT [session_id]) DESC
                        FOR JSON PATH
                    );

                    -- Country breakdown JSON
                    DECLARE @Countries NVARCHAR(MAX);
                    SELECT @Countries = (
                        SELECT
                            COALESCE([country], 'Unknown') AS [country],
                            COUNT(DISTINCT [session_id]) AS [visitors]
                        FROM [dbo].[analytics_events]
                        WHERE [timestamp] >= @BucketDate AND [timestamp] < @BucketEnd
                        GROUP BY [country]
                        ORDER BY COUNT(DISTINCT [session_id]) DESC
                        FOR JSON PATH
                    );

                    -- MERGE (upsert)
                    MERGE [dbo].[analytics_daily_rollups] AS target
                    USING (SELECT @BucketDate AS [bucket_date]) AS source
                    ON target.[bucket_date] = source.[bucket_date]
                    WHEN MATCHED THEN
                        UPDATE SET
                            target.[pageviews] = @Pageviews,
                            target.[unique_visitors] = @UniqueVisitors,
                            target.[sessions] = @Sessions,
                            target.[bounce_rate_percent] = ISNULL(@BounceRate, 0),
                            target.[avg_session_duration_seconds] = ISNULL(@AvgDuration, 0),
                            target.[top_pages_json] = @TopPages,
                            target.[top_sources_json] = @TopSources,
                            target.[device_breakdown_json] = @Devices,
                            target.[country_breakdown_json] = @Countries,
                            target.[updated_at] = SYSUTCDATETIME()
                    WHEN NOT MATCHED THEN
                        INSERT ([bucket_date], [pageviews], [unique_visitors], [sessions],
                                [bounce_rate_percent], [avg_session_duration_seconds],
                                [top_pages_json], [top_sources_json],
                                [device_breakdown_json], [country_breakdown_json])
                        VALUES (@BucketDate, @Pageviews, @UniqueVisitors, @Sessions,
                                ISNULL(@BounceRate, 0), ISNULL(@AvgDuration, 0),
                                @TopPages, @TopSources, @Devices, @Countries);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_RollupDaily];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[usp_RollupHourly];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[analytics_daily_rollups];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[analytics_hourly_rollups];");
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
