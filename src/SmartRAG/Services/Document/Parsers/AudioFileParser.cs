using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Models;
using SmartRAG.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                _logger.LogDebug("AudioFileParser: Language detected");
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
            // If language is explicitly provided and not "auto", use it
            if (!string.IsNullOrEmpty(apiLanguage))
            {
                if (apiLanguage.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    // Fall through to system locale detection
                }
                else
                {
                    return apiLanguage;
                }
            }

            // Try to detect language from filename (e.g., "audio_tr.m4a" or "audio-tr-TR.m4a")
            var fileNameLower = fileName.ToLowerInvariant();
            var iso6391Pattern = @"\b([a-z]{2})(?:[-_]([a-z]{2}))?\b";
            var matches = Regex.Matches(fileNameLower, iso6391Pattern);

            foreach (Match match in matches)
            {
                var languageCode = match.Groups[1].Value;
                var regionCode = match.Groups[2].Success ? match.Groups[2].Value : null;

                if (languageCode.Length == 2 && char.IsLetter(languageCode[0]) && char.IsLetter(languageCode[1]))
                {
                    // Whisper.net uses ISO 639-1 (2-letter) codes, not locale format
                    // Return just the language code (e.g., "tr" not "tr-TR")
                    _logger.LogDebug("Detected language from filename");
                    return languageCode;
                }
            }

            // Fallback to system locale if no language detected from filename
            try
            {
                // Try CurrentUICulture first (user interface language), then CurrentCulture (regional settings)
                var uiCulture = CultureInfo.CurrentUICulture;
                var culture = CultureInfo.CurrentCulture;
                
                // Prefer UI culture language code
                var twoLetterCode = uiCulture.TwoLetterISOLanguageName;
                
                // If UI culture is "en" but we're in a non-English region, check environment variables
                // This is cross-platform and works on Windows, Linux, and macOS
                if (twoLetterCode == "en")
                {
                    // Check LANG environment variable (common on Unix-like systems: Linux, macOS)
                    var langEnv = Environment.GetEnvironmentVariable("LANG");
                    if (!string.IsNullOrEmpty(langEnv))
                    {
                        // LANG format is usually "language_REGION.encoding" (e.g., "tr_TR.UTF-8")
                        var langParts = langEnv.Split('.')[0].Split('_');
                        if (langParts.Length > 0 && langParts[0].Length == 2)
                        {
                            var envLanguageCode = langParts[0].ToLowerInvariant();
                            if (envLanguageCode != "en" && envLanguageCode.Length == 2)
                            {
                                _logger.LogDebug("No language detected from filename or config. Using LANG environment variable");
                                return envLanguageCode;
                            }
                        }
                    }
                    
                    // Also check LC_ALL (overrides LANG on Unix-like systems)
                    var lcAllEnv = Environment.GetEnvironmentVariable("LC_ALL");
                    if (!string.IsNullOrEmpty(lcAllEnv))
                    {
                        var lcAllParts = lcAllEnv.Split('.')[0].Split('_');
                        if (lcAllParts.Length > 0 && lcAllParts[0].Length == 2)
                        {
                            var lcAllLanguageCode = lcAllParts[0].ToLowerInvariant();
                            if (lcAllLanguageCode != "en" && lcAllLanguageCode.Length == 2)
                            {
                                _logger.LogDebug("No language detected from filename or config. Using LC_ALL environment variable");
                                return lcAllLanguageCode;
                            }
                        }
                    }
                }
                
                _logger.LogDebug("No language detected from filename or config. Using system UI locale");
                return twoLetterCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect system locale, falling back to auto-detect");
                return "auto";
            }
        }
    }
}
