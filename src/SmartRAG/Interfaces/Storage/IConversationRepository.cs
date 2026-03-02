namespace SmartRAG.Interfaces.Storage;


public interface IConversationRepository
{
    Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<(DateTime? CreatedAt, DateTime? LastUpdated)> GetSessionTimestampsAsync(string sessionId, CancellationToken cancellationToken = default)
        => Task.FromResult<(DateTime?, DateTime?)>((null, null));
    Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default);
    Task SetConversationHistoryAsync(string sessionId, string conversation, CancellationToken cancellationToken = default);
    Task ClearConversationAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task ClearAllConversationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends sources JSON for the latest assistant turn (one JSON array per turn).
    /// </summary>
    Task AppendSourcesForTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stored sources for a session as JSON array of arrays, or null if none.
    /// </summary>
    Task<string> GetSourcesForSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all known conversation session IDs for the current storage provider.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Array of session IDs.</returns>
    Task<string[]> GetAllSessionIdsAsync(CancellationToken cancellationToken = default);
}

