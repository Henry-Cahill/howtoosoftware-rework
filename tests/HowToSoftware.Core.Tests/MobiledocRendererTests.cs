using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class MobiledocRendererTests
{
    private readonly MobiledocRenderer _renderer = new();

    // Helper: wrap sections, markups, atoms, cards into a full Mobiledoc document
    private static string Doc(
        string sections = "[]",
        string markups = "[]",
        string atoms = "[]",
        string cards = "[]") =>
        $$"""{"version":"0.3.1","atoms":{{atoms}},"cards":{{cards}},"markups":{{markups}},"sections":{{sections}},"ghostVersion":"4.0"}""";

    // --- Null / empty / invalid ---

    [Fact]
    public void Render_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal("", _renderer.Render(""));
        Assert.Equal("", _renderer.Render("   "));
        Assert.Equal("", _renderer.Render(null!));
    }

    [Fact]
    public void Render_NoSections_ReturnsEmpty()
    {
        Assert.Equal("", _renderer.Render(Doc()));
    }

    // --- Markup sections (type 1) ---

    [Fact]
    public void Render_PlainParagraph()
    {
        var json = Doc(sections: """[[1,"p",[[0,[],0,"Hello world"]]]]""");
        Assert.Equal("<p>Hello world</p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_HeadingH2()
    {
        var json = Doc(sections: """[[1,"h2",[[0,[],0,"My heading"]]]]""");
        Assert.Equal("<h2>My heading</h2>", _renderer.Render(json));
    }

    [Fact]
    public void Render_Blockquote()
    {
        var json = Doc(sections: """[[1,"blockquote",[[0,[],0,"A quote"]]]]""");
        Assert.Equal("<blockquote>A quote</blockquote>", _renderer.Render(json));
    }

    [Fact]
    public void Render_MultipleSections()
    {
        var json = Doc(sections: """[[1,"h1",[[0,[],0,"Title"]]],[1,"p",[[0,[],0,"Body text"]]]]""");
        Assert.Equal("<h1>Title</h1><p>Body text</p>", _renderer.Render(json));
    }

    // --- Markups (bold, italic, links) ---

    [Fact]
    public void Render_BoldText()
    {
        var json = Doc(
            markups: """[["strong"]]""",
            sections: """[[1,"p",[[0,[0],1,"bold"]]]]""");
        Assert.Equal("<p><strong>bold</strong></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_ItalicText()
    {
        var json = Doc(
            markups: """[["em"]]""",
            sections: """[[1,"p",[[0,[0],1,"italic"]]]]""");
        Assert.Equal("<p><em>italic</em></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_LinkMarkup()
    {
        var json = Doc(
            markups: """[["a",["href","https://example.com"]]]""",
            sections: """[[1,"p",[[0,[0],1,"click here"]]]]""");
        Assert.Equal(
            """<p><a href="https://example.com">click here</a></p>""",
            _renderer.Render(json));
    }

    [Fact]
    public void Render_NestedMarkups_BoldLink()
    {
        var json = Doc(
            markups: """[["a",["href","https://example.com"]],["strong"]]""",
            sections: """[[1,"p",[[0,[0,1],2,"bold link"]]]]""");
        Assert.Equal(
            """<p><a href="https://example.com"><strong>bold link</strong></a></p>""",
            _renderer.Render(json));
    }

    [Fact]
    public void Render_LinkWithMixedText()
    {
        // "before " + <a>link</a> + " after"
        var json = Doc(
            markups: """[["a",["href","#/portal/"]]]""",
            sections: """[[1,"p",[[0,[],0,"before "],[0,[0],1,"link"],[0,[],0," after"]]]]""");
        Assert.Equal(
            """<p>before <a href="#/portal/">link</a> after</p>""",
            _renderer.Render(json));
    }

    // --- HTML encoding ---

    [Fact]
    public void Render_HtmlEncodesText()
    {
        var json = Doc(sections: """[[1,"p",[[0,[],0,"<script>alert('xss')</script>"]]]]""");
        Assert.Contains("&lt;script&gt;", _renderer.Render(json));
        Assert.DoesNotContain("<script>", _renderer.Render(json));
    }

    // --- List sections (type 3) ---

    [Fact]
    public void Render_UnorderedList()
    {
        var json = Doc(sections: """[[3,"ul",[[[0,[],0,"Item 1"]],[[0,[],0,"Item 2"]]]]]""");
        Assert.Equal("<ul><li>Item 1</li><li>Item 2</li></ul>", _renderer.Render(json));
    }

    [Fact]
    public void Render_OrderedList()
    {
        var json = Doc(sections: """[[3,"ol",[[[0,[],0,"First"]],[[0,[],0,"Second"]]]]]""");
        Assert.Equal("<ol><li>First</li><li>Second</li></ol>", _renderer.Render(json));
    }

    [Fact]
    public void Render_ListWithMarkup()
    {
        var json = Doc(
            markups: """[["strong"]]""",
            sections: """[[3,"ul",[[[0,[0],1,"Bold item"]],[[0,[],0,"Plain item"]]]]]""");
        Assert.Equal("<ul><li><strong>Bold item</strong></li><li>Plain item</li></ul>", _renderer.Render(json));
    }

    // --- Cards (type 2) ---

    [Fact]
    public void Render_HtmlCard()
    {
        var json = Doc(
            cards: """[["html",{"html":"<div class=\"custom\">content</div>"}]]""",
            sections: """[[2,0]]""");
        Assert.Contains("<!--kg-card-begin: html-->", _renderer.Render(json));
        Assert.Contains("<div class=\"custom\">content</div>", _renderer.Render(json));
        Assert.Contains("<!--kg-card-end: html-->", _renderer.Render(json));
    }

    [Fact]
    public void Render_ImageCard()
    {
        var json = Doc(
            cards: """[["image",{"src":"https://example.com/img.jpg","alt":"A photo","caption":"","cardWidth":""}]]""",
            sections: """[[2,0]]""");
        var html = _renderer.Render(json);
        Assert.Contains("kg-image-card", html);
        Assert.Contains("src=\"https://example.com/img.jpg\"", html);
        Assert.Contains("alt=\"A photo\"", html);
    }

    [Fact]
    public void Render_ImageCard_WideWithCaption()
    {
        var json = Doc(
            cards: """[["image",{"src":"https://example.com/img.jpg","alt":"","caption":"My caption","cardWidth":"wide"}]]""",
            sections: """[[2,0]]""");
        var html = _renderer.Render(json);
        Assert.Contains("kg-width-wide", html);
        Assert.Contains("kg-card-hascaption", html);
        Assert.Contains("<figcaption>My caption</figcaption>", html);
    }

    [Fact]
    public void Render_CodeCard()
    {
        var json = Doc(
            cards: """[["code",{"code":"console.log('hi');","language":"javascript","caption":""}]]""",
            sections: """[[2,0]]""");
        var html = _renderer.Render(json);
        Assert.Contains("kg-code-card", html);
        Assert.Contains("language-javascript", html);
        Assert.Contains("console.log(&#39;hi&#39;);", html);
    }

    [Fact]
    public void Render_HrCard()
    {
        var json = Doc(
            cards: """[["hr",{}]]""",
            sections: """[[2,0]]""");
        Assert.Equal("<hr />", _renderer.Render(json));
    }

    [Fact]
    public void Render_EmbedCard()
    {
        var json = Doc(
            cards: """[["embed",{"html":"<iframe src=\"https://youtube.com/embed/x\"></iframe>"}]]""",
            sections: """[[2,0]]""");
        var html = _renderer.Render(json);
        Assert.Contains("kg-embed-card", html);
        Assert.Contains("<iframe", html);
    }

    [Fact]
    public void Render_BookmarkCard()
    {
        var json = Doc(
            cards: """[["bookmark",{"url":"https://example.com","title":"Example","description":"A site","icon":"","thumbnail":"","publisher":"Example Inc"}]]""",
            sections: """[[2,0]]""");
        var html = _renderer.Render(json);
        Assert.Contains("kg-bookmark-card", html);
        Assert.Contains("href=\"https://example.com\"", html);
        Assert.Contains("kg-bookmark-title", html);
        Assert.Contains("Example Inc", html);
    }

    // --- Atoms ---

    [Fact]
    public void Render_SoftReturnAtom()
    {
        var json = Doc(
            atoms: """[["soft-return","",{}]]""",
            sections: """[[1,"p",[[0,[],0,"Line 1"],[1,[],0,0],[0,[],0,"Line 2"]]]]""");
        Assert.Equal("<p>Line 1<br />Line 2</p>", _renderer.Render(json));
    }

    // --- Image section (legacy type 10) ---

    [Fact]
    public void Render_LegacyImageSection()
    {
        var json = Doc(sections: """[[10,"https://example.com/photo.jpg"]]""");
        Assert.Contains("src=\"https://example.com/photo.jpg\"", _renderer.Render(json));
    }

    // --- Real Ghost data: "Coming soon" post ---

    [Fact]
    public void Render_GhostComingSoonPost()
    {
        var json = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[["a",["href","#/portal/"]]],"sections":[[1,"p",[[0,[],0,"This is howtoosoftware, a brand new site by Henry Lawrence Cahill that's just getting started. Things will be up and running here shortly, but you can "],[0,[0],1,"subscribe"],[0,[],0," in the meantime if you'd like to stay up to date and receive emails when new content is published!"]]]],"ghostVersion":"4.0"}""";

        var expected = """<p>This is howtoosoftware, a brand new site by Henry Lawrence Cahill that&#39;s just getting started. Things will be up and running here shortly, but you can <a href="#/portal/">subscribe</a> in the meantime if you&#39;d like to stay up to date and receive emails when new content is published!</p>""";

        Assert.Equal(expected, _renderer.Render(json));
    }

    // --- Gallery card ---

    [Fact]
    public void Render_GalleryCard()
    {
        var json = Doc(
            cards: """[["gallery",{"images":[{"src":"https://example.com/1.jpg","alt":"First"},{"src":"https://example.com/2.jpg","alt":"Second"}],"caption":""}]]""",
            sections: """[[2,0]]""");
        var html = _renderer.Render(json);
        Assert.Contains("kg-gallery-card", html);
        Assert.Contains("src=\"https://example.com/1.jpg\"", html);
        Assert.Contains("src=\"https://example.com/2.jpg\"", html);
    }

    // --- Unknown card type is ignored ---

    [Fact]
    public void Render_UnknownCard_SkipsGracefully()
    {
        var json = Doc(
            cards: """[["unsupported-thing",{"foo":"bar"}]]""",
            sections: """[[2,0]]""");
        Assert.Equal("", _renderer.Render(json));
    }

    // --- Unknown section type is ignored ---

    [Fact]
    public void Render_UnknownSectionType_SkipsGracefully()
    {
        var json = Doc(sections: """[[99,"p",[[0,[],0,"ignored"]]]]""");
        Assert.Equal("", _renderer.Render(json));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
