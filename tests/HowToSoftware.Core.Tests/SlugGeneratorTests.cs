using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class SlugGeneratorTests
{
    private readonly SlugGenerator _sut = new();

    // ── GenerateSlug ────────────────────────────────────────────

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Hello  World  ", "hello-world")]
    [InlineData("My First Blog Post", "my-first-blog-post")]
    public void GenerateSlug_BasicTitles_ReturnsExpected(string input, string expected)
    {
        Assert.Equal(expected, _sut.GenerateSlug(input));
    }

    [Theory]
    [InlineData("Hello, World! How's It Going?", "hello-world-hows-it-going")]
    [InlineData("C# & .NET 10: What's New?", "c-net-10-whats-new")]
    [InlineData("Price: $9.99!", "price-999")]
    public void GenerateSlug_SpecialCharacters_Stripped(string input, string expected)
    {
        Assert.Equal(expected, _sut.GenerateSlug(input));
    }

    [Theory]
    [InlineData("Héllo Wörld", "hello-world")]
    [InlineData("Ñoño señor", "nono-senor")]
    [InlineData("Über café résumé", "uber-cafe-resume")]
    public void GenerateSlug_AccentedCharacters_Normalized(string input, string expected)
    {
        Assert.Equal(expected, _sut.GenerateSlug(input));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void GenerateSlug_EmptyOrWhitespace_ReturnsEmpty(string? input, string expected)
    {
        Assert.Equal(expected, _sut.GenerateSlug(input!));
    }

    [Fact]
    public void GenerateSlug_AlreadyASlug_ReturnsSame()
    {
        Assert.Equal("already-a-slug", _sut.GenerateSlug("already-a-slug"));
    }

    [Fact]
    public void GenerateSlug_LeadingTrailingDashes_Trimmed()
    {
        Assert.Equal("hello-world", _sut.GenerateSlug("---hello---world---"));
    }

    [Fact]
    public void GenerateSlug_Numbers_Preserved()
    {
        Assert.Equal("top-10-tips-for-2025", _sut.GenerateSlug("Top 10 Tips for 2025"));
    }

    [Fact]
    public void GenerateSlug_CyrillicText_Preserved()
    {
        // Cyrillic should be preserved as valid unicode letters
        Assert.Equal("привет-мир", _sut.GenerateSlug("Привет Мир"));
    }

    [Fact]
    public void GenerateSlug_VeryLongTitle_TruncatedTo191()
    {
        var longTitle = string.Join(" ", Enumerable.Repeat("word", 100));
        var slug = _sut.GenerateSlug(longTitle);
        Assert.True(slug.Length <= 191);
        Assert.DoesNotContain("--", slug);
    }

    [Fact]
    public void GenerateSlug_Tabs_And_Newlines_ReplacedWithDash()
    {
        Assert.Equal("hello-world", _sut.GenerateSlug("hello\t\nworld"));
    }

    // ── GenerateUniqueSlugAsync ─────────────────────────────────

    [Fact]
    public async Task GenerateUniqueSlugAsync_NoConflict_ReturnsBaseSlug()
    {
        var slug = await _sut.GenerateUniqueSlugAsync(
            "Hello World",
            _ => Task.FromResult(false));

        Assert.Equal("hello-world", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_OneConflict_AppendsDash2()
    {
        var existing = new HashSet<string> { "hello-world" };

        var slug = await _sut.GenerateUniqueSlugAsync(
            "Hello World",
            s => Task.FromResult(existing.Contains(s)));

        Assert.Equal("hello-world-2", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_MultipleConflicts_IncrementsCorrectly()
    {
        var existing = new HashSet<string>
        {
            "hello-world",
            "hello-world-2",
            "hello-world-3"
        };

        var slug = await _sut.GenerateUniqueSlugAsync(
            "Hello World",
            s => Task.FromResult(existing.Contains(s)));

        Assert.Equal("hello-world-4", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_EmptyText_ReturnsUntitled()
    {
        var slug = await _sut.GenerateUniqueSlugAsync(
            "",
            _ => Task.FromResult(false));

        Assert.Equal("untitled", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_EmptyWithConflict_ReturnsUntitled2()
    {
        var existing = new HashSet<string> { "untitled" };

        var slug = await _sut.GenerateUniqueSlugAsync(
            "",
            s => Task.FromResult(existing.Contains(s)));

        Assert.Equal("untitled-2", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_CancellationRespected()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.GenerateUniqueSlugAsync(
                "Hello World",
                _ => Task.FromResult(true), // every slug "exists"
                cts.Token));
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ResultAlwaysFitsMax191()
    {
        var longTitle = string.Join(" ", Enumerable.Repeat("toolong", 50));
        var existing = new HashSet<string>();
        // Pre-fill base slug so it needs suffix
        existing.Add(_sut.GenerateSlug(longTitle));

        var slug = await _sut.GenerateUniqueSlugAsync(
            longTitle,
            s => Task.FromResult(existing.Contains(s)));

        Assert.True(slug.Length <= 191);
        Assert.EndsWith("-2", slug);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
