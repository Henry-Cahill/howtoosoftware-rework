using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Services;

/// <summary>
/// Background service that polls for Email records currently in
/// <c>AbTestPhase == "testing"</c> whose wait window has elapsed
/// (<c>AbTestStartedAt + AbTestWaitMinutes &lt;= now</c>) and resolves them via
/// <see cref="INewsletterService.SendAbTestWinnerAsync"/>: pick the variant with
/// more opens, send the holdout cohort with the winning subject, and mark the
/// test completed.
/// </summary>
public sealed class EmailAbTestWinnerService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EmailAbTestWinnerService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ResolveCompletedAbTestsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email A/B winner resolver encountered an error");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ResolveCompletedAbTestsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var newsletterService = scope.ServiceProvider.GetRequiredService<INewsletterService>();

        var due = await emailRepository.GetAbTestsAwaitingWinnerAsync(DateTime.UtcNow, ct);
        if (due.Count == 0)
            return;

        var siteUrl = configuration["Site:Url"] ?? "https://localhost";

        logger.LogInformation("Found {Count} A/B test(s) awaiting winner resolution", due.Count);

        foreach (var email in due)
        {
            try
            {
                logger.LogInformation("Resolving A/B winner for email {EmailId}", email.Id);
                var result = await newsletterService.SendAbTestWinnerAsync(email.Id, siteUrl, ct);
                logger.LogInformation(
                    "Resolved A/B winner for email {EmailId}: variant={Winner} opensA={OpensA} opensB={OpensB}",
                    result.Id, result.AbTestWinnerVariant, result.AbTestOpensA, result.AbTestOpensB);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resolve A/B winner for email {EmailId}", email.Id);

                // Move to completed phase with no winner to prevent infinite retry; the original
                // test cohort sends remain valid, only the holdout was skipped.
                email.AbTestPhase = "completed";
                email.Error = ex.Message;
                email.UpdatedAt = DateTime.UtcNow;
                await emailRepository.UpdateEmailAsync(email, ct);
            }
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
