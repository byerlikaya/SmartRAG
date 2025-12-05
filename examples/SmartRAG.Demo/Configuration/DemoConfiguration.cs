using SmartRAG.Enums;

namespace SmartRAG.Demo.Configuration;

/// <summary>
/// Configuration settings for the demo application
/// </summary>
public class DemoConfiguration
{
    #region Properties

    public string SelectedLanguage { get; set; } = "English";
    public bool UseLocalEnvironment { get; set; } = true;
    public AIProvider SelectedAIProvider { get; set; } = AIProvider.Custom;
    public StorageProvider SelectedStorageProvider { get; set; } = StorageProvider.Redis;

    #endregion
}

