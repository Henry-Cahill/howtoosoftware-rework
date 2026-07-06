namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Aggregate delivery statistics for a single automated email.
/// Sent counts every recipient row regardless of outcome (one row per attempt).
/// Delivered/Opened/Clicked/Bounced are populated from Mailgun webhook events.
/// Failed counts rows where the initial SMTP submission failed.
/// </summary>
public sealed record AutomatedEmailStatistics(
    int Sent,
    int Delivered,
    int Opened,
    int Clicked,
    int Failed,
    int Bounced)
{
    public double OpenRate => Delivered > 0 ? (double)Opened / Delivered : 0.0;
    public double ClickRate => Delivered > 0 ? (double)Clicked / Delivered : 0.0;
    public double DeliveryRate => Sent > 0 ? (double)Delivered / Sent : 0.0;

    public static AutomatedEmailStatistics Empty { get; } = new(0, 0, 0, 0, 0, 0);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
