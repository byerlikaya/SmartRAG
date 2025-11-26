using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Services.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document.Parsers
{
    public class ImageFileParser : IFileParser
    {
        private readonly IImageParserService _imageParserService;
        private readonly ILogger<ImageFileParser> _logger;

        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
        private static readonly string[] SupportedContentTypes = {
            "image/jpeg", "image/jpg", "image/png", "image/gif",
            "image/bmp", "image/tiff", "image/webp"
        };

        public ImageFileParser(IImageParserService imageParserService, ILogger<ImageFileParser> logger)
        {
            _imageParserService = imageParserService;
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
                // CRITICAL: Pass language parameter to OCR service for proper character recognition
                // Language parameter ensures correct handling of Turkish, German, Russian, etc. characters
                var extractedText = await _imageParserService.ExtractTextFromImageAsync(fileStream, language);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new FileParserResult { Content = string.Empty };
                }

                return new FileParserResult { Content = TextCleaningHelper.CleanContent(extractedText) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse image document with OCR");
                return new FileParserResult { Content = string.Empty };
            }
        }
    }
}
