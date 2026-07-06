namespace HowToSoftware.Core.Interfaces;

public interface IMemberImportService
{
    Task<MemberImportResult> ImportAsync(string csvContent, CancellationToken ct = default);
}

public sealed class MemberImportResult
{
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int SkippedDuplicates { get; set; }
    public int Failed { get; set; }
    public int LabelsCreated { get; set; }
    public int StripeCustomersLinked { get; set; }
    public List<MemberImportRowError> Errors { get; set; } = [];
}

public sealed record MemberImportRowError(int LineNumber, string? Email, string Message);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
