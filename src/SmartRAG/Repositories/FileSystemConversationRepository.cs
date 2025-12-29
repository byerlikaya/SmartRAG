using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading;
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

        public async Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
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

                return await File.ReadAllTextAsync(filePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history");
                return string.Empty;
            }
        }

        public async Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                if (string.IsNullOrEmpty(question))
                {
                    var sessionFilePath = GetConversationFilePath(sessionId);
                    await File.WriteAllTextAsync(sessionFilePath, answer, cancellationToken);
                    return;
                }

                var currentHistory = await GetConversationHistoryAsync(sessionId, cancellationToken);
                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                if (newEntry.Length > MaxConversationLength)
                {
                    newEntry = TruncateConversation(newEntry);
                }

                var filePath = GetConversationFilePath(sessionId);
                await File.WriteAllTextAsync(filePath, newEntry, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation");
            }
        }

        public async Task ClearConversationAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing conversation");
            }
        }

        public async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                cancellationToken.ThrowIfCancellationRequested();
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session existence");
                return false;
            }
        }

        public async Task SetConversationHistoryAsync(string sessionId, string conversation, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                await File.WriteAllTextAsync(filePath, conversation, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting conversation history");
            }
        }

        public async Task ClearAllConversationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (Directory.Exists(_conversationsPath))
                {
                    var files = Directory.GetFiles(_conversationsPath, "*.txt");
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        File.Delete(file);
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
