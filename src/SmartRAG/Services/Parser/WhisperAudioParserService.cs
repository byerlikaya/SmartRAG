using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace SmartRAG.Services.Parser
{
    /// <summary>
    /// Service for parsing audio files using Whisper.net for local transcription.
    /// Supports multiple audio formats with 99+ languages and hardware acceleration.
    /// </summary>
    public class WhisperAudioParserService : IAudioParserService
    {

        #region Fields

        private readonly ILogger<WhisperAudioParserService> _logger;
        private readonly WhisperConfig _config;
        private readonly AudioConversionService _audioConversionService;
        private WhisperFactory _whisperFactory = null;
        private bool _disposed;

        #endregion

        #region Constructor

        public WhisperAudioParserService(ILogger<WhisperAudioParserService> logger, IOptions<SmartRagOptions> options, AudioConversionService audioConversionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = options?.Value?.WhisperConfig ?? throw new ArgumentNullException(nameof(options));
            _audioConversionService = audioConversionService ?? throw new ArgumentNullException(nameof(audioConversionService));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// [AI Query] Transcribes audio content from a stream using Whisper.net
        /// </summary>
        public async Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string language = null)
        {
            if (audioStream == null)
                throw new ArgumentNullException(nameof(audioStream));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            try
            {
                _logger.LogInformation("Starting Whisper transcription for {FileName} ({Size} bytes)",
                    SanitizeFileName(fileName), audioStream.Length);

                ValidateAudioStream(audioStream);

                await EnsureWhisperFactoryInitializedAsync();

                var result = await PerformTranscriptionAsync(audioStream, fileName, language);

                _logger.LogInformation("Whisper transcription completed: {Length} characters with {Confidence} confidence",
                    result.Text?.Length ?? 0, result.Confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Whisper transcription failed for {FileName}", SanitizeFileName(fileName));
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures Whisper factory is initialized and model is loaded
        /// </summary>
        private async Task EnsureWhisperFactoryInitializedAsync()
        {
            if (_whisperFactory != null)
                return;

            _logger.LogInformation("Initializing Whisper factory with model: {ModelPath}", _config.ModelPath);

            await EnsureModelExistsAsync();

            _whisperFactory = WhisperFactory.FromPath(_config.ModelPath);
        }

        /// <summary>
        /// Ensures Whisper model file exists, downloads if necessary
        /// </summary>
        private async Task EnsureModelExistsAsync()
        {
            if (File.Exists(_config.ModelPath))
            {
                _logger.LogDebug("Whisper model found at {ModelPath}", _config.ModelPath);
                return;
            }

            _logger.LogInformation("Whisper model not found at {ModelPath}, downloading...", _config.ModelPath);

            var modelDirectory = Path.GetDirectoryName(_config.ModelPath);
            if (!string.IsNullOrEmpty(modelDirectory) && !Directory.Exists(modelDirectory))
            {
                Directory.CreateDirectory(modelDirectory);
                _logger.LogDebug("Created model directory: {Directory}", modelDirectory);
            }

            var modelType = DetermineModelType(_config.ModelPath);

            _logger.LogInformation("Downloading Whisper model: {ModelType}", modelType);

            using (var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType))
            {
                using var fileWriter = File.OpenWrite(_config.ModelPath);
                await modelStream.CopyToAsync(fileWriter);
            }

            _logger.LogInformation("Whisper model downloaded successfully to {ModelPath}", _config.ModelPath);
        }

        /// <summary>
        /// Determines Whisper model type from filename
        /// </summary>
        private static GgmlType DetermineModelType(string modelPath)
        {
            var fileName = Path.GetFileName(modelPath).ToLowerInvariant();

            if (fileName.Contains("tiny")) return GgmlType.Tiny;
            if (fileName.Contains("base")) return GgmlType.Base;
            if (fileName.Contains("small")) return GgmlType.Small;
            if (fileName.Contains("medium")) return GgmlType.Medium;
            if (fileName.Contains("large-v1")) return GgmlType.LargeV1;
            if (fileName.Contains("large-v2")) return GgmlType.LargeV2;
            if (fileName.Contains("large-v3")) return GgmlType.LargeV3;
            if (fileName.Contains("large")) return GgmlType.LargeV3; // Default large to v3

            return GgmlType.Base;
        }

        /// <summary>
        /// Performs the actual audio transcription using Whisper.net
        /// </summary>
        private async Task<AudioTranscriptionResult> PerformTranscriptionAsync(Stream audioStream, string fileName, string language)
        {
            var originalLanguage = language ?? _config.DefaultLanguage;

            var result = new AudioTranscriptionResult
            {
                Language = originalLanguage,
                Confidence = 0.0,
                Text = string.Empty,
                Metadata = new Dictionary<string, object>
                {
                    ["TranscriptionService"] = "Whisper.net",
                    ["Timestamp"] = DateTime.UtcNow,
                    ["FileName"] = fileName,
                    ["ModelPath"] = _config.ModelPath
                }
            };

            try
            {
                if (_whisperFactory == null)
                    throw new InvalidOperationException("Whisper factory not initialized");

                var threadCount = _config.MaxThreads > 0
                    ? _config.MaxThreads
                    : Environment.ProcessorCount;

                var languageToUse = GetLanguageForWhisper(language ?? _config.DefaultLanguage);

                var builder = _whisperFactory.CreateBuilder()
                    .WithLanguage(languageToUse)
                    .WithThreads(threadCount)
                    .WithProbabilities();

                if (!string.IsNullOrEmpty(_config.PromptHint))
                {
                    builder = builder.WithPrompt(_config.PromptHint);
                }

                using var processor = builder.Build();
                Stream waveStream = null;
                var needsConversion = RequiresConversion(fileName);

                try
                {
                    if (needsConversion)
                    {
                        _logger.LogDebug("Converting audio file to WAV format: {FileName}", SanitizeFileName(fileName));
                        var (convertedStream, _) = await _audioConversionService.ConvertToCompatibleFormatAsync(audioStream, fileName);
                        waveStream = convertedStream;
                    }
                    else
                    {
                        audioStream.Position = 0;
                        waveStream = audioStream;
                    }

                    var transcriptionText = string.Empty;
                    var totalConfidence = 0.0;
                    var segmentCount = 0;
                    var lastSegmentText = string.Empty;
                    var duplicateCount = 0;
                    var skippedLowConfidence = 0;
                    var skippedDuplicates = 0;

                    _logger.LogDebug("Starting Whisper processing on WAV stream ({StreamLength} bytes)", waveStream.Length);

                    var segments = processor.ProcessAsync(waveStream);
                    var enumerator = segments.GetAsyncEnumerator();
                    try
                    {
                        while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var segment = enumerator.Current;
                            var segmentText = segment.Text?.Trim() ?? string.Empty;

                            if (segment.Probability < _config.MinConfidenceThreshold)
                            {
                                _logger.LogDebug("Skipping low-confidence segment (P={Probability}): '{Text}'",
                                    segment.Probability, segmentText);
                                skippedLowConfidence++;
                                continue;
                            }

                            if (segmentText == lastSegmentText)
                            {
                                duplicateCount++;
                                if (duplicateCount > 2)
                                {
                                    _logger.LogDebug("Skipping duplicate segment (#{Count}): '{Text}'",
                                        duplicateCount, segmentText);
                                    skippedDuplicates++;
                                    continue;
                                }
                            }
                            else
                            {
                                duplicateCount = 0;
                                lastSegmentText = segmentText;
                            }

                            _logger.LogDebug("Whisper segment: Start={Start}, End={End}, Text='{Text}', Probability={Probability}",
                                segment.Start, segment.End, segment.Text, segment.Probability);

                            transcriptionText += segment.Text + " ";
                            totalConfidence += segment.Probability;
                            segmentCount++;

                            List<AudioSegmentMetadata> typedSegments;
                            if (result.Metadata.TryGetValue("Segments", out var segmentsMetadata))
                            {
                                typedSegments = segmentsMetadata as List<AudioSegmentMetadata>;
                                if (typedSegments == null)
                                {
                                    typedSegments = new List<AudioSegmentMetadata>();
                                    result.Metadata["Segments"] = typedSegments;
                                }
                            }
                            else
                            {
                                typedSegments = new List<AudioSegmentMetadata>();
                                result.Metadata["Segments"] = typedSegments;
                            }

                            typedSegments.Add(new AudioSegmentMetadata
                            {
                                Start = segment.Start.TotalSeconds,
                                End = segment.End.TotalSeconds,
                                Text = segmentText,
                                Probability = segment.Probability
                            });
                        }

                        _logger.LogInformation("Whisper processing completed: {SegmentCount} segments processed, {SkippedLowConf} low-confidence skipped, {SkippedDup} duplicates skipped",
                            segmentCount, skippedLowConfidence, skippedDuplicates);
                    }
                    finally
                    {
                        await enumerator.DisposeAsync().ConfigureAwait(false);
                    }

                    result.Text = transcriptionText.Trim();
                    result.Confidence = segmentCount > 0 ? totalConfidence / segmentCount : 0.0;

                    if (result.Confidence < _config.MinConfidenceThreshold)
                    {
                        _logger.LogWarning("Transcription confidence {Confidence} below threshold {Threshold}",
                            result.Confidence, _config.MinConfidenceThreshold);
                        result.Text = string.Empty;
                        result.Confidence = 0.0;
                    }

                    return result;
                }
                finally
                {
                    if (needsConversion && waveStream != null && waveStream != audioStream)
                    {
                        waveStream.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Whisper transcription processing failed");
                throw;
            }
        }

        /// <summary>
        /// Converts language parameter for Whisper.net: "auto" or null means auto-detect (null)
        /// </summary>
        private static string GetLanguageForWhisper(string language)
        {
            if (string.IsNullOrEmpty(language) || language.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return language;
        }

        /// <summary>
        /// Validates the audio stream for processing
        /// </summary>
        private void ValidateAudioStream(Stream audioStream)
        {
            if (audioStream.Length == 0)
                throw new ArgumentException("Audio stream is empty");

            if (!audioStream.CanRead)
                throw new ArgumentException("Audio stream is not readable");
        }

        /// <summary>
        /// Determines if audio file requires conversion to WAV
        /// </summary>
        private static bool RequiresConversion(string fileName)
        {
            return AudioConversionService.RequiresConversion(fileName);
        }

        /// <summary>
        /// Sanitizes file name for logging to prevent log injection
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unknown";

            return fileName.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
                return;
            _whisperFactory?.Dispose();
            _whisperFactory = null;

            _disposed = true;

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

