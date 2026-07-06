using System.Net.Mail;
using System.Text;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace HowToSoftware.Core.Services;

public sealed class MemberImportService : IMemberImportService
{
    private readonly IMemberRepository _members;
    private readonly ILabelRepository _labels;
    private readonly INewsletterRepository _newsletters;
    private readonly INewsletterService _newsletterService;
    private readonly ILogger<MemberImportService> _logger;

    private static readonly char[] LabelSeparators = [';', '|'];

    public MemberImportService(
        IMemberRepository members,
        ILabelRepository labels,
        INewsletterRepository newsletters,
        INewsletterService newsletterService,
        ILogger<MemberImportService> logger)
    {
        _members = members;
        _labels = labels;
        _newsletters = newsletters;
        _newsletterService = newsletterService;
        _logger = logger;
    }

    public async Task<MemberImportResult> ImportAsync(string csvContent, CancellationToken ct = default)
    {
        var result = new MemberImportResult();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            result.Errors.Add(new MemberImportRowError(0, null, "CSV content is empty."));
            return result;
        }

        var rows = ParseCsv(csvContent, out var header, out var parseErrors);
        foreach (var err in parseErrors)
            result.Errors.Add(new MemberImportRowError(err.LineNumber, null, err.Message));

        if (header is null || !header.ContainsKey("email"))
        {
            result.Errors.Add(new MemberImportRowError(0, null, "CSV must include an 'email' column header."));
            return result;
        }

        // Cache labels by lowercased name for quick lookup/dedupe across rows.
        var existingLabels = await _labels.GetAllAsync(ct);
        var labelsByName = existingLabels.ToDictionary(
            l => l.Name.Trim().ToLowerInvariant(),
            l => l,
            StringComparer.Ordinal);

        // Default newsletters to subscribe new members to when subscribed_to_emails=true.
        var defaultNewsletters = (await _newsletters.GetActiveAsync(ct))
            .Where(n => n.SubscribeOnSignup)
            .ToList();

        // Track emails seen so far in this batch (in addition to DB lookups).
        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (lineNumber, fields) in rows)
        {
            ct.ThrowIfCancellationRequested();
            result.TotalRows++;

            var email = GetField(fields, header, "email")?.Trim();
            if (string.IsNullOrEmpty(email))
            {
                result.Failed++;
                result.Errors.Add(new MemberImportRowError(lineNumber, null, "Missing email."));
                continue;
            }

            if (!IsValidEmail(email))
            {
                result.Failed++;
                result.Errors.Add(new MemberImportRowError(lineNumber, email, "Invalid email format."));
                continue;
            }

            if (!seenInBatch.Add(email))
            {
                result.SkippedDuplicates++;
                result.Errors.Add(new MemberImportRowError(lineNumber, email, "Duplicate email earlier in file — skipped."));
                continue;
            }

            var existing = await _members.GetByEmailAsync(email, ct);
            if (existing is not null)
            {
                result.SkippedDuplicates++;
                continue;
            }

            var name = GetField(fields, header, "name")?.Trim();
            var statusRaw = GetField(fields, header, "status")?.Trim().ToLowerInvariant();
            var complimentary = ParseBool(GetField(fields, header, "complimentary_plan"));
            var subscribedToEmails = ParseBool(GetField(fields, header, "subscribed_to_emails")) ?? true;
            var stripeCustomerId = GetField(fields, header, "stripe_customer_id")?.Trim();
            var labelsRaw = GetField(fields, header, "labels");

            var status = NormalizeStatus(statusRaw, complimentary);

            var now = DateTime.UtcNow;
            var memberId = ObjectIdGenerator.New();
            var member = new Member
            {
                Id = memberId,
                Uuid = Guid.NewGuid().ToString("D"),
                TransientId = ObjectIdGenerator.New(),
                Email = email,
                Name = string.IsNullOrEmpty(name) ? null : name,
                Status = status,
                EmailDisabled = !subscribedToEmails,
                CreatedAt = now,
                UpdatedAt = now,
            };

            try
            {
                await _members.AddAsync(member, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to insert imported member {Email} on line {Line}", email, lineNumber);
                result.Failed++;
                result.Errors.Add(new MemberImportRowError(lineNumber, email, $"Insert failed: {ex.Message}"));
                continue;
            }

            // Record creation/status events to keep parity with normal signup.
            await _members.AddCreatedEventAsync(new MembersCreatedEvent
            {
                Id = ObjectIdGenerator.New(),
                MemberId = memberId,
                Source = "import",
                CreatedAt = now,
            }, ct);
            await _members.AddStatusEventAsync(new MembersStatusEvent
            {
                Id = ObjectIdGenerator.New(),
                MemberId = memberId,
                FromStatus = null,
                ToStatus = status,
                CreatedAt = now,
            }, ct);

            // Labels: get-or-create each, then attach.
            if (!string.IsNullOrWhiteSpace(labelsRaw))
            {
                foreach (var labelName in SplitLabels(labelsRaw))
                {
                    var key = labelName.ToLowerInvariant();
                    if (!labelsByName.TryGetValue(key, out var label))
                    {
                        label = new Label
                        {
                            Id = ObjectIdGenerator.New(),
                            Name = labelName,
                            Slug = GenerateSlug(labelName),
                            CreatedAt = now,
                        };
                        try
                        {
                            await _labels.AddAsync(label, ct);
                            labelsByName[key] = label;
                            result.LabelsCreated++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to create label {Label} during import", labelName);
                            result.Errors.Add(new MemberImportRowError(lineNumber, email, $"Could not create label '{labelName}': {ex.Message}"));
                            continue;
                        }
                    }
                    await _members.AddLabelToMemberAsync(memberId, label.Id, ct);
                }
            }

            // Newsletter subscriptions: only when subscribed_to_emails is true.
            if (subscribedToEmails)
            {
                foreach (var nl in defaultNewsletters)
                {
                    try
                    {
                        await _newsletterService.SubscribeAsync(memberId, nl.Id, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to subscribe imported member {Email} to newsletter {Newsletter}", email, nl.Id);
                    }
                }
            }

            // Stripe customer link (best-effort).
            if (!string.IsNullOrWhiteSpace(stripeCustomerId))
            {
                try
                {
                    var linked = await _members.LinkStripeCustomerAsync(memberId, stripeCustomerId, name, email, ct);
                    if (linked) result.StripeCustomersLinked++;
                    else result.Errors.Add(new MemberImportRowError(lineNumber, email, $"Stripe customer '{stripeCustomerId}' is already linked to another member."));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to link Stripe customer {Customer} for {Email}", stripeCustomerId, email);
                    result.Errors.Add(new MemberImportRowError(lineNumber, email, $"Stripe link failed: {ex.Message}"));
                }
            }

            result.Imported++;
        }

        _logger.LogInformation(
            "Member CSV import complete: {Imported} imported, {Skipped} skipped, {Failed} failed, {Labels} labels created, {Stripe} stripe links",
            result.Imported, result.SkippedDuplicates, result.Failed, result.LabelsCreated, result.StripeCustomersLinked);

        return result;
    }

    private static string NormalizeStatus(string? statusRaw, bool? complimentary)
    {
        if (complimentary == true) return "comped";
        return statusRaw switch
        {
            "paid" => "paid",
            "comped" => "comped",
            "free" => "free",
            _ => "free",
        };
    }

    private static bool? ParseBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var v = raw.Trim().ToLowerInvariant();
        return v switch
        {
            "true" or "1" or "yes" or "y" => true,
            "false" or "0" or "no" or "n" => false,
            _ => null,
        };
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Length > 254) return false;
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> SplitLabels(string raw)
    {
        // Accept ';' or '|' (commas are the CSV separator).
        foreach (var part in raw.Split(LabelSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrEmpty(part))
                yield return part;
        }
    }

    private static string GenerateSlug(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch == ' ' || ch == '-' || ch == '_') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-");
        if (slug.Length == 0)
            slug = "label-" + Guid.NewGuid().ToString("N")[..8];
        return slug;
    }

    private static string? GetField(List<string> fields, Dictionary<string, int> header, string columnName)
    {
        if (!header.TryGetValue(columnName, out var idx)) return null;
        if (idx < 0 || idx >= fields.Count) return null;
        return fields[idx];
    }

    private sealed record ParseError(int LineNumber, string Message);

    private static List<(int LineNumber, List<string> Fields)> ParseCsv(
        string content,
        out Dictionary<string, int>? header,
        out List<ParseError> errors)
    {
        errors = [];
        header = null;
        var rows = new List<(int, List<string>)>();

        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');

        // Use a state machine to correctly handle multi-line quoted fields.
        var current = new StringBuilder();
        var fields = new List<string>();
        var inQuotes = false;
        var lineNumber = 1;
        var rowLineNumber = 1;
        var hasCellContent = false; // any character or comma encountered on this row

        void CommitField()
        {
            fields.Add(current.ToString());
            current.Clear();
        }

        void CommitRow()
        {
            // Commit pending field if any character was consumed (or a trailing comma was seen).
            if (hasCellContent || current.Length > 0 || fields.Count > 0)
            {
                fields.Add(current.ToString());
                current.Clear();
                // Skip blank lines (single empty field with no content).
                if (!(fields.Count == 1 && string.IsNullOrWhiteSpace(fields[0])))
                    rows.Add((rowLineNumber, new List<string>(fields)));
                fields.Clear();
            }
            hasCellContent = false;
            rowLineNumber = lineNumber + 1;
        }

        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < normalized.Length && normalized[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    if (c == '\n') lineNumber++;
                    current.Append(c);
                }
            }
            else
            {
                switch (c)
                {
                    case ',':
                        CommitField();
                        hasCellContent = true;
                        break;
                    case '"' when current.Length == 0:
                        inQuotes = true;
                        hasCellContent = true;
                        break;
                    case '\n':
                        CommitRow();
                        lineNumber++;
                        break;
                    default:
                        current.Append(c);
                        hasCellContent = true;
                        break;
                }
            }
        }

        // Final row (no trailing newline).
        if (current.Length > 0 || fields.Count > 0 || hasCellContent)
            CommitRow();

        if (rows.Count == 0)
            return rows;

        // First non-empty row is the header.
        var headerRow = rows[0];
        rows.RemoveAt(0);

        header = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerRow.Item2.Count; i++)
        {
            var name = headerRow.Item2[i].Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(name)) continue;
            header.TryAdd(name, i);
        }

        return rows;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
