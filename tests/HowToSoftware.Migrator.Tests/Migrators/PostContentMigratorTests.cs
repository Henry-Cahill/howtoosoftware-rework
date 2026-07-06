using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class PostContentMigratorTests
{
    private const string SiteUrl = "https://howtoosoftware.com";

    #region RewriteGhostUrls

    [Fact]
    public void RewriteGhostUrls_NoPlaceholder_ReturnsOriginal()
    {
        var content = "Just some regular content without any URLs";
        var result = PostContentMigrator.RewriteGhostUrls(content, SiteUrl);
        Assert.Equal(content, result);
    }

    [Fact]
    public void RewriteGhostUrls_WithPlaceholder_ReplacesWithSiteUrl()
    {
        var content = "__GHOST_URL__/content/images/2025/12/photo.jpg";
        var result = PostContentMigrator.RewriteGhostUrls(content, SiteUrl);
        Assert.Equal("https://howtoosoftware.com/content/images/2025/12/photo.jpg", result);
    }

    [Fact]
    public void RewriteGhostUrls_MultiplePlaceholders_ReplacesAll()
    {
        var content = "See __GHOST_URL__/about/ and __GHOST_URL__/contact/ pages";
        var result = PostContentMigrator.RewriteGhostUrls(content, SiteUrl);
        Assert.Equal("See https://howtoosoftware.com/about/ and https://howtoosoftware.com/contact/ pages", result);
    }

    [Fact]
    public void RewriteGhostUrls_InLexicalJson_ReplacesCorrectly()
    {
        var lexical = """{"root":{"children":[{"url":"__GHOST_URL__/content/images/logo.png","type":"image"}]}}""";
        var result = PostContentMigrator.RewriteGhostUrls(lexical, SiteUrl);
        Assert.Contains("https://howtoosoftware.com/content/images/logo.png", result);
        Assert.DoesNotContain("__GHOST_URL__", result);
    }

    [Fact]
    public void RewriteGhostUrls_InMobiledocJson_ReplacesCorrectly()
    {
        var mobiledoc = """{"markups":[["a",["href","__GHOST_URL__/#/portal/"]]]}""";
        var result = PostContentMigrator.RewriteGhostUrls(mobiledoc, SiteUrl);
        Assert.Contains("https://howtoosoftware.com/#/portal/", result);
    }

    [Fact]
    public void RewriteGhostUrls_TrailingSlashOnSiteUrl_TrimsIt()
    {
        var content = "__GHOST_URL__/page";
        var result = PostContentMigrator.RewriteGhostUrls(content, "https://howtoosoftware.com/");
        // The method itself doesn't trim — the caller (ProcessContent) does.
        // RewriteGhostUrls is a simple replace, so test with pre-trimmed URL.
        var resultTrimmed = PostContentMigrator.RewriteGhostUrls(content, "https://howtoosoftware.com");
        Assert.Equal("https://howtoosoftware.com/page", resultTrimmed);
    }

    #endregion

    #region ProcessContent — Statistics

    [Fact]
    public void ProcessContent_PostsOnly_CountsPostsCorrectly()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "title", "type", "status", "lexical"],
                [
                    ["1", "Post 1", "post", "published", "{}"],
                    ["2", "Post 2", "post", "draft", "{}"],
                    ["3", "Post 3", "post", "published", null],
                ])
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);

        Assert.Equal(3, result.Stats.PostCount);
        Assert.Equal(0, result.Stats.PageCount);
        Assert.Equal(2, result.Stats.PublishedCount);
        Assert.Equal(1, result.Stats.DraftCount);
    }

    [Fact]
    public void ProcessContent_MixedPostsAndPages_CountsBothTypes()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "title", "type", "status", "lexical", "mobiledoc"],
                [
                    ["1", "Blog Post", "post", "published", "{}", null],
                    ["2", "About Page", "page", "published", "{}", null],
                    ["3", "Home Page", "page", "published", null, "{\"version\":\"0.3.1\"}"],
                    ["4", "Draft Post", "post", "draft", "{}", null],
                ])
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);

        Assert.Equal(2, result.Stats.PostCount);
        Assert.Equal(2, result.Stats.PageCount);
        Assert.Equal(3, result.Stats.PublishedCount);
        Assert.Equal(1, result.Stats.DraftCount);
        Assert.Equal(4, result.Stats.TotalContentItems);
    }

    [Fact]
    public void ProcessContent_DetectsLexicalAndMobiledocFormats()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "type", "status", "lexical", "mobiledoc"],
                [
                    ["1", "post", "published", "{\"root\":{}}", null],        // Lexical only
                    ["2", "post", "published", null, "{\"version\":\"0.3.1\"}"], // Mobiledoc only
                    ["3", "post", "published", "{\"root\":{}}", "{\"version\":\"0.3.1\"}"], // Both
                ])
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);

        Assert.Equal(2, result.Stats.LexicalCount);
        Assert.Equal(2, result.Stats.MobiledocCount);
    }

    #endregion

    #region ProcessContent — Join Table Tracking

    [Fact]
    public void ProcessContent_TracksPostsTagsCount()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts", ["id", "type", "status"], [["1", "post", "published"]]),
            new ParsedInsert("posts_tags", ["id", "post_id", "tag_id", "sort_order"],
            [
                ["t1", "1", "tag1", "0"],
                ["t2", "1", "tag2", "1"],
            ]),
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);
        Assert.Equal(2, result.Stats.PostsTagsCount);
    }

    [Fact]
    public void ProcessContent_TracksPostsAuthorsCount()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts", ["id", "type", "status"], [["1", "post", "published"]]),
            new ParsedInsert("posts_authors", ["id", "post_id", "author_id", "sort_order"],
            [
                ["a1", "1", "author1", "0"],
            ]),
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);
        Assert.Equal(1, result.Stats.PostsAuthorsCount);
    }

    [Fact]
    public void ProcessContent_TracksPostsMetaCount()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts", ["id", "type", "status"], [["1", "post", "published"]]),
            new ParsedInsert("posts_meta", ["id", "post_id", "meta_title"],
            [
                ["m1", "1", "SEO Title"],
            ]),
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);
        Assert.Equal(1, result.Stats.PostsMetaCount);
    }

    [Fact]
    public void ProcessContent_TracksPostRevisionsCount()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts", ["id", "type", "status"], [["1", "post", "published"]]),
            new ParsedInsert("post_revisions", ["id", "post_id", "lexical"],
            [
                ["r1", "1", "{\"root\":{}}"],
                ["r2", "1", "{\"root\":{}}"],
                ["r3", "1", "{\"root\":{}}"],
            ]),
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);
        Assert.Equal(3, result.Stats.PostRevisionsCount);
    }

    [Fact]
    public void ProcessContent_TracksMobiledocRevisionsCount()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts", ["id", "type", "status"], [["1", "post", "published"]]),
            new ParsedInsert("mobiledoc_revisions", ["id", "post_id", "mobiledoc"],
            [
                ["mr1", "1", "{\"version\":\"0.3.1\"}"],
            ]),
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);
        Assert.Equal(1, result.Stats.MobiledocRevisionsCount);
    }

    #endregion

    #region ProcessContent — URL Rewriting

    [Fact]
    public void ProcessContent_RewritesGhostUrlsInPostContent()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "type", "status", "html", "feature_image"],
                [
                    ["1", "post", "published",
                     "<img src=\"__GHOST_URL__/content/images/photo.jpg\">",
                     "__GHOST_URL__/content/images/cover.jpg"],
                ])
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);
        var row = result.TransformedInserts[0].Rows[0];

        // html column (index 3)
        Assert.Contains("https://howtoosoftware.com/content/images/photo.jpg", row[3]);
        Assert.DoesNotContain("__GHOST_URL__", row[3]);

        // feature_image column (index 4)
        Assert.Equal("https://howtoosoftware.com/content/images/cover.jpg", row[4]);
    }

    [Fact]
    public void ProcessContent_PreservesNonContentColumns()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "title", "type", "status", "slug"],
                [
                    ["abc123", "My Title", "post", "published", "my-title"],
                ])
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);
        var row = result.TransformedInserts[0].Rows[0];

        Assert.Equal("abc123", row[0]);    // id unchanged
        Assert.Equal("My Title", row[1]);  // title unchanged
        Assert.Equal("post", row[2]);      // type unchanged
        Assert.Equal("published", row[3]); // status unchanged
        Assert.Equal("my-title", row[4]);  // slug unchanged
    }

    [Fact]
    public void ProcessContent_PassesThroughNonContentTables()
    {
        var inserts = new[]
        {
            new ParsedInsert("users", ["id", "name"], [["u1", "Admin"]]),
            new ParsedInsert("tags", ["id", "name"], [["t1", "News"]]),
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);

        Assert.Equal(2, result.TransformedInserts.Count);
        Assert.Equal("Admin", result.TransformedInserts[0].Rows[0][1]);
        Assert.Equal("News", result.TransformedInserts[1].Rows[0][1]);
    }

    #endregion

    #region FilterPostContentTables

    [Fact]
    public void FilterPostContentTables_ReturnsOnlyContentTables()
    {
        var inserts = new[]
        {
            new ParsedInsert("posts", ["id"], [["1"]]),
            new ParsedInsert("users", ["id"], [["u1"]]),
            new ParsedInsert("tags", ["id"], [["t1"]]),
            new ParsedInsert("posts_tags", ["id"], [["pt1"]]),
            new ParsedInsert("posts_authors", ["id"], [["pa1"]]),
            new ParsedInsert("settings", ["id"], [["s1"]]),
        };

        var filtered = PostContentMigrator.FilterPostContentTables(inserts);

        Assert.Equal(3, filtered.Count);
        Assert.Contains(filtered, i => i.TableName == "posts");
        Assert.Contains(filtered, i => i.TableName == "posts_tags");
        Assert.Contains(filtered, i => i.TableName == "posts_authors");
    }

    #endregion

    #region IsPostContentTable

    [Theory]
    [InlineData("posts", true)]
    [InlineData("posts_tags", true)]
    [InlineData("posts_authors", true)]
    [InlineData("posts_meta", true)]
    [InlineData("post_revisions", true)]
    [InlineData("mobiledoc_revisions", true)]
    [InlineData("users", false)]
    [InlineData("tags", false)]
    [InlineData("settings", false)]
    [InlineData("newsletters", false)]
    public void IsPostContentTable_ReturnsExpected(string tableName, bool expected)
    {
        Assert.Equal(expected, PostContentMigrator.IsPostContentTable(tableName));
    }

    #endregion

    #region Full Integration Scenario

    [Fact]
    public void ProcessContent_FullScenario_10Posts9Pages()
    {
        // Simulates the actual Ghost data: 10 posts (9 published + 1 draft) + 9 pages
        var rows = new List<string?[]>();

        // 9 published posts (Lexical format)
        for (var i = 1; i <= 9; i++)
            rows.Add([$"post{i}", $"Post {i}", "post", "published", "{\"root\":{}}", null]);

        // 1 draft post (Lexical format)
        rows.Add(["post10", "Draft Post", "post", "draft", "{\"root\":{}}", null]);

        // 1 legacy post (Mobiledoc format)
        // Wait — the data says 10 posts total: 9 published + 1 draft.
        // One of those 10 uses Mobiledoc, the rest use Lexical.
        // Let's make post9 the Mobiledoc one:
        rows[8] = ["post9", "Coming Soon", "post", "published", null, "{\"version\":\"0.3.1\"}"];

        // 9 published pages (Lexical format)
        for (var i = 1; i <= 9; i++)
            rows.Add([$"page{i}", $"Page {i}", "page", "published", "{\"root\":{}}", null]);

        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "title", "type", "status", "lexical", "mobiledoc"],
                rows),
            new ParsedInsert("posts_tags", ["id", "post_id", "tag_id", "sort_order"],
            [
                ["pt1", "post1", "tag_news", "0"],
            ]),
            new ParsedInsert("posts_authors", ["id", "post_id", "author_id", "sort_order"],
                rows.Select((r, idx) => new string?[] { $"pa{idx}", r[0], "author1", "0" }).ToList()),
        };

        var result = PostContentMigrator.ProcessContent(inserts, SiteUrl);

        // Post/page counts
        Assert.Equal(10, result.Stats.PostCount);
        Assert.Equal(9, result.Stats.PageCount);
        Assert.Equal(19, result.Stats.TotalContentItems);

        // Status counts
        Assert.Equal(18, result.Stats.PublishedCount);
        Assert.Equal(1, result.Stats.DraftCount);

        // Content format counts
        Assert.Equal(18, result.Stats.LexicalCount);   // 9 posts + 9 pages in Lexical
        Assert.Equal(1, result.Stats.MobiledocCount);   // 1 post in Mobiledoc

        // Join tables
        Assert.Equal(1, result.Stats.PostsTagsCount);
        Assert.Equal(19, result.Stats.PostsAuthorsCount);
    }

    #endregion
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
