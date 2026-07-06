using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace HowToSoftware.Infrastructure.Services;

public sealed class AdminAuditService : IAdminAuditService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminAuditService> _logger;

    public AdminAuditService(AppDbContext db, ILogger<AdminAuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(AdminAuditEntry entry, CancellationToken ct = default)
    {
        try
        {
            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                Id = Guid.NewGuid().ToString("D"),
                AdminUserId = entry.AdminUserId,
                AdminUserEmail = entry.AdminUserEmail,
                Action = entry.Action,
                TargetType = entry.TargetType,
                TargetId = entry.TargetId,
                Metadata = entry.Metadata,
                IpAddress = entry.IpAddress,
                UserAgent = Truncate(entry.UserAgent, 512),
                CreatedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Audit recording must never block the underlying action.
            _logger.LogWarning(ex, "Failed to record admin audit entry {Action} by {AdminUserId}",
                entry.Action, entry.AdminUserId);
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (value is null) return null;
        return value.Length <= max ? value : value[..max];
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
