using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Storage
{
    public interface IConversationRepository
    {
        Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default);
        Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default);
        Task SetConversationHistoryAsync(string sessionId, string conversation, CancellationToken cancellationToken = default);
        Task ClearConversationAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default);
        Task ClearAllConversationsAsync(CancellationToken cancellationToken = default);
    }
}
