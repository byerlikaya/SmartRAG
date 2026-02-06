using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace SmartRAG.Services.Document.Parsers;


public class WordFileParser : IFileParser
{
    private static readonly string[] SupportedExtensions = { ".docx", ".doc" };
    private static readonly string[] SupportedContentTypes = {
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "application/vnd.ms-word"
    };

    public bool CanParse(string fileName, string contentType)
    {
        return SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
               SupportedContentTypes.Any(contentType.Contains);
    }

    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName, string? language = null)
    {
        try
        {
            var memoryStream = await CreateMemoryStreamCopy(fileStream);
            using var document = WordprocessingDocument.Open(memoryStream, false);
            var body = document.MainDocumentPart?.Document?.Body;

            if (body == null)
            {
                return new FileParserResult { Content = string.Empty };
            }

            var textBuilder = new StringBuilder();
            ExtractTextFromElement(body, textBuilder);

            var content = textBuilder.ToString();
            return new FileParserResult { Content = TextCleaningHelper.CleanContent(content) };
        }
        catch (Exception)
        {
            return new FileParserResult { Content = string.Empty };
        }
    }

    private static async Task<MemoryStream> CreateMemoryStreamCopy(Stream fileStream)
    {
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    private static void ExtractTextFromElement(OpenXmlElement element, StringBuilder textBuilder)
    {
        foreach (var child in element.Elements())
        {
            switch (child)
            {
                case DocumentFormat.OpenXml.Wordprocessing.Text text:
                    textBuilder.Append(text.Text);
                    break;
                case DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph:
                    ExtractTextFromElement(paragraph, textBuilder);
                    textBuilder.AppendLine();
                    break;
                case DocumentFormat.OpenXml.Wordprocessing.Table table:
                    ExtractTextFromElement(table, textBuilder);
                    textBuilder.AppendLine();
                    break;
                default:
                    ExtractTextFromElement(child, textBuilder);
                    break;
            }
        }
    }
}

