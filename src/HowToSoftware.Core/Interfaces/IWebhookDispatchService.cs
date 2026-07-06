namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Enqueues webhook events to be dispatched asynchronously to registered target URLs.
/// </summary>
public interface IWebhookDispatchService
{
    /// <summary>
    /// Enqueues a webhook event for background dispatch to all webhooks registered for the given event.
    /// </summary>
    /// <param name="eventName">The event name (e.g. "post.published", "member.added").</param>
    /// <param name="payload">The data to serialize as the webhook JSON body.</param>
    void Enqueue(string eventName, object payload);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
