using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Tests;

public class TagMapperTests
{
    private static Tag CreateTestTag() => new()
    {
        Id = "tag-1",
        Name = "Getting Started",
        Slug = "getting-started",
        Description = "Posts to help you get started",
        FeatureImage = "/content/images/getting-started.jpg",
        Visibility = "public",
        OgTitle = "OG Getting Started",
        OgDescription = "OG description",
        MetaTitle = "Meta Getting Started",
        MetaDescription = "Meta description",
        AccentColor = "#ff0000",
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void ToResource_MapsAllFields()
    {
        var tag = CreateTestTag();

        var result = TagMapper.ToResource(tag);

        Assert.Equal("tag-1", result.Id);
        Assert.Equal("Getting Started", result.Name);
        Assert.Equal("getting-started", result.Slug);
        Assert.Equal("Posts to help you get started", result.Description);
        Assert.Equal("/content/images/getting-started.jpg", result.FeatureImage);
        Assert.Equal("public", result.Visibility);
        Assert.Equal("OG Getting Started", result.OgTitle);
        Assert.Equal("OG description", result.OgDescription);
        Assert.Equal("Meta Getting Started", result.MetaTitle);
        Assert.Equal("Meta description", result.MetaDescription);
        Assert.Equal("#ff0000", result.AccentColor);
        Assert.Equal("/tag/getting-started/", result.Url);
    }

    [Fact]
    public void ToResource_WithoutPostCount_CountIsNull()
    {
        var tag = CreateTestTag();

        var result = TagMapper.ToResource(tag);

        Assert.Null(result.Count);
    }

    [Fact]
    public void ToResource_WithPostCount_CountIsPopulated()
    {
        var tag = CreateTestTag();

        var result = TagMapper.ToResource(tag, postCount: 5);

        Assert.NotNull(result.Count);
        Assert.Equal(5, result.Count.Posts);
    }

    [Fact]
    public void ToResource_WithZeroPostCount_CountIsPopulated()
    {
        var tag = CreateTestTag();

        var result = TagMapper.ToResource(tag, postCount: 0);

        Assert.NotNull(result.Count);
        Assert.Equal(0, result.Count.Posts);
    }

    [Fact]
    public void ToResource_NullOptionalFields_MappedAsNull()
    {
        var tag = new Tag
        {
            Id = "tag-2",
            Name = "Minimal",
            Slug = "minimal",
            Visibility = "public",
            CreatedAt = DateTime.UtcNow,
        };

        var result = TagMapper.ToResource(tag);

        Assert.Equal("tag-2", result.Id);
        Assert.Equal("Minimal", result.Name);
        Assert.Null(result.Description);
        Assert.Null(result.FeatureImage);
        Assert.Null(result.OgTitle);
        Assert.Null(result.MetaTitle);
        Assert.Null(result.AccentColor);
        Assert.Equal("/tag/minimal/", result.Url);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
