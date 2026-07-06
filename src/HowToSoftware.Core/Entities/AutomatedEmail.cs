namespace HowToSoftware.Core.Entities;

public class AutomatedEmail
{
    public string Id { get; set; } = null!;
    public string Status { get; set; } = "inactive";
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string? Lexical { get; set; }
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderReplyTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Drip / sequence support (AUTO.4) ──

    /// <summary>
    /// Delay in minutes before the email is sent after the trigger fires.
    /// 0 = send immediately (default, backwards compatible).
    /// </summary>
    public int DelayMinutes { get; set; }

    /// <summary>
    /// Optional trigger event. When set, this email is dispatched whenever
    /// <see cref="HowToSoftware.Core.Interfaces.IAutomatedEmailService.SendAsync"/>
    /// is called with a matching trigger string. Multiple emails sharing the
    /// same trigger event form a drip sequence (each with its own
    /// <see cref="DelayMinutes"/>). When null, the email is dispatched only
    /// by direct slug match (legacy behavior).
    /// </summary>
    public string? TriggerEvent { get; set; }

    // Navigation properties
    public ICollection<AutomatedEmailRecipient> Recipients { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
