namespace HowToSoftware.Core.Interfaces;

public interface ISuppressionService
{
    /// <summary>
    /// Processes a permanent bounce: creates a suppression record,
    /// records the event, and flags the member's email as disabled.
    /// </summary>
    Task HandleBounceAsync(string emailAddress, string? emailId, CancellationToken ct = default);

    /// <summary>
    /// Processes a spam complaint: creates a suppression record,
    /// records the spam complaint event, and flags the member's email as disabled.
    /// </summary>
    Task HandleSpamComplaintAsync(string emailAddress, string? emailId, CancellationToken ct = default);

    /// <summary>
    /// Removes a suppression and re-enables email delivery for the member.
    /// Used by admins to manually un-suppress an address.
    /// </summary>
    Task RemoveSuppressionAsync(string emailAddress, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
