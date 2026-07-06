using System.Security.Cryptography;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Services;

public class IntegrationService(Data.AppDbContext db, ISlugGenerator slugGenerator) : IIntegrationService
{
    public async Task<List<Integration>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Integrations
            .Include(i => i.ApiKeys)
            .Include(i => i.Webhooks)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
    }

    public async Task<Integration?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Integrations
            .Include(i => i.ApiKeys)
            .Include(i => i.Webhooks)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<Integration> CreateAsync(string name, string? description, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var slug = await slugGenerator.GenerateUniqueSlugAsync(
            name,
            async s => await db.Integrations.AnyAsync(i => i.Slug == s, ct),
            ct);

        var integration = new Integration
        {
            Id = GenerateId(),
            Name = name,
            Slug = slug,
            Description = description,
            Type = "custom",
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Create Content API key
        var contentKey = new ApiKey
        {
            Id = GenerateId(),
            Type = "content",
            Secret = GenerateApiKeySecret(),
            IntegrationId = integration.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Create Admin API key
        var adminKey = new ApiKey
        {
            Id = GenerateId(),
            Type = "admin",
            Secret = GenerateApiKeySecret(),
            IntegrationId = integration.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Integrations.Add(integration);
        db.ApiKeys.Add(contentKey);
        db.ApiKeys.Add(adminKey);
        await db.SaveChangesAsync(ct);

        // Reload with navigation properties
        return (await GetByIdAsync(integration.Id, ct))!;
    }

    public async Task UpdateAsync(Integration integration, CancellationToken ct = default)
    {
        integration.UpdatedAt = DateTime.UtcNow;
        db.Integrations.Update(integration);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        // Webhooks cascade-delete; API keys set null — remove them explicitly
        await db.ApiKeys.Where(k => k.IntegrationId == id).ExecuteDeleteAsync(ct);
        await db.Integrations.Where(i => i.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<ApiKey> RegenerateApiKeyAsync(string apiKeyId, CancellationToken ct = default)
    {
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId, ct)
            ?? throw new InvalidOperationException("API key not found.");

        key.Secret = GenerateApiKeySecret();
        key.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return key;
    }

    public async Task<Webhook> AddWebhookAsync(string integrationId, string name, string eventName, string targetUrl, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var webhook = new Webhook
        {
            Id = GenerateId(),
            IntegrationId = integrationId,
            Name = name,
            Event = eventName,
            TargetUrl = targetUrl,
            Secret = GenerateWebhookSecret(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Webhooks.Add(webhook);
        await db.SaveChangesAsync(ct);
        return webhook;
    }

    public async Task UpdateWebhookAsync(Webhook webhook, CancellationToken ct = default)
    {
        webhook.UpdatedAt = DateTime.UtcNow;
        db.Webhooks.Update(webhook);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteWebhookAsync(string webhookId, CancellationToken ct = default)
    {
        await db.Webhooks.Where(w => w.Id == webhookId).ExecuteDeleteAsync(ct);
    }

    private static string GenerateId()
    {
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateApiKeySecret()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateWebhookSecret()
    {
        Span<byte> bytes = stackalloc byte[20];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
