using System.Threading.RateLimiting;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RazorLight;

namespace HowToSoftware.Infrastructure.Services;

public sealed class MailgunEmailService : IEmailService, IDisposable
{
    private readonly MailSettings _settings;
    private readonly ILogger<MailgunEmailService> _logger;
    private readonly RazorLightEngine _razor;
    private readonly TokenBucketRateLimiter _rateLimiter;

    public MailgunEmailService(
        IOptions<MailSettings> settings,
        ILogger<MailgunEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _razor = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(MailgunEmailService).Assembly, "HowToSoftware.Infrastructure.Services.EmailTemplates")
            .UseMemoryCachingProvider()
            .Build();

        // Rate limiter: allow 1 batch per BatchDelayMs window to respect Mailgun limits
        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(Math.Max(_settings.BatchDelayMs, 100)),
            TokensPerPeriod = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100,
            AutoReplenishment = true,
        });
    }

    public async Task<string> RenderTemplateAsync(
        string templateName,
        EmailTemplateModel model,
        CancellationToken ct = default)
    {
        var cacheKey = $"template_{templateName}";

        var result = await _razor.CompileRenderAsync(templateName, model);
        return result;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            using var smtp = await ConnectAsync(ct);

            var mime = BuildMimeMessage(message.From, message.To, message.Subject,
                message.Html, message.Plaintext, message.ReplyTo);

            await smtp.SendAsync(mime, ct);
            await smtp.DisconnectAsync(quit: true, ct);

            _logger.LogInformation("Email sent subject=\"{Subject}\"",
                LogSanitizer.SanitizeForLog(message.Subject));

            return new EmailSendResult
            {
                RecipientEmail = message.To,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");

            return new EmailSendResult
            {
                RecipientEmail = message.To,
                Success = false,
                ErrorMessage = ex.Message,
            };
        }
    }

    public async Task<List<EmailSendResult>> SendBatchAsync(
        EmailBatchRequest request,
        CancellationToken ct = default)
    {
        var results = new List<EmailSendResult>(request.Recipients.Count);
        var recipientList = request.Recipients.ToList();

        // Process in chunks of BatchSize
        for (var offset = 0; offset < recipientList.Count; offset += _settings.BatchSize)
        {
            ct.ThrowIfCancellationRequested();

            // Rate limit between chunks
            if (offset > 0)
            {
                using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, ct);
                if (!lease.IsAcquired)
                {
                    _logger.LogWarning("Rate limiter queue full at offset {Offset}, pausing", offset);
                    await Task.Delay(_settings.BatchDelayMs, ct);
                }
            }

            var chunk = recipientList.Skip(offset).Take(_settings.BatchSize).ToList();

            _logger.LogInformation(
                "Sending batch chunk {Offset}-{End} of {Total} recipients",
                offset, Math.Min(offset + _settings.BatchSize, recipientList.Count), recipientList.Count);

            var chunkResults = await SendChunkAsync(request, chunk, ct);
            results.AddRange(chunkResults);
        }

        // Tally outcomes with primitive counters so the summary isn't derived
        // from the email-bearing result list (cs/exposure-of-sensitive-information).
        var succeeded = 0;
        var total = 0;
        foreach (var r in results)
        {
            total++;
            if (r.Success)
                succeeded++;
        }
        var failed = total - succeeded;
        _logger.LogInformation("Batch complete: {Succeeded} sent, {Failed} failed out of {Total}",
            succeeded, failed, total);

        return results;
    }

    public void Dispose()
    {
        _rateLimiter.Dispose();
    }

    private async Task<List<EmailSendResult>> SendChunkAsync(
        EmailBatchRequest request,
        List<KeyValuePair<string, Dictionary<string, string>>> chunk,
        CancellationToken ct)
    {
        var results = new List<EmailSendResult>(chunk.Count);

        SmtpClient? smtp = null;
        try
        {
            try
            {
                smtp = await ConnectAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SMTP server for batch chunk");
                results.AddRange(chunk.Select(kv => new EmailSendResult
                {
                    RecipientEmail = kv.Key,
                    Success = false,
                    ErrorMessage = $"SMTP connection failed: {ex.Message}",
                }));
                return results;
            }

            foreach (var (email, variables) in chunk)
            {
                ct.ThrowIfCancellationRequested();

                var attempt = 0;
                EmailSendResult? result = null;

                while (attempt <= _settings.MaxRetries)
                {
                    try
                    {
                        var html = ApplyVariables(request.Html, variables);
                        var plaintext = request.Plaintext != null
                            ? ApplyVariables(request.Plaintext, variables)
                            : null;

                        var mime = BuildMimeMessage(request.From, email, request.Subject,
                            html, plaintext, request.ReplyTo);

                        var response = await smtp.SendAsync(mime, ct);

                        result = new EmailSendResult
                        {
                            RecipientEmail = email,
                            Success = true,
                            ProviderId = response,
                        };
                        break;
                    }
                    catch (SmtpCommandException ex) when (IsTransientError(ex) && attempt < _settings.MaxRetries)
                    {
                        attempt++;
                        _logger.LogWarning(ex, "Transient SMTP error, retry {Attempt}/{Max}",
                            attempt, _settings.MaxRetries);

                        // Reconnect on transient errors
                        smtp.Dispose();
                        smtp = await ConnectAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email after {Attempts} attempts",
                            attempt + 1);

                        result = new EmailSendResult
                        {
                            RecipientEmail = email,
                            Success = false,
                            ErrorMessage = ex.Message,
                            ErrorCode = ex is SmtpCommandException smtpEx ? (int)smtpEx.StatusCode : null,
                        };
                        break;
                    }
                }

                results.Add(result!);
            }
        }
        finally
        {
            if (smtp is not null)
            {
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(quit: true, ct);
                smtp.Dispose();
            }
        }

        return results;
    }

    private async Task<SmtpClient> ConnectAsync(CancellationToken ct)
    {
        var smtp = new SmtpClient();
        smtp.Timeout = _settings.TimeoutSeconds * 1000;

        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, ct);

        // SMTP auth: prefer explicit username, otherwise fall back to postmaster@{domain}.
        var username = string.IsNullOrWhiteSpace(_settings.SmtpUsername)
            ? $"postmaster@{_settings.MailgunDomain}"
            : _settings.SmtpUsername;
        await smtp.AuthenticateAsync(username, _settings.MailgunApiKey, ct);

        return smtp;
    }

    private static MimeMessage BuildMimeMessage(
        string from, string to, string subject,
        string html, string? plaintext, string? replyTo)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        if (!string.IsNullOrEmpty(replyTo))
            message.ReplyTo.Add(MailboxAddress.Parse(replyTo));

        var builder = new BodyBuilder { HtmlBody = html };
        if (!string.IsNullOrEmpty(plaintext))
            builder.TextBody = plaintext;

        message.Body = builder.ToMessageBody();
        return message;
    }

    private static string ApplyVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"%%{key}%%", value, StringComparison.OrdinalIgnoreCase);
        }
        return result;
    }

    private static bool IsTransientError(SmtpCommandException ex)
    {
        // 4xx SMTP codes are transient
        return (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 500;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
