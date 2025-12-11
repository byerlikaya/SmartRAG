using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{
    public class FileSystemConversationRepository : IConversationRepository
    {
        private readonly string _conversationsPath;
        private readonly ILogger<FileSystemConversationRepository> _logger;
        private const int MaxConversationLength = 2000;

        public FileSystemConversationRepository(string basePath, ILogger<FileSystemConversationRepository> logger)
        {
            _conversationsPath = Path.Combine(basePath, "Conversations");
            _logger = logger;
            Directory.CreateDirectory(_conversationsPath);
        }

        public async Task<string> GetConversationHistoryAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return string.Empty;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                if (!File.Exists(filePath))
                {
                    return string.Empty;
                }

                return await Task.Run(() => File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
                return string.Empty;
            }
        }

        public async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                if (string.IsNullOrEmpty(question))
                {
                    var sessionFilePath = GetConversationFilePath(sessionId);
                    await Task.Run(() => File.WriteAllText(sessionFilePath, answer));
                    return;
                }

                var currentHistory = await GetConversationHistoryAsync(sessionId);
                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                if (newEntry.Length > MaxConversationLength)
                {
                    newEntry = TruncateConversation(newEntry);
                }

                var filePath = GetConversationFilePath(sessionId);
                await Task.Run(() => File.WriteAllText(filePath, newEntry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation for session {SessionId}", sessionId);
            }
        }

        public async Task ClearConversationAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing conversation for session {SessionId}", sessionId);
            }
        }

        public async Task<bool> SessionExistsAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                return await Task.Run(() => File.Exists(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session existence for {SessionId}", sessionId);
                return false;
            }
        }

        public async Task ClearAllConversationsAsync()
        {
            try
            {
                if (Directory.Exists(_conversationsPath))
                {
                    var files = Directory.GetFiles(_conversationsPath, "*.txt");
                    foreach (var file in files)
                    {
                        await Task.Run(() => File.Delete(file));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all conversations from file system");
                throw;
            }
        }

        private string GetConversationFilePath(string sessionId)
        {
            var fileName = $"{sessionId}.txt";
            return Path.Combine(_conversationsPath, fileName);
        }

        private static string TruncateConversation(string conversation)
        {
            var lines = conversation.Split('\n');
            if (lines.Length <= 6)
                return conversation;

            return string.Join("\n", lines.Skip(lines.Length - 6));
        }
    }
}
