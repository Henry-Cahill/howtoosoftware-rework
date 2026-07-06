using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class MobiledocToLexicalConverterTests
{
    [Fact]
    public void Convert_NullInput_ReturnsNull()
    {
        Assert.Null(MobiledocToLexicalConverter.Convert(null));
    }

    [Fact]
    public void Convert_EmptyInput_ReturnsNull()
    {
        Assert.Null(MobiledocToLexicalConverter.Convert(""));
    }

    [Fact]
    public void Convert_SimpleParagraph_ProducesLexicalJson()
    {
        var mobiledoc = """
        {
            "version":"0.3.1",
            "atoms":[],
            "cards":[],
            "markups":[],
            "sections":[[1,"p",[[0,[],0,"Hello world"]]]]
        }
        """;

        var result = MobiledocToLexicalConverter.Convert(mobiledoc);
        Assert.NotNull(result);
        Assert.Contains("\"type\":\"paragraph\"", result);
        Assert.Contains("\"text\":\"Hello world\"", result);
        Assert.Contains("\"type\":\"root\"", result);
    }

    [Fact]
    public void Convert_Heading_ProducesHeadingNode()
    {
        var mobiledoc = """
        {
            "version":"0.3.1",
            "atoms":[],
            "cards":[],
            "markups":[],
            "sections":[[1,"h2",[[0,[],0,"My Heading"]]]]
        }
        """;

        var result = MobiledocToLexicalConverter.Convert(mobiledoc);
        Assert.NotNull(result);
        Assert.Contains("\"type\":\"heading\"", result);
        Assert.Contains("\"tag\":\"h2\"", result);
        Assert.Contains("\"text\":\"My Heading\"", result);
    }

    [Fact]
    public void Convert_BoldText_ProducesFormattedText()
    {
        var mobiledoc = """
        {
            "version":"0.3.1",
            "atoms":[],
            "cards":[],
            "markups":[["strong"]],
            "sections":[[1,"p",[[0,[0],1,"Bold text"]]]]
        }
        """;

        var result = MobiledocToLexicalConverter.Convert(mobiledoc);
        Assert.NotNull(result);
        Assert.Contains("\"format\":1", result); // Bold = 1
        Assert.Contains("\"text\":\"Bold text\"", result);
    }

    [Fact]
    public void Convert_Link_ProducesLinkNode()
    {
        var mobiledoc = """
        {
            "version":"0.3.1",
            "atoms":[],
            "cards":[],
            "markups":[["a",["href","https://example.com"]]],
            "sections":[[1,"p",[[0,[0],1,"click here"]]]]
        }
        """;

        var result = MobiledocToLexicalConverter.Convert(mobiledoc);
        Assert.NotNull(result);
        Assert.Contains("\"type\":\"link\"", result);
        Assert.Contains("\"url\":\"https://example.com\"", result);
        Assert.Contains("\"text\":\"click here\"", result);
    }

    [Fact]
    public void Convert_List_ProducesListNode()
    {
        var mobiledoc = """
        {
            "version":"0.3.1",
            "atoms":[],
            "cards":[],
            "markups":[],
            "sections":[[3,"ul",[[[0,[],0,"item one"]],[[0,[],0,"item two"]]]]]
        }
        """;

        var result = MobiledocToLexicalConverter.Convert(mobiledoc);
        Assert.NotNull(result);
        Assert.Contains("\"type\":\"list\"", result);
        Assert.Contains("\"listType\":\"bullet\"", result);
        Assert.Contains("\"type\":\"listitem\"", result);
        Assert.Contains("\"text\":\"item one\"", result);
        Assert.Contains("\"text\":\"item two\"", result);
    }

    [Fact]
    public void Convert_HrCard_ProducesHorizontalRule()
    {
        var mobiledoc = """
        {
            "version":"0.3.1",
            "atoms":[],
            "cards":[["hr",{}]],
            "markups":[],
            "sections":[[2,0]]
        }
        """;

        var result = MobiledocToLexicalConverter.Convert(mobiledoc);
        Assert.NotNull(result);
        Assert.Contains("\"type\":\"horizontalrule\"", result);
    }

    [Fact]
    public void Convert_ImageCard_ProducesImageNode()
    {
        var mobiledoc = """
        {
            "version":"0.3.1",
            "atoms":[],
            "cards":[["image",{"src":"https://img.example.com/photo.jpg","alt":"A photo","caption":"My photo"}]],
            "markups":[],
            "sections":[[2,0]]
        }
        """;

        var result = MobiledocToLexicalConverter.Convert(mobiledoc);
        Assert.NotNull(result);
        Assert.Contains("\"type\":\"image\"", result);
        Assert.Contains("\"src\":\"https://img.example.com/photo.jpg\"", result);
        Assert.Contains("\"altText\":\"A photo\"", result);
    }

    [Fact]
    public void Convert_GhostSamplePost_ParsesSuccessfully()
    {
        // Real-world sample from ghost_posts_dump
        var mobiledoc = """
        {
            "version":"0.3.1",
            "atoms":[],
            "cards":[],
            "markups":[["a",["href","#/portal/"]]],
            "sections":[
                [1,"p",[
                    [0,[],0,"This is howtoosoftware, a brand new site by Henry Lawrence Cahill that\u0027s just getting started. Things will be up and running here shortly, but you can "],
                    [0,[0],1,"subscribe"],
                    [0,[],0," in the meantime if you\u0027d like to stay up to date and receive emails when new content is published!"]
                ]]
            ],
            "ghostVersion":"4.0"
        }
        """;

        var result = MobiledocToLexicalConverter.Convert(mobiledoc);
        Assert.NotNull(result);
        Assert.Contains("\"type\":\"paragraph\"", result);
        Assert.Contains("\"type\":\"link\"", result);
        Assert.Contains("\"url\":\"#/portal/\"", result);
        Assert.Contains("subscribe", result);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
