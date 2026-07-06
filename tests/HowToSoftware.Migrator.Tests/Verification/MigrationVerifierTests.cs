using HowToSoftware.Migrator;

namespace HowToSoftware.Migrator.Tests;

public class MigrationVerifierTests
{
    private readonly MigrationVerifier _verifier = new();
    private const string SiteUrl = "https://howtoosoftware.com";

    #region Mobiledoc Verification

    [Fact]
    public void Verify_MobiledocPost_MatchesGhostHtml()
    {
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"Hello world"]]]],"ghostVersion":"4.0"}""";
        var ghostHtml = "<p>Hello world</p>";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: null,
            html: ghostHtml,
            type: "post",
            status: "published");

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.TotalCount);
        Assert.Equal(1, report.MatchCount);
        Assert.Equal(0, report.MismatchCount);
        Assert.Equal(VerificationStatus.Match, report.Results[0].VerificationStatus);
        Assert.Equal("mobiledoc", report.Results[0].Format);
    }

    [Fact]
    public void Verify_MobiledocWithMarkups_MatchesGhostHtml()
    {
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[["strong"],["em"]],"sections":[[1,"p",[[0,[0],1,"Bold"],[0,[],0," and "],[0,[1],1,"italic"]]]],"ghostVersion":"4.0"}""";
        var ghostHtml = "<p><strong>Bold</strong> and <em>italic</em></p>";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: null,
            html: ghostHtml);

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.MatchCount);
    }

    [Fact]
    public void Verify_MobiledocWithLink_MatchesGhostHtml()
    {
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[["a",["href","https://example.com"]]],"sections":[[1,"p",[[0,[0],1,"Click here"]]]],"ghostVersion":"4.0"}""";
        var ghostHtml = """<p><a href="https://example.com">Click here</a></p>""";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: null,
            html: ghostHtml);

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.MatchCount);
    }

    #endregion

    #region Lexical Verification

    [Fact]
    public void Verify_LexicalPost_MatchesGhostHtml()
    {
        var lexical = """{"root":{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Hello world","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}],"direction":"ltr","format":"","indent":0,"type":"root","version":1}}""";
        var ghostHtml = "<p>Hello world</p>";

        var inserts = CreatePostInserts(
            mobiledoc: null,
            lexical: lexical,
            html: ghostHtml,
            type: "post",
            status: "published");

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.TotalCount);
        Assert.Equal(1, report.MatchCount);
        Assert.Equal("lexical", report.Results[0].Format);
    }

    [Fact]
    public void Verify_LexicalWithBoldText_MatchesGhostHtml()
    {
        var lexical = """{"root":{"children":[{"children":[{"detail":0,"format":1,"mode":"normal","style":"","text":"Bold text","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}],"direction":"ltr","format":"","indent":0,"type":"root","version":1}}""";
        var ghostHtml = "<p><strong>Bold text</strong></p>";

        var inserts = CreatePostInserts(
            mobiledoc: null,
            lexical: lexical,
            html: ghostHtml);

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.MatchCount);
    }

    [Fact]
    public void Verify_LexicalWithHeading_MatchesGhostHtml()
    {
        var lexical = """{"root":{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"My Heading","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"heading","version":1,"tag":"h2"}],"direction":"ltr","format":"","indent":0,"type":"root","version":1}}""";
        var ghostHtml = """<h2 id="my-heading">My Heading</h2>""";

        var inserts = CreatePostInserts(
            mobiledoc: null,
            lexical: lexical,
            html: ghostHtml);

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.MatchCount);
    }

    [Fact]
    public void Verify_LexicalPreferredOverMobiledoc()
    {
        // When both lexical and mobiledoc exist, lexical takes precedence
        var lexical = """{"root":{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"From lexical","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}],"direction":"ltr","format":"","indent":0,"type":"root","version":1}}""";
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"From mobiledoc"]]]],"ghostVersion":"4.0"}""";
        var ghostHtml = "<p>From lexical</p>";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: lexical,
            html: ghostHtml);

        var report = _verifier.Verify(inserts);

        Assert.Equal("lexical", report.Results[0].Format);
        Assert.Equal(1, report.MatchCount);
    }

    #endregion

    #region Mismatch Detection

    [Fact]
    public void Verify_DifferentContent_ReportsMismatch()
    {
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"Clone content"]]]],"ghostVersion":"4.0"}""";
        var ghostHtml = "<p>Ghost content is different</p>";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: null,
            html: ghostHtml);

        var report = _verifier.Verify(inserts);

        Assert.Equal(0, report.MatchCount);
        Assert.Equal(1, report.MismatchCount);
        Assert.Equal(VerificationStatus.Mismatch, report.Results[0].VerificationStatus);
        Assert.NotNull(report.Results[0].Detail);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Verify_NoContent_ReportsEmpty()
    {
        var inserts = CreatePostInserts(
            mobiledoc: null,
            lexical: null,
            html: null);

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.TotalCount);
        Assert.Equal(1, report.EmptyCount);
        Assert.Equal(VerificationStatus.Empty, report.Results[0].VerificationStatus);
    }

    [Fact]
    public void Verify_HtmlOnly_ReportsHtmlOnly()
    {
        var inserts = CreatePostInserts(
            mobiledoc: null,
            lexical: null,
            html: "<p>Pre-rendered only</p>");

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.TotalCount);
        Assert.Equal(1, report.HtmlOnlyCount);
        Assert.Equal(VerificationStatus.HtmlOnly, report.Results[0].VerificationStatus);
    }

    [Fact]
    public void Verify_InvalidJson_ReportsRenderError()
    {
        var inserts = CreatePostInserts(
            mobiledoc: "not valid json {{{",
            lexical: null,
            html: "<p>Something</p>");

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.RenderErrorCount);
        Assert.Equal(VerificationStatus.RenderError, report.Results[0].VerificationStatus);
        Assert.Contains("render failed", report.Results[0].Detail);
    }

    [Fact]
    public void Verify_InvalidLexicalJson_ReportsRenderError()
    {
        var inserts = CreatePostInserts(
            mobiledoc: null,
            lexical: "{bad json!!!",
            html: "<p>Something</p>");

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.RenderErrorCount);
        Assert.Equal(VerificationStatus.RenderError, report.Results[0].VerificationStatus);
    }

    [Fact]
    public void Verify_SkipsNonPostTables()
    {
        var inserts = new[]
        {
            new ParsedInsert("tags", ["id", "name"], [["1", "Test Tag"]]),
            new ParsedInsert("users", ["id", "name"], [["2", "Admin"]]),
        };

        var report = _verifier.Verify(inserts);

        Assert.Equal(0, report.TotalCount);
    }

    [Fact]
    public void Verify_MultipleRows_TracksEachPost()
    {
        var mobiledoc1 = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"Post one"]]]],"ghostVersion":"4.0"}""";
        var mobiledoc2 = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"Post two"]]]],"ghostVersion":"4.0"}""";

        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "title", "slug", "type", "status", "mobiledoc", "lexical", "html"],
                [
                    ["1", "First Post", "first-post", "post", "published", mobiledoc1, null, "<p>Post one</p>"],
                    ["2", "Second Post", "second-post", "post", "published", mobiledoc2, null, "<p>Post two</p>"],
                ])
        };

        var report = _verifier.Verify(inserts);

        Assert.Equal(2, report.TotalCount);
        Assert.Equal(2, report.MatchCount);
        Assert.Equal(2, report.PostCount);
    }

    [Fact]
    public void Verify_PagesCountedSeparately()
    {
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"About page"]]]],"ghostVersion":"4.0"}""";

        var inserts = new[]
        {
            new ParsedInsert("posts",
                ["id", "title", "slug", "type", "status", "mobiledoc", "lexical", "html"],
                [
                    ["1", "About", "about", "page", "published", mobiledoc, null, "<p>About page</p>"],
                ])
        };

        var report = _verifier.Verify(inserts);

        Assert.Equal(1, report.TotalCount);
        Assert.Equal(0, report.PostCount);
        Assert.Equal(1, report.PageCount);
        Assert.Equal("page", report.Results[0].Type);
    }

    #endregion

    #region URL Rewriting Verification

    [Fact]
    public void Verify_WithSiteUrl_RewritesBeforeCompare()
    {
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[["a",["href","__GHOST_URL__/#/portal/"]]],"sections":[[1,"p",[[0,[0],1,"Subscribe"]]]],"ghostVersion":"4.0"}""";
        var ghostHtml = """<p><a href="__GHOST_URL__/#/portal/">Subscribe</a></p>""";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: null,
            html: ghostHtml);

        var report = _verifier.Verify(inserts, SiteUrl);

        Assert.Equal(1, report.MatchCount);
    }

    #endregion

    #region HTML Normalization

    [Fact]
    public void NormalizeHtml_SelfClosingBr_Normalized()
    {
        Assert.Equal("<p>line<br>two</p>", MigrationVerifier.NormalizeHtml("<p>line<br />two</p>"));
        Assert.Equal("<p>line<br>two</p>", MigrationVerifier.NormalizeHtml("<p>line<br/>two</p>"));
        Assert.Equal("<p>line<br>two</p>", MigrationVerifier.NormalizeHtml("<p>line<br>two</p>"));
    }

    [Fact]
    public void NormalizeHtml_SelfClosingHr_Normalized()
    {
        Assert.Equal("<hr>", MigrationVerifier.NormalizeHtml("<hr />"));
        Assert.Equal("<hr>", MigrationVerifier.NormalizeHtml("<hr/>"));
        Assert.Equal("<hr>", MigrationVerifier.NormalizeHtml("<hr>"));
    }

    [Fact]
    public void NormalizeHtml_SelfClosingImg_Normalized()
    {
        Assert.Equal("<img src=\"x.png\">", MigrationVerifier.NormalizeHtml("<img src=\"x.png\" />"));
        Assert.Equal("<img src=\"x.png\">", MigrationVerifier.NormalizeHtml("<img src=\"x.png\"/>"));
    }

    [Fact]
    public void NormalizeHtml_WhitespaceBetweenTags_Collapsed()
    {
        Assert.Equal("<p>text</p><p>more</p>", MigrationVerifier.NormalizeHtml("<p>text</p>  \n  <p>more</p>"));
    }

    [Fact]
    public void NormalizeHtml_MultipleWhitespace_Collapsed()
    {
        Assert.Equal("<p>hello world</p>", MigrationVerifier.NormalizeHtml("<p>hello   world</p>"));
    }

    [Fact]
    public void NormalizeHtml_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MigrationVerifier.NormalizeHtml(""));
        Assert.Equal(string.Empty, MigrationVerifier.NormalizeHtml(null!));
    }

    [Fact]
    public void NormalizeHtml_HtmlEntities_Decoded()
    {
        // &#39; → ', &amp; → &, &#160; → space (NBSP normalized)
        Assert.Equal("<p>it's here</p>", MigrationVerifier.NormalizeHtml("<p>it&#39;s here</p>"));
        Assert.Equal("<p>it's here</p>", MigrationVerifier.NormalizeHtml("<p>it's here</p>"));
        Assert.Equal("<p>A & B</p>", MigrationVerifier.NormalizeHtml("<p>A &amp; B</p>"));
    }

    [Fact]
    public void NormalizeHtml_Nbsp_NormalizedToSpace()
    {
        Assert.Equal("<p>hello world</p>", MigrationVerifier.NormalizeHtml("<p>hello&nbsp;world</p>"));
        Assert.Equal("<p>hello world</p>", MigrationVerifier.NormalizeHtml("<p>hello&#160;world</p>"));
    }

    [Fact]
    public void NormalizeHtml_PercentEncoded_Decoded()
    {
        // Cyrillic percent-encoded in heading IDs
        var ghost = "<h1 id=\"articles-%D1%81%D1%82%D0%B0%D1%82%D1%8C%D0%B8\">Articles</h1>";
        var clone = "<h1 id=\"articles-\u0441\u0442\u0430\u0442\u044C\u0438\">Articles</h1>";
        Assert.Equal(
            MigrationVerifier.NormalizeHtml(clone),
            MigrationVerifier.NormalizeHtml(ghost));
    }

    [Fact]
    public void Verify_WhitespaceDifferences_StillMatch()
    {
        // Ghost might output <br /> while our renderer outputs <br>
        var mobiledoc = """{"version":"0.3.1","atoms":[["soft-return","",{}]],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"Line one"],[1,[],0,0],[0,[],0,"Line two"]]]],"ghostVersion":"4.0"}""";
        var ghostHtml = "<p>Line one<br />Line two</p>";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: null,
            html: ghostHtml);

        var report = _verifier.Verify(inserts);

        // After normalization, <br /> and <br /> should both become <br>
        Assert.Equal(1, report.MatchCount);
    }

    #endregion

    #region Report Output

    [Fact]
    public void Report_ToString_IncludesSummary()
    {
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"Hello"]]]],"ghostVersion":"4.0"}""";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: null,
            html: "<p>Hello</p>");

        var report = _verifier.Verify(inserts);
        var output = report.ToString();

        Assert.Contains("Verification Report:", output);
        Assert.Contains("Match: 1", output);
        Assert.Contains("[OK]", output);
    }

    [Fact]
    public void Report_ToString_ShowsMismatchDetails()
    {
        var mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[[1,"p",[[0,[],0,"Different"]]]],"ghostVersion":"4.0"}""";

        var inserts = CreatePostInserts(
            mobiledoc: mobiledoc,
            lexical: null,
            html: "<p>Original Ghost content</p>");

        var report = _verifier.Verify(inserts);
        var output = report.ToString();

        Assert.Contains("[DIFF]", output);
        Assert.Contains("Mismatch: 1", output);
    }

    #endregion

    #region MySQL Unescape

    [Fact]
    public void UnescapeMySql_Null_ReturnsNull()
    {
        Assert.Null(MigrationVerifier.UnescapeMySql(null));
    }

    [Fact]
    public void UnescapeMySql_NoEscapes_ReturnsOriginal()
    {
        Assert.Equal("hello world", MigrationVerifier.UnescapeMySql("hello world"));
    }

    [Fact]
    public void UnescapeMySql_EscapedSingleQuote_Unescaped()
    {
        Assert.Equal("it's here", MigrationVerifier.UnescapeMySql(@"it\'s here"));
    }

    [Fact]
    public void UnescapeMySql_EscapedDoubleQuote_Unescaped()
    {
        Assert.Equal("he said \"hi\"", MigrationVerifier.UnescapeMySql("he said \\\"hi\\\""));
    }

    [Fact]
    public void UnescapeMySql_EscapedBackslash_Unescaped()
    {
        Assert.Equal(@"path\to\file", MigrationVerifier.UnescapeMySql(@"path\\to\\file"));
    }

    [Fact]
    public void UnescapeMySql_EscapedNewline_Unescaped()
    {
        Assert.Equal("line1\nline2", MigrationVerifier.UnescapeMySql(@"line1\nline2"));
    }

    [Fact]
    public void UnescapeMySql_JsonContent_ValidAfterUnescape()
    {
        // Simulate what MySqlDumpParser produces for JSON with escaped quotes
        var mysqlEscaped = "{\\\"root\\\":{\\\"children\\\":[]}}";
        var unescaped = MigrationVerifier.UnescapeMySql(mysqlEscaped);
        Assert.Equal("{\"root\":{\"children\":[]}}", unescaped);
    }

    #endregion

    #region Helpers

    private static ParsedInsert[] CreatePostInserts(
        string? mobiledoc, string? lexical, string? html,
        string type = "post", string status = "published",
        string id = "abc123", string title = "Test Post", string slug = "test-post")
    {
        return
        [
            new ParsedInsert("posts",
                ["id", "title", "slug", "type", "status", "mobiledoc", "lexical", "html"],
                [[id, title, slug, type, status, mobiledoc, lexical, html]])
        ];
    }

    #endregion
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
