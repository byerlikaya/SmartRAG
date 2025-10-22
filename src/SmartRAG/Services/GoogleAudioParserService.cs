using Google.Cloud.Speech.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Models;
using SmartRAG.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Service for parsing audio files and extracting text using Google Speech-to-Text.
    /// Supports multiple audio formats including MP3, WAV, M4A, AAC, OGG, FLAC, and WMA.
    /// Provides enterprise-grade speech recognition with confidence scoring and detailed results.
    /// </summary>
    public class GoogleAudioParserService : IAudioParserService
    {
        private readonly ILogger<GoogleAudioParserService> _logger;
        private readonly SmartRagOptions _options;
        private SpeechClient _speechClient;

        public GoogleAudioParserService(ILogger<GoogleAudioParserService> logger, IOptions<SmartRagOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Transcribes audio content from a stream using Google Speech-to-Text
        /// </summary>
        public async Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string language = null)
        {
            if (audioStream == null)
                throw new ArgumentNullException(nameof(audioStream));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            try
            {
                _logger.LogInformation("Starting audio transcription for {Size} bytes in language {Language}", 
                    audioStream.Length, SanitizeLanguageParameter(language ?? "auto"));

                // Validate audio stream
                ValidateAudioStream(audioStream);

                // Create transcription options
                var options = CreateTranscriptionOptions(language);

                // Perform transcription
                var result = await PerformTranscriptionAsync(audioStream, fileName, options);

                _logger.LogInformation("Audio transcription completed: {Length} characters with {Confidence} confidence",
                    result.Text?.Length ?? 0, result.Confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Audio transcription failed: {Error}", SanitizeErrorMessage(ex.Message));
                throw;
            }
        }


        /// <summary>
        /// Creates transcription options from language parameter
        /// </summary>
        private AudioTranscriptionOptions CreateTranscriptionOptions(string language)
        {
            var config = _options.GoogleSpeechConfig;
            if (config == null)
            {
                throw new InvalidOperationException("Google Speech-to-Text configuration not found");
            }

            return new AudioTranscriptionOptions
            {
                Language = language ?? config.DefaultLanguage ?? "tr-TR",
                MinConfidenceThreshold = config.MinConfidenceThreshold,
                IncludeWordTimestamps = config.IncludeWordTimestamps
            };
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
        /// Performs the actual audio transcription using Google Speech-to-Text
        /// </summary>
        private async Task<AudioTranscriptionResult> PerformTranscriptionAsync(Stream audioStream, string fileName, AudioTranscriptionOptions options)
        {
            var result = new AudioTranscriptionResult
            {
                Language = options.Language,
                Confidence = 0.0,
                Text = string.Empty,
                Metadata = new Dictionary<string, object>
                {
                    ["TranscriptionService"] = "Google Speech-to-Text",
                    ["Timestamp"] = DateTime.UtcNow,
                    ["FileName"] = fileName,
                    ["Options"] = options
                }
            };

            try
            {
                // Initialize Google Speech client
                await InitializeSpeechClient();

                // Reset stream position
                audioStream.Position = 0;

                // Read audio data
                var audioBytes = new byte[audioStream.Length];
                await audioStream.ReadAsync(audioBytes, 0, audioBytes.Length);

                // Create recognition request
                var request = CreateRecognitionRequest(audioBytes, options);

                // Perform recognition
                var response = await _speechClient.RecognizeAsync(request);

                // Process results
                ProcessRecognitionResponse(response, result, options);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Google Speech-to-Text transcription failed: {Error}", SanitizeErrorMessage(ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Initializes Google Speech-to-Text client
        /// </summary>
        private async Task InitializeSpeechClient()
        {
            if (_speechClient == null)
            {
                var config = _options.GoogleSpeechConfig;
                if (config == null || string.IsNullOrEmpty(config.ApiKey))
                {
                    throw new InvalidOperationException("Google Speech-to-Text API key not configured");
                }

                // For development, you can set GOOGLE_APPLICATION_CREDENTIALS environment variable
                // or use API key directly
                if (!string.IsNullOrEmpty(config.ApiKey))
                {
                    // If ApiKey is a file path to service account JSON
                    if (File.Exists(config.ApiKey))
                    {
                        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", config.ApiKey);
                    }
                    // If ApiKey is actual API key (for REST API calls)
                    else
                    {
                        _logger.LogWarning("API key provided but Google Speech-to-Text requires service account JSON file. Please provide path to service account JSON file.");
                        throw new InvalidOperationException("Google Speech-to-Text requires service account JSON file path, not API key");
                    }
                }

                _speechClient = await SpeechClient.CreateAsync();
                _logger.LogDebug("Google Speech-to-Text client initialized");
            }
        }

        /// <summary>
        /// Creates recognition request for Google Speech-to-Text
        /// </summary>
        private RecognizeRequest CreateRecognitionRequest(byte[] audioBytes, AudioTranscriptionOptions options)
        {
            var config = _options.GoogleSpeechConfig;

            var recognitionConfig = new RecognitionConfig
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.WebmOpus, // Default encoding
                SampleRateHertz = 16000, // Default sample rate
                LanguageCode = options.Language,
                EnableAutomaticPunctuation = config.EnableAutomaticPunctuation,
                MaxAlternatives = 1
            };

            // Set speaker count if diarization is enabled
            if (config.EnableSpeakerDiarization && config.MaxSpeakerCount > 0)
            {
                recognitionConfig.DiarizationConfig = new SpeakerDiarizationConfig
                {
                    MinSpeakerCount = 1,
                    MaxSpeakerCount = config.MaxSpeakerCount
                };
            }

            // Set audio format based on file extension
            var audioFormat = DetectAudioFormat(audioBytes);
            recognitionConfig.Encoding = audioFormat;

            var audio = RecognitionAudio.FromBytes(audioBytes);

            return new RecognizeRequest
            {
                Config = recognitionConfig,
                Audio = audio
            };
        }

        /// <summary>
        /// Detects audio format from byte data
        /// </summary>
        private RecognitionConfig.Types.AudioEncoding DetectAudioFormat(byte[] audioBytes)
        {
            // Check file headers to determine format
            if (audioBytes.Length >= 4)
            {
                // Check for WAV format (RIFF header)
                if (audioBytes[0] == 0x52 && audioBytes[1] == 0x49 && audioBytes[2] == 0x46 && audioBytes[3] == 0x46)
                {
                    return RecognitionConfig.Types.AudioEncoding.Linear16;
                }

                // Check for MP3 format (ID3 or MP3 header)
                if ((audioBytes[0] == 0x49 && audioBytes[1] == 0x44 && audioBytes[2] == 0x33) ||
                    (audioBytes[0] == 0xFF && (audioBytes[1] & 0xE0) == 0xE0))
                {
                    return RecognitionConfig.Types.AudioEncoding.Mp3;
                }

                // Check for FLAC format
                if (audioBytes[0] == 0x66 && audioBytes[1] == 0x4C && audioBytes[2] == 0x61 && audioBytes[3] == 0x43)
                {
                    return RecognitionConfig.Types.AudioEncoding.Flac;
                }
            }

            // Default to WebM Opus for compressed audio
            return RecognitionConfig.Types.AudioEncoding.WebmOpus;
        }

        /// <summary>
        /// Processes recognition response from Google Speech-to-Text
        /// </summary>
        private void ProcessRecognitionResponse(RecognizeResponse response, AudioTranscriptionResult result, AudioTranscriptionOptions options)
        {
            if (response.Results.Count == 0)
            {
                _logger.LogWarning("No recognition results returned from Google Speech-to-Text");
                return;
            }

            var bestResult = response.Results[0];
            var bestAlternative = bestResult.Alternatives[0];

            result.Text = bestAlternative.Transcript;
            result.Confidence = bestAlternative.Confidence;

            // Filter by confidence threshold
            if (result.Confidence < options.MinConfidenceThreshold)
            {
                _logger.LogWarning("Recognition confidence {Confidence} below threshold {Threshold}",
                    result.Confidence, options.MinConfidenceThreshold);
                result.Text = string.Empty;
                result.Confidence = 0.0;
                return;
            }

            // Add word timestamps if requested
            if (options.IncludeWordTimestamps && bestAlternative.Words.Count > 0)
            {
                result.Metadata["WordTimestamps"] = bestAlternative.Words;
            }

            _logger.LogDebug("Recognition successful: '{Text}' with confidence {Confidence}",
                result.Text, result.Confidence);
        }

        /// <summary>
        /// Sanitizes language parameter to prevent log injection attacks
        /// </summary>
        /// <param name="language">Language parameter to sanitize</param>
        /// <returns>Sanitized language parameter</returns>
        private static string SanitizeLanguageParameter(string language)
        {
            if (string.IsNullOrEmpty(language))
                return "auto";

            // Remove any potentially dangerous characters for logging
            return language.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
        }

        /// <summary>
        /// Sanitizes error message to prevent log injection attacks
        /// </summary>
        /// <param name="errorMessage">Error message to sanitize</param>
        /// <returns>Sanitized error message</returns>
        private static string SanitizeErrorMessage(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return "Unknown error";

            // Remove any potentially dangerous characters for logging
            return errorMessage.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim();
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            // Google Speech client doesn't need explicit shutdown
            _speechClient = null;
        }
    }
}