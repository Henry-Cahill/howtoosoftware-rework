using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HowToSoftware.Infrastructure.Services;

public sealed class MemberImpersonationService : IMemberImpersonationService
{
    private const string TokenType = "admin-impersonate";

    private readonly AppDbContext _db;
    private readonly IMemberRepository _members;
    private readonly ILogger<MemberImpersonationService> _logger;

    public MemberImpersonationService(
        AppDbContext db,
        IMemberRepository members,
        ILogger<MemberImpersonationService> logger)
    {
        _db = db;
        _members = members;
        _logger = logger;
    }

    public async Task<string> CreateTokenAsync(
        string memberId,
        string adminUserId,
        string? adminUserEmail,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(memberId))
            throw new ArgumentException("memberId is required", nameof(memberId));
        if (string.IsNullOrWhiteSpace(adminUserId))
            throw new ArgumentException("adminUserId is required", nameof(adminUserId));

        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new InvalidOperationException($"Member {memberId} not found");

        var rawToken = GenerateToken();
        var hashedToken = HashToken(rawToken);

        var data = JsonSerializer.Serialize(new ImpersonationTokenData
        {
            Type = TokenType,
            MemberId = member.Id,
            AdminUserId = adminUserId,
            AdminUserEmail = adminUserEmail,
        });

        _db.Tokens.Add(new Token
        {
            Id = ObjectIdGenerator.New(),
            TokenValue = hashedToken,
            Uuid = Guid.NewGuid().ToString("D"),
            Data = data,
            CreatedAt = DateTime.UtcNow,
            UsedCount = 0,
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Impersonation token issued for member {MemberId} by admin {AdminUserId}",
            member.Id, adminUserId);

        return rawToken;
    }

    public async Task<ImpersonationVerifyResult?> VerifyAndConsumeAsync(
        string rawToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;

        var hashedToken = HashToken(rawToken);

        var token = await _db.Tokens
            .FirstOrDefaultAsync(t => t.TokenValue == hashedToken, ct);

        if (token is null)
        {
            _logger.LogWarning("Impersonation verify failed: token not found");
            return null;
        }

        if (DateTime.UtcNow - token.CreatedAt > IMemberImpersonationService.TokenLifetime)
        {
            _logger.LogWarning("Impersonation verify failed: token expired (created {CreatedAt})", token.CreatedAt);
            return null;
        }

        if (token.UsedCount > 0)
        {
            _logger.LogWarning("Impersonation verify failed: token already used");
            return null;
        }

        ImpersonationTokenData? data = null;
        if (!string.IsNullOrEmpty(token.Data))
        {
            try { data = JsonSerializer.Deserialize<ImpersonationTokenData>(token.Data); }
            catch { /* fall through */ }
        }

        if (data is null || data.Type != TokenType || string.IsNullOrEmpty(data.MemberId) || string.IsNullOrEmpty(data.AdminUserId))
        {
            _logger.LogWarning("Impersonation verify failed: token payload invalid");
            return null;
        }

        // Consume the token before resolving the member so a race can't double-spend it.
        token.UsedCount++;
        token.FirstUsedAt ??= DateTime.UtcNow;
        token.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var member = await _members.GetByIdAsync(data.MemberId, ct);
        if (member is null)
        {
            _logger.LogWarning("Impersonation verify failed: member {MemberId} not found", data.MemberId);
            return null;
        }

        return new ImpersonationVerifyResult(member, data.AdminUserId, data.AdminUserEmail);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashToken(string tokenValue)
    {
        var bytes = Encoding.UTF8.GetBytes(tokenValue);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class ImpersonationTokenData
    {
        public string? Type { get; set; }
        public string? MemberId { get; set; }
        public string? AdminUserId { get; set; }
        public string? AdminUserEmail { get; set; }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
