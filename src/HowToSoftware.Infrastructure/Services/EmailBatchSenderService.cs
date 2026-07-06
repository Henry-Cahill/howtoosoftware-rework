using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Services;

/// <summary>
/// Background service that polls for Email records with status "pending"
/// (created by <see cref="IContentService.SendAsEmailAsync"/>) and processes
/// them via <see cref="INewsletterService.ProcessPendingEmailAsync"/>:
/// segment members → batch (500/batch) → send via Mailgun → update status.
/// </summary>
public sealed class EmailBatchSenderService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EmailBatchSenderService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEmailsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email batch sender encountered an error");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var newsletterService = scope.ServiceProvider.GetRequiredService<INewsletterService>();

        var pendingEmails = await emailRepository.GetPendingEmailsAsync(ct);

        if (pendingEmails.Count == 0)
            return;

        var siteUrl = configuration["Site:Url"] ?? "https://localhost";

        logger.LogInformation("Found {Count} pending email(s) to process", pendingEmails.Count);

        foreach (var email in pendingEmails)
        {
            try
            {
                logger.LogInformation("Processing email {EmailId} for post {PostId}",
                    email.Id, email.PostId);

                await newsletterService.ProcessPendingEmailAsync(email.Id, siteUrl, ct);

                logger.LogInformation("Successfully processed email {EmailId}", email.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process email {EmailId}", email.Id);

                // Mark as failed so we don't retry indefinitely
                email.Status = "failed";
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
