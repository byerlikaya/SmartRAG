namespace SmartRAG.Models.Schema;


/// <summary>
/// Options for configuring a specific search request
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// Enable searching in databases
    /// </summary>
    public bool EnableDatabaseSearch { get; set; } = true;

    /// <summary>
    /// Enable searching in documents
    /// </summary>
    public bool EnableDocumentSearch { get; set; } = true;

    /// <summary>
    /// Enable searching in audio files (transcriptions)
    /// </summary>
    public bool EnableAudioSearch { get; set; } = true;

    /// <summary>
    /// Enable searching in images (OCR)
    /// </summary>
    public bool EnableImageSearch { get; set; } = true;

    /// <summary>
    /// Enable searching via MCP (Model Context Protocol) integration
    /// </summary>
    public bool EnableMcpSearch { get; set; } = true;

    /// <summary>
    /// Preferred language for AI responses (ISO 639-1 language code, e.g., "tr", "en", "de")
    /// If not specified, AI will attempt to detect language from the query
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Creates search options from global configuration
    /// </summary>
    public static SearchOptions FromConfig(SmartRagOptions options)
    {
        return new SearchOptions
        {
            EnableDatabaseSearch = options.Features.EnableDatabaseSearch,
            EnableDocumentSearch = options.Features.EnableDocumentSearch,
            EnableAudioSearch = options.Features.EnableAudioSearch,
            EnableImageSearch = options.Features.EnableImageSearch,
            EnableMcpSearch = options.Features.EnableMcpSearch,
            PreferredLanguage = options.DefaultLanguage
        };
    }

    /// <summary>
    /// Creates search options for document-only search
    /// </summary>
    public static SearchOptions CreateDocumentOnly(SearchOptions baseOptions)
    {
        return new SearchOptions
        {
            EnableDocumentSearch = true,
            EnableDatabaseSearch = false,
            EnableMcpSearch = false,
            EnableAudioSearch = false,
            EnableImageSearch = false,
            PreferredLanguage = baseOptions.PreferredLanguage
        };
    }

    /// <summary>
    /// Creates search options for database-only search
    /// Note: EnableDatabaseSearch is only set from baseOptions
    /// This ensures global feature flag is respected
    /// </summary>
    public static SearchOptions CreateDatabaseOnly(SearchOptions baseOptions)
    {
        return new SearchOptions
        {
            EnableDatabaseSearch = baseOptions.EnableDatabaseSearch,
            EnableDocumentSearch = false,
            EnableMcpSearch = false,
            EnableAudioSearch = false,
            EnableImageSearch = false,
            PreferredLanguage = baseOptions.PreferredLanguage
        };
    }

    /// <summary>
    /// Creates search options for MCP-only search
    /// Note: EnableMcpSearch is only set to true if it was already enabled in baseOptions
    /// This ensures global feature flag is respected
    /// </summary>
    public static SearchOptions CreateMcpOnly(SearchOptions baseOptions)
    {
        return new SearchOptions
        {
            EnableMcpSearch = baseOptions.EnableMcpSearch,
            EnableDocumentSearch = false,
            EnableDatabaseSearch = false,
            EnableAudioSearch = false,
            EnableImageSearch = false,
            PreferredLanguage = baseOptions.PreferredLanguage
        };
    }

    /// <summary>
    /// Creates search options for audio-only search
    /// </summary>
    public static SearchOptions CreateAudioOnly(SearchOptions baseOptions)
    {
        return new SearchOptions
        {
            EnableAudioSearch = true,
            EnableDocumentSearch = false,
            EnableDatabaseSearch = false,
            EnableMcpSearch = false,
            EnableImageSearch = false,
            PreferredLanguage = baseOptions.PreferredLanguage
        };
    }

    /// <summary>
    /// Creates search options for image-only search
    /// </summary>
    public static SearchOptions CreateImageOnly(SearchOptions baseOptions)
    {
        return new SearchOptions
        {
            EnableImageSearch = true,
            EnableDocumentSearch = false,
            EnableDatabaseSearch = false,
            EnableMcpSearch = false,
            EnableAudioSearch = false,
            PreferredLanguage = baseOptions.PreferredLanguage
        };
    }
}

