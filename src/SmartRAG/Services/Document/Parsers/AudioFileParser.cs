using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Models;
using SmartRAG.Services.Helpers;
using System.IO;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Document.Parsers;


public class AudioFileParser : IFileParser
{
    private readonly IAudioParserService _audioParserService;
    private readonly ILogger<AudioFileParser> _logger;
    private readonly WhisperConfig _whisperConfig;

    private static readonly string[] SupportedExtensions = { ".wav", ".mp3", ".m4a", ".flac", ".ogg" };
    private static readonly string[] SupportedContentTypes = {
        "audio/wav", "audio/mpeg", "audio/mp4", "audio/x-m4a", "audio/flac", "audio/ogg"
    };

    public AudioFileParser(IAudioParserService audioParserService, ILogger<AudioFileParser> logger, IOptions<SmartRagOptions> options)
    {
        _audioParserService = audioParserService;
        _logger = logger;
        _whisperConfig = options?.Value?.WhisperConfig ?? new WhisperConfig();
    }

    public bool CanParse(string fileName, string contentType)
    {
        return SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
               SupportedContentTypes.Any(ct => contentType.Contains(ct));
    }

    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName, string language = null)
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

    private string DetectAudioLanguage(string apiLanguage, string fileName)
    {
        if (!string.IsNullOrEmpty(apiLanguage) && !apiLanguage.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return apiLanguage;
        }

        var fileNameLower = fileName.ToLowerInvariant();
        var iso6391Pattern = @"\b([a-z]{2})(?:[-_]([a-z]{2}))?\b";
        var matches = Regex.Matches(fileNameLower, iso6391Pattern);

        foreach (Match match in matches)
        {
            var languageCode = match.Groups[1].Value;
            if (languageCode.Length == 2 && char.IsLetter(languageCode[0]) && char.IsLetter(languageCode[1]))
            {
                _logger.LogDebug("Detected language from filename: {Language}", languageCode);
                return languageCode;
            }
        }

        var configDefault = _whisperConfig?.DefaultLanguage;
        if (!string.IsNullOrEmpty(configDefault) && !configDefault.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            var code = configDefault.Length >= 2 ? configDefault.Substring(0, 2).ToLowerInvariant() : configDefault;
            if (code.Length == 2 && char.IsLetter(code[0]) && char.IsLetter(code[1]))
            {
                _logger.LogDebug("Using WhisperConfig.DefaultLanguage for transcription: {Language}", code);
                return code;
            }
        }

        _logger.LogDebug("No language specified; using auto-detect so Whisper transcribes in the detected language (no translation).");
        return "auto";
    }
}

