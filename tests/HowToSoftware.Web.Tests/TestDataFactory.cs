using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Pages;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HowToSoftware.Web.Tests;

/// <summary>
/// Helper to create test Post entities with common fields populated.
/// </summary>
internal static class TestDataFactory
{
    private static int _counter;

    public static Post CreatePublishedPost(string? slug = null, string? title = null, string type = "post")
    {
        var n = Interlocked.Increment(ref _counter);
        return new Post
        {
            Id = n.ToString().PadLeft(24, '0'),
            Uuid = Guid.NewGuid().ToString(),
            Title = title ?? $"Test Post {n}",
            Slug = slug ?? $"test-post-{n}",
            Html = $"<p>Content of post {n}</p>",
            Plaintext = $"Content of post {n} with enough words to calculate reading time properly for the test suite",
            Status = "published",
            Type = type,
            Visibility = "public",
            Featured = false,
            CreatedAt = DateTime.UtcNow.AddDays(-n),
            PublishedAt = DateTime.UtcNow.AddDays(-n),
            ShowTitleAndFeatureImage = true,
            PostsTags = new List<PostsTag>(),
            PostsAuthors = new List<PostsAuthor>(),
        };
    }

    public static Tag CreateTag(string? id = null, string? slug = null, string? name = null)
    {
        var n = Interlocked.Increment(ref _counter);
        return new Tag
        {
            Id = id ?? n.ToString().PadLeft(24, '0'),
            Name = name ?? $"Tag {n}",
            Slug = slug ?? $"tag-{n}",
            Visibility = "public",
            CreatedAt = DateTime.UtcNow,
            PostsTags = new List<PostsTag>(),
        };
    }

    public static User CreateUser(string? id = null, string? slug = null, string? name = null)
    {
        var n = Interlocked.Increment(ref _counter);
        return new User
        {
            Id = id ?? n.ToString().PadLeft(24, '0'),
            Name = name ?? $"Author {n}",
            Slug = slug ?? $"author-{n}",
            Email = $"author{n}@test.com",
            Status = "active",
            Visibility = "public",
            CreatedAt = DateTime.UtcNow,
            PostsAuthors = new List<PostsAuthor>(),
        };
    }

    public static void LinkPostToTag(Post post, Tag tag, int sortOrder = 0)
    {
        var pt = new PostsTag
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            PostId = post.Id,
            TagId = tag.Id,
            SortOrder = sortOrder,
            Post = post,
            Tag = tag,
        };
        ((List<PostsTag>)post.PostsTags).Add(pt);
        ((List<PostsTag>)tag.PostsTags).Add(pt);
    }

    public static void LinkPostToAuthor(Post post, User author, int sortOrder = 0)
    {
        var pa = new PostsAuthor
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            PostId = post.Id,
            AuthorId = author.Id,
            SortOrder = sortOrder,
            Post = post,
            Author = author,
        };
        ((List<PostsAuthor>)post.PostsAuthors).Add(pa);
        ((List<PostsAuthor>)author.PostsAuthors).Add(pa);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
