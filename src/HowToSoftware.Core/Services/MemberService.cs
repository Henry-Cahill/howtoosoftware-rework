using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Core.Services;

public sealed class MemberService : IMemberService
{
    private readonly IMemberRepository _members;
    private readonly INewsletterRepository _newsletters;
    private readonly INewsletterService _newsletterService;
    private readonly IAutomatedEmailService _automatedEmails;
    private readonly ISettingsService _settings;
    private readonly IWebhookDispatchService _webhookDispatch;
    private readonly ILogger<MemberService> _logger;

    public MemberService(
        IMemberRepository members,
        INewsletterRepository newsletters,
        INewsletterService newsletterService,
        IAutomatedEmailService automatedEmails,
        ISettingsService settings,
        IWebhookDispatchService webhookDispatch,
        ILogger<MemberService> logger)
    {
        _members = members;
        _newsletters = newsletters;
        _newsletterService = newsletterService;
        _automatedEmails = automatedEmails;
        _settings = settings;
        _webhookDispatch = webhookDispatch;
        _logger = logger;
    }

    public async Task<Member?> SignupAsync(MemberSignupRequest request, CancellationToken ct = default)
    {
        // Check for existing member
        var existing = await _members.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
        {
            _logger.LogInformation("Signup attempt for existing email (suppressed)");
            return null;
        }

        var now = DateTime.UtcNow;
        var memberId = ObjectIdGenerator.New();

        var member = new Member
        {
            Id = memberId,
            Uuid = Guid.NewGuid().ToString("D"),
            TransientId = ObjectIdGenerator.New(),
            Email = request.Email,
            Name = request.Name,
            Status = "free",
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _members.AddAsync(member, ct);

        // Determine which newsletters to subscribe to
        var newslettersToSubscribe = await GetSignupNewslettersAsync(request.NewsletterIds, ct);

        foreach (var newsletter in newslettersToSubscribe)
        {
            await _newsletterService.SubscribeAsync(memberId, newsletter.Id, ct);
        }

        // Record events (created, status, subscribe)
        await RecordSignupEventsAsync(member, newslettersToSubscribe, ct);

        // Send welcome email via automated email service
        await _automatedEmails.SendAsync("welcome", member, request.SiteUrl, ct);

        _logger.LogInformation("New member signed up: {MemberId} ({Email})", memberId, LogSanitizer.SanitizeForLog(request.Email));

        _webhookDispatch.Enqueue("member.added", new { id = member.Id, email = member.Email, name = member.Name, status = member.Status });

        return member;
    }

    private async Task<List<Newsletter>> GetSignupNewslettersAsync(
        List<string>? requestedIds, CancellationToken ct)
    {
        var activeNewsletters = await _newsletters.GetActiveAsync(ct);

        if (requestedIds is { Count: > 0 })
        {
            var requestedSet = new HashSet<string>(requestedIds, StringComparer.OrdinalIgnoreCase);
            return activeNewsletters
                .Where(n => requestedSet.Contains(n.Id))
                .ToList();
        }

        // Auto-subscribe to all newsletters with SubscribeOnSignup enabled
        return activeNewsletters
            .Where(n => n.SubscribeOnSignup)
            .ToList();
    }

    private async Task RecordSignupEventsAsync(
        Member member, List<Newsletter> newsletters, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // MembersCreatedEvent
        var createdEvent = new MembersCreatedEvent
        {
            Id = ObjectIdGenerator.New(),
            MemberId = member.Id,
            Source = "member",
            CreatedAt = now,
        };

        // MembersStatusEvent (null → free)
        var statusEvent = new MembersStatusEvent
        {
            Id = ObjectIdGenerator.New(),
            MemberId = member.Id,
            FromStatus = null,
            ToStatus = "free",
            CreatedAt = now,
        };

        // MembersSubscribeEvent per newsletter
        var subscribeEvents = newsletters.Select(n => new MembersSubscribeEvent
        {
            Id = ObjectIdGenerator.New(),
            MemberId = member.Id,
            NewsletterId = n.Id,
            Subscribed = true,
            Source = "member",
            CreatedAt = now,
        }).ToList();

        // Use the member repository to save event records
        await _members.AddCreatedEventAsync(createdEvent, ct);
        await _members.AddStatusEventAsync(statusEvent, ct);
        foreach (var evt in subscribeEvents)
        {
            await _members.AddSubscribeEventAsync(evt, ct);
        }
    }

}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
