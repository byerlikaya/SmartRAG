using FFMpegCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Xabe.FFmpeg.Downloader;

namespace SmartRAG.Services.Parser
{
    /// <summary>
    /// Service for converting audio files to WAV format using FFMpeg.
    /// Supports multiple audio formats including M4A, AAC, MP3, OGG, FLAC, and WMA.
    /// </summary>
    public class AudioConversionService
    {
        #region Constants

        private const string FfmpegDownloadUrl = "https://ffmpeg.org/download.html";

        #endregion

        #region Fields

        private readonly ILogger<AudioConversionService> _logger;
        private static bool _ffmpegInitialized = false;
        private static readonly object _ffmpegLock = new object();

        #endregion

        #region Constructor

        public AudioConversionService(ILogger<AudioConversionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts audio stream to Whisper.net compatible format
        /// </summary>
        public async Task<(Stream Stream, string FilePath)> ConvertToCompatibleFormatAsync(Stream audioStream, string fileName)
        {
            if (audioStream == null)
                throw new ArgumentNullException(nameof(audioStream));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            // Reset stream position
            audioStream.Position = 0;

            var tempInputFile = Path.GetTempFileName() + extension;
            var tempOutputFile = Path.GetTempFileName() + ".wav"; // WAV format (Whisper.net preferred)

            try
            {
                _logger.LogInformation("Starting audio conversion: {Extension} → Compatible Format", extension);

                // Ensure FFmpeg is initialized
                EnsureFfmpegInitialized();

                // Save input stream to temp file
                using (var tempFileStream = File.Create(tempInputFile))
                {
                    await audioStream.CopyToAsync(tempFileStream).ConfigureAwait(false);
                }

                _logger.LogDebug("Input file saved: {InputFile} ({Size} bytes)", tempInputFile, new FileInfo(tempInputFile).Length);

                // Convert to WAV using FFMpeg
                // 16kHz, 16-bit, mono - Standard for speech recognition
                try
                {
                _logger.LogInformation("Running FFmpeg conversion: {InputFile} → {OutputFile}", tempInputFile, tempOutputFile);
                
                // Try multiple conversion strategies for better compatibility
                var conversionStrategies = new[]
                {
                    // Strategy 1: WAV (Whisper.net preferred)
                    new { Codec = "pcm_s16le", Args = new[] { "-ac 1", "-f wav" }, Ext = ".wav", Name = "WAV" },
                    // Strategy 2: FLAC (Lossless)
                    new { Codec = "flac", Args = new[] { "-ac 1" }, Ext = ".flac", Name = "FLAC" },
                    // Strategy 3: MP3 (Fallback)
                    new { Codec = "mp3", Args = new[] { "-ac 1", "-b:a 128k" }, Ext = ".mp3", Name = "MP3" }
                };

                bool conversionSuccessful = false;
                string finalOutputFile = tempOutputFile;

                foreach (var strategy in conversionStrategies)
                {
                    try
                    {
                        var strategyOutputFile = Path.GetTempFileName() + strategy.Ext;
                        _logger.LogInformation("Trying conversion strategy: {Strategy} ({Codec})", strategy.Name, strategy.Codec);
                        
                        await FFMpegArguments
                            .FromFileInput(tempInputFile)
                            .OutputToFile(strategyOutputFile, overwrite: true, options => options
                                .WithAudioCodec(strategy.Codec)
                                .WithAudioSamplingRate(16000)
                                .WithCustomArgument(string.Join(" ", strategy.Args)))
                            .ProcessAsynchronously();
                        
                        // Verify file was created and has content
                        var fileInfo = new FileInfo(strategyOutputFile);
                        if (fileInfo.Exists && fileInfo.Length > 0)
                        {
                            _logger.LogInformation("✓ Conversion successful: {Strategy} ({Size} bytes)", strategy.Name, fileInfo.Length);
                            finalOutputFile = strategyOutputFile;
                            conversionSuccessful = true;
                            break;
                        }
                        else
                        {
                            _logger.LogWarning("✗ Conversion failed: {Strategy} - Empty or missing file", strategy.Name);
                            if (File.Exists(strategyOutputFile))
                                File.Delete(strategyOutputFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("✗ Conversion strategy {Strategy} failed: {Error}", strategy.Name, ex.Message);
                    }
                }

                if (!conversionSuccessful)
                {
                    throw new InvalidOperationException("All conversion strategies failed. The audio file may be corrupted or incompatible.");
                }

                // Update tempOutputFile to the successful conversion
                tempOutputFile = finalOutputFile;
                    
                _logger.LogInformation("FFmpeg conversion completed successfully");
                
                // Log converted file details for debugging
                var outputFileInfo = new FileInfo(tempOutputFile);
                _logger.LogInformation("Converted file created: {Size} bytes, Path: {Path}", outputFileInfo.Length, tempOutputFile);
                }
                catch (Exception ffmpegEx)
                {
                    _logger.LogError(ffmpegEx, "FFmpeg conversion failed. Is FFmpeg installed?");
                    throw new InvalidOperationException(
                        $"FFmpeg is required for audio format conversion but not found or failed to execute. " +
                        $"Please install FFmpeg from {FfmpegDownloadUrl} or convert your audio to WAV format manually.",
                        ffmpegEx);
                }

                // Read converted WAV file into memory stream
                var outputStream = new MemoryStream();
                using (var wavFileStream = File.OpenRead(tempOutputFile))
                {
                    await wavFileStream.CopyToAsync(outputStream).ConfigureAwait(false);
                }

                outputStream.Position = 0;

                _logger.LogDebug("Audio conversion completed: {InputSize} → {OutputSize} bytes",
                    new FileInfo(tempInputFile).Length, outputStream.Length);

                return (outputStream, tempOutputFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio format conversion failed for {FileName}", SanitizeFileName(fileName));
                throw;
            }
            finally
            {
                // Cleanup temp files
                try
                {
                    if (File.Exists(tempInputFile))
                        File.Delete(tempInputFile);
                    if (File.Exists(tempOutputFile))
                        File.Delete(tempOutputFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Determines if audio file requires conversion to WAV
        /// </summary>
        public static bool RequiresConversion(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension != ".wav";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures FFmpeg is initialized and available
        /// </summary>
        private void EnsureFfmpegInitialized()
        {
            if (_ffmpegInitialized)
                return;

            lock (_ffmpegLock)
            {
                if (_ffmpegInitialized)
                    return;

                try
                {
                    _logger.LogInformation("Initializing FFmpeg for audio conversion...");

                    // Create FFmpeg directory
                    var ffmpegDir = Path.Combine(Path.GetTempPath(), "SmartRAG", "ffmpeg");
                    if (!Directory.Exists(ffmpegDir))
                    {
                        Directory.CreateDirectory(ffmpegDir);
                    }

                    // Download FFmpeg if not already present
                    var task = Task.Run(async () =>
                    {
                        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegDir);
                    });
                    task.Wait();

                    // Configure FFMpegCore to use downloaded binaries
                    GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegDir });

                    _ffmpegInitialized = true;
                    _logger.LogInformation("FFmpeg initialized successfully at {Path}", ffmpegDir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FFmpeg auto-download failed. Falling back to system FFmpeg.");
                    // Try to use system FFmpeg if download fails
                    _ffmpegInitialized = true;
                }
            }
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
    }
}
