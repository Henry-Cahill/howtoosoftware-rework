using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Tests;

public class AuthorMapperTests
{
    private static User CreateTestUser() => new()
    {
        Id = "user-1",
        Name = "Jane Author",
        Slug = "jane-author",
        Email = "jane@example.com",
        PasswordHash = "hash",
        ProfileImage = "/content/images/jane.jpg",
        CoverImage = "/content/images/jane-cover.jpg",
        Bio = "Writes about software",
        Website = "https://example.com",
        Location = "Portland, OR",
        Facebook = "janeauthor",
        Twitter = "@janeauthor",
        MetaTitle = "Jane Author - Writer",
        MetaDescription = "Jane writes about software",
        Status = "active",
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void ToResource_MapsAllFields()
    {
        var user = CreateTestUser();

        var result = AuthorMapper.ToResource(user);

        Assert.Equal("user-1", result.Id);
        Assert.Equal("Jane Author", result.Name);
        Assert.Equal("jane-author", result.Slug);
        Assert.Equal("/content/images/jane.jpg", result.ProfileImage);
        Assert.Equal("/content/images/jane-cover.jpg", result.CoverImage);
        Assert.Equal("Writes about software", result.Bio);
        Assert.Equal("https://example.com", result.Website);
        Assert.Equal("Portland, OR", result.Location);
        Assert.Equal("janeauthor", result.Facebook);
        Assert.Equal("@janeauthor", result.Twitter);
        Assert.Equal("Jane Author - Writer", result.MetaTitle);
        Assert.Equal("Jane writes about software", result.MetaDescription);
        Assert.Equal("/author/jane-author/", result.Url);
    }

    [Fact]
    public void ToResource_WithoutPostCount_CountIsNull()
    {
        var user = CreateTestUser();

        var result = AuthorMapper.ToResource(user);

        Assert.Null(result.Count);
    }

    [Fact]
    public void ToResource_WithPostCount_CountIsPopulated()
    {
        var user = CreateTestUser();

        var result = AuthorMapper.ToResource(user, postCount: 7);

        Assert.NotNull(result.Count);
        Assert.Equal(7, result.Count.Posts);
    }

    [Fact]
    public void ToResource_WithZeroPostCount_CountIsPopulated()
    {
        var user = CreateTestUser();

        var result = AuthorMapper.ToResource(user, postCount: 0);

        Assert.NotNull(result.Count);
        Assert.Equal(0, result.Count.Posts);
    }

    [Fact]
    public void ToResource_NullOptionalFields_MappedAsNull()
    {
        var user = new User
        {
            Id = "user-2",
            Name = "Minimal User",
            Slug = "minimal-user",
            Email = "min@example.com",
            PasswordHash = "hash",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
        };

        var result = AuthorMapper.ToResource(user);

        Assert.Equal("user-2", result.Id);
        Assert.Equal("Minimal User", result.Name);
        Assert.Null(result.ProfileImage);
        Assert.Null(result.CoverImage);
        Assert.Null(result.Bio);
        Assert.Null(result.Website);
        Assert.Null(result.Location);
        Assert.Null(result.MetaTitle);
        Assert.Equal("/author/minimal-user/", result.Url);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
