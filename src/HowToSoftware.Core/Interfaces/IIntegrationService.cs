using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IIntegrationService
{
    Task<List<Integration>> GetAllAsync(CancellationToken ct = default);
    Task<Integration?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Integration> CreateAsync(string name, string? description, CancellationToken ct = default);
    Task UpdateAsync(Integration integration, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<ApiKey> RegenerateApiKeyAsync(string apiKeyId, CancellationToken ct = default);
    Task<Webhook> AddWebhookAsync(string integrationId, string name, string eventName, string targetUrl, CancellationToken ct = default);
    Task UpdateWebhookAsync(Webhook webhook, CancellationToken ct = default);
    Task DeleteWebhookAsync(string webhookId, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
