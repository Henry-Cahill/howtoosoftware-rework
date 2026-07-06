using System.Text;
using System.Text.RegularExpressions;

namespace HowToSoftware.Migrator;

/// <summary>
/// Generates T-SQL INSERT statements from parsed MySQL dump data.
/// Handles type conversions: MySQL escaping → T-SQL escaping,
/// datetime format conversion, BIT handling, and NVARCHAR prefixing.
/// </summary>
public static partial class TSqlGenerator
{
    // MySQL datetime: '2025-11-03 23:39:57'
    private static readonly Regex MySqlDateTimePattern = DateTimeRegex();

    /// <summary>
    /// Escapes a SQL Server bracket-delimited identifier by doubling any ] characters.
    /// e.g. "my]table" → "my]]table", used inside [identifier] brackets.
    /// </summary>
    private static string EscapeIdentifier(string identifier)
        => identifier.Replace("]", "]]");

    /// <summary>
    /// Known MySQL column types per table, used to determine conversion strategy.
    /// </summary>
    internal enum ColType
    {
        /// <summary>String columns (varchar, text, longtext) → NVARCHAR with N'' prefix</summary>
        String,
        /// <summary>Integer columns → passed through as-is</summary>
        Integer,
        /// <summary>Boolean columns (tinyint(1)) → BIT (0/1)</summary>
        Boolean,
        /// <summary>DateTime columns → DATETIME2(7) format</summary>
        DateTime,
    }

    // Posts table column type mapping (Ghost MySQL → SQL Server)
    private static readonly Dictionary<string, ColType> PostsColumnTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = ColType.String,
        ["uuid"] = ColType.String,
        ["title"] = ColType.String,
        ["slug"] = ColType.String,
        ["mobiledoc"] = ColType.String,
        ["lexical"] = ColType.String,
        ["html"] = ColType.String,
        ["comment_id"] = ColType.String,
        ["plaintext"] = ColType.String,
        ["feature_image"] = ColType.String,
        ["featured"] = ColType.Boolean,
        ["type"] = ColType.String,
        ["status"] = ColType.String,
        ["locale"] = ColType.String,
        ["visibility"] = ColType.String,
        ["email_recipient_filter"] = ColType.String,
        ["created_at"] = ColType.DateTime,
        ["updated_at"] = ColType.DateTime,
        ["published_at"] = ColType.DateTime,
        ["published_by"] = ColType.String,
        ["custom_excerpt"] = ColType.String,
        ["codeinjection_head"] = ColType.String,
        ["codeinjection_foot"] = ColType.String,
        ["custom_template"] = ColType.String,
        ["canonical_url"] = ColType.String,
        ["newsletter_id"] = ColType.String,
        ["show_title_and_feature_image"] = ColType.Boolean,
    };

    // Column type mappings for additional Ghost tables
    private static readonly Dictionary<string, Dictionary<string, ColType>> TableColumnTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["posts"] = PostsColumnTypes,
        ["users"] = BuildColumnTypes(
            strings: ["id", "name", "slug", "password", "email", "profile_image", "cover_image", "bio",
                       "website", "location", "facebook", "twitter", "threads", "bluesky", "mastodon",
                       "tik_tok", "you_tube", "instagram", "linked_in", "accessibility", "status",
                       "locale", "visibility", "meta_title", "meta_description", "tour"],
            booleans: ["comment_notifications", "free_member_signup_notification",
                       "paid_subscription_started_notification", "paid_subscription_canceled_notification",
                       "mention_notifications", "recommendation_notifications",
                       "milestone_notifications", "donation_notifications"],
            dateTimes: ["last_seen", "created_at", "updated_at"]),
        ["tags"] = BuildColumnTypes(
            strings: ["id", "name", "slug", "description", "feature_image", "parent_id",
                       "visibility", "og_image", "og_title", "og_description",
                       "twitter_image", "twitter_title", "twitter_description",
                       "meta_title", "meta_description", "codeinjection_head", "codeinjection_foot",
                       "canonical_url", "accent_color"],
            integers: ["sort_order"],
            dateTimes: ["created_at", "updated_at"]),
        ["newsletters"] = BuildColumnTypes(
            strings: ["id", "uuid", "name", "description", "slug", "sender_name", "sender_email",
                       "sender_reply_to", "status", "visibility", "header_image",
                       "title_font_category", "title_alignment", "body_font_category",
                       "footer_content", "background_color", "post_title_color",
                       "button_corners", "button_style", "title_font_weight", "link_style",
                       "image_corners", "header_background_color", "section_title_color",
                       "divider_color", "button_color", "link_color"],
            integers: ["sort_order"],
            booleans: ["feedback_enabled", "subscribe_on_signup", "show_header_icon",
                       "show_header_title", "show_excerpt", "show_feature_image",
                       "show_badge", "show_header_name", "show_post_title_section",
                       "show_comment_cta", "show_subscription_details", "show_latest_posts"],
            dateTimes: ["created_at", "updated_at"]),
        ["members"] = BuildColumnTypes(
            strings: ["id", "uuid", "transient_id", "email", "status", "name",
                       "expertise", "note", "geolocation", "commenting"],
            integers: ["email_count", "email_opened_count", "email_open_rate"],
            booleans: ["enable_comment_notifications", "email_disabled"],
            dateTimes: ["created_at", "updated_at", "last_seen_at", "last_commented_at"]),
        ["roles"] = BuildColumnTypes(
            strings: ["id", "name", "description"],
            dateTimes: ["created_at", "updated_at"]),
        ["permissions"] = BuildColumnTypes(
            strings: ["id", "name", "object_type", "action_type", "object_id"],
            dateTimes: ["created_at", "updated_at"]),
        ["settings"] = BuildColumnTypes(
            strings: ["id", "group", "key", "value", "type", "flags"],
            dateTimes: ["created_at", "updated_at"]),
        ["custom_theme_settings"] = BuildColumnTypes(
            strings: ["id", "theme", "key", "type", "value"]),
        ["products"] = BuildColumnTypes(
            strings: ["id", "name", "slug", "welcome_page_url", "visibility",
                       "description", "type", "currency", "monthly_price_id", "yearly_price_id"],
            integers: ["trial_days", "monthly_price", "yearly_price"],
            booleans: ["active"],
            dateTimes: ["created_at", "updated_at"]),
        ["emails"] = BuildColumnTypes(
            strings: ["id", "post_id", "uuid", "status", "recipient_filter",
                       "error", "error_data", "html", "plaintext", "subject",
                       "from", "reply_to", "newsletter_id", "source", "source_type",
                       "feedback_enabled"],
            integers: ["email_count", "delivered_count", "opened_count", "failed_count"],
            dateTimes: ["submitted_at", "created_at", "updated_at"]),
        ["comments"] = BuildColumnTypes(
            strings: ["id", "post_id", "member_id", "parent_id", "in_reply_to_id",
                       "status", "html", "edited_at"],
            dateTimes: ["created_at", "updated_at"]),
        ["collections"] = BuildColumnTypes(
            strings: ["id", "title", "slug", "description", "type", "filter", "feature_image"],
            dateTimes: ["created_at", "updated_at"]),
        ["integrations"] = BuildColumnTypes(
            strings: ["id", "type", "name", "slug", "icon_image", "description"],
            dateTimes: ["created_at", "updated_at"]),
        ["labels"] = BuildColumnTypes(
            strings: ["id", "name", "slug"],
            dateTimes: ["created_at", "updated_at"]),
        ["benefits"] = BuildColumnTypes(
            strings: ["id", "name", "slug"],
            dateTimes: ["created_at", "updated_at"]),
        ["offers"] = BuildColumnTypes(
            strings: ["id", "name", "code", "product_id", "stripe_coupon_id",
                       "interval", "currency", "discount_type", "duration",
                       "portal_title", "portal_description", "redemption_type"],
            integers: ["discount_amount", "duration_in_months"],
            booleans: ["active"],
            dateTimes: ["created_at", "updated_at"]),
        ["posts_tags"] = BuildColumnTypes(
            strings: ["id", "post_id", "tag_id"],
            integers: ["sort_order"]),
        ["posts_authors"] = BuildColumnTypes(
            strings: ["id", "post_id", "author_id"],
            integers: ["sort_order"]),
        ["posts_meta"] = BuildColumnTypes(
            strings: ["id", "post_id", "og_image", "og_title", "og_description",
                       "twitter_image", "twitter_title", "twitter_description",
                       "meta_title", "meta_description", "email_subject",
                       "frontmatter", "feature_image_alt", "feature_image_caption"],
            booleans: ["email_only"]),
        ["post_revisions"] = BuildColumnTypes(
            strings: ["id", "post_id", "lexical", "author_id", "title",
                       "post_status", "reason", "feature_image",
                       "feature_image_alt", "feature_image_caption", "custom_excerpt"],
            integers: ["created_at_ts"],
            dateTimes: ["created_at"]),
        ["mobiledoc_revisions"] = BuildColumnTypes(
            strings: ["id", "post_id", "mobiledoc"],
            integers: ["created_at_ts"],
            dateTimes: ["created_at"]),
        ["collections_posts"] = BuildColumnTypes(
            strings: ["id", "collection_id", "post_id"],
            integers: ["sort_order"]),
        ["posts_products"] = BuildColumnTypes(
            strings: ["id", "post_id", "product_id"],
            integers: ["sort_order"]),
        // Member-related tables
        ["members_labels"] = BuildColumnTypes(
            strings: ["id", "member_id", "label_id"],
            integers: ["sort_order"]),
        ["members_newsletters"] = BuildColumnTypes(
            strings: ["id", "member_id", "newsletter_id"]),
        ["members_products"] = BuildColumnTypes(
            strings: ["id", "member_id", "product_id"],
            integers: ["sort_order"],
            dateTimes: ["expiry_at"]),
        ["members_stripe_customers"] = BuildColumnTypes(
            strings: ["id", "member_id", "customer_id", "name", "email"],
            dateTimes: ["created_at", "updated_at"]),
        ["members_stripe_customers_subscriptions"] = BuildColumnTypes(
            strings: ["id", "customer_id", "ghost_subscription_id", "subscription_id",
                       "stripe_price_id", "status", "cancellation_reason",
                       "default_payment_card_last4", "offer_id",
                       "plan_id", "plan_nickname", "plan_interval", "plan_currency"],
            integers: ["mrr", "plan_amount"],
            booleans: ["cancel_at_period_end"],
            dateTimes: ["current_period_end", "start_date", "created_at", "updated_at",
                         "trial_start_at", "trial_end_at", "discount_start", "discount_end"]),
        ["subscriptions"] = BuildColumnTypes(
            strings: ["id", "type", "status", "member_id", "tier_id", "cadence",
                       "currency", "payment_provider", "payment_subscription_url",
                       "payment_user_url", "offer_id"],
            integers: ["amount"],
            dateTimes: ["expires_at", "created_at", "updated_at"]),
        // Member event tables
        ["members_cancel_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "from_plan"],
            dateTimes: ["created_at"]),
        ["members_click_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "redirect_id"],
            dateTimes: ["created_at"]),
        ["members_created_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "attribution_id", "attribution_type",
                       "attribution_url", "referrer_source", "referrer_medium",
                       "referrer_url", "source", "batch_id",
                       "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content"],
            dateTimes: ["created_at"]),
        ["members_email_change_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "to_email", "from_email"],
            dateTimes: ["created_at"]),
        ["members_feedback"] = BuildColumnTypes(
            strings: ["id", "member_id", "post_id"],
            integers: ["score"],
            dateTimes: ["created_at", "updated_at"]),
        ["members_login_events"] = BuildColumnTypes(
            strings: ["id", "member_id"],
            dateTimes: ["created_at"]),
        ["members_paid_subscription_events"] = BuildColumnTypes(
            strings: ["id", "type", "member_id", "subscription_id",
                       "from_plan", "to_plan", "currency", "source"],
            integers: ["mrr_delta"],
            dateTimes: ["created_at"]),
        ["members_payment_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "currency", "source"],
            integers: ["amount"],
            dateTimes: ["created_at"]),
        ["members_product_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "product_id", "action"],
            dateTimes: ["created_at"]),
        ["members_status_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "from_status", "to_status"],
            dateTimes: ["created_at"]),
        ["members_subscribe_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "source", "newsletter_id"],
            booleans: ["subscribed"],
            dateTimes: ["created_at"]),
        ["members_subscription_created_events"] = BuildColumnTypes(
            strings: ["id", "member_id", "subscription_id", "attribution_id",
                       "attribution_type", "attribution_url", "referrer_source",
                       "referrer_medium", "referrer_url", "batch_id",
                       "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content"],
            dateTimes: ["created_at"]),
        // Analytics
        ["analytics_events"] = BuildColumnTypes(
            strings: ["session_id", "action", "version", "payload", "site_uuid",
                       "page_url", "page_url_path", "referrer", "device", "browser",
                       "os", "country", "member_uuid", "member_status", "post_uuid"],
            dateTimes: ["timestamp", "backed_up_at"]),
    };

    private static Dictionary<string, ColType> BuildColumnTypes(
        string[]? strings = null,
        string[]? integers = null,
        string[]? booleans = null,
        string[]? dateTimes = null)
    {
        var dict = new Dictionary<string, ColType>(StringComparer.OrdinalIgnoreCase);
        if (strings is not null) foreach (var s in strings) dict[s] = ColType.String;
        if (integers is not null) foreach (var s in integers) dict[s] = ColType.Integer;
        if (booleans is not null) foreach (var s in booleans) dict[s] = ColType.Boolean;
        if (dateTimes is not null) foreach (var s in dateTimes) dict[s] = ColType.DateTime;
        return dict;
    }

    /// <summary>
    /// Generates a complete T-SQL migration script from parsed MySQL INSERT statements.
    /// Wraps output in SET IDENTITY_INSERT, disables FK constraints, and uses transactions.
    /// </summary>
    public static string Generate(IEnumerable<ParsedInsert> inserts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- =============================================================================");
        sb.AppendLine("-- Ghost MySQL → SQL Server Migration Script");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- =============================================================================");
        sb.AppendLine();
        sb.AppendLine("SET NOCOUNT ON;");
        sb.AppendLine("SET XACT_ABORT ON;");
        sb.AppendLine("GO");
        sb.AppendLine();

        // Disable all FK constraints
        sb.AppendLine("-- Disable foreign key constraints for bulk insert");
        sb.AppendLine("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';");
        sb.AppendLine("GO");
        sb.AppendLine();

        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine("GO");
        sb.AppendLine();

        // Collect table names for cleanup
        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var insertList = inserts.ToList();
        foreach (var insert in insertList)
            tableNames.Add(insert.TableName);

        // Delete existing rows in migrated tables (idempotent re-runs)
        foreach (var table in tableNames)
        {
            sb.AppendLine($"DELETE FROM [dbo].[{EscapeIdentifier(table)}];");
        }
        sb.AppendLine();

        foreach (var insert in insertList)
        {
            GenerateTableInserts(sb, insert);
        }

        sb.AppendLine("COMMIT TRANSACTION;");
        sb.AppendLine("GO");
        sb.AppendLine();

        // Re-enable FK constraints (CHECK only, no WITH CHECK validation —
        // referencing tables may not be fully populated yet in partial migrations)
        sb.AppendLine("-- Re-enable foreign key constraints");
        sb.AppendLine("EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';");
        sb.AppendLine("GO");

        return sb.ToString();
    }

    /// <summary>
    /// Generates T-SQL INSERT statements for a single table.
    /// </summary>
    private static void GenerateTableInserts(StringBuilder sb, ParsedInsert insert)
    {
        sb.AppendLine($"-- Table: {insert.TableName} ({insert.Rows.Count} rows)");
        sb.AppendLine($"PRINT N'Migrating {insert.TableName} ({insert.Rows.Count} rows)...';");
        sb.AppendLine();

        var columnTypes = GetColumnTypes(insert.TableName);
        var columnList = string.Join(", ", insert.Columns.Select(c => $"[{EscapeIdentifier(c)}]"));

        foreach (var row in insert.Rows)
        {
            sb.Append($"INSERT INTO [dbo].[{EscapeIdentifier(insert.TableName)}] ({columnList}) VALUES (");

            for (var i = 0; i < row.Length; i++)
            {
                if (i > 0) sb.Append(", ");

                var colName = i < insert.Columns.Length ? insert.Columns[i] : null;
                var colType = colName is not null && columnTypes.TryGetValue(colName, out var ct)
                    ? ct
                    : (colName is not null ? InferColumnType(colName) : ColType.String);
                var value = row[i];

                sb.Append(ConvertValue(value, colType));
            }

            sb.AppendLine(");");
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Converts a single MySQL value to its T-SQL representation.
    /// </summary>
    internal static string ConvertValue(string? mysqlValue, ColType colType)
    {
        if (mysqlValue is null)
            return "NULL";

        return colType switch
        {
            ColType.String => ConvertString(mysqlValue),
            ColType.Integer => ConvertInteger(mysqlValue),
            ColType.Boolean => ConvertBoolean(mysqlValue),
            ColType.DateTime => ConvertDateTime(mysqlValue),
            _ => ConvertString(mysqlValue),
        };
    }

    /// <summary>
    /// Converts a MySQL string value to a T-SQL NVARCHAR literal.
    /// Handles MySQL backslash escapes → T-SQL single-quote escapes.
    /// </summary>
    internal static string ConvertString(string mysqlValue)
    {
        // Convert MySQL backslash escapes to their actual characters,
        // then re-escape for T-SQL (single quotes only)
        var sb = new StringBuilder(mysqlValue.Length + 10);

        for (var i = 0; i < mysqlValue.Length; i++)
        {
            if (mysqlValue[i] == '\\' && i + 1 < mysqlValue.Length)
            {
                var next = mysqlValue[i + 1];
                switch (next)
                {
                    case '\'':
                        sb.Append("''"); // MySQL \' → T-SQL ''
                        i++;
                        break;
                    case '"':
                        sb.Append('"');
                        i++;
                        break;
                    case '\\':
                        sb.Append('\\');
                        i++;
                        break;
                    case 'n':
                        sb.Append('\n');
                        i++;
                        break;
                    case 'r':
                        sb.Append('\r');
                        i++;
                        break;
                    case 't':
                        sb.Append('\t');
                        i++;
                        break;
                    case '0':
                        sb.Append('\0');
                        i++;
                        break;
                    default:
                        sb.Append(next); // Unknown escape, just use the char
                        i++;
                        break;
                }
            }
            else if (mysqlValue[i] == '\'')
            {
                // Already-doubled single quote from MySQL '' escaping
                sb.Append("''");
                if (i + 1 < mysqlValue.Length && mysqlValue[i + 1] == '\'')
                    i++; // skip second quote of ''
            }
            else
            {
                sb.Append(mysqlValue[i]);
            }
        }

        return $"N'{sb}'";
    }

    /// <summary>
    /// Converts a MySQL integer value to T-SQL. Passes through numeric values.
    /// </summary>
    private static string ConvertInteger(string mysqlValue)
    {
        // Numeric values pass through as-is
        return mysqlValue;
    }

    /// <summary>
    /// Converts a MySQL tinyint(1) boolean to T-SQL BIT (0 or 1).
    /// </summary>
    private static string ConvertBoolean(string mysqlValue)
    {
        return mysqlValue == "0" ? "0" : "1";
    }

    /// <summary>
    /// Converts a MySQL datetime string to T-SQL DATETIME2 format.
    /// MySQL: '2025-11-03 23:39:57' → T-SQL: '2025-11-03T23:39:57.0000000'
    /// </summary>
    internal static string ConvertDateTime(string mysqlValue)
    {
        var match = MySqlDateTimePattern.Match(mysqlValue);
        if (match.Success)
        {
            // MySQL zero-date → NULL (SQL Server DATETIME2 min is 0001-01-01)
            if (match.Groups[1].Value == "0000-00-00")
                return "NULL";

            // MySQL: YYYY-MM-DD HH:mm:ss → T-SQL ISO 8601: YYYY-MM-DDTHH:mm:ss.0000000
            return $"'{match.Groups[1].Value}T{match.Groups[2].Value}.0000000'";
        }

        // If it doesn't match expected pattern, wrap as string
        return $"N'{mysqlValue}'";
    }

    private static Dictionary<string, ColType> GetColumnTypes(string tableName)
    {
        if (TableColumnTypes.TryGetValue(tableName, out var types))
            return types;

        // For unknown tables, return empty dict — GenerateTableInserts uses InferColumnType as fallback
        return new Dictionary<string, ColType>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves the column type for a given table and column name.
    /// Uses explicit mappings first, then falls back to inference from column name patterns.
    /// </summary>
    internal static ColType ResolveColumnType(string tableName, string columnName)
    {
        var columnTypes = GetColumnTypes(tableName);
        if (columnTypes.TryGetValue(columnName, out var ct))
            return ct;
        return InferColumnType(columnName);
    }

    // Join tables and other simple tables for which we auto-detect types
    // based on common column name patterns
    internal static ColType InferColumnType(string columnName)
    {
        if (columnName.EndsWith("_at", StringComparison.OrdinalIgnoreCase))
            return ColType.DateTime;
        if (columnName.Equals("sort_order", StringComparison.OrdinalIgnoreCase))
            return ColType.Integer;
        if (columnName.Equals("order", StringComparison.OrdinalIgnoreCase))
            return ColType.Integer;
        return ColType.String;
    }

    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2})\s+(\d{2}:\d{2}:\d{2})$")]
    private static partial Regex DateTimeRegex();
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
