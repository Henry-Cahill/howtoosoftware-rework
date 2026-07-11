using System.Security.Cryptography;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace HowToSoftware.Core.Services;

public class SuppressionService(
    IEmailRepository emailRepository,
    IMemberRepository memberRepository,
    ILogger<SuppressionService> logger) : ISuppressionService
{
    public async Task HandleBounceAsync(string emailAddress, string? emailId, CancellationToken ct = default)
    {
        logger.LogInformation("Processing bounce event");

        await SuppressEmailAsync(emailAddress, emailId, "bounce", ct);
        await DisableMemberEmailAsync(emailAddress, ct);
    }

    public async Task HandleSpamComplaintAsync(string emailAddress, string? emailId, CancellationToken ct = default)
    {
        logger.LogInformation("Processing spam complaint event");

        await SuppressEmailAsync(emailAddress, emailId, "spam", ct);
        await RecordSpamComplaintAsync(emailAddress, emailId, ct);
        await DisableMemberEmailAsync(emailAddress, ct);
    }

    public async Task RemoveSuppressionAsync(string emailAddress, CancellationToken ct = default)
    {
        logger.LogInformation("Removing suppression");

        await emailRepository.RemoveSuppressionAsync(emailAddress, ct);

        var member = await memberRepository.GetByEmailAsync(emailAddress, ct);
        if (member is not null)
        {
            member.EmailDisabled = false;
            member.UpdatedAt = DateTime.UtcNow;
            await memberRepository.UpdateAsync(member, ct);
        }
    }

    private async Task SuppressEmailAsync(string emailAddress, string? emailId, string reason, CancellationToken ct)
    {
        if (await emailRepository.IsEmailSuppressedAsync(emailAddress, ct))
        {
            logger.LogDebug("Email {EmailHash} is already suppressed, skipping", LogSanitizer.MaskEmail(emailAddress));
            return;
        }

        var suppression = new Suppression
        {
            Id = GenerateId(),
            Email = emailAddress,
            EmailId = emailId,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
        };

        await emailRepository.AddSuppressionAsync(suppression, ct);
    }

    private async Task DisableMemberEmailAsync(string emailAddress, CancellationToken ct)
    {
        var member = await memberRepository.GetByEmailAsync(emailAddress, ct);
        if (member is null)
        {
            logger.LogWarning("No member found for suppressed email");
            return;
        }

        if (member.EmailDisabled)
            return;

        member.EmailDisabled = true;
        member.UpdatedAt = DateTime.UtcNow;
        await memberRepository.UpdateAsync(member, ct);

        logger.LogInformation("Disabled email for member {MemberId}", member.Id);
    }

    private async Task RecordSpamComplaintAsync(string emailAddress, string? emailId, CancellationToken ct)
    {
        var member = await memberRepository.GetByEmailAsync(emailAddress, ct);
        if (member is null || emailId is null)
            return;

        var evt = new EmailSpamComplaintEvent
        {
            Id = GenerateId(),
            MemberId = member.Id,
            EmailId = emailId,
            EmailAddress = emailAddress,
            CreatedAt = DateTime.UtcNow,
        };

        await emailRepository.AddSpamComplaintEventAsync(evt, ct);
    }

    private static string GenerateId()
    {
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
