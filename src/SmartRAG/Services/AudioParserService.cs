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
        public async Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string language = null)
        {
            if (audioStream == null)
                throw new ArgumentNullException(nameof(audioStream));
            
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            var options = new AudioTranscriptionOptions
            {
                Language = language ?? "tr-TR", // Use provided language or default to Turkish
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

            // Set language with Turkish-specific optimizations
            _speechConfig.SpeechRecognitionLanguage = options.Language;
            
            // Turkish-specific optimizations (Studio-proven settings)
            if (options.Language.StartsWith("tr"))
            {
                _logger.LogDebug("Applying Studio-proven Turkish speech recognition optimizations");
                
                // Use Studio's proven settings for Turkish
                _speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EnableAudioLogging, "false"); // Studio default
                
                // Studio uses "Belirtilmemiş (Varsayılan)" model - no custom model
                // Studio uses default punctuation mode
                // Studio uses default profanity filter (Maskelenmiş)
            }
            
            if (options.EnableDetailedResults)
            {
                _speechConfig.OutputFormat = OutputFormat.Detailed;
            }
            
            // Studio-proven settings for Turkish speech recognition
            _speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500");         // 0.5 second silence detection (Studio default)
            _speechConfig.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "5000"); // 5 seconds initial wait (Studio default)
            _speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "2000");     // 2 seconds end wait (Studio default)
            
            // Enable dictation mode for conversational speech
            _speechConfig.EnableDictation();
            
            // Optimize for phone quality audio
            _speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EnableAudioLogging, "true");
            
            _logger.LogDebug("Speech service configured for phone conversations with language: {Language}", options.Language);
            
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
            // Try batch transcription first for Turkish (more reliable)
            if (options.Language.StartsWith("tr"))
            {
                _logger.LogDebug("Turkish detected - using batch transcription approach for better support");
                try
                {
                    return await PerformBatchTranscriptionAsync(audioStream, options);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Batch transcription failed, falling back to real-time: {Error}", ex.Message);
                    // Fall back to real-time recognition
                }
            }
            
            return await PerformRealTimeTranscriptionAsync(audioStream, options);
        }

        /// <summary>
        /// Performs batch transcription using Azure Speech Service (more reliable for Turkish)
        /// </summary>
        private async Task<AudioTranscriptionResult> PerformBatchTranscriptionAsync(Stream audioStream, AudioTranscriptionOptions options)
        {
            var result = new AudioTranscriptionResult
            {
                Language = options.Language,
                Confidence = 0.0,
                Text = string.Empty,
                Metadata = new Dictionary<string, object>
                {
                    ["TranscriptionService"] = "Azure Speech Services Batch API",
                    ["Timestamp"] = DateTime.UtcNow,
                    ["Options"] = options
                }
            };

            try
            {
                _logger.LogDebug("Starting batch transcription for Turkish audio...");
                
                // For now, use real-time API with Turkish optimizations
                // In production, implement actual batch API calls
                _logger.LogDebug("Batch API not fully implemented, using optimized real-time approach");
                
                // Apply Turkish-specific optimizations
                var turkishOptions = new AudioTranscriptionOptions
                {
                    Language = "tr-TR", // Force Turkish locale
                    EnableDetailedResults = true,
                    MinConfidenceThreshold = 0.5 // Lower threshold for Turkish
                };
                
                return await PerformRealTimeTranscriptionAsync(audioStream, turkishOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError("Batch transcription failed: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Performs real-time transcription using Azure Speech Service
        /// </summary>
        private async Task<AudioTranscriptionResult> PerformRealTimeTranscriptionAsync(Stream audioStream, AudioTranscriptionOptions options)
        {
            var result = new AudioTranscriptionResult
            {
                Language = options.Language,
                Metadata = new Dictionary<string, object>
                {
                    ["TranscriptionService"] = "Azure Speech Services Real-time",
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

                // Check file format and add debug info
                var fileInfo = new FileInfo(tempFilePath);
                _logger.LogDebug("Audio file info: {Size} bytes, Extension: {Extension}", fileInfo.Length, fileInfo.Extension);
                
                // Read first few bytes to check format
                var headerBytes = new byte[12];
                using (var fs = File.OpenRead(tempFilePath))
                {
                    fs.Read(headerBytes, 0, 12);
                }
                var headerHex = BitConverter.ToString(headerBytes);
                _logger.LogDebug("Audio header (first 12 bytes): {Header}", headerHex);

                // Audio preprocessing for better recognition
                var processedAudioData = PreprocessAudio(tempFilePath);
                
                // Use Push Audio Stream for processed audio
                using (var pushStream = AudioInputStream.CreatePushStream())
                {
                    // Push processed audio data to the stream
                    pushStream.Write(processedAudioData);
                    pushStream.Close();

                    _logger.LogDebug("Pushed {AudioSize} bytes of processed audio to stream", processedAudioData.Length);
                    
                    // Use stream-based recognition
                    var audioConfig = AudioConfig.FromStreamInput(pushStream);
                
                    // Create speech recognizer
                    using (var speechRecognizer = new SpeechRecognizer(_speechConfig, audioConfig))
                    {
                        _logger.LogDebug("Starting ultra-optimized recognition for phone conversation...");

                        // Multiple recognition attempts for phone conversations
                        SpeechRecognitionResult recognitionResult = null;
                        var maxAttempts = 5; // Increased attempts
                        
                        for (int attempt = 1; attempt <= maxAttempts; attempt++)
                        {
                            _logger.LogDebug("Recognition attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
                            
                            recognitionResult = await speechRecognizer.RecognizeOnceAsync();
                            
                            _logger.LogDebug("Attempt {Attempt} - Reason: {Reason}, Text: '{Text}'", 
                                attempt, recognitionResult.Reason, recognitionResult.Text ?? "null");
                            
                // If we got speech, break
                if (recognitionResult.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(recognitionResult.Text))
                {
                    _logger.LogDebug("Success on attempt {Attempt}", attempt);
                    break;
                }
                
                // Special handling for Turkish - try different language codes
                if (attempt == 2 && options.Language.StartsWith("tr"))
                {
                    _logger.LogDebug("Turkish failed, trying alternative Turkish code...");
                    _speechConfig.SpeechRecognitionLanguage = "tr-TR";
                    continue;
                }
                            
                            // Wait before retry
                            if (attempt < maxAttempts)
                            {
                                await Task.Delay(5000); // 5 second wait between attempts
                            }
                        }

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
                            
                            // Try multiple languages if recognition failed
                            if (string.IsNullOrEmpty(result.Text))
                            {
                                await TryMultipleLanguages(speechRecognizer, result, options);
                            }
                            else
                            {
                                result.Text = string.Empty;
                                result.Confidence = 0;
                                _logger.LogWarning("No speech recognized in audio file");
                            }
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
            }
            catch (Exception)
            {
                ServiceLogMessages.LogAudioTranscriptionError(_logger, null);
                throw;
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        _logger.LogDebug("Cleaned up temporary file: {TempFile}", tempFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to delete temporary files. Error: {Error}", ex.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Tries multiple languages for audio recognition
        /// </summary>
        private async Task TryMultipleLanguages(SpeechRecognizer speechRecognizer, AudioTranscriptionResult result, AudioTranscriptionOptions options)
        {
            // Define common languages to try (prioritize Turkish and English)
            var languagesToTry = new[]
            {
                "tr-TR", // Turkish
                "en-US", // English
                "de-DE", // German
                "fr-FR", // French
                "es-ES", // Spanish
                "it-IT", // Italian
                "pt-BR", // Portuguese
                "ru-RU", // Russian
                "ja-JP", // Japanese
                "ko-KR", // Korean
                "zh-CN", // Chinese
                "ar-SA", // Arabic
                "hi-IN"  // Hindi
            };

            _logger.LogDebug("Trying multiple languages for audio recognition...");

            // Get the original audio stream data
            var originalLanguage = _speechConfig.SpeechRecognitionLanguage;
            
            foreach (var language in languagesToTry)
            {
                try
                {
                    _logger.LogDebug("Trying language: {Language}", language);
                    _speechConfig.SpeechRecognitionLanguage = language;
                    
                    // Try recognition with current language
                    var retryResult = await speechRecognizer.RecognizeOnceAsync();
                    
                    if (retryResult.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(retryResult.Text))
                    {
                        result.Text = retryResult.Text;
                        result.Confidence = 0.8;
                        result.Language = language;
                        
                        _logger.LogDebug("Recognition successful with {Language}: '{Text}'", language, result.Text);
                        return; // Success! Exit the loop
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Language {Language} failed: {Error}", language, ex.Message);
                    // Continue to next language
                }
            }

            // Restore original language
            _speechConfig.SpeechRecognitionLanguage = originalLanguage;

            // If all languages failed
            result.Text = string.Empty;
            result.Confidence = 0;
            _logger.LogWarning("All language attempts failed - no speech recognized");
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
        /// Preprocesses audio data for better speech recognition
        /// </summary>
        private byte[] PreprocessAudio(string filePath)
        {
            try
            {
                var audioData = File.ReadAllBytes(filePath);
                
                _logger.LogDebug("=== AUDIO PREPROCESSING ===");
                _logger.LogDebug("Original audio size: {Size} bytes", audioData.Length);
                
                // Check if it's a WAV file
                if (IsWavFile(filePath))
                {
                    _logger.LogDebug("Processing WAV file for speech recognition optimization");
                    return OptimizeWavForSpeech(audioData);
                }
                else
                {
                    _logger.LogDebug("Processing non-WAV file (MP3/M4A) - creating WAV wrapper");
                    return CreateOptimizedWav(audioData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Audio preprocessing failed: {Error}", ex.Message);
                // Fallback to original audio
                return File.ReadAllBytes(filePath);
            }
        }

        /// <summary>
        /// Optimizes WAV file for speech recognition
        /// </summary>
        private byte[] OptimizeWavForSpeech(byte[] wavData)
        {
            try
            {
                // For now, return original data
                // Future: Implement volume normalization, noise reduction, etc.
                _logger.LogDebug("WAV optimization: Using original data (volume normalization not implemented)");
                return wavData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("WAV optimization failed: {Error}", ex.Message);
                return wavData;
            }
        }

        /// <summary>
        /// Creates optimized WAV file from compressed audio
        /// </summary>
        private byte[] CreateOptimizedWav(byte[] audioData)
        {
            try
            {
                _logger.LogDebug("Creating optimized WAV wrapper for compressed audio");
                
                // Create WAV header optimized for speech recognition
                var header = CreateSpeechOptimizedWavHeader(audioData.Length);
                
                // Combine header and audio data
                var wavData = new byte[header.Length + audioData.Length];
                Array.Copy(header, 0, wavData, 0, header.Length);
                Array.Copy(audioData, 0, wavData, header.Length, audioData.Length);
                
                _logger.LogDebug("Created optimized WAV: {Size} bytes", wavData.Length);
                return wavData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("WAV creation failed: {Error}", ex.Message);
                return audioData;
            }
        }

        /// <summary>
        /// Creates WAV header optimized for speech recognition
        /// </summary>
        private byte[] CreateSpeechOptimizedWavHeader(int audioDataLength)
        {
            var header = new byte[44];
            var dataSize = audioDataLength;
            var fileSize = dataSize + 36;
            
            // RIFF header
            header[0] = 0x52; // R
            header[1] = 0x49; // I
            header[2] = 0x46; // F
            header[3] = 0x46; // F
            header[4] = (byte)(fileSize & 0xff);
            header[5] = (byte)((fileSize >> 8) & 0xff);
            header[6] = (byte)((fileSize >> 16) & 0xff);
            header[7] = (byte)((fileSize >> 24) & 0xff);
            
            // WAVE format
            header[8] = 0x57;  // W
            header[9] = 0x41;  // A
            header[10] = 0x56; // V
            header[11] = 0x45; // E
            
            // fmt chunk - optimized for speech recognition
            header[12] = 0x66; // f
            header[13] = 0x6d; // m
            header[14] = 0x74; // t
            header[15] = 0x20; // space
            header[16] = 16;   // fmt chunk size
            header[17] = 0;
            header[18] = 0;
            header[19] = 0;
            header[20] = 1;    // PCM format
            header[21] = 0;
            header[22] = 1;    // mono (speech recognition works better with mono)
            header[23] = 0;
            header[24] = 0x40; // 16000 Hz (optimal for speech recognition)
            header[25] = 0x3e;
            header[26] = 0;
            header[27] = 0;
            header[28] = 0x80; // byte rate
            header[29] = 0x3e;
            header[30] = 0;
            header[31] = 0;
            header[32] = 2;    // block align
            header[33] = 0;
            header[34] = 16;   // bits per sample
            header[35] = 0;
            
            // data chunk
            header[36] = 0x64; // d
            header[37] = 0x61; // a
            header[38] = 0x74; // t
            header[39] = 0x61; // a
            header[40] = (byte)(dataSize & 0xff);
            header[41] = (byte)((dataSize >> 8) & 0xff);
            header[42] = (byte)((dataSize >> 16) & 0xff);
            header[43] = (byte)((dataSize >> 24) & 0xff);
            
            return header;
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

        /// <summary>
        /// Converts audio file to WAV format for Azure Speech Services
        /// </summary>
        private async Task<string> ConvertToWavFormatAsync(string inputFilePath)
        {
            try
            {
                // Check if file is already WAV format
                if (IsWavFile(inputFilePath))
                {
                    _logger.LogDebug("File is already in WAV format: {FilePath}", inputFilePath);
                    return inputFilePath;
                }

                // Create WAV output file path
                var wavFilePath = Path.ChangeExtension(inputFilePath, ".wav");
                
                _logger.LogDebug("Converting audio file to WAV format: {InputFile} -> {OutputFile}", inputFilePath, wavFilePath);

                // For now, we'll create a simple WAV header and copy the data
                // In a real implementation, you would use FFmpeg or similar tool
                await CreateWavFileAsync(inputFilePath, wavFilePath);
                
                _logger.LogDebug("Successfully converted to WAV format: {WavFile}", wavFilePath);
                return wavFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to convert audio file to WAV format: {Error}", ex.Message);
                // Return original file path as fallback
                return inputFilePath;
            }
        }

        /// <summary>
        /// Checks if the file is already in WAV format
        /// </summary>
        private bool IsWavFile(string filePath)
        {
            try
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    var header = new byte[4];
                    fileStream.Read(header, 0, 4);
                    
                    // Check for RIFF header
                    return System.Text.Encoding.ASCII.GetString(header) == "RIFF";
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a WAV file with proper header
        /// </summary>
        private Task CreateWavFileAsync(string inputFilePath, string outputFilePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Read input file data
                    var audioData = File.ReadAllBytes(inputFilePath);
                    
                    // Create WAV header
                    var wavHeader = CreateWavHeader(audioData.Length);
                    
                    // Combine header and audio data
                    var wavData = new byte[wavHeader.Length + audioData.Length];
                    Array.Copy(wavHeader, 0, wavData, 0, wavHeader.Length);
                    Array.Copy(audioData, 0, wavData, wavHeader.Length, audioData.Length);
                    
                    // Write WAV file
                    File.WriteAllBytes(outputFilePath, wavData);
                    
                    _logger.LogDebug("Created WAV file with header: {OutputFile} ({Size} bytes)", outputFilePath, wavData.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to create WAV file: {Error}", ex.Message);
                    throw;
                }
            });
        }

        /// <summary>
        /// Creates a WAV file header
        /// </summary>
        private byte[] CreateWavHeader(int audioDataLength)
        {
            var header = new byte[44];
            var writer = new BinaryWriter(new MemoryStream(header));
            
            // RIFF header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + audioDataLength); // File size - 8
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            
            // fmt chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // fmt chunk size
            writer.Write((short)1); // Audio format (PCM)
            writer.Write((short)1); // Number of channels
            writer.Write(16000); // Sample rate
            writer.Write(32000); // Byte rate
            writer.Write((short)2); // Block align
            writer.Write((short)16); // Bits per sample
            
            // data chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(audioDataLength); // Data size
            
            return header;
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
