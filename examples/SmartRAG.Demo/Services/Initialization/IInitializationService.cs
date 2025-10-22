using SmartRAG.Enums;

namespace SmartRAG.Demo.Services.Initialization;

/// <summary>
/// Service for initialization operations
/// </summary>
public interface IInitializationService
{
    Task SetupTestDatabasesAsync();
    Task<(bool UseLocal, AIProvider AIProvider, StorageProvider StorageProvider, AudioProvider AudioProvider)> SelectEnvironmentAsync();
    Task<string> SelectLanguageAsync();
    Task InitializeServicesAsync(AIProvider aiProvider, StorageProvider storageProvider, AudioProvider audioProvider);
}

