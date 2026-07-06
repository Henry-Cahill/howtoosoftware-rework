using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

internal sealed record WebhookEvent(string EventName, object Payload);

/// <summary>
/// Singleton channel that buffers webhook events for background dispatch.
/// </summary>
internal sealed class WebhookDispatchChannel
{
    private readonly Channel<WebhookEvent> _channel =
        Channel.CreateBounded<WebhookEvent>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    public ChannelWriter<WebhookEvent> Writer => _channel.Writer;
    public ChannelReader<WebhookEvent> Reader => _channel.Reader;
}

/// <summary>
/// Enqueues webhook events into the background channel (non-blocking).
/// </summary>
internal sealed class WebhookDispatchService(
    WebhookDispatchChannel channel,
    ILogger<WebhookDispatchService> logger) : IWebhookDispatchService
{
    public void Enqueue(string eventName, object payload)
    {
        if (!channel.Writer.TryWrite(new WebhookEvent(eventName, payload)))
        {
            logger.LogWarning("Webhook dispatch channel is full; dropping event {Event}", eventName);
        }
    }
}

/// <summary>
/// Background service that reads from the channel and dispatches HTTP POST requests
/// to all webhooks registered for each event.
/// </summary>
internal sealed class WebhookDispatchBackgroundService(
    WebhookDispatchChannel channel,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<WebhookDispatchBackgroundService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Webhook dispatch background service started");

        await foreach (var evt in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DispatchEventAsync(evt, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error dispatching webhook event {Event}", evt.EventName);
            }
        }
    }

    private async Task DispatchEventAsync(WebhookEvent evt, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var webhooks = await db.Webhooks
            .Where(w => w.Event == evt.EventName)
            .ToListAsync(ct);

        if (webhooks.Count == 0)
            return;

        var bodyJson = JsonSerializer.Serialize(new
        {
            @event = evt.EventName,
            timestamp = DateTime.UtcNow,
            data = evt.Payload,
        }, JsonOptions);

        var client = httpClientFactory.CreateClient("WebhookDispatch");

        foreach (var webhook in webhooks)
        {
            await DispatchSingleAsync(client, db, webhook, bodyJson, ct);
        }
    }

    private async Task DispatchSingleAsync(
        HttpClient client,
        AppDbContext db,
        Core.Entities.Webhook webhook,
        string bodyJson,
        CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, webhook.TargetUrl)
            {
                Content = new StringContent(bodyJson, Encoding.UTF8, "application/json"),
            };

            // Sign with HMAC-SHA256 if a secret is configured
            if (!string.IsNullOrEmpty(webhook.Secret))
            {
                var signature = ComputeSignature(bodyJson, webhook.Secret);
                request.Headers.TryAddWithoutValidation("X-Ghost-Signature", $"sha256={signature}");
            }

            request.Headers.TryAddWithoutValidation("User-Agent", "HowToSoftware-Webhooks/1.0");

            using var response = await client.SendAsync(request, ct);

            webhook.LastTriggeredAt = DateTime.UtcNow;
            webhook.LastTriggeredStatus = ((int)response.StatusCode).ToString();
            webhook.LastTriggeredError = response.IsSuccessStatusCode ? null : response.ReasonPhrase;

            logger.LogInformation(
                "Webhook {WebhookId} dispatched to {Url} — {Status}",
                webhook.Id, webhook.TargetUrl, response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            webhook.LastTriggeredAt = DateTime.UtcNow;
            webhook.LastTriggeredStatus = "error";
            webhook.LastTriggeredError = Truncate(ex.Message, 50);

            logger.LogWarning(ex,
                "Webhook {WebhookId} dispatch to {Url} failed",
                webhook.Id, webhook.TargetUrl);
        }

        try
        {
            webhook.UpdatedAt = DateTime.UtcNow;
            db.Webhooks.Update(webhook);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update webhook {WebhookId} last-triggered status", webhook.Id);
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
