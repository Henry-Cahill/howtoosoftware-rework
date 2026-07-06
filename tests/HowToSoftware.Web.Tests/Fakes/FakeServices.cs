using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using System.Net;

namespace HowToSoftware.Web.Tests.Fakes;

internal class FakeAnalyticsRepository : IAnalyticsRepository
{
    public List<AnalyticsEvent> Events { get; } = [];

    public Task AddEventAsync(AnalyticsEvent analyticsEvent, CancellationToken ct = default)
    { Events.Add(analyticsEvent); return Task.CompletedTask; }

    public Task AddEventsAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default)
    { Events.AddRange(events); return Task.CompletedTask; }

    public Task<PagedResult<AnalyticsEvent>> GetEventsAsync(DateTime from, DateTime to, int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<AnalyticsEvent> { Items = [], Page = page, PageSize = pageSize, TotalCount = 0 });

    public Task<int> GetVisitorCountAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> GetVisitCountAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> GetPageviewCountAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(0);
    public Task<decimal> GetBounceRateAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(0m);
    public Task<decimal> GetAvgSessionDurationAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(0m);
    public Task<AnalyticsKpiSummary> GetKpiSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<(string PageUrl, int Views)>> GetTopPagesAsync(DateTime from, DateTime to, int count, CancellationToken ct = default) => Task.FromResult(new List<(string, int)>());
    public Task<List<(string Source, int Visits)>> GetTopSourcesAsync(DateTime from, DateTime to, int count, CancellationToken ct = default) => Task.FromResult(new List<(string, int)>());
    public Task<List<(string Device, int Count)>> GetDeviceBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(new List<(string, int)>());
    public Task<List<(string Browser, int Count)>> GetBrowserBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(new List<(string, int)>());
    public Task<List<(string Os, int Count)>> GetOsBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(new List<(string, int)>());
    public Task<List<(string Referrer, int Count)>> GetReferrerBreakdownAsync(DateTime from, DateTime to, int count, CancellationToken ct = default) => Task.FromResult(new List<(string, int)>());
    public Task<List<(string Country, int Count)>> GetCountryBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(new List<(string, int)>());
}

internal class FakeGeoIpService : IGeoIpService
{
    public Dictionary<string, string> Lookups { get; } = [];

    public string? LookupCountry(IPAddress ipAddress)
        => Lookups.GetValueOrDefault(ipAddress.ToString());
}

internal class FakeContentGatingService : IContentGatingService
{
    public ContentAccessLevel DefaultLevel { get; set; } = ContentAccessLevel.Full;

    public Task<ContentAccessLevel> CheckAccessAsync(Post post, string? memberId, CancellationToken ct = default)
        => Task.FromResult(DefaultLevel);
}

internal class FakeLexicalRenderer : ILexicalRenderer
{
    public string Render(string lexicalJson) => $"[lexical:{lexicalJson}]";
}

internal class FakeMobiledocRenderer : IMobiledocRenderer
{
    public string Render(string mobiledocJson) => $"[mobiledoc:{mobiledocJson}]";
}

internal class FakeContentSanitizer : IContentSanitizer
{
    public string Sanitize(string html) => html;
}

internal class FakeApiKeyRepository : IApiKeyRepository
{
    public List<ApiKey> ApiKeys { get; } = [];

    public Task<ApiKey?> GetBySecretAsync(string secret, CancellationToken ct = default)
        => Task.FromResult(ApiKeys.FirstOrDefault(k => k.Secret == secret));
}

internal class FakeMentionService : IMentionService
{
    public List<Mention> Mentions { get; } = [];

    public Task<List<Mention>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Mentions.Where(m => !m.Deleted).OrderByDescending(m => m.CreatedAt).ToList());

    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
        => Task.FromResult(Mentions.Count(m => !m.Deleted && m.Status == MentionStatus.Pending));

    public Task<Mention?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(Mentions.FirstOrDefault(m => m.Id == id));

    public Task<List<Mention>> GetByTargetAsync(string targetUrl, CancellationToken ct = default)
        => Task.FromResult(Mentions.Where(m => m.Target == targetUrl && m.Verified && !m.Deleted && m.Status == MentionStatus.Approved).ToList());

    public Task<List<Mention>> GetByResourceAsync(string resourceId, string resourceType, CancellationToken ct = default)
        => Task.FromResult(Mentions.Where(m => m.ResourceId == resourceId && m.ResourceType == resourceType && m.Verified && !m.Deleted && m.Status == MentionStatus.Approved).ToList());

    public Task<Mention> ReceiveAsync(string source, string target, CancellationToken ct = default)
    {
        var mention = new Mention { Id = Guid.NewGuid().ToString("N")[..24], Source = source, Target = target, CreatedAt = DateTime.UtcNow, Status = MentionStatus.Pending };
        Mentions.Add(mention);
        return Task.FromResult(mention);
    }

    public Task VerifyAsync(string id, CancellationToken ct = default) => Task.CompletedTask;

    public Task ApproveAsync(string id, CancellationToken ct = default)
    {
        var m = Mentions.FirstOrDefault(m => m.Id == id);
        if (m is not null) m.Status = MentionStatus.Approved;
        return Task.CompletedTask;
    }

    public Task RejectAsync(string id, CancellationToken ct = default)
    {
        var m = Mentions.FirstOrDefault(m => m.Id == id);
        if (m is not null) m.Status = MentionStatus.Rejected;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var m = Mentions.FirstOrDefault(m => m.Id == id);
        if (m is not null) m.Deleted = true;
        return Task.CompletedTask;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
