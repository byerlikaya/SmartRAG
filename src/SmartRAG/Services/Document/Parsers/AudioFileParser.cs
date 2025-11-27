using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Services.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document.Parsers
{
    public class AudioFileParser : IFileParser
    {
        private readonly IAudioParserService _audioParserService;
        private readonly ILogger<AudioFileParser> _logger;

        private static readonly string[] SupportedExtensions = { ".wav", ".mp3", ".m4a", ".flac", ".ogg" };
        private static readonly string[] SupportedContentTypes = {
            "audio/wav", "audio/mpeg", "audio/mp4", "audio/x-m4a", "audio/flac", "audio/ogg"
        };
        
        public AudioFileParser(IAudioParserService audioParserService, ILogger<AudioFileParser> logger)
        {
            _audioParserService = audioParserService;
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
                var detectedLanguage = DetectAudioLanguage(language, fileName);
                var transcriptionResult = await _audioParserService.TranscribeAudioAsync(fileStream, fileName, detectedLanguage);
                
                var result = new FileParserResult
                {
                    Content = string.Empty,
                    Metadata = transcriptionResult.Metadata != null 
                        ? new Dictionary<string, object>(transcriptionResult.Metadata) 
                        : new Dictionary<string, object>()
                };

                if (!string.IsNullOrWhiteSpace(transcriptionResult.Text))
                {
                    result.Content = TextCleaningHelper.CleanContent(transcriptionResult.Text);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse audio document with Speech-to-Text");
                return new FileParserResult { Content = string.Empty };
            }
        }

        private string DetectAudioLanguage(string apiLanguage, string fileName)
        {
            if (!string.IsNullOrEmpty(apiLanguage))
            {
                if (apiLanguage.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    return "auto";
                }
                return apiLanguage;
            }

            var fileNameLower = fileName.ToLowerInvariant();
            var iso6391Pattern = @"\b([a-z]{2})(?:[-_]([a-z]{2}))?\b";
            var matches = Regex.Matches(fileNameLower, iso6391Pattern);
            
            foreach (Match match in matches)
            {
                var languageCode = match.Groups[1].Value;
                var regionCode = match.Groups[2].Success ? match.Groups[2].Value : null;
                
                if (languageCode.Length == 2 && char.IsLetter(languageCode[0]) && char.IsLetter(languageCode[1]))
                {
                    var locale = regionCode != null && regionCode.Length == 2
                        ? $"{languageCode}-{regionCode.ToUpperInvariant()}"
                        : $"{languageCode}-{languageCode.ToUpperInvariant()}";
                    return locale;
                }
            }

            return "auto";
        }
    }
}
