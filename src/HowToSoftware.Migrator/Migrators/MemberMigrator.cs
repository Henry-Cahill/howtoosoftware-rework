namespace HowToSoftware.Migrator;

/// <summary>
/// Handles member data migration from Ghost MySQL dumps.
/// Processes member records, labels, newsletter subscriptions,
/// Stripe customer mappings, and member event tables.
/// </summary>
public static class MemberMigrator
{
    /// <summary>
    /// All member-related tables that this migrator tracks.
    /// </summary>
    private static readonly HashSet<string> MemberTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "members",
        "labels",
        "members_labels",
        "members_newsletters",
        "members_products",
        "members_stripe_customers",
        "members_stripe_customers_subscriptions",
        "subscriptions",
        "members_cancel_events",
        "members_click_events",
        "members_created_events",
        "members_email_change_events",
        "members_feedback",
        "members_login_events",
        "members_paid_subscription_events",
        "members_payment_events",
        "members_product_events",
        "members_status_events",
        "members_subscribe_events",
        "members_subscription_created_events",
    };

    /// <summary>
    /// Processes parsed INSERT statements to collect member migration statistics.
    /// </summary>
    /// <param name="inserts">Parsed INSERT statements from the MySQL dump.</param>
    /// <returns>The original inserts (unmodified) and member migration statistics.</returns>
    public static MemberMigrationResult ProcessMembers(IReadOnlyList<ParsedInsert> inserts)
    {
        var stats = new MemberMigrationStats();

        foreach (var insert in inserts)
        {
            if (!MemberTables.Contains(insert.TableName))
                continue;

            switch (insert.TableName.ToLowerInvariant())
            {
                case "members":
                    ProcessMembersTable(insert, stats);
                    break;
                case "labels":
                    stats.LabelCount += insert.Rows.Count;
                    break;
                case "members_labels":
                    stats.MembersLabelsCount += insert.Rows.Count;
                    break;
                case "members_newsletters":
                    stats.MembersNewslettersCount += insert.Rows.Count;
                    break;
                case "members_products":
                    stats.MembersProductsCount += insert.Rows.Count;
                    break;
                case "members_stripe_customers":
                    stats.StripeCustomersCount += insert.Rows.Count;
                    break;
                case "members_stripe_customers_subscriptions":
                    stats.StripeSubscriptionsCount += insert.Rows.Count;
                    break;
                case "subscriptions":
                    stats.SubscriptionsCount += insert.Rows.Count;
                    break;
                case "members_cancel_events":
                    stats.CancelEventsCount += insert.Rows.Count;
                    break;
                case "members_click_events":
                    stats.ClickEventsCount += insert.Rows.Count;
                    break;
                case "members_created_events":
                    stats.CreatedEventsCount += insert.Rows.Count;
                    break;
                case "members_email_change_events":
                    stats.EmailChangeEventsCount += insert.Rows.Count;
                    break;
                case "members_feedback":
                    stats.FeedbackCount += insert.Rows.Count;
                    break;
                case "members_login_events":
                    stats.LoginEventsCount += insert.Rows.Count;
                    break;
                case "members_paid_subscription_events":
                    stats.PaidSubscriptionEventsCount += insert.Rows.Count;
                    break;
                case "members_payment_events":
                    stats.PaymentEventsCount += insert.Rows.Count;
                    break;
                case "members_product_events":
                    stats.ProductEventsCount += insert.Rows.Count;
                    break;
                case "members_status_events":
                    stats.StatusEventsCount += insert.Rows.Count;
                    break;
                case "members_subscribe_events":
                    stats.SubscribeEventsCount += insert.Rows.Count;
                    break;
                case "members_subscription_created_events":
                    stats.SubscriptionCreatedEventsCount += insert.Rows.Count;
                    break;
            }
        }

        return new MemberMigrationResult(inserts, stats);
    }

    /// <summary>
    /// Processes the members table to extract status breakdown statistics.
    /// </summary>
    private static void ProcessMembersTable(ParsedInsert insert, MemberMigrationStats stats)
    {
        var statusColIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("status", StringComparison.OrdinalIgnoreCase));

        foreach (var row in insert.Rows)
        {
            stats.MemberCount++;

            var status = statusColIdx >= 0 ? row[statusColIdx] : null;
            switch (status?.ToLowerInvariant())
            {
                case "free":
                    stats.FreeMemberCount++;
                    break;
                case "paid":
                    stats.PaidMemberCount++;
                    break;
                case "comped":
                    stats.CompedMemberCount++;
                    break;
            }
        }
    }

    /// <summary>
    /// Checks if a table name is a member-related table.
    /// </summary>
    public static bool IsMemberTable(string tableName)
        => MemberTables.Contains(tableName);
}

/// <summary>
/// Result of member migration processing.
/// </summary>
public sealed record MemberMigrationResult(
    IReadOnlyList<ParsedInsert> Inserts,
    MemberMigrationStats Stats);

/// <summary>
/// Statistics collected during member migration.
/// </summary>
public sealed class MemberMigrationStats
{
    // Core member counts
    public int MemberCount { get; set; }
    public int FreeMemberCount { get; set; }
    public int PaidMemberCount { get; set; }
    public int CompedMemberCount { get; set; }

    // Related entity counts
    public int LabelCount { get; set; }
    public int MembersLabelsCount { get; set; }
    public int MembersNewslettersCount { get; set; }
    public int MembersProductsCount { get; set; }
    public int SubscriptionsCount { get; set; }

    // Stripe counts
    public int StripeCustomersCount { get; set; }
    public int StripeSubscriptionsCount { get; set; }

    // Event counts
    public int CancelEventsCount { get; set; }
    public int ClickEventsCount { get; set; }
    public int CreatedEventsCount { get; set; }
    public int EmailChangeEventsCount { get; set; }
    public int FeedbackCount { get; set; }
    public int LoginEventsCount { get; set; }
    public int PaidSubscriptionEventsCount { get; set; }
    public int PaymentEventsCount { get; set; }
    public int ProductEventsCount { get; set; }
    public int StatusEventsCount { get; set; }
    public int SubscribeEventsCount { get; set; }
    public int SubscriptionCreatedEventsCount { get; set; }

    public int TotalEventCount =>
        CancelEventsCount + ClickEventsCount + CreatedEventsCount +
        EmailChangeEventsCount + FeedbackCount + LoginEventsCount +
        PaidSubscriptionEventsCount + PaymentEventsCount + ProductEventsCount +
        StatusEventsCount + SubscribeEventsCount + SubscriptionCreatedEventsCount;

    public override string ToString() =>
        $"Members: {MemberCount} (free: {FreeMemberCount}, paid: {PaidMemberCount}, comped: {CompedMemberCount}) | " +
        $"Labels: {LabelCount}, Newsletters: {MembersNewslettersCount}, Products: {MembersProductsCount} | " +
        $"Stripe: {StripeCustomersCount} customers, {StripeSubscriptionsCount} subscriptions | " +
        $"Events: {TotalEventCount} total";
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
