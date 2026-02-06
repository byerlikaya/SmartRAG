
namespace SmartRAG.Services.Document.Parsers;


public class DatabaseFileParser : IFileParser
{
    private readonly IDatabaseParserService _databaseParserService;
    private readonly ILogger<DatabaseFileParser> _logger;

    private static readonly string[] SupportedExtensions = { ".db", ".sqlite", ".sqlite3", ".db3" };
    private static readonly string[] SupportedContentTypes = {
        "application/x-sqlite3", "application/vnd.sqlite3", "application/octet-stream"
    };

    public DatabaseFileParser(IDatabaseParserService databaseParserService, ILogger<DatabaseFileParser> logger)
    {
        _databaseParserService = databaseParserService;
        _logger = logger;
    }

    public bool CanParse(string fileName, string contentType)
    {
        return SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
               SupportedContentTypes.Any(ct => contentType.Contains(ct));
    }

    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName, string language = null)
    {
        try
        {
            var content = await _databaseParserService.ParseDatabaseFileAsync(fileStream, fileName);

            if (string.IsNullOrWhiteSpace(content))
            {
                return new FileParserResult { Content = string.Empty };
            }

            _logger.LogInformation("Database document upload successful: {FileName}, Content length: {ContentLength}", fileName, content.Length);
            return new FileParserResult { Content = content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse database document");
            return new FileParserResult { Content = string.Empty };
        }
    }
}

