using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Service for parsing audio files and extracting text content through Azure Speech Services
    /// </summary>
    public class AudioParserService : IAudioParserService
    {
        #region Constants

        // Audio format validation constants
        private const int MinAudioDurationSeconds = 1;
        private const int MaxAudioDurationHours = 24;
        private const double MinConfidenceThreshold = 0.1;
        private const double MaxConfidenceThreshold = 1.0;

        // Azure Speech Service constants
        private const string DefaultLanguage = "tr-TR";
        private const string DefaultRegion = "eastus";
        private const int DefaultSampleRate = 16000;
        private const int DefaultChannels = 1;

        // File extension constants
        private static readonly string[] AudioExtensions = new string[]
        {
            ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".flac", ".wma"
        };

        // Content type constants
        private static readonly string[] AudioContentTypes = new string[]
        {
            "audio/mpeg", "audio/wav", "audio/mp4", "audio/aac",
            "audio/ogg", "audio/flac", "audio/x-ms-wma"
        };

        #endregion

        #region Fields

        private readonly SmartRagOptions _options;
        private readonly ILogger<AudioParserService> _logger;
        private SpeechConfig _speechConfig;
        private bool _disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AudioParserService
        /// </summary>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance</param>
        public AudioParserService(IOptions<SmartRagOptions> options, ILogger<AudioParserService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Lazy initialization - only initialize when actually needed
            // InitializeSpeechConfig();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Transcribes audio content from a stream to text using default options
        /// </summary>
        public async Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName)
        {
            if (audioStream == null)
                throw new ArgumentNullException(nameof(audioStream));
            
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            var options = new AudioTranscriptionOptions
            {
                Language = "en-US", // Try English first for testing
                EnableDetailedResults = true,
                MinConfidenceThreshold = MinConfidenceThreshold
            };

            return await TranscribeAudioAsync(audioStream, options);
        }

        /// <summary>
        /// Transcribes audio content from a stream to text using custom options
        /// </summary>
        public async Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, AudioTranscriptionOptions options)
        {
            if (audioStream == null)
                throw new ArgumentNullException(nameof(audioStream));
            
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            try
            {
                ServiceLogMessages.LogAudioTranscriptionStarted(_logger, audioStream.Length, options.Language, null);

                // Validate audio stream
                ValidateAudioStream(audioStream);

                // Configure speech service with options
                ConfigureSpeechService(options);

                // Perform transcription
                var result = await PerformTranscriptionAsync(audioStream, options);

                ServiceLogMessages.LogAudioTranscriptionCompleted(_logger, result.Text.Length, result.Confidence, null);

                return result;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogAudioTranscriptionFailed(_logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Checks if the given file name represents a supported audio format
        /// </summary>
        public bool IsSupportedFormat(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AudioExtensions.Contains(extension);
        }

        /// <summary>
        /// Gets a list of supported audio file extensions
        /// </summary>
        public IEnumerable<string> GetSupportedFormats()
        {
            return AudioExtensions.ToList();
        }

        /// <summary>
        /// Gets a list of supported MIME content types for audio
        /// </summary>
        public IEnumerable<string> GetSupportedContentTypes()
        {
            return AudioContentTypes.ToList();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the Azure Speech Service configuration
        /// </summary>
        private void InitializeSpeechConfig()
        {
            try
            {
                var subscriptionKey = GetAzureSpeechKey();
                var region = GetAzureSpeechRegion();

                // Test API key validity
                _logger.LogDebug("Testing Azure Speech Service API key...");
                _logger.LogDebug("API Key (first 10 chars): {ApiKeyPrefix}", subscriptionKey.Substring(0, Math.Min(10, subscriptionKey.Length)));
                _logger.LogDebug("Region: {Region}", region);

                _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
                _speechConfig.SpeechRecognitionLanguage = DefaultLanguage;
                _speechConfig.EnableDictation();

                ServiceLogMessages.LogAudioServiceInitialized(_logger, region, null);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogAudioServiceInitializationFailed(_logger, ex);
                _logger.LogError("Failed to initialize Azure Speech Service. Check API key and region. Error: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Configures the speech service with custom options
        /// </summary>
        private void ConfigureSpeechService(AudioTranscriptionOptions options)
        {
            // Lazy initialization - initialize when first needed
            if (_speechConfig == null)
            {
                InitializeSpeechConfig();
            }

            _speechConfig.SpeechRecognitionLanguage = options.Language;
            
            if (options.EnableDetailedResults)
            {
                _speechConfig.OutputFormat = OutputFormat.Detailed;
            }
            
            // Add additional configuration for better recognition
            _speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "2000");
            _speechConfig.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "5000");
            _speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "2000");
            
            _logger.LogDebug("Speech service configured with language: {Language}", options.Language);
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
        /// Performs the actual audio transcription using Azure Speech Services
        /// </summary>
        private async Task<AudioTranscriptionResult> PerformTranscriptionAsync(Stream audioStream, AudioTranscriptionOptions options)
        {
            var result = new AudioTranscriptionResult
            {
                Language = options.Language,
                Metadata = new Dictionary<string, object>
                {
                    ["TranscriptionService"] = "Azure Speech Services",
                    ["Timestamp"] = DateTime.UtcNow,
                    ["Options"] = options
                }
            };

            string tempFilePath = null;
            try
            {
                // Reset stream position
                audioStream.Position = 0;

                // Log audio stream info
                _logger.LogDebug("Audio stream length: {Length} bytes", audioStream.Length);

                // Create temporary file for audio processing
                tempFilePath = Path.GetTempFileName();
                using (var fileStream = File.Create(tempFilePath))
                {
                    await audioStream.CopyToAsync(fileStream);
                }

                _logger.LogDebug("Created temporary audio file: {TempFile}", tempFilePath);

                // Use file-based recognition instead of stream-based
                var audioConfig = AudioConfig.FromWavFileInput(tempFilePath);
                
                // Create speech recognizer
                using (var speechRecognizer = new SpeechRecognizer(_speechConfig, audioConfig))
                {
                    _logger.LogDebug("Starting file-based recognition...");

                    // Perform recognition
                    var recognitionResult = await speechRecognizer.RecognizeOnceAsync();

                // Debug logging for recognition result
                _logger.LogDebug("Recognition result reason: {Reason}", recognitionResult.Reason);
                _logger.LogDebug("Recognition result text: '{Text}'", recognitionResult.Text ?? "null");
                
                if (recognitionResult.Reason == ResultReason.RecognizedSpeech)
                {
                    result.Text = recognitionResult.Text;
                    // Azure Speech SDK doesn't provide confidence in basic recognition result
                    result.Confidence = 0.8; // Default confidence
                    
                    _logger.LogDebug("Speech recognized successfully: '{Text}'", result.Text);
                    
                    // Add segments if detailed results are enabled
                    if (options.EnableDetailedResults)
                    {
                        ParseDetailedResults(recognitionResult, result, options);
                    }
                }
                else if (recognitionResult.Reason == ResultReason.NoMatch)
                {
                    ServiceLogMessages.LogAudioNoMatch(_logger, null);
                    result.Text = string.Empty;
                    result.Confidence = 0;
                    _logger.LogWarning("No speech recognized in audio file");
                }
                else if (recognitionResult.Reason == ResultReason.Canceled)
                {
                    var errorMessage = $"Recognition canceled: {recognitionResult.Reason}";
                    ServiceLogMessages.LogAudioRecognitionFailed(_logger, errorMessage, null);
                    _logger.LogError("Audio recognition was canceled");
                    throw new InvalidOperationException(errorMessage);
                }
                else
                {
                    var errorMessage = $"Recognition failed: {recognitionResult.Reason}";
                    ServiceLogMessages.LogAudioRecognitionFailed(_logger, errorMessage, null);
                    _logger.LogError("Audio recognition failed with reason: {Reason}", recognitionResult.Reason);
                    throw new InvalidOperationException(errorMessage);
                }
                }
            }
            catch (Exception)
            {
                ServiceLogMessages.LogAudioTranscriptionError(_logger, null);
                throw;
            }
            finally
            {
                // Clean up temporary file
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        _logger.LogDebug("Cleaned up temporary file: {TempFile}", tempFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to delete temporary file: {TempFile}. Error: {Error}", tempFilePath, ex.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Parses detailed results from Azure Speech Services response
        /// </summary>
        private void ParseDetailedResults(SpeechRecognitionResult recognitionResult, AudioTranscriptionResult result, AudioTranscriptionOptions options)
        {
            try
            {
                // This is a simplified implementation
                // In a real implementation, you would parse the JSON response from Azure Speech Services
                // to extract detailed segments with timestamps
                
                var segment = new AudioSegment
                {
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromSeconds(30), // Placeholder
                    Text = result.Text,
                    Confidence = result.Confidence
                };

                if (segment.Confidence >= options.MinConfidenceThreshold)
                {
                    result.Segments.Add(segment);
                }
            }
            catch (Exception)
            {
                ServiceLogMessages.LogAudioSegmentParsingFailed(_logger, null);
                // Continue without detailed segments
            }
        }

        /// <summary>
        /// Gets the Azure Speech Service subscription key from configuration
        /// </summary>
        private string GetAzureSpeechKey()
        {
            // Try configuration first, then environment variables
            if (_options.AzureSpeechConfig != null && !string.IsNullOrEmpty(_options.AzureSpeechConfig.SubscriptionKey))
            {
                return _options.AzureSpeechConfig.SubscriptionKey;
            }
            
            var envKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                return envKey;
            }
            
            throw new InvalidOperationException("Azure Speech Service key not configured");
        }

        /// <summary>
        /// Gets the Azure Speech Service region from configuration
        /// </summary>
        private string GetAzureSpeechRegion()
        {
            if (_options.AzureSpeechConfig != null && !string.IsNullOrEmpty(_options.AzureSpeechConfig.Region))
            {
                return _options.AzureSpeechConfig.Region;
            }
            
            var envRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");
            if (!string.IsNullOrEmpty(envRegion))
            {
                return envRegion;
            }
            
            return DefaultRegion;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the audio parser service resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // SpeechConfig doesn't implement IDisposable in this version
                _speechConfig = null;
                _disposed = true;
            }
        }

        #endregion
    }
}
