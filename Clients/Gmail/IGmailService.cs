namespace Agent.Clients.Gmail;

public interface IGmailService
{
    /// <summary>
    /// Gets the latest emails from the user's inbox.
    /// </summary>
    /// <param name="maxResults">Maximum number of messages to retrieve (default 20).</param>
    /// <returns>List of email messages with details.</returns>
    Task<List<GmailMessage>> GetMailAsync(int maxResults = 20);

    /// <summary>
    /// Searches emails using Gmail search syntax.
    /// </summary>
    /// <param name="query">Gmail search query (e.g., "from:user@example.com is:unread").</param>
    /// <param name="maxResults">Maximum number of messages to retrieve (default 20).</param>
    /// <returns>List of matching email messages with details.</returns>
    Task<List<GmailMessage>> SearchMailAsync(string query, int maxResults = 20);

    /// <summary>
    /// Gets the full contents of a specific email by ID.
    /// </summary>
    /// <param name="messageId">The Gmail message ID.</param>
    /// <returns>Full email details including body, or null if not found.</returns>
    Task<GmailMessageDetail?> GetMailContentsAsync(string messageId);
}
