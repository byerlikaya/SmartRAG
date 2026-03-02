
namespace SmartRAG.Demo.Services.Initialization;

/// <summary>
/// Service for initialization operations
/// </summary>
public interface IInitializationService
{
    Task SetupTestDatabasesAsync();
    Task<(bool UseLocal, AIProvider AIProvider, StorageProvider StorageProvider, ConversationStorageProvider ConversationStorageProvider)> SelectEnvironmentAsync();
    Task<string> SelectLanguageAsync();
    Task InitializeServicesAsync(AIProvider aiProvider, StorageProvider storageProvider, ConversationStorageProvider conversationStorageProvider, string? defaultLanguage = null);
}

