using System.Text.RegularExpressions;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Tests;

public class RedirectServiceTests
{
    private readonly FakeRedirectService _sut = new();

    // ── CreateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesRedirect()
    {
        var result = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/old-post/",
            To = "/new-post/",
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("/old-post/", result.From);
        Assert.Equal("/new-post/", result.To);
    }

    [Fact]
    public async Task CreateAsync_SetsTimestamps()
    {
        var before = DateTime.UtcNow;
        var result = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/a/",
            To = "/b/",
        });

        Assert.True(result.CreatedAt >= before);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_TrimsUrls()
    {
        var result = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "  /padded/  ",
            To = "  /destination/  ",
        });

        Assert.Equal("/padded/", result.From);
        Assert.Equal("/destination/", result.To);
    }

    [Fact]
    public async Task CreateAsync_ExternalUrl()
    {
        var result = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/old/",
            To = "https://example.com/new-page",
        });

        Assert.Equal("https://example.com/new-page", result.To);
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
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/first/", To = "/a/" });
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/second/", To = "/b/" });

        var results = await _sut.GetAllAsync();
        Assert.Equal(2, results.Count);
        Assert.Equal("/second/", results[0].From);
        Assert.Equal("/first/", results[1].From);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsRedirect()
    {
        var created = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/test/",
            To = "/dest/",
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

    // ── GetByFromAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByFromAsync_ValidUrl_ReturnsRedirect()
    {
        await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/old-url/",
            To = "/new-url/",
        });

        var result = await _sut.GetByFromAsync("/old-url/");
        Assert.NotNull(result);
        Assert.Equal("/new-url/", result.To);
    }

    [Fact]
    public async Task GetByFromAsync_InvalidUrl_ReturnsNull()
    {
        var result = await _sut.GetByFromAsync("/nonexistent/");
        Assert.Null(result);
    }

    // ── MatchAsync ──────────────────────────────────────────────

    [Fact]
    public async Task MatchAsync_ExactRedirect_Matches()
    {
        var created = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/old/",
            To = "/new/",
        });

        var match = await _sut.MatchAsync("/old/");

        Assert.NotNull(match);
        Assert.Equal(created.Id, match.Id);
        Assert.Equal("/new/", match.Target);
    }

    [Fact]
    public async Task MatchAsync_NoMatch_ReturnsNull()
    {
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/a/", To = "/b/" });
        var match = await _sut.MatchAsync("/zzz/");
        Assert.Null(match);
    }

    [Fact]
    public async Task MatchAsync_ExactNotMatchedAsRegex()
    {
        await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/a/",
            To = "/b/",
            IsRegex = false,
        });

        // Substring should not match an exact (non-regex) entry
        var match = await _sut.MatchAsync("/prefix/a/");
        Assert.Null(match);
    }

    [Fact]
    public async Task MatchAsync_RegexWithBackreference_SubstitutesGroup()
    {
        var created = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "^/old-blog/(.*)$",
            To = "/blog/$1",
            IsRegex = true,
        });

        var match = await _sut.MatchAsync("/old-blog/hello-world");

        Assert.NotNull(match);
        Assert.Equal(created.Id, match.Id);
        Assert.Equal("/blog/hello-world", match.Target);
    }

    [Fact]
    public async Task MatchAsync_RegexNoMatch_ReturnsNull()
    {
        await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "^/old-blog/(.*)$",
            To = "/blog/$1",
            IsRegex = true,
        });

        var match = await _sut.MatchAsync("/somewhere-else/");
        Assert.Null(match);
    }

    [Fact]
    public async Task MatchAsync_ExactPreferredOverRegex()
    {
        await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "^/foo/.*$",
            To = "/regex-target/",
            IsRegex = true,
        });
        await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/foo/bar",
            To = "/exact-target/",
        });

        var match = await _sut.MatchAsync("/foo/bar");

        Assert.NotNull(match);
        Assert.Equal("/exact-target/", match.Target);
    }

    [Fact]
    public async Task CreateAsync_InvalidRegexPattern_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateAsync(new CreateRedirectRequest
            {
                From = "[unclosed",
                To = "/x/",
                IsRegex = true,
            }));
    }

    // ── UpdateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesFrom()
    {
        var created = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/old/",
            To = "/dest/",
        });

        await _sut.UpdateAsync(created.Id, new UpdateRedirectRequest
        {
            From = "/updated/",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Equal("/updated/", result!.From);
        Assert.Equal("/dest/", result.To);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTo()
    {
        var created = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/source/",
            To = "/old-dest/",
        });

        await _sut.UpdateAsync(created.Id, new UpdateRedirectRequest
        {
            To = "/new-dest/",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Equal("/source/", result!.From);
        Assert.Equal("/new-dest/", result.To);
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateAsync("nonexistent", new UpdateRedirectRequest { From = "/x/" }));
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var created = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/test/",
            To = "/dest/",
        });

        var before = DateTime.UtcNow;
        await _sut.UpdateAsync(created.Id, new UpdateRedirectRequest
        {
            From = "/updated/",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.NotNull(result!.UpdatedAt);
        Assert.True(result.UpdatedAt >= before);
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_PreservesOtherFields()
    {
        var created = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/original-from/",
            To = "/original-to/",
        });

        await _sut.UpdateAsync(created.Id, new UpdateRedirectRequest
        {
            From = "/updated-from/",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Equal("/updated-from/", result!.From);
        Assert.Equal("/original-to/", result.To);
    }

    // ── DeleteAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesRedirect()
    {
        var created = await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/to-delete/",
            To = "/dest/",
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

    // ── BulkImportAsync ─────────────────────────────────────────

    [Fact]
    public async Task BulkImportAsync_ImportsRowsWithHeader()
    {
        const string csv = "from_url,to_url\n/old/,/new/\n/old2/,/new2/\n";

        var result = await _sut.BulkImportAsync(csv);

        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Skipped);
        Assert.Empty(result.Errors);

        var all = await _sut.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task BulkImportAsync_ImportsRowsWithoutHeader()
    {
        const string csv = "/a/,/x/\n/b/,/y/";

        var result = await _sut.BulkImportAsync(csv);

        Assert.Equal(2, result.Imported);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task BulkImportAsync_SkipsExistingByDefault()
    {
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/dup/", To = "/old-target/" });

        const string csv = "from_url,to_url\n/dup/,/new-target/\n/fresh/,/dest/";
        var result = await _sut.BulkImportAsync(csv);

        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Updated);
        Assert.Equal(1, result.Skipped);

        var existing = await _sut.GetByFromAsync("/dup/");
        Assert.Equal("/old-target/", existing!.To);
    }

    [Fact]
    public async Task BulkImportAsync_OverwriteUpdatesExisting()
    {
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/dup/", To = "/old-target/" });

        const string csv = "/dup/,/new-target/";
        var result = await _sut.BulkImportAsync(csv, overwriteExisting: true);

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Skipped);

        var updated = await _sut.GetByFromAsync("/dup/");
        Assert.Equal("/new-target/", updated!.To);
    }

    [Fact]
    public async Task BulkImportAsync_RecordsErrorsForBadRows()
    {
        const string csv = "from_url,to_url\n/ok/,/dest/\n/no-target/,\nonlyone\n,/dangling/";

        var result = await _sut.BulkImportAsync(csv);

        Assert.Equal(1, result.Imported);
        Assert.True(result.Errors.Count >= 2);
    }

    [Fact]
    public async Task BulkImportAsync_DeduplicatesWithinFile()
    {
        const string csv = "from_url,to_url\n/same/,/a/\n/same/,/b/";

        var result = await _sut.BulkImportAsync(csv);

        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.Skipped);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task BulkImportAsync_EmptyContent_ReturnsZero()
    {
        var result = await _sut.BulkImportAsync("");

        Assert.Equal(0, result.Imported);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Skipped);
        Assert.Empty(result.Errors);
    }

    // ── DetectChainAsync ────────────────────────────────────────

    [Fact]
    public async Task DetectChainAsync_NoExistingTarget_ReturnsNull()
    {
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/x/", To = "/y/" });

        var result = await _sut.DetectChainAsync("/a/", "/b/");

        Assert.Null(result);
    }

    [Fact]
    public async Task DetectChainAsync_SingleHopChain_ReturnsSuggestedTarget()
    {
        // Existing: B → C
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/b/", To = "/c/" });

        // Proposed: A → B
        var result = await _sut.DetectChainAsync("/a/", "/b/");

        Assert.NotNull(result);
        Assert.Equal("/b/", result.IntermediateFrom);
        Assert.Equal("/c/", result.IntermediateTo);
        Assert.Equal("/c/", result.SuggestedTarget);
        Assert.Equal(1, result.HopCount);
        Assert.False(result.CycleDetected);
    }

    [Fact]
    public async Task DetectChainAsync_MultiHopChain_FlattensToTerminalTarget()
    {
        // Existing: B → C, C → D, D → E
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/b/", To = "/c/" });
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/c/", To = "/d/" });
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/d/", To = "/e/" });

        // Proposed: A → B
        var result = await _sut.DetectChainAsync("/a/", "/b/");

        Assert.NotNull(result);
        Assert.Equal("/e/", result.SuggestedTarget);
        Assert.Equal(3, result.HopCount);
        Assert.False(result.CycleDetected);
    }

    [Fact]
    public async Task DetectChainAsync_CycleAmongExistingRedirects_FlagsCycle()
    {
        // Existing: B → C, C → B (loop between two existing rows)
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/b/", To = "/c/" });
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/c/", To = "/b/" });

        var result = await _sut.DetectChainAsync("/a/", "/b/");

        Assert.NotNull(result);
        Assert.True(result.CycleDetected);
    }

    [Fact]
    public async Task DetectChainAsync_ChainLeadsBackToProposedSource_FlagsCycle()
    {
        // Existing: B → A (would form A → B → A)
        await _sut.CreateAsync(new CreateRedirectRequest { From = "/b/", To = "/a/" });

        var result = await _sut.DetectChainAsync("/a/", "/b/");

        Assert.NotNull(result);
        Assert.True(result.CycleDetected);
    }

    [Fact]
    public async Task DetectChainAsync_RegexTargetIgnored()
    {
        // A regex redirect whose pattern equals the proposed target should
        // not be considered for chain detection.
        await _sut.CreateAsync(new CreateRedirectRequest
        {
            From = "/b/",
            To = "/c/",
            IsRegex = true,
        });

        var result = await _sut.DetectChainAsync("/a/", "/b/");

        Assert.Null(result);
    }

    [Fact]
    public async Task DetectChainAsync_ExcludesEditedRedirect()
    {
        // Editing the existing redirect itself should not detect a chain
        // against its own previous target.
        var existing = await _sut.CreateAsync(new CreateRedirectRequest { From = "/b/", To = "/c/" });

        var result = await _sut.DetectChainAsync("/x/", "/b/", excludeId: existing.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task DetectChainAsync_EmptyInputs_ReturnsNull()
    {
        Assert.Null(await _sut.DetectChainAsync("", "/x/"));
        Assert.Null(await _sut.DetectChainAsync("/x/", ""));
    }

    // ================================================================
    // Fake in-memory implementation
    // ================================================================

    private sealed class FakeRedirectService : IRedirectService
    {
        private readonly List<Redirect> _redirects = [];

        public Task<List<Redirect>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_redirects
                .OrderByDescending(r => r.CreatedAt)
                .ToList());
        }

        public Task<Redirect?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return Task.FromResult(_redirects.FirstOrDefault(r => r.Id == id));
        }

        public Task<Redirect?> GetByFromAsync(string fromUrl, CancellationToken ct = default)
        {
            return Task.FromResult(_redirects.FirstOrDefault(r => r.From == fromUrl));
        }

        public Task<RedirectMatch?> MatchAsync(string path, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(path))
                return Task.FromResult<RedirectMatch?>(null);

            var exact = _redirects.FirstOrDefault(r => !r.IsRegex && r.From == path);
            if (exact is not null)
                return Task.FromResult<RedirectMatch?>(
                    new RedirectMatch { Id = exact.Id, Target = exact.To });

            foreach (var r in _redirects.Where(r => r.IsRegex).OrderBy(r => r.CreatedAt))
            {
                try
                {
                    var rx = new Regex(r.From, RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(100));
                    var match = rx.Match(path);
                    if (!match.Success) continue;
                    return Task.FromResult<RedirectMatch?>(new RedirectMatch
                    {
                        Id = r.Id,
                        Target = match.Result(r.To),
                    });
                }
                catch (RegexMatchTimeoutException) { }
                catch (ArgumentException) { }
            }

            return Task.FromResult<RedirectMatch?>(null);
        }

        public Task<Redirect> CreateAsync(
            CreateRedirectRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var redirect = new Redirect
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                From = request.From.Trim(),
                To = request.To.Trim(),
                IsRegex = request.IsRegex,
                CreatedAt = now,
                UpdatedAt = now,
            };

            if (redirect.IsRegex)
            {
                try { _ = new Regex(redirect.From); }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException(
                        $"Invalid regex pattern: {ex.Message}", ex);
                }
            }

            _redirects.Add(redirect);
            return Task.FromResult(redirect);
        }

        public Task UpdateAsync(
            string id, UpdateRedirectRequest request, CancellationToken ct = default)
        {
            var redirect = _redirects.FirstOrDefault(r => r.Id == id)
                ?? throw new InvalidOperationException($"Redirect {id} not found");

            if (request.From is not null)
                redirect.From = request.From.Trim();

            if (request.To is not null)
                redirect.To = request.To.Trim();

            if (request.IsRegex.HasValue)
                redirect.IsRegex = request.IsRegex.Value;

            if (redirect.IsRegex)
            {
                try { _ = new Regex(redirect.From); }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException(
                        $"Invalid regex pattern: {ex.Message}", ex);
                }
            }

            redirect.UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var redirect = _redirects.FirstOrDefault(r => r.Id == id)
                ?? throw new InvalidOperationException($"Redirect {id} not found");

            _redirects.Remove(redirect);
            return Task.CompletedTask;
        }

        public Task IncrementHitCountAsync(string id, CancellationToken ct = default)
        {
            var redirect = _redirects.FirstOrDefault(r => r.Id == id);
            if (redirect is not null)
                redirect.HitCount++;
            return Task.CompletedTask;
        }

        public Task<RedirectChainInfo?> DetectChainAsync(
            string from, string to, string? excludeId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return Task.FromResult<RedirectChainInfo?>(null);

            var fromTrim = from.Trim();
            var toTrim = to.Trim();

            var firstHop = _redirects.FirstOrDefault(
                r => !r.IsRegex && r.From == toTrim && (excludeId == null || r.Id != excludeId));
            if (firstHop is null)
                return Task.FromResult<RedirectChainInfo?>(null);

            var visited = new HashSet<string>(StringComparer.Ordinal) { fromTrim, firstHop.From };
            var currentTarget = firstHop.To;
            var hops = 1;
            var cycleDetected = false;

            while (hops < 10)
            {
                if (visited.Contains(currentTarget))
                {
                    cycleDetected = true;
                    break;
                }

                var next = _redirects.FirstOrDefault(
                    r => !r.IsRegex && r.From == currentTarget && (excludeId == null || r.Id != excludeId));
                if (next is null) break;

                visited.Add(next.From);
                currentTarget = next.To;
                hops++;
            }

            return Task.FromResult<RedirectChainInfo?>(new RedirectChainInfo
            {
                IntermediateFrom = firstHop.From,
                IntermediateTo = firstHop.To,
                SuggestedTarget = currentTarget,
                HopCount = hops,
                CycleDetected = cycleDetected,
            });
        }

        public Task<BulkImportRedirectsResult> BulkImportAsync(
            string csvContent, bool overwriteExisting = false, CancellationToken ct = default)
        {
            var imported = 0;
            var updated = 0;
            var skipped = 0;
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(csvContent))
                return Task.FromResult(new BulkImportRedirectsResult());

            var lines = csvContent.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var checkedHeader = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = line.Split(',');

                if (!checkedHeader)
                {
                    checkedHeader = true;
                    var f0 = fields.Length > 0 ? fields[0].Trim().ToLowerInvariant() : "";
                    var f1 = fields.Length > 1 ? fields[1].Trim().ToLowerInvariant() : "";
                    if (IsHeader(f0) && IsHeader(f1))
                        continue;
                }

                if (fields.Length < 2)
                {
                    errors.Add($"Line {lineNumber}: expected 2 columns (from_url, to_url), got {fields.Length}.");
                    continue;
                }

                var fromTrim = fields[0].Trim();
                var toTrim = fields[1].Trim();

                if (string.IsNullOrEmpty(fromTrim) || string.IsNullOrEmpty(toTrim))
                {
                    errors.Add($"Line {lineNumber}: from and to are required.");
                    skipped++;
                    continue;
                }

                if (!seen.Add(fromTrim))
                {
                    errors.Add($"Line {lineNumber}: duplicate 'from' value '{fromTrim}' in import — skipped.");
                    skipped++;
                    continue;
                }

                var existing = _redirects.FirstOrDefault(r => r.From == fromTrim);
                if (existing is not null)
                {
                    if (overwriteExisting && existing.To != toTrim)
                    {
                        existing.To = toTrim;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                    continue;
                }

                _redirects.Add(new Redirect
                {
                    Id = Guid.NewGuid().ToString("N")[..24],
                    From = fromTrim,
                    To = toTrim,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
                imported++;
            }

            return Task.FromResult(new BulkImportRedirectsResult
            {
                Imported = imported,
                Updated = updated,
                Skipped = skipped,
                Errors = errors,
            });
        }

        private static bool IsHeader(string field) =>
            field is "from" or "from_url" or "fromurl" or "source"
                or "to" or "to_url" or "tourl" or "destination" or "target";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
