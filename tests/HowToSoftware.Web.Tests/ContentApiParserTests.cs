using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Tests;

public class FilterParserTests
{
    [Fact]
    public void Parse_NullInput_ReturnsEmptyDictionary()
    {
        var result = FilterParser.Parse(null);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyDictionary()
    {
        var result = FilterParser.Parse("");
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_SingleFilter_ParsesCorrectly()
    {
        var result = FilterParser.Parse("tag:getting-started");
        Assert.Single(result);
        Assert.Equal("getting-started", result["tag"]);
    }

    [Fact]
    public void Parse_MultipleFilters_ParsesAll()
    {
        var result = FilterParser.Parse("tag:news+status:published");
        Assert.Equal(2, result.Count);
        Assert.Equal("news", result["tag"]);
        Assert.Equal("published", result["status"]);
    }

    [Fact]
    public void Parse_FilterWithAuthor_ParsesCorrectly()
    {
        var result = FilterParser.Parse("author:john-doe");
        Assert.Equal("john-doe", result["author"]);
    }

    [Fact]
    public void Parse_InvalidSegmentNoColon_Skipped()
    {
        var result = FilterParser.Parse("invalidfilter");
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_MixedValidAndInvalid_OnlyValidKept()
    {
        var result = FilterParser.Parse("tag:news+badfilter+status:draft");
        Assert.Equal(2, result.Count);
        Assert.Equal("news", result["tag"]);
        Assert.Equal("draft", result["status"]);
    }
}

public class IncludeParserTests
{
    [Fact]
    public void Parse_NullInput_ReturnsEmptySet()
    {
        var result = IncludeParser.Parse(null);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_Tags_ReturnsTags()
    {
        var result = IncludeParser.Parse("tags");
        Assert.Single(result);
        Assert.Contains("tags", result);
    }

    [Fact]
    public void Parse_TagsAndAuthors_ReturnsBoth()
    {
        var result = IncludeParser.Parse("tags,authors");
        Assert.Equal(2, result.Count);
        Assert.Contains("tags", result);
        Assert.Contains("authors", result);
    }

    [Fact]
    public void Parse_UnknownInclude_IsIgnored()
    {
        var result = IncludeParser.Parse("tags,comments,authors");
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain("comments", result);
    }

    [Fact]
    public void Parse_WhitespaceAroundValues_Trimmed()
    {
        var result = IncludeParser.Parse(" tags , authors ");
        Assert.Equal(2, result.Count);
    }
}

public class FieldParserTests
{
    [Fact]
    public void Parse_NullInput_ReturnsNull()
    {
        var result = FieldParser.Parse(null);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsNull()
    {
        var result = FieldParser.Parse("");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_SingleField_AlwaysIncludesId()
    {
        var result = FieldParser.Parse("title");
        Assert.NotNull(result);
        Assert.Contains("id", result);
        Assert.Contains("title", result);
    }

    [Fact]
    public void Parse_MultipleFields_AllPresent()
    {
        var result = FieldParser.Parse("title,slug,html");
        Assert.NotNull(result);
        Assert.Equal(4, result.Count); // title + slug + html + id
        Assert.Contains("title", result);
        Assert.Contains("slug", result);
        Assert.Contains("html", result);
        Assert.Contains("id", result);
    }

    [Fact]
    public void Parse_WhitespaceAroundFields_Trimmed()
    {
        var result = FieldParser.Parse(" title , slug ");
        Assert.NotNull(result);
        Assert.Contains("title", result);
        Assert.Contains("slug", result);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
