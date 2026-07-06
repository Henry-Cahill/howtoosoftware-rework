using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class MemberMigratorTests
{
    #region IsMemberTable

    [Theory]
    [InlineData("members", true)]
    [InlineData("labels", true)]
    [InlineData("members_labels", true)]
    [InlineData("members_newsletters", true)]
    [InlineData("members_products", true)]
    [InlineData("members_stripe_customers", true)]
    [InlineData("members_stripe_customers_subscriptions", true)]
    [InlineData("subscriptions", true)]
    [InlineData("members_cancel_events", true)]
    [InlineData("members_click_events", true)]
    [InlineData("members_created_events", true)]
    [InlineData("members_email_change_events", true)]
    [InlineData("members_feedback", true)]
    [InlineData("members_login_events", true)]
    [InlineData("members_paid_subscription_events", true)]
    [InlineData("members_payment_events", true)]
    [InlineData("members_product_events", true)]
    [InlineData("members_status_events", true)]
    [InlineData("members_subscribe_events", true)]
    [InlineData("members_subscription_created_events", true)]
    [InlineData("posts", false)]
    [InlineData("tags", false)]
    [InlineData("users", false)]
    [InlineData("newsletters", false)]
    public void IsMemberTable_ReturnsCorrectResult(string tableName, bool expected)
    {
        Assert.Equal(expected, MemberMigrator.IsMemberTable(tableName));
    }

    [Fact]
    public void IsMemberTable_CaseInsensitive()
    {
        Assert.True(MemberMigrator.IsMemberTable("MEMBERS"));
        Assert.True(MemberMigrator.IsMemberTable("Members_Labels"));
    }

    #endregion

    #region ProcessMembers — Member Counts & Status Breakdown

    [Fact]
    public void ProcessMembers_EmptyInserts_ReturnsZeroStats()
    {
        var result = MemberMigrator.ProcessMembers([]);
        Assert.Equal(0, result.Stats.MemberCount);
        Assert.Equal(0, result.Stats.TotalEventCount);
    }

    [Fact]
    public void ProcessMembers_MembersTable_CountsTotal()
    {
        var inserts = new[]
        {
            new ParsedInsert("members",
                ["id", "email", "status"],
                [
                    ["1", "alice@example.com", "free"],
                    ["2", "bob@example.com", "paid"],
                    ["3", "carol@example.com", "free"],
                ])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(3, result.Stats.MemberCount);
    }

    [Fact]
    public void ProcessMembers_MembersTable_BreaksDownByStatus()
    {
        var inserts = new[]
        {
            new ParsedInsert("members",
                ["id", "email", "status"],
                [
                    ["1", "alice@example.com", "free"],
                    ["2", "bob@example.com", "paid"],
                    ["3", "carol@example.com", "free"],
                    ["4", "dave@example.com", "comped"],
                    ["5", "eve@example.com", "paid"],
                ])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(5, result.Stats.MemberCount);
        Assert.Equal(2, result.Stats.FreeMemberCount);
        Assert.Equal(2, result.Stats.PaidMemberCount);
        Assert.Equal(1, result.Stats.CompedMemberCount);
    }

    [Fact]
    public void ProcessMembers_MembersWithoutStatusColumn_CountsTotalOnly()
    {
        var inserts = new[]
        {
            new ParsedInsert("members",
                ["id", "email"],
                [
                    ["1", "alice@example.com"],
                    ["2", "bob@example.com"],
                ])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(2, result.Stats.MemberCount);
        Assert.Equal(0, result.Stats.FreeMemberCount);
        Assert.Equal(0, result.Stats.PaidMemberCount);
        Assert.Equal(0, result.Stats.CompedMemberCount);
    }

    #endregion

    #region ProcessMembers — Related Entities

    [Fact]
    public void ProcessMembers_Labels_CountsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("labels",
                ["id", "name", "slug"],
                [
                    ["1", "VIP", "vip"],
                    ["2", "Beta", "beta"],
                ])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(2, result.Stats.LabelCount);
    }

    [Fact]
    public void ProcessMembers_MembersLabels_CountsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("members_labels",
                ["id", "member_id", "label_id", "sort_order"],
                [
                    ["1", "m1", "l1", "0"],
                    ["2", "m2", "l1", "0"],
                    ["3", "m1", "l2", "1"],
                ])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(3, result.Stats.MembersLabelsCount);
    }

    [Fact]
    public void ProcessMembers_MembersNewsletters_CountsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("members_newsletters",
                ["id", "member_id", "newsletter_id"],
                [
                    ["1", "m1", "n1"],
                    ["2", "m2", "n1"],
                ])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(2, result.Stats.MembersNewslettersCount);
    }

    [Fact]
    public void ProcessMembers_MembersProducts_CountsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("members_products",
                ["id", "member_id", "product_id"],
                [["1", "m1", "p1"]])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(1, result.Stats.MembersProductsCount);
    }

    [Fact]
    public void ProcessMembers_StripeCustomers_CountsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("members_stripe_customers",
                ["id", "member_id", "customer_id"],
                [
                    ["1", "m1", "cus_abc"],
                    ["2", "m2", "cus_def"],
                ])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(2, result.Stats.StripeCustomersCount);
    }

    [Fact]
    public void ProcessMembers_StripeSubscriptions_CountsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("members_stripe_customers_subscriptions",
                ["id", "customer_id", "subscription_id", "status"],
                [["1", "cus_abc", "sub_123", "active"]])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(1, result.Stats.StripeSubscriptionsCount);
    }

    #endregion

    #region ProcessMembers — Event Tables

    [Fact]
    public void ProcessMembers_AllEventTypes_CountedCorrectly()
    {
        var inserts = new ParsedInsert[]
        {
            new("members_cancel_events", ["id", "member_id"], [["1", "m1"]]),
            new("members_click_events", ["id", "member_id"], [["1", "m1"], ["2", "m2"]]),
            new("members_created_events", ["id", "member_id", "source"], [["1", "m1", "admin"]]),
            new("members_email_change_events", ["id", "member_id"], [["1", "m1"]]),
            new("members_feedback", ["id", "member_id", "post_id"], [["1", "m1", "p1"]]),
            new("members_login_events", ["id", "member_id"], [["1", "m1"], ["2", "m1"], ["3", "m2"]]),
            new("members_paid_subscription_events", ["id", "member_id"], [["1", "m1"]]),
            new("members_payment_events", ["id", "member_id"], [["1", "m1"]]),
            new("members_product_events", ["id", "member_id", "product_id"], [["1", "m1", "p1"]]),
            new("members_status_events", ["id", "member_id"], [["1", "m1"], ["2", "m1"]]),
            new("members_subscribe_events", ["id", "member_id"], [["1", "m1"]]),
            new("members_subscription_created_events", ["id", "member_id", "subscription_id"], [["1", "m1", "s1"]]),
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(1, result.Stats.CancelEventsCount);
        Assert.Equal(2, result.Stats.ClickEventsCount);
        Assert.Equal(1, result.Stats.CreatedEventsCount);
        Assert.Equal(1, result.Stats.EmailChangeEventsCount);
        Assert.Equal(1, result.Stats.FeedbackCount);
        Assert.Equal(3, result.Stats.LoginEventsCount);
        Assert.Equal(1, result.Stats.PaidSubscriptionEventsCount);
        Assert.Equal(1, result.Stats.PaymentEventsCount);
        Assert.Equal(1, result.Stats.ProductEventsCount);
        Assert.Equal(2, result.Stats.StatusEventsCount);
        Assert.Equal(1, result.Stats.SubscribeEventsCount);
        Assert.Equal(1, result.Stats.SubscriptionCreatedEventsCount);
        Assert.Equal(16, result.Stats.TotalEventCount);
    }

    #endregion

    #region ProcessMembers — Mixed Tables

    [Fact]
    public void ProcessMembers_MixedMemberAndNonMemberTables_OnlyCountsMemberTables()
    {
        var inserts = new ParsedInsert[]
        {
            new("members", ["id", "email", "status"], [["1", "a@b.com", "free"]]),
            new("labels", ["id", "name", "slug"], [["1", "VIP", "vip"]]),
            new("posts", ["id", "title"], [["1", "My Post"]]),
            new("tags", ["id", "name"], [["1", "Tech"]]),
            new("members_newsletters", ["id", "member_id", "newsletter_id"], [["1", "1", "n1"]]),
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(1, result.Stats.MemberCount);
        Assert.Equal(1, result.Stats.FreeMemberCount);
        Assert.Equal(1, result.Stats.LabelCount);
        Assert.Equal(1, result.Stats.MembersNewslettersCount);
        // Non-member tables should not affect any counts
        Assert.Equal(0, result.Stats.PaidMemberCount);
    }

    [Fact]
    public void ProcessMembers_ReturnsOriginalInserts()
    {
        var inserts = new List<ParsedInsert>
        {
            new("members", ["id", "email", "status"], [["1", "a@b.com", "free"]]),
            new("posts", ["id", "title"], [["1", "Post"]]),
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Same(inserts, result.Inserts);
    }

    #endregion

    #region ProcessMembers — Subscriptions

    [Fact]
    public void ProcessMembers_Subscriptions_CountsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("subscriptions",
                ["id", "type", "status", "member_id", "tier_id"],
                [
                    ["1", "paid", "active", "m1", "t1"],
                    ["2", "paid", "canceled", "m2", "t1"],
                ])
        };

        var result = MemberMigrator.ProcessMembers(inserts);

        Assert.Equal(2, result.Stats.SubscriptionsCount);
    }

    #endregion

    #region TSqlGenerator — Member Column Type Mappings

    [Fact]
    public void ResolveColumnType_MembersTable_ReturnsCorrectTypes()
    {
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members", "id"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members", "email"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members", "status"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members", "commenting"));
        Assert.Equal(TSqlGenerator.ColType.Boolean, TSqlGenerator.ResolveColumnType("members", "enable_comment_notifications"));
        Assert.Equal(TSqlGenerator.ColType.Boolean, TSqlGenerator.ResolveColumnType("members", "email_disabled"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("members", "email_count"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("members", "email_open_rate"));
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("members", "created_at"));
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("members", "last_seen_at"));
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("members", "last_commented_at"));
    }

    [Fact]
    public void ResolveColumnType_MemberJoinTables_ReturnsCorrectTypes()
    {
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members_labels", "id"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("members_labels", "sort_order"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members_newsletters", "member_id"));
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("members_products", "expiry_at"));
    }

    [Fact]
    public void ResolveColumnType_StripeCustomerTables_ReturnsCorrectTypes()
    {
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members_stripe_customers", "customer_id"));
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("members_stripe_customers", "created_at"));
        Assert.Equal(TSqlGenerator.ColType.Boolean, TSqlGenerator.ResolveColumnType("members_stripe_customers_subscriptions", "cancel_at_period_end"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("members_stripe_customers_subscriptions", "mrr"));
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("members_stripe_customers_subscriptions", "current_period_end"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("members_stripe_customers_subscriptions", "plan_amount"));
    }

    [Fact]
    public void ResolveColumnType_SubscriptionsTable_ReturnsCorrectTypes()
    {
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("subscriptions", "amount"));
        Assert.Equal(TSqlGenerator.ColType.DateTime, TSqlGenerator.ResolveColumnType("subscriptions", "expires_at"));
    }

    [Fact]
    public void ResolveColumnType_MemberEventTables_ReturnsCorrectTypes()
    {
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members_cancel_events", "from_plan"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members_created_events", "source"));
        Assert.Equal(TSqlGenerator.ColType.String, TSqlGenerator.ResolveColumnType("members_created_events", "utm_source"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("members_feedback", "score"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("members_paid_subscription_events", "mrr_delta"));
        Assert.Equal(TSqlGenerator.ColType.Integer, TSqlGenerator.ResolveColumnType("members_payment_events", "amount"));
        Assert.Equal(TSqlGenerator.ColType.Boolean, TSqlGenerator.ResolveColumnType("members_subscribe_events", "subscribed"));
    }

    #endregion

    #region TSqlGenerator — Member T-SQL Generation

    [Fact]
    public void Generate_MembersTable_ProducesValidTSql()
    {
        var inserts = new[]
        {
            new ParsedInsert("members",
                ["id", "uuid", "email", "status", "name", "enable_comment_notifications",
                 "email_count", "created_at"],
                [
                    ["abc123", "550e8400-e29b-41d4-a716-446655440000", "alice@test.com",
                     "free", "Alice", "1", "0", "2025-06-15 10:30:00"],
                ])
        };

        var tsql = TSqlGenerator.Generate(inserts);

        Assert.Contains("INSERT INTO [dbo].[members]", tsql);
        Assert.Contains("N'abc123'", tsql);
        Assert.Contains("N'alice@test.com'", tsql);
        Assert.Contains("N'free'", tsql);
        Assert.Contains("1", tsql); // enable_comment_notifications as boolean
        Assert.Contains("0", tsql); // email_count as integer
        Assert.Contains("'2025-06-15T10:30:00.0000000'", tsql); // datetime conversion
    }

    [Fact]
    public void Generate_MembersLabelsJoin_ProducesValidTSql()
    {
        var inserts = new[]
        {
            new ParsedInsert("members_labels",
                ["id", "member_id", "label_id", "sort_order"],
                [["ml1", "m1", "l1", "0"]])
        };

        var tsql = TSqlGenerator.Generate(inserts);

        Assert.Contains("INSERT INTO [dbo].[members_labels]", tsql);
        Assert.Contains("N'ml1'", tsql);
        Assert.Contains("0", tsql); // sort_order as integer
    }

    [Fact]
    public void Generate_StripeSubscription_ProducesValidTSql()
    {
        var inserts = new[]
        {
            new ParsedInsert("members_stripe_customers_subscriptions",
                ["id", "customer_id", "subscription_id", "status",
                 "cancel_at_period_end", "mrr", "current_period_end", "start_date",
                 "plan_id", "plan_nickname", "plan_interval", "plan_amount", "plan_currency"],
                [["s1", "cus_abc", "sub_123", "active",
                  "0", "500", "2026-01-15 00:00:00", "2025-01-15 00:00:00",
                  "price_abc", "Monthly", "month", "500", "usd"]])
        };

        var tsql = TSqlGenerator.Generate(inserts);

        Assert.Contains("INSERT INTO [dbo].[members_stripe_customers_subscriptions]", tsql);
        Assert.Contains("N'sub_123'", tsql);
        Assert.Contains("0", tsql); // cancel_at_period_end as boolean
        Assert.Contains("500", tsql); // mrr as integer
        Assert.Contains("'2026-01-15T00:00:00.0000000'", tsql); // datetime
    }

    #endregion

    #region GhostMigrator Integration

    [Fact]
    public void GhostMigrator_WithMemberData_PopulatesMemberStats()
    {
        var dumpFile = Path.GetTempFileName();
        var outputFile = Path.Combine(Path.GetTempPath(), $"migration_members_{Guid.NewGuid()}.sql");

        try
        {
            File.WriteAllText(dumpFile,
                "INSERT INTO `members` (`id`, `email`, `status`, `created_at`) VALUES " +
                "('m1','alice@test.com','free','2025-06-15 10:30:00')," +
                "('m2','bob@test.com','paid','2025-07-20 14:00:00');\n" +
                "INSERT INTO `labels` (`id`, `name`, `slug`, `created_at`) VALUES " +
                "('l1','VIP','vip','2025-01-01 00:00:00');\n" +
                "INSERT INTO `members_labels` (`id`, `member_id`, `label_id`, `sort_order`) VALUES " +
                "('ml1','m1','l1',0);\n" +
                "INSERT INTO `members_newsletters` (`id`, `member_id`, `newsletter_id`) VALUES " +
                "('mn1','m1','n1'),('mn2','m2','n1');\n");

            var migrator = new GhostMigrator();
            var result = migrator.Migrate([dumpFile], outputFile);

            Assert.NotNull(result.MemberStats);
            Assert.Equal(2, result.MemberStats.MemberCount);
            Assert.Equal(1, result.MemberStats.FreeMemberCount);
            Assert.Equal(1, result.MemberStats.PaidMemberCount);
            Assert.Equal(1, result.MemberStats.LabelCount);
            Assert.Equal(1, result.MemberStats.MembersLabelsCount);
            Assert.Equal(2, result.MemberStats.MembersNewslettersCount);

            // Verify T-SQL output
            var tsql = File.ReadAllText(outputFile);
            Assert.Contains("INSERT INTO [dbo].[members]", tsql);
            Assert.Contains("INSERT INTO [dbo].[labels]", tsql);
            Assert.Contains("INSERT INTO [dbo].[members_labels]", tsql);
            Assert.Contains("INSERT INTO [dbo].[members_newsletters]", tsql);
        }
        finally
        {
            File.Delete(dumpFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }

    [Fact]
    public void GhostMigrator_NoMemberData_MemberStatsIsNull()
    {
        var dumpFile = Path.GetTempFileName();
        var outputFile = Path.Combine(Path.GetTempPath(), $"migration_nomembers_{Guid.NewGuid()}.sql");

        try
        {
            File.WriteAllText(dumpFile,
                "INSERT INTO `posts` (`id`, `title`, `created_at`) VALUES ('p1','Test','2025-01-01 00:00:00');");

            var migrator = new GhostMigrator();
            var result = migrator.Migrate([dumpFile], outputFile);

            Assert.Null(result.MemberStats);
        }
        finally
        {
            File.Delete(dumpFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }

    #endregion

    #region MemberMigrationStats.ToString

    [Fact]
    public void MemberMigrationStats_ToString_FormatsCorrectly()
    {
        var stats = new MemberMigrationStats
        {
            MemberCount = 5,
            FreeMemberCount = 3,
            PaidMemberCount = 1,
            CompedMemberCount = 1,
            LabelCount = 2,
            MembersNewslettersCount = 4,
            MembersProductsCount = 1,
            StripeCustomersCount = 1,
            StripeSubscriptionsCount = 1,
            LoginEventsCount = 10,
        };

        var str = stats.ToString();

        Assert.Contains("Members: 5", str);
        Assert.Contains("free: 3", str);
        Assert.Contains("paid: 1", str);
        Assert.Contains("comped: 1", str);
        Assert.Contains("Labels: 2", str);
        Assert.Contains("Newsletters: 4", str);
        Assert.Contains("Events: 10 total", str);
    }

    #endregion
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
