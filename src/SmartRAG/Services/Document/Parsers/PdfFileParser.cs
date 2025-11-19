using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Services.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document.Parsers
{
    public class PdfFileParser : IFileParser
    {
        private static readonly string[] SupportedExtensions = { ".pdf" };
        private const string SupportedContentType = "application/pdf";

        public bool CanParse(string fileName, string contentType)
        {
            return SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
                   contentType == SupportedContentType;
        }

        public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
        {
            try
            {
                var memoryStream = await CreateMemoryStreamCopy(fileStream);
                var bytes = memoryStream.ToArray();

                using (var pdfReader = new PdfReader(new MemoryStream(bytes)))
                {
                    using (var pdfDocument = new PdfDocument(pdfReader))
                    {
                        var textBuilder = new StringBuilder();
                        ExtractTextFromPdfPages(pdfDocument, textBuilder);

                        var content = textBuilder.ToString();
                        return new FileParserResult { Content = TextCleaningHelper.CleanContent(content) };
                    }
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

        private static void ExtractTextFromPdfPages(PdfDocument pdfDocument, StringBuilder textBuilder)
        {
            var pageCount = pdfDocument.GetNumberOfPages();

            for (int i = 1; i <= pageCount; i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    textBuilder.AppendLine(text);
                }
            }
        }
    }
}
