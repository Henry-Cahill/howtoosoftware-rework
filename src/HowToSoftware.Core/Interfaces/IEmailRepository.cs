using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IEmailRepository
{
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default);

    Task<Email?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Email>> GetPendingEmailsAsync(CancellationToken ct = default);
    /// <summary>
    /// Returns Emails currently in A/B testing phase whose <c>AbTestStartedAt + AbTestWaitMinutes</c>
    /// is at or before <paramref name="now"/>, ordered by <c>AbTestStartedAt</c>.
    /// </summary>
    Task<List<Email>> GetAbTestsAwaitingWinnerAsync(DateTime now, CancellationToken ct = default);
    /// <summary>
    /// Returns the list of EmailRecipient records belonging to <paramref name="emailId"/> with
    /// <c>AbVariant == "holdout"</c> that have not yet been processed (<c>ProcessedAt is null</c>).
    /// </summary>
    Task<List<EmailRecipient>> GetHoldoutRecipientsAsync(string emailId, CancellationToken ct = default);
    /// <summary>
    /// Returns the total open count grouped by <c>AbVariant</c> for the given Email. Keys are
    /// "a" and "b" (any null or "holdout" variants are excluded).
    /// </summary>
    Task<Dictionary<string, int>> GetAbVariantOpenCountsAsync(string emailId, CancellationToken ct = default);
    Task AddEmailAsync(Email email, CancellationToken ct = default);
    Task UpdateEmailAsync(Email email, CancellationToken ct = default);

    Task AddBatchAsync(EmailBatch batch, CancellationToken ct = default);
    Task UpdateBatchAsync(EmailBatch batch, CancellationToken ct = default);

    Task AddRecipientsAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default);
    Task<EmailRecipient?> GetRecipientByIdAsync(string id, CancellationToken ct = default);
    Task UpdateRecipientAsync(EmailRecipient recipient, CancellationToken ct = default);
    Task UpdateRecipientsAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default);

    Task AddRedirectAsync(Redirect redirect, CancellationToken ct = default);
    Task<Redirect?> GetRedirectByIdAsync(string id, CancellationToken ct = default);

    Task AddClickEventAsync(MembersClickEvent clickEvent, CancellationToken ct = default);

    Task<List<string>> GetSuppressedEmailsAsync(CancellationToken ct = default);
    Task<bool> IsEmailSuppressedAsync(string email, CancellationToken ct = default);
    Task AddSuppressionAsync(Suppression suppression, CancellationToken ct = default);
    Task RemoveSuppressionAsync(string email, CancellationToken ct = default);

    Task AddSpamComplaintEventAsync(EmailSpamComplaintEvent evt, CancellationToken ct = default);

    Task<MembersNewsletter?> GetSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default);
    Task<List<MembersNewsletter>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default);
    Task AddSubscriptionAsync(MembersNewsletter subscription, CancellationToken ct = default);
    Task RemoveSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default);

    Task<MembersFeedback?> GetFeedbackAsync(string memberId, string postId, CancellationToken ct = default);
    Task AddFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default);
    Task UpdateFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
