using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class ImageMigratorTests
{
    private const string SiteUrl = "https://howtoosoftware.com";

    #region RewriteAbsoluteImageUrls

    [Fact]
    public void RewriteAbsoluteImageUrls_NoImageUrls_ReturnsOriginal()
    {
        var content = "Just some regular text without any image references.";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(content, SiteUrl);
        Assert.Equal(content, result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_AbsoluteUrl_ConvertedToRelative()
    {
        var content = "https://howtoosoftware.com/content/images/2026/03/hts-logo-white.png";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(content, SiteUrl);
        Assert.Equal("/content/images/2026/03/hts-logo-white.png", result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_MultipleAbsoluteUrls_AllConverted()
    {
        var content = "Logo: https://howtoosoftware.com/content/images/2026/03/hts-logo-white.png " +
                      "Icon: https://howtoosoftware.com/content/images/2025/12/H2S_Thumbnail_White.png";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(content, SiteUrl);
        Assert.Contains("/content/images/2026/03/hts-logo-white.png", result);
        Assert.Contains("/content/images/2025/12/H2S_Thumbnail_White.png", result);
        Assert.DoesNotContain("https://howtoosoftware.com/content/images/", result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_AlreadyRelative_Unchanged()
    {
        var content = "/content/images/2026/03/hts-logo-white.png";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(content, SiteUrl);
        Assert.Equal("/content/images/2026/03/hts-logo-white.png", result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_InHtml_ConvertsCorrectly()
    {
        var html = """<img src="https://howtoosoftware.com/content/images/2026/03/photo.webp" alt="test" />""";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(html, SiteUrl);
        Assert.Equal("""<img src="/content/images/2026/03/photo.webp" alt="test" />""", result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_InLexicalJson_ConvertsCorrectly()
    {
        var lexical = """{"root":{"children":[{"src":"https://howtoosoftware.com/content/images/2026/03/photo.png","type":"image"}]}}""";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(lexical, SiteUrl);
        Assert.Contains("/content/images/2026/03/photo.png", result);
        Assert.DoesNotContain("https://howtoosoftware.com/content/images/", result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_EscapedJsonUrls_ConvertsCorrectly()
    {
        var json = """{"url":"https:\/\/howtoosoftware.com\/content\/images\/2026\/03\/photo.png"}""";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(json, SiteUrl);
        Assert.Contains("\\/content\\/images\\/", result);
        Assert.DoesNotContain("https:\\/\\/howtoosoftware.com\\/content\\/images\\/", result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_OtherDomainImages_NotTouched()
    {
        var content = "https://static.ghost.org/v5.0.0/images/publication-cover.jpg";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(content, SiteUrl);
        Assert.Equal(content, result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_MixedUrls_OnlySiteUrlConverted()
    {
        var content = "Local: https://howtoosoftware.com/content/images/logo.png " +
                      "External: https://other.com/content/images/external.png";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(content, SiteUrl);
        Assert.Contains("/content/images/logo.png", result);
        Assert.Contains("https://other.com/content/images/external.png", result);
    }

    [Fact]
    public void RewriteAbsoluteImageUrls_CaseInsensitive_Converts()
    {
        var content = "HTTPS://HOWTOOSOFTWARE.COM/content/images/2026/03/photo.png";
        var result = ImageMigrator.RewriteAbsoluteImageUrls(content, SiteUrl);
        Assert.StartsWith("/content/images/", result);
    }

    #endregion

    #region RewriteImageUrls (full pipeline)

    [Fact]
    public void RewriteImageUrls_PostsTable_RewritesFeatureImage()
    {
        var inserts = new List<ParsedInsert>
        {
            new("posts",
                ["id", "title", "feature_image"],
                [["1", "Test", "https://howtoosoftware.com/content/images/2026/03/hero.png"]])
        };

        var result = ImageMigrator.RewriteImageUrls(inserts, SiteUrl);

        var post = result.TransformedInserts[0];
        Assert.Equal("/content/images/2026/03/hero.png", post.Rows[0][2]);
        Assert.Equal(1, result.Stats.UrlsRewritten);
    }

    [Fact]
    public void RewriteImageUrls_PostsTable_RewritesHtmlContent()
    {
        var html = """<p><img src="https://howtoosoftware.com/content/images/2026/03/screenshot.webp"></p>""";
        var inserts = new List<ParsedInsert>
        {
            new("posts",
                ["id", "html"],
                [["1", html]])
        };

        var result = ImageMigrator.RewriteImageUrls(inserts, SiteUrl);

        var post = result.TransformedInserts[0];
        Assert.Contains("/content/images/2026/03/screenshot.webp", post.Rows[0][1]);
        Assert.DoesNotContain("https://howtoosoftware.com/content/images/", post.Rows[0][1]);
    }

    [Fact]
    public void RewriteImageUrls_SettingsTable_RewritesValueColumn()
    {
        var inserts = new List<ParsedInsert>
        {
            new("settings",
                ["id", "key", "value"],
                [["1", "logo", "https://howtoosoftware.com/content/images/2026/03/hts-logo-white.png"]])
        };

        var result = ImageMigrator.RewriteImageUrls(inserts, SiteUrl);

        var settings = result.TransformedInserts[0];
        Assert.Equal("/content/images/2026/03/hts-logo-white.png", settings.Rows[0][2]);
    }

    [Fact]
    public void RewriteImageUrls_NonImageTable_SkipsProcessing()
    {
        var inserts = new List<ParsedInsert>
        {
            new("members",
                ["id", "email"],
                [["1", "user@example.com"]])
        };

        var result = ImageMigrator.RewriteImageUrls(inserts, SiteUrl);

        Assert.Equal(0, result.Stats.RowsScanned);
        Assert.Equal(0, result.Stats.UrlsRewritten);
    }

    [Fact]
    public void RewriteImageUrls_NullValues_NotCrashed()
    {
        var inserts = new List<ParsedInsert>
        {
            new("posts",
                ["id", "feature_image", "html"],
                [["1", null, null]])
        };

        var result = ImageMigrator.RewriteImageUrls(inserts, SiteUrl);

        var post = result.TransformedInserts[0];
        Assert.Null(post.Rows[0][1]);
        Assert.Null(post.Rows[0][2]);
    }

    [Fact]
    public void RewriteImageUrls_AlreadyRelativeUrls_NoRewritesCounted()
    {
        var inserts = new List<ParsedInsert>
        {
            new("posts",
                ["id", "feature_image"],
                [["1", "/content/images/2026/03/already-relative.png"]])
        };

        var result = ImageMigrator.RewriteImageUrls(inserts, SiteUrl);

        Assert.Equal(0, result.Stats.UrlsRewritten);
    }

    [Fact]
    public void RewriteImageUrls_PostsMeta_RewritesOgAndTwitterImages()
    {
        var inserts = new List<ParsedInsert>
        {
            new("posts_meta",
                ["id", "post_id", "og_image", "twitter_image"],
                [["1", "p1",
                    "https://howtoosoftware.com/content/images/2026/03/og.png",
                    "https://howtoosoftware.com/content/images/2026/03/twitter.png"]])
        };

        var result = ImageMigrator.RewriteImageUrls(inserts, SiteUrl);

        var meta = result.TransformedInserts[0];
        Assert.Equal("/content/images/2026/03/og.png", meta.Rows[0][2]);
        Assert.Equal("/content/images/2026/03/twitter.png", meta.Rows[0][3]);
    }

    #endregion

    #region FindImageReferences

    [Fact]
    public void FindImageReferences_FindsAllUniquePaths()
    {
        var inserts = new List<ParsedInsert>
        {
            new("posts",
                ["id", "feature_image", "html"],
                [
                    ["1", "/content/images/2026/03/hero.png",
                        """<img src="/content/images/2026/03/inline.webp">"""],
                    ["2", "/content/images/2026/03/hero.png", null],
                ])
        };

        var refs = ImageMigrator.FindImageReferences(inserts);

        Assert.Contains("/content/images/2026/03/hero.png", refs);
        Assert.Contains("/content/images/2026/03/inline.webp", refs);
        Assert.Equal(2, refs.Count); // hero.png deduplicated
    }

    [Fact]
    public void FindImageReferences_IgnoresNonImageTables()
    {
        var inserts = new List<ParsedInsert>
        {
            new("members",
                ["id", "note"],
                [["1", "/content/images/should-be-ignored.png"]])
        };

        var refs = ImageMigrator.FindImageReferences(inserts);
        Assert.Empty(refs);
    }

    #endregion

    #region CopyImages

    [Fact]
    public void CopyImages_CopiesFilesPreservingStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ImageMigratorTest_{Guid.NewGuid():N}");
        try
        {
            // Arrange: create source structure
            var sourceDir = Path.Combine(tempDir, "source");
            var yearMonth = Path.Combine(sourceDir, "2026", "03");
            Directory.CreateDirectory(yearMonth);
            File.WriteAllText(Path.Combine(yearMonth, "photo.png"), "fake-png-data");
            File.WriteAllText(Path.Combine(sourceDir, "logo.png"), "fake-logo-data");

            var targetDir = Path.Combine(tempDir, "target");

            // Act
            var stats = ImageMigrator.CopyImages(sourceDir, targetDir);

            // Assert
            Assert.Equal(2, stats.FilesCopied);
            Assert.True(File.Exists(Path.Combine(targetDir, "2026", "03", "photo.png")));
            Assert.True(File.Exists(Path.Combine(targetDir, "logo.png")));
            Assert.Equal("fake-png-data", File.ReadAllText(Path.Combine(targetDir, "2026", "03", "photo.png")));
            Assert.True(stats.DirectoriesCreated >= 1); // 2026 + 2026/03 counted separately
            Assert.True(stats.TotalBytes > 0);
            Assert.True(stats.CountByExtension.ContainsKey(".png"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CopyImages_EmptySourceDir_ReturnsZeroStats()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ImageMigratorTest_{Guid.NewGuid():N}");
        try
        {
            var sourceDir = Path.Combine(tempDir, "source");
            Directory.CreateDirectory(sourceDir);
            var targetDir = Path.Combine(tempDir, "target");

            var stats = ImageMigrator.CopyImages(sourceDir, targetDir);

            Assert.Equal(0, stats.FilesCopied);
            Assert.Equal(0, stats.TotalBytes);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CopyImages_SourceDoesNotExist_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            ImageMigrator.CopyImages("/nonexistent/path", "/some/target"));
    }

    [Fact]
    public void CopyImages_MultipleExtensions_TracksStats()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ImageMigratorTest_{Guid.NewGuid():N}");
        try
        {
            var sourceDir = Path.Combine(tempDir, "source");
            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, "photo.jpg"), "jpg-data");
            File.WriteAllText(Path.Combine(sourceDir, "icon.webp"), "webp-data");
            File.WriteAllText(Path.Combine(sourceDir, "logo.png"), "png-data");

            var targetDir = Path.Combine(tempDir, "target");

            var stats = ImageMigrator.CopyImages(sourceDir, targetDir);

            Assert.Equal(3, stats.FilesCopied);
            Assert.Equal(1, stats.CountByExtension[".jpg"]);
            Assert.Equal(1, stats.CountByExtension[".webp"]);
            Assert.Equal(1, stats.CountByExtension[".png"]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region Stats.ToString

    [Fact]
    public void ImageCopyStats_ToString_FormatsCorrectly()
    {
        var stats = new ImageCopyStats
        {
            FilesCopied = 3,
            TotalBytes = 1048576, // 1 MB
            DirectoriesCreated = 2,
        };
        stats.CountByExtension[".png"] = 2;
        stats.CountByExtension[".webp"] = 1;

        var str = stats.ToString();

        Assert.Contains("Files: 3", str);
        Assert.Contains("1.00 MB", str);
        Assert.Contains(".png: 2", str);
    }

    [Fact]
    public void ImageUrlRewriteStats_ToString_FormatsCorrectly()
    {
        var stats = new ImageUrlRewriteStats { RowsScanned = 10, UrlsRewritten = 5 };
        var str = stats.ToString();
        Assert.Contains("Rows scanned: 10", str);
        Assert.Contains("URLs rewritten: 5", str);
    }

    #endregion
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
