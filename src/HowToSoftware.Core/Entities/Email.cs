namespace HowToSoftware.Core.Entities;

public class Email
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string Uuid { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public string RecipientFilter { get; set; } = "all";
    public string? Error { get; set; }
    public string? ErrorData { get; set; }
    public int EmailCount { get; set; }
    public int? CsdEmailCount { get; set; }
    public int DeliveredCount { get; set; }
    public int OpenedCount { get; set; }
    public int FailedCount { get; set; }
    public string? Subject { get; set; }
    public string? From { get; set; }
    public string? ReplyTo { get; set; }
    public string? Html { get; set; }
    public string? Plaintext { get; set; }
    public string? Source { get; set; }
    public string SourceType { get; set; } = "html";
    public bool TrackOpens { get; set; }
    public bool TrackClicks { get; set; }
    public bool FeedbackEnabled { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? NewsletterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // A/B subject-line testing
    /// <summary>Alternate subject line tested against <see cref="Subject"/>. Non-null enables A/B testing for this send.</summary>
    public string? SubjectB { get; set; }
    /// <summary>Percentage of subscribers per variant (e.g. 10 = 10% A + 10% B + 80% holdout). Effective range 1..50.</summary>
    public int AbTestSplitPercent { get; set; }
    /// <summary>How long to wait after the test cohort send before picking a winner and sending to the holdout.</summary>
    public int AbTestWaitMinutes { get; set; }
    /// <summary>Lifecycle: null (no A/B), "testing" (A+B sent, awaiting winner), "completed" (winner sent or cancelled).</summary>
    public string? AbTestPhase { get; set; }
    /// <summary>When the test cohort send finished. Used to schedule the winner send at StartedAt + WaitMinutes.</summary>
    public DateTime? AbTestStartedAt { get; set; }
    /// <summary>"a" or "b" once the winner has been resolved; null while phase="testing".</summary>
    public string? AbTestWinnerVariant { get; set; }
    /// <summary>Cached open count for variant A at winner-resolution time (snapshot, not live).</summary>
    public int? AbTestOpensA { get; set; }
    /// <summary>Cached open count for variant B at winner-resolution time (snapshot, not live).</summary>
    public int? AbTestOpensB { get; set; }

    // Navigation properties
    public Post Post { get; set; } = null!;
    public Newsletter? Newsletter { get; set; }
    public ICollection<EmailBatch> Batches { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
