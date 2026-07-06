using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class LexicalRendererTests
{
    private readonly LexicalRenderer _renderer = new();

    private static string Wrap(string childrenJson) =>
        $$$"""{"root":{"children":[{{{childrenJson}}}],"direction":"ltr","format":"","indent":0,"type":"root","version":1}}""";

    [Fact]
    public void Render_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal("", _renderer.Render(""));
        Assert.Equal("", _renderer.Render("   "));
        Assert.Equal("", _renderer.Render(null!));
    }

    [Fact]
    public void Render_NoRootProperty_ReturnsEmpty()
    {
        Assert.Equal("", _renderer.Render("{}"));
    }

    [Fact]
    public void Render_EmptyRoot_ReturnsEmpty()
    {
        var json = """{"root":{"children":[],"type":"root","version":1}}""";
        Assert.Equal("", _renderer.Render(json));
    }

    [Fact]
    public void Render_Paragraph_PlainText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Hello world","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p>Hello world</p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_Paragraph_HtmlEncoded()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"<script>alert('xss')</script>","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        var result = _renderer.Render(json);
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void Render_ExtendedText_SameAsText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Extended text node","type":"extended-text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p>Extended text node</p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_BoldText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":1,"mode":"normal","style":"","text":"bold","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><strong>bold</strong></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_ItalicText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":2,"mode":"normal","style":"","text":"italic","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><em>italic</em></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_BoldItalicText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":3,"mode":"normal","style":"","text":"bold italic","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><strong><em>bold italic</em></strong></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_StrikethroughText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":4,"mode":"normal","style":"","text":"deleted","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><s>deleted</s></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_UnderlineText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":8,"mode":"normal","style":"","text":"underlined","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><u>underlined</u></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_CodeText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":16,"mode":"normal","style":"","text":"var x = 1;","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><code>var x = 1;</code></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_SubscriptText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":32,"mode":"normal","style":"","text":"2","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><sub>2</sub></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_SuperscriptText()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":64,"mode":"normal","style":"","text":"n","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><sup>n</sup></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_Heading_H3_WithId()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Access all areas","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"heading","tag":"h3","version":1}""");
        Assert.Equal("<h3 id=\"access-all-areas\">Access all areas</h3>", _renderer.Render(json));
    }

    [Fact]
    public void Render_ExtendedHeading_Works()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Hello","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"extended-heading","tag":"h2","version":1}""");
        Assert.Equal("<h2 id=\"hello\">Hello</h2>", _renderer.Render(json));
    }

    [Fact]
    public void Render_Heading_DefaultsToH2()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Test","type":"heading","version":1}],"direction":"ltr","format":"","indent":0,"type":"heading","version":1}""");
        var result = _renderer.Render(json);
        Assert.StartsWith("<h2", result);
    }

    [Fact]
    public void Render_Link()
    {
        var json = Wrap("""{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Ghost","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"link","rel":null,"target":null,"title":null,"url":"https://ghost.org","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p><a href=\"https://ghost.org\">Ghost</a></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_Link_WithRelAndTarget()
    {
        var json = Wrap("""{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Link","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"link","rel":"noopener","target":"_blank","title":"My title","url":"https://example.com","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        var result = _renderer.Render(json);
        Assert.Contains("rel=\"noopener\"", result);
        Assert.Contains("target=\"_blank\"", result);
        Assert.Contains("title=\"My title\"", result);
    }

    [Fact]
    public void Render_OrderedList()
    {
        var json = Wrap("""{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Item one","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"listitem","version":1,"value":1}],"direction":"ltr","format":"","indent":0,"type":"list","version":1,"listType":"number","start":1,"tag":"ol"}""");
        Assert.Equal("<ol><li>Item one</li></ol>", _renderer.Render(json));
    }

    [Fact]
    public void Render_UnorderedList()
    {
        var json = Wrap("""{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Bullet","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"listitem","version":1,"value":1}],"direction":"ltr","format":"","indent":0,"type":"list","version":1,"listType":"bullet","start":1,"tag":"ul"}""");
        Assert.Equal("<ul><li>Bullet</li></ul>", _renderer.Render(json));
    }

    [Fact]
    public void Render_OrderedList_CustomStart()
    {
        var json = Wrap("""{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Item","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"listitem","version":1,"value":5}],"direction":"ltr","format":"","indent":0,"type":"list","version":1,"listType":"number","start":5,"tag":"ol"}""");
        Assert.Equal("<ol start=\"5\"><li>Item</li></ol>", _renderer.Render(json));
    }

    [Fact]
    public void Render_Linebreak()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Before","type":"text","version":1},{"type":"linebreak","version":1},{"detail":0,"format":0,"mode":"normal","style":"","text":"After","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p>Before<br>After</p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_HorizontalRule()
    {
        var json = Wrap("""{"type":"horizontalrule"}""");
        Assert.Equal("<hr>", _renderer.Render(json));
    }

    [Fact]
    public void Render_Image_Regular()
    {
        var json = Wrap("""{"type":"image","version":1,"src":"https://example.com/photo.jpg","width":800,"height":600,"title":"","alt":"Photo","caption":"","cardWidth":"regular","href":""}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-image-card\"", result);
        Assert.Contains("src=\"https://example.com/photo.jpg\"", result);
        Assert.Contains("class=\"kg-image\"", result);
        Assert.Contains("alt=\"Photo\"", result);
        Assert.Contains("loading=\"lazy\"", result);
        Assert.Contains("width=\"800\"", result);
        Assert.Contains("height=\"600\"", result);
        Assert.DoesNotContain("kg-width-wide", result);
    }

    [Fact]
    public void Render_Image_Wide()
    {
        var json = Wrap("""{"type":"image","version":1,"src":"https://example.com/wide.jpg","width":1200,"height":400,"alt":"Wide","caption":"","cardWidth":"wide","href":""}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-width-wide", result);
    }

    [Fact]
    public void Render_Image_Full()
    {
        var json = Wrap("""{"type":"image","version":1,"src":"https://example.com/full.jpg","width":1600,"height":900,"alt":"Full","caption":"","cardWidth":"full","href":""}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-width-full", result);
    }

    [Fact]
    public void Render_Image_WithCaption()
    {
        var json = Wrap("""{"type":"image","version":1,"src":"https://example.com/img.jpg","width":800,"height":600,"alt":"Test","caption":"A caption","cardWidth":"regular","href":""}""");
        var result = _renderer.Render(json);
        Assert.Contains("<figcaption>A caption</figcaption>", result);
    }

    [Fact]
    public void Render_Image_WithLink()
    {
        var json = Wrap("""{"type":"image","version":1,"src":"https://example.com/img.jpg","width":800,"height":600,"alt":"Test","caption":"","cardWidth":"regular","href":"https://example.com"}""");
        var result = _renderer.Render(json);
        Assert.Contains("<a href=\"https://example.com\">", result);
        Assert.Contains("</a>", result);
    }

    [Fact]
    public void Render_Blockquote()
    {
        var json = Wrap("""{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Quote text","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}],"direction":"ltr","format":"","indent":0,"type":"quote","version":1}""");
        var result = _renderer.Render(json);
        Assert.Equal("<blockquote><p>Quote text</p></blockquote>", result);
    }

    [Fact]
    public void Render_Aside()
    {
        var json = Wrap("""{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Aside text","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}],"direction":"ltr","format":"","indent":0,"type":"aside","version":1}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-blockquote-alt", result);
    }

    [Fact]
    public void Render_CodeBlock_WithLanguage()
    {
        var json = Wrap("""{"type":"codeblock","version":1,"code":"console.log('hello');","language":"javascript","caption":""}""");
        var result = _renderer.Render(json);
        Assert.Contains("<pre><code class=\"language-javascript\">", result);
        Assert.Contains("console.log(&#39;hello&#39;);", result);
        Assert.Contains("</code></pre>", result);
    }

    [Fact]
    public void Render_CodeBlock_NoLanguage()
    {
        var json = Wrap("""{"type":"codeblock","version":1,"code":"some code","language":"","caption":""}""");
        var result = _renderer.Render(json);
        Assert.Contains("<pre><code>some code</code></pre>", result);
    }

    [Fact]
    public void Render_Bookmark()
    {
        var json = Wrap("""{"type":"bookmark","version":1,"url":"https://example.com","title":"Example","description":"An example site","icon":"https://example.com/icon.png","author":"Author","publisher":"Publisher","thumbnail":"https://example.com/thumb.jpg","caption":""}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-bookmark-card", result);
        Assert.Contains("kg-bookmark-container", result);
        Assert.Contains("href=\"https://example.com\"", result);
        Assert.Contains("kg-bookmark-title", result);
        Assert.Contains("Example", result);
        Assert.Contains("kg-bookmark-description", result);
        Assert.Contains("kg-bookmark-icon", result);
        Assert.Contains("kg-bookmark-author", result);
        Assert.Contains("kg-bookmark-publisher", result);
        Assert.Contains("kg-bookmark-thumbnail", result);
    }

    [Fact]
    public void Render_Embed()
    {
        var json = Wrap("""{"type":"embed","version":1,"html":"<iframe src=\"https://youtube.com/embed/abc\"></iframe>","caption":"A video"}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-embed-card", result);
        Assert.Contains("<iframe", result);
        Assert.Contains("<figcaption>A video</figcaption>", result);
    }

    [Fact]
    public void Render_Callout()
    {
        var json = Wrap("""{"type":"callout","version":1,"calloutEmoji":"💡","backgroundColor":"yellow","children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Important note","type":"text","version":1}]}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-callout-card kg-callout-card-yellow", result);
        Assert.Contains("kg-callout-emoji", result);
        Assert.Contains("kg-callout-text", result);
        Assert.Contains("Important note", result);
    }

    [Fact]
    public void Render_Toggle()
    {
        var json = Wrap("""{"type":"toggle","version":1,"heading":"FAQ Question","children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Answer here","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}]}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-toggle-card", result);
        Assert.Contains("data-kg-toggle-state=\"close\"", result);
        Assert.Contains("kg-toggle-heading-text", result);
        Assert.Contains("FAQ Question", result);
        Assert.Contains("kg-toggle-content", result);
        Assert.Contains("Answer here", result);
    }

    [Fact]
    public void Render_Video()
    {
        var json = Wrap("""{"type":"video","version":1,"src":"https://example.com/video.mp4","width":1920,"height":1080,"caption":"My video","cardWidth":"wide","customThumbnailSrc":"https://example.com/poster.jpg"}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-video-card kg-width-wide", result);
        Assert.Contains("kg-video-container", result);
        Assert.Contains("src=\"https://example.com/video.mp4\"", result);
        Assert.Contains("controls", result);
        Assert.Contains("poster=\"https://example.com/poster.jpg\"", result);
        Assert.Contains("<figcaption>My video</figcaption>", result);
    }

    [Fact]
    public void Render_Audio()
    {
        var json = Wrap("""{"type":"audio","version":1,"src":"https://example.com/audio.mp3","title":"Podcast Episode","caption":"Episode 1"}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-audio-card", result);
        Assert.Contains("kg-audio-title", result);
        Assert.Contains("Podcast Episode", result);
        Assert.Contains("src=\"https://example.com/audio.mp3\"", result);
        Assert.Contains("<figcaption>Episode 1</figcaption>", result);
    }

    [Fact]
    public void Render_File()
    {
        var json = Wrap("""{"type":"file","version":1,"src":"https://example.com/doc.pdf","fileName":"doc.pdf","fileTitle":"My Document","fileCaption":"Download here","fileSize":1048576}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-file-card", result);
        Assert.Contains("href=\"https://example.com/doc.pdf\"", result);
        Assert.Contains("My Document", result);
        Assert.Contains("doc.pdf", result);
        Assert.Contains("1 MB", result);
    }

    [Fact]
    public void Render_Button()
    {
        var json = Wrap("""{"type":"button","version":1,"url":"https://example.com/signup","buttonText":"Sign Up Now","alignment":"center"}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-button-card kg-align-center", result);
        Assert.Contains("href=\"https://example.com/signup\"", result);
        Assert.Contains("kg-btn kg-btn-accent", result);
        Assert.Contains("Sign Up Now", result);
    }

    [Fact]
    public void Render_Gallery()
    {
        var json = Wrap("""{"type":"gallery","version":1,"images":[{"src":"https://example.com/1.jpg","alt":"One","width":800,"height":600},{"src":"https://example.com/2.jpg","alt":"Two","width":800,"height":600}],"caption":"Gallery caption"}""");
        var result = _renderer.Render(json);
        Assert.Contains("kg-card kg-gallery-card", result);
        Assert.Contains("kg-gallery-container", result);
        Assert.Contains("kg-gallery-row", result);
        Assert.Contains("kg-gallery-image", result);
        Assert.Contains("src=\"https://example.com/1.jpg\"", result);
        Assert.Contains("src=\"https://example.com/2.jpg\"", result);
        Assert.Contains("<figcaption>Gallery caption</figcaption>", result);
    }

    [Fact]
    public void Render_HtmlCard()
    {
        var json = Wrap("""{"type":"html","version":1,"html":"<div class=\"custom\">Custom HTML</div>"}""");
        var result = _renderer.Render(json);
        Assert.Contains("<!--kg-card-begin: html-->", result);
        Assert.Contains("<div class=\"custom\">Custom HTML</div>", result);
        Assert.Contains("<!--kg-card-end: html-->", result);
    }

    [Fact]
    public void Render_Divider()
    {
        var json = Wrap("""{"type":"divider","version":1}""");
        Assert.Equal("<hr>", _renderer.Render(json));
    }

    [Fact]
    public void Render_UnknownType_RendersChildren()
    {
        var json = Wrap("""{"type":"unknown-future-card","version":1,"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Fallback","type":"text","version":1}]}""");
        Assert.Equal("Fallback", _renderer.Render(json));
    }

    [Fact]
    public void Render_RealGhostPost_AboutPage()
    {
        // Real Lexical JSON from the Ghost dump - "About this site" page
        var json = """{"root":{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"howtoosoftware is an independent publication launched in December 2025 by Henry Lawrence Cahill.","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1},{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Access all areas","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"heading","tag":"h3","version":1},{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Get started for free using ","type":"text","version":1},{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Ghost","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"link","rel":null,"target":null,"title":null,"url":"https://ghost.org","version":1},{"detail":0,"format":0,"mode":"normal","style":"","text":".","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}],"direction":"ltr","format":"","indent":0,"type":"root","version":1}}""";

        var result = _renderer.Render(json);

        Assert.Contains("<p>howtoosoftware is an independent publication", result);
        Assert.Contains("<h3 id=\"access-all-areas\">Access all areas</h3>", result);
        Assert.Contains("<a href=\"https://ghost.org\">Ghost</a>", result);
    }

    [Fact]
    public void Render_MultipleParagraphs()
    {
        var json = Wrap(
            """{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"First","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1},{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Second","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p>First</p><p>Second</p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_MixedInlineFormatting()
    {
        // Paragraph with: "Hello " + bold "world" + " and " + italic "more"
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Hello ","type":"text","version":1},{"detail":0,"format":1,"mode":"normal","style":"","text":"world","type":"text","version":1},{"detail":0,"format":0,"mode":"normal","style":"","text":" and ","type":"text","version":1},{"detail":0,"format":2,"mode":"normal","style":"","text":"more","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"paragraph","version":1}""");
        Assert.Equal("<p>Hello <strong>world</strong> and <em>more</em></p>", _renderer.Render(json));
    }

    [Fact]
    public void Render_HeadingSlug_SpecialCharacters()
    {
        var json = Wrap("""{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Hello, World! #1","type":"text","version":1}],"direction":"ltr","format":"","indent":0,"type":"heading","tag":"h2","version":1}""");
        var result = _renderer.Render(json);
        Assert.Contains("id=\"hello-world-1\"", result);
    }

    [Fact]
    public void Render_ListWithLinebreak()
    {
        var json = Wrap("""{"children":[{"children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"First line","type":"extended-text","version":1},{"type":"linebreak","version":1},{"detail":0,"format":0,"mode":"normal","style":"","text":"Second line","type":"extended-text","version":1}],"direction":"ltr","format":"","indent":0,"type":"listitem","version":1,"value":1}],"direction":"ltr","format":"","indent":0,"type":"list","version":1,"listType":"number","start":1,"tag":"ol"}""");
        var result = _renderer.Render(json);
        Assert.Equal("<ol><li>First line<br>Second line</li></ol>", result);
    }

    [Fact]
    public void Render_Callout_NoEmoji()
    {
        var json = Wrap("""{"type":"callout","version":1,"calloutEmoji":"","backgroundColor":"grey","children":[{"detail":0,"format":0,"mode":"normal","style":"","text":"Note","type":"text","version":1}]}""");
        var result = _renderer.Render(json);
        Assert.DoesNotContain("kg-callout-emoji", result);
        Assert.Contains("kg-callout-text", result);
    }

    [Fact]
    public void Render_Image_NoWidthHeight()
    {
        var json = Wrap("""{"type":"image","version":1,"src":"https://example.com/img.jpg","width":0,"height":0,"alt":"","caption":"","cardWidth":"","href":""}""");
        var result = _renderer.Render(json);
        Assert.DoesNotContain("width=", result);
        Assert.DoesNotContain("height=", result);
    }

    [Fact]
    public void Render_CodeBlock_WithCaption()
    {
        var json = Wrap("""{"type":"codeblock","version":1,"code":"x = 1","language":"python","caption":"Example code"}""");
        var result = _renderer.Render(json);
        Assert.Contains("<figcaption>Example code</figcaption>", result);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
