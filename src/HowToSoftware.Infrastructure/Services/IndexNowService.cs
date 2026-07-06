using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Services;

/// <summary>
/// Singleton channel that buffers URLs for IndexNow submission.
/// </summary>
internal sealed class IndexNowChannel
{
    private readonly Channel<string> _channel =
        Channel.CreateBounded<string>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    public ChannelWriter<string> Writer => _channel.Writer;
    public ChannelReader<string> Reader => _channel.Reader;
}

/// <summary>
/// Enqueues URLs into the IndexNow background channel (non-blocking).
/// </summary>
internal sealed class IndexNowService(
    IndexNowChannel channel,
    ILogger<IndexNowService> logger) : IIndexNowService
{
    public void Enqueue(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!channel.Writer.TryWrite(url))
        {
            logger.LogWarning("IndexNow channel is full; dropping URL {Url}", url);
        }
    }
}

/// <summary>
/// Background service that reads URLs from the channel and submits them
/// to search engines via the IndexNow protocol.
/// IndexNow spec: https://www.indexnow.org/documentation
/// </summary>
internal sealed class IndexNowBackgroundService(
    IndexNowChannel channel,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<IndexNowBackgroundService> logger) : BackgroundService
{
    // IndexNow search engines to notify (round-robin)
    private static readonly string[] SearchEngines =
    [
        "api.indexnow.org",
        "www.bing.com",
        "yandex.com",
    ];

    // Settings keys stored via ISettingsService
    internal const string SettingsKeyEnabled = "indexnow_enabled";
    internal const string SettingsKeyApiKey = "indexnow_api_key";
    internal const string SettingsKeySiteUrl = "indexnow_site_url";

    // Batch URLs together to reduce HTTP calls (IndexNow supports batch submission)
    private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(5);
    private const int MaxBatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IndexNow background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var urls = await CollectBatchAsync(stoppingToken);
                if (urls.Count > 0)
                {
                    await SubmitBatchAsync(urls, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in IndexNow background service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task<List<string>> CollectBatchAsync(CancellationToken ct)
    {
        var urls = new List<string>();

        // Wait for the first URL
        var firstUrl = await channel.Reader.ReadAsync(ct);
        urls.Add(firstUrl);

        // Collect more URLs within the batch window
        using var batchCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        batchCts.CancelAfter(BatchDelay);

        try
        {
            while (urls.Count < MaxBatchSize)
            {
                var url = await channel.Reader.ReadAsync(batchCts.Token);
                urls.Add(url);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Batch window expired — proceed with what we have
        }

        // Deduplicate
        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task SubmitBatchAsync(List<string> urls, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

        var enabled = await settingsService.GetBoolAsync(SettingsKeyEnabled, ct);
        if (enabled != true)
        {
            logger.LogDebug("IndexNow is disabled; skipping {Count} URL(s)", urls.Count);
            return;
        }

        var apiKey = await settingsService.GetStringAsync(SettingsKeyApiKey, ct);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("IndexNow API key is not configured; skipping {Count} URL(s)", urls.Count);
            return;
        }

        var siteUrl = await settingsService.GetStringAsync(SettingsKeySiteUrl, ct);
        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            logger.LogWarning("IndexNow site URL is not configured; skipping {Count} URL(s)", urls.Count);
            return;
        }

        // Normalize: ensure no trailing slash on host
        var host = siteUrl.TrimEnd('/');
        var hostUri = new Uri(host);

        var client = httpClientFactory.CreateClient("IndexNow");

        // Use the first search engine (round-robin could be added later)
        var searchEngine = SearchEngines[0];

        if (urls.Count == 1)
        {
            // Single URL — use GET endpoint
            await SubmitSingleAsync(client, searchEngine, hostUri.Host, apiKey, urls[0], ct);
        }
        else
        {
            // Multiple URLs — use POST batch endpoint
            await SubmitBatchPostAsync(client, searchEngine, hostUri.Host, apiKey, urls, ct);
        }
    }

    private async Task SubmitSingleAsync(
        HttpClient client, string searchEngine, string host,
        string apiKey, string url, CancellationToken ct)
    {
        var requestUri = $"https://{searchEngine}/indexnow?url={Uri.EscapeDataString(url)}&key={Uri.EscapeDataString(apiKey)}";

        try
        {
            using var response = await client.GetAsync(requestUri, ct);
            logger.LogInformation(
                "IndexNow submitted {Url} to {Engine} — {Status}",
                url, searchEngine, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "IndexNow submission failed for {Url} to {Engine}", url, searchEngine);
        }
    }

    private async Task SubmitBatchPostAsync(
        HttpClient client, string searchEngine, string host,
        string apiKey, List<string> urls, CancellationToken ct)
    {
        var requestUri = $"https://{searchEngine}/indexnow";

        var payload = new
        {
            host,
            key = apiKey,
            urlList = urls,
        };

        try
        {
            using var response = await client.PostAsJsonAsync(requestUri, payload, ct);
            logger.LogInformation(
                "IndexNow batch submitted {Count} URL(s) to {Engine} — {Status}",
                urls.Count, searchEngine, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "IndexNow batch submission failed for {Count} URL(s) to {Engine}", urls.Count, searchEngine);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
