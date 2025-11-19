using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Services.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document.Parsers
{
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
                   SupportedContentTypes.Any(ct => contentType.Contains(ct));
        }

        public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
        {
            try
            {
                var memoryStream = await CreateMemoryStreamCopy(fileStream);
                using (var document = WordprocessingDocument.Open(memoryStream, false))
                {
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
                if (child is DocumentFormat.OpenXml.Wordprocessing.Text text)
                {
                    textBuilder.Append(text.Text);
                }
                else if (child is DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
                {
                    ExtractTextFromElement(paragraph, textBuilder);
                    textBuilder.AppendLine();
                }
                else if (child is DocumentFormat.OpenXml.Wordprocessing.Table table)
                {
                    ExtractTextFromElement(table, textBuilder);
                    textBuilder.AppendLine();
                }
                else
                {
                    ExtractTextFromElement(child, textBuilder);
                }
            }
        }
    }
}
