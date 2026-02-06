namespace SmartRAG.Models.Configuration;


/// <summary>
/// Configuration for a watched folder for automatic document indexing
/// </summary>
public class WatchedFolderConfig
{
    /// <summary>
    /// Folder path to watch
    /// </summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Allowed file extensions (e.g., ".pdf", ".docx")
    /// If empty, all supported file types are allowed
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new();

    /// <summary>
    /// Whether to include subdirectories
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>
    /// Whether to automatically upload new files to SmartRAG
    /// </summary>
    public bool AutoUpload { get; set; } = true;

    /// <summary>
    /// User ID for document ownership
    /// </summary>
    public string UserId { get; set; } = "system";

    /// <summary>
    /// Language code for document processing (optional)
    /// </summary>
    public string Language { get; set; }
}



