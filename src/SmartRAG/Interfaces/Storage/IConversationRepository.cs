using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Storage
{
    public interface IConversationRepository
    {
        Task<string> GetConversationHistoryAsync(string sessionId);
        Task AddToConversationAsync(string sessionId, string question, string answer);
        Task SetConversationHistoryAsync(string sessionId, string conversation);
        Task ClearConversationAsync(string sessionId);
        Task<bool> SessionExistsAsync(string sessionId);
        Task ClearAllConversationsAsync();
    }
}
