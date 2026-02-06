
namespace SmartRAG.Services.Document.Parsers;


public class TextFileParser : IFileParser
{
    private static readonly string[] SupportedExtensions = { ".txt", ".md", ".json", ".xml", ".csv", ".html", ".htm" };
    private static readonly string[] SupportedContentTypes = { "text/", "application/json", "application/xml", "application/csv" };

    public bool CanParse(string fileName, string contentType)
    {
        return SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
               SupportedContentTypes.Any(contentType.StartsWith);
    }

    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName, string language = null)
    {
        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();
            return new FileParserResult { Content = TextCleaningHelper.CleanContent(content) };
        }
        catch (Exception)
        {
            return new FileParserResult { Content = string.Empty };
        }
    }
}

