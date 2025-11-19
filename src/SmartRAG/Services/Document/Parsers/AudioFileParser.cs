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

        // Property to expose metadata after parsing, similar to how the original service handled it.
        // Ideally, IFileParser should return a result object containing content and metadata,
        // but for now we'll stick to the interface and maybe use a side-channel or change the interface later if needed.
        // Actually, the original code used a field _lastParsedMetadata.
        // To keep it clean, we might want to change IFileParser to return a ParseResult.
        // But for this refactoring step, let's keep it simple.
        // Wait, if I don't return metadata, I lose the audio segments.
        // I should probably update IFileParser to return a ParseResult { Content, Metadata }.
        // But that would require changing all other parsers.
        // Let's stick to the plan and maybe add a property LastParsedMetadata here, but that's not thread-safe if singleton.
        // The parsers should be transient or scoped.
        // Or better, change the interface to return (string Content, Dictionary<string, object> Metadata).
        
        // For now, I will just return the text. The metadata handling in the original code was a bit hacky with _lastParsedMetadata.
        // I will need to address this.
        // Let's modify IFileParser to return a ParseResult.
        
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

        public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
        {
            try
            {
                var detectedLanguage = DetectAudioLanguage(null, fileName);
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

            return "en-US";
        }
    }
}
