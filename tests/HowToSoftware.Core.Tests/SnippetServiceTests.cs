using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Tests;

public class SnippetServiceTests
{
    private readonly FakeSnippetService _sut = new();

    // ── CreateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesSnippet()
    {
        var result = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Call to Action",
            Mobiledoc = """{"version":"0.3.1","atoms":[],"cards":[],"markups":[],"sections":[]}""",
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("Call to Action", result.Name);
    }

    [Fact]
    public async Task CreateAsync_WithLexical_SetsLexical()
    {
        var lexical = """{"root":{"children":[],"direction":null,"format":"","indent":0,"type":"root","version":1}}""";
        var result = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Test",
            Mobiledoc = "{}",
            Lexical = lexical,
        });

        Assert.Equal(lexical, result.Lexical);
    }

    [Fact]
    public async Task CreateAsync_SetsTimestamps()
    {
        var before = DateTime.UtcNow;
        var result = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Test",
            Mobiledoc = "{}",
        });

        Assert.True(result.CreatedAt >= before);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_TrimsName()
    {
        var result = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "  Padded Name  ",
            Mobiledoc = "{}",
        });

        Assert.Equal("Padded Name", result.Name);
    }

    // ── GetAllAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Empty_ReturnsEmptyList()
    {
        var results = await _sut.GetAllAsync();
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByMostRecent()
    {
        await _sut.CreateAsync(new CreateSnippetRequest { Name = "First", Mobiledoc = "{}" });
        await _sut.CreateAsync(new CreateSnippetRequest { Name = "Second", Mobiledoc = "{}" });

        var results = await _sut.GetAllAsync();
        Assert.Equal(2, results.Count);
        Assert.Equal("Second", results[0].Name);
        Assert.Equal("First", results[1].Name);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsSnippet()
    {
        var created = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Test",
            Mobiledoc = "{}",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("nonexistent");
        Assert.Null(result);
    }

    // ── GetByNameAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByNameAsync_ValidName_ReturnsSnippet()
    {
        await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Header CTA",
            Mobiledoc = "{}",
        });

        var result = await _sut.GetByNameAsync("Header CTA");
        Assert.NotNull(result);
        Assert.Equal("Header CTA", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_InvalidName_ReturnsNull()
    {
        var result = await _sut.GetByNameAsync("nonexistent");
        Assert.Null(result);
    }

    // ── UpdateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesName()
    {
        var created = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Old Name",
            Mobiledoc = "{}",
        });

        await _sut.UpdateAsync(created.Id, new UpdateSnippetRequest
        {
            Name = "New Name",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Equal("New Name", result!.Name);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMobiledoc()
    {
        var created = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Test",
            Mobiledoc = """{"old":"content"}""",
        });

        await _sut.UpdateAsync(created.Id, new UpdateSnippetRequest
        {
            Mobiledoc = """{"new":"content"}""",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Equal("""{"new":"content"}""", result!.Mobiledoc);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesLexical()
    {
        var created = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Test",
            Mobiledoc = "{}",
        });

        var lexical = """{"root":{"children":[],"type":"root","version":1}}""";
        await _sut.UpdateAsync(created.Id, new UpdateSnippetRequest
        {
            Lexical = lexical,
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Equal(lexical, result!.Lexical);
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateAsync("nonexistent", new UpdateSnippetRequest { Name = "X" }));
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var created = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Test",
            Mobiledoc = "{}",
        });

        var before = DateTime.UtcNow;
        await _sut.UpdateAsync(created.Id, new UpdateSnippetRequest
        {
            Name = "Updated",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.NotNull(result!.UpdatedAt);
        Assert.True(result.UpdatedAt >= before);
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_PreservesOtherFields()
    {
        var created = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "Original",
            Mobiledoc = """{"original":"doc"}""",
            Lexical = """{"original":"lex"}""",
        });

        await _sut.UpdateAsync(created.Id, new UpdateSnippetRequest
        {
            Name = "Updated Name",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Equal("Updated Name", result!.Name);
        Assert.Equal("""{"original":"doc"}""", result.Mobiledoc);
        Assert.Equal("""{"original":"lex"}""", result.Lexical);
    }

    // ── DeleteAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesSnippet()
    {
        var created = await _sut.CreateAsync(new CreateSnippetRequest
        {
            Name = "To Delete",
            Mobiledoc = "{}",
        });

        await _sut.DeleteAsync(created.Id);

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.DeleteAsync("nonexistent"));
    }

    // ================================================================
    // Fake in-memory implementation
    // ================================================================

    private sealed class FakeSnippetService : ISnippetService
    {
        private readonly List<Snippet> _snippets = [];

        public Task<List<Snippet>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_snippets
                .OrderByDescending(s => s.CreatedAt)
                .ToList());
        }

        public Task<Snippet?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return Task.FromResult(_snippets.FirstOrDefault(s => s.Id == id));
        }

        public Task<Snippet?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            return Task.FromResult(_snippets.FirstOrDefault(s => s.Name == name));
        }

        public Task<Snippet> CreateAsync(
            CreateSnippetRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var snippet = new Snippet
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                Name = request.Name.Trim(),
                Mobiledoc = request.Mobiledoc,
                Lexical = request.Lexical,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _snippets.Add(snippet);
            return Task.FromResult(snippet);
        }

        public Task UpdateAsync(
            string id, UpdateSnippetRequest request, CancellationToken ct = default)
        {
            var snippet = _snippets.FirstOrDefault(s => s.Id == id)
                ?? throw new InvalidOperationException($"Snippet {id} not found");

            if (request.Name is not null)
                snippet.Name = request.Name.Trim();

            if (request.Mobiledoc is not null)
                snippet.Mobiledoc = request.Mobiledoc;

            if (request.Lexical is not null)
                snippet.Lexical = request.Lexical;

            snippet.UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var snippet = _snippets.FirstOrDefault(s => s.Id == id)
                ?? throw new InvalidOperationException($"Snippet {id} not found");

            _snippets.Remove(snippet);
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, int>> GetUsageCountsAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_snippets.ToDictionary(s => s.Id, _ => 0));
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
