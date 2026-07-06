using HowToSoftware.Core.Entities;
using HowToSoftware.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class PostRepositoryTests(DatabaseFixture db) : IAsyncLifetime
{
    private const string AuthorId = "author000000000000000001";

    public async Task InitializeAsync()
    {
        await using var ctx = db.CreateContext();
        // Clean slate for each test
        await ctx.Posts.ExecuteDeleteAsync();
        await ctx.Tags.ExecuteDeleteAsync();
        await ctx.Users.Where(u => u.Id == AuthorId).ExecuteDeleteAsync();

        ctx.Users.Add(new User
        {
            Id = AuthorId,
            Name = "Test Author",
            Slug = "test-author",
            UserName = "testauthor",
            Email = "author@test.com",
            CreatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private PostRepository CreateRepo() => new(db.CreateContext());

    private static Post MakePost(string title = "Test Post", string status = "draft", string type = "post")
    {
        var id = Guid.NewGuid().ToString("N")[..24];
        return new Post
        {
            Id = id,
            Uuid = Guid.NewGuid().ToString("D"),
            Title = title,
            Slug = title.ToLowerInvariant().Replace(' ', '-'),
            Status = status,
            Type = type,
            Visibility = "public",
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Add / GetById ──────────────────────────────────────────

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_RoundTrips()
    {
        var repo = CreateRepo();
        var post = MakePost("Roundtrip Test");

        await repo.AddAsync(post);

        var repo2 = CreateRepo();
        var loaded = await repo2.GetByIdAsync(post.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Roundtrip Test", loaded.Title);
        Assert.Equal("roundtrip-test", loaded.Slug);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var repo = CreateRepo();
        var result = await repo.GetByIdAsync("nonexistent000000000000");

        Assert.Null(result);
    }

    // ── GetBySlug ──────────────────────────────────────────────

    [Fact]
    public async Task GetBySlugAsync_FindsPost()
    {
        var repo = CreateRepo();
        var post = MakePost("Slug Lookup");
        await repo.AddAsync(post);

        var repo2 = CreateRepo();
        var loaded = await repo2.GetBySlugAsync("slug-lookup");

        Assert.NotNull(loaded);
        Assert.Equal(post.Id, loaded.Id);
    }

    // ── Published posts (paging) ───────────────────────────────

    [Fact]
    public async Task GetPublishedPostsAsync_OnlyReturnsPublishedPosts()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakePost("Draft 1", status: "draft"));
        await repo.AddAsync(MakePost("Published 1", status: "published"));
        await repo.AddAsync(MakePost("Published 2", status: "published"));

        var repo2 = CreateRepo();
        var result = await repo2.GetPublishedPostsAsync(1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p => Assert.Equal("published", p.Status));
    }

    [Fact]
    public async Task GetPublishedPostsAsync_ExcludesPages()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakePost("A Post", status: "published", type: "post"));
        await repo.AddAsync(MakePost("A Page", status: "published", type: "page"));

        var repo2 = CreateRepo();
        var result = await repo2.GetPublishedPostsAsync(1, 10);

        Assert.Single(result.Items);
        Assert.Equal("A Post", result.Items[0].Title);
    }

    [Fact]
    public async Task GetPublishedPostsAsync_Pagination_Works()
    {
        var repo = CreateRepo();
        for (int i = 0; i < 5; i++)
            await repo.AddAsync(MakePost($"Page Post {i}", status: "published"));

        var repo2 = CreateRepo();
        var page1 = await repo2.GetPublishedPostsAsync(1, 2);
        var page2 = await repo2.GetPublishedPostsAsync(2, 2);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.True(page1.HasNextPage);
    }

    // ── GetPublishedPostsByNewsletterAsync ─────────────────────

    [Fact]
    public async Task GetPublishedPostsByNewsletterAsync_FiltersByNewsletterAndStatus()
    {
        var newsletterId = "newsletter000000000000a1";
        await using (var ctx = db.CreateContext())
        {
            await ctx.Newsletters.Where(n => n.Id == newsletterId).ExecuteDeleteAsync();
            ctx.Newsletters.Add(new Newsletter
            {
                Id = newsletterId,
                Uuid = Guid.NewGuid().ToString("D"),
                Name = $"Test Newsletter {Guid.NewGuid():N}",
                Slug = $"test-nl-{Guid.NewGuid():N}",
            });
            await ctx.SaveChangesAsync();
        }

        var repo = CreateRepo();
        var matching = MakePost("Matching Edition", status: "published");
        matching.NewsletterId = newsletterId;
        matching.PublishedAt = DateTime.UtcNow.AddDays(-1);
        await repo.AddAsync(matching);

        var draft = MakePost("Draft Edition", status: "draft");
        draft.NewsletterId = newsletterId;
        await repo.AddAsync(draft);

        var otherNewsletter = MakePost("Other Newsletter", status: "published");
        otherNewsletter.NewsletterId = "other00000000000000000a1";
        await repo.AddAsync(otherNewsletter);

        var unlinked = MakePost("Unlinked", status: "published");
        await repo.AddAsync(unlinked);

        var page = MakePost("A Page", status: "published", type: "page");
        page.NewsletterId = newsletterId;
        await repo.AddAsync(page);

        var repo2 = CreateRepo();
        var result = await repo2.GetPublishedPostsByNewsletterAsync(newsletterId, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Matching Edition", result.Items[0].Title);

        await using var cleanup = db.CreateContext();
        await cleanup.Newsletters.Where(n => n.Id == newsletterId).ExecuteDeleteAsync();
    }

    // ── GetAllAsync (admin) ────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_FiltersByStatus()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakePost("Draft", status: "draft"));
        await repo.AddAsync(MakePost("Published", status: "published"));

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync("draft", null, 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Draft", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByType()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakePost("Post1", type: "post"));
        await repo.AddAsync(MakePost("Page1", type: "page"));

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync(null, "page", 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Page1", result.Items[0].Title);
    }

    // ── Update ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var repo = CreateRepo();
        var post = MakePost("Original Title");
        await repo.AddAsync(post);

        await using var ctx2 = db.CreateContext();
        var toUpdate = await ctx2.Posts.FirstAsync(p => p.Id == post.Id);
        toUpdate.Title = "Updated Title";
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var repo2 = new PostRepository(ctx2);
        await repo2.UpdateAsync(toUpdate);

        var repo3 = CreateRepo();
        var loaded = await repo3.GetByIdAsync(post.Id);
        Assert.Equal("Updated Title", loaded!.Title);
    }

    // ── Delete ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesPost()
    {
        var repo = CreateRepo();
        var post = MakePost("To Delete");
        await repo.AddAsync(post);

        var repo2 = CreateRepo();
        await repo2.DeleteAsync(post.Id);

        var repo3 = CreateRepo();
        var loaded = await repo3.GetByIdAsync(post.Id);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task DeleteManyAsync_RemovesMultiple()
    {
        var repo = CreateRepo();
        var p1 = MakePost("Del1");
        var p2 = MakePost("Del2");
        var p3 = MakePost("Keep");
        await repo.AddAsync(p1);
        await repo.AddAsync(p2);
        await repo.AddAsync(p3);

        var repo2 = CreateRepo();
        await repo2.DeleteManyAsync([p1.Id, p2.Id]);

        var repo3 = CreateRepo();
        Assert.Null(await repo3.GetByIdAsync(p1.Id));
        Assert.Null(await repo3.GetByIdAsync(p2.Id));
        Assert.NotNull(await repo3.GetByIdAsync(p3.Id));
    }

    // ── Featured ───────────────────────────────────────────────

    [Fact]
    public async Task SetFeaturedAsync_UpdatesFeaturedFlag()
    {
        var repo = CreateRepo();
        var post = MakePost("Feature Me", status: "published");
        await repo.AddAsync(post);

        var repo2 = CreateRepo();
        await repo2.SetFeaturedAsync([post.Id], true);

        var repo3 = CreateRepo();
        var loaded = await repo3.GetByIdAsync(post.Id);
        Assert.True(loaded!.Featured);
    }

    [Fact]
    public async Task GetFeaturedPostsAsync_ReturnsFeaturedOnly()
    {
        var repo = CreateRepo();
        var featured = MakePost("Featured", status: "published");
        featured.Featured = true;
        await repo.AddAsync(featured);
        await repo.AddAsync(MakePost("Not Featured", status: "published"));

        var repo2 = CreateRepo();
        var result = await repo2.GetFeaturedPostsAsync(10);

        Assert.Single(result);
        Assert.Equal("Featured", result[0].Title);
    }

    // ── Count ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCountAsync_FiltersCorrectly()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakePost("D1", status: "draft", type: "post"));
        await repo.AddAsync(MakePost("D2", status: "draft", type: "post"));
        await repo.AddAsync(MakePost("P1", status: "published", type: "post"));
        await repo.AddAsync(MakePost("Pg1", status: "draft", type: "page"));

        var repo2 = CreateRepo();
        Assert.Equal(4, await repo2.GetCountAsync(null, null));
        Assert.Equal(3, await repo2.GetCountAsync("draft", null));
        Assert.Equal(1, await repo2.GetCountAsync("published", null));
        Assert.Equal(3, await repo2.GetCountAsync(null, "post"));
    }

    // ── Tags inclusion ─────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_IncludesTags()
    {
        await using var ctx = db.CreateContext();
        var tag = new Tag
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Name = "CSharp",
            Slug = "csharp",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Tags.Add(tag);

        var post = MakePost("Tagged Post");
        post.PostsTags.Add(new PostsTag
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            PostId = post.Id,
            TagId = tag.Id,
            SortOrder = 0,
        });
        ctx.Posts.Add(post);
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        var loaded = await repo.GetByIdAsync(post.Id);

        Assert.NotNull(loaded);
        Assert.Single(loaded.PostsTags);
        Assert.Equal("CSharp", loaded.PostsTags.First().Tag.Name);
    }

    // ── Published by tag ───────────────────────────────────────

    [Fact]
    public async Task GetPublishedPostsByTagAsync_FiltersCorrectly()
    {
        await using var ctx = db.CreateContext();
        var tag = new Tag
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Name = "DotNet",
            Slug = "dotnet",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Tags.Add(tag);

        var tagged = MakePost("Tagged", status: "published");
        tagged.PostsTags.Add(new PostsTag
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            PostId = tagged.Id,
            TagId = tag.Id,
            SortOrder = 0,
        });
        ctx.Posts.Add(tagged);
        ctx.Posts.Add(MakePost("Untagged", status: "published"));
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        var result = await repo.GetPublishedPostsByTagAsync("dotnet", 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Tagged", result.Items[0].Title);
    }

    // ── Revisions ──────────────────────────────────────────────

    [Fact]
    public async Task GetByIdWithRevisionsAsync_IncludesRevisions()
    {
        await using var ctx = db.CreateContext();
        var post = MakePost("Rev Post");
        post.Revisions.Add(new PostRevision
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            PostId = post.Id,
            Title = "Rev Post",
            Reason = "initial",
            CreatedAt = DateTime.UtcNow,
            CreatedAtTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        ctx.Posts.Add(post);
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        var loaded = await repo.GetByIdWithRevisionsAsync(post.Id);

        Assert.NotNull(loaded);
        Assert.Single(loaded.Revisions);
        Assert.Equal("initial", loaded.Revisions.First().Reason);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
