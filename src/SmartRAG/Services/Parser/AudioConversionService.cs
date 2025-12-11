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
        private const string FfmpegDownloadUrl = "https://ffmpeg.org/download.html";

        private readonly ILogger<AudioConversionService> _logger;
        private static bool _ffmpegInitialized = false;
        private static readonly System.Threading.SemaphoreSlim _ffmpegSemaphore = new System.Threading.SemaphoreSlim(1, 1);

        public AudioConversionService(ILogger<AudioConversionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

            audioStream.Position = 0;

            var tempInputFile = Path.GetTempFileName() + extension;
            var tempOutputFile = Path.GetTempFileName() + ".wav"; // WAV format (Whisper.net preferred)

            try
            {
                _logger.LogInformation("Starting audio conversion: {Extension} → Compatible Format", extension);

                await EnsureFfmpegInitializedAsync();

                using (var tempFileStream = File.Create(tempInputFile))
                {
                    await audioStream.CopyToAsync(tempFileStream).ConfigureAwait(false);
                }

                _logger.LogDebug("Input file saved: {InputFile} ({Size} bytes)", tempInputFile, new FileInfo(tempInputFile).Length);

                try
                {
                    _logger.LogInformation("Running FFmpeg conversion: {InputFile} → {OutputFile}", tempInputFile, tempOutputFile);

                    var conversionStrategies = new[]
                    {
                    new { Codec = "pcm_s16le", Args = new[] { "-ac 1", "-f wav" }, Ext = ".wav", Name = "WAV" },
                    new { Codec = "flac", Args = new[] { "-ac 1" }, Ext = ".flac", Name = "FLAC" },
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

                    tempOutputFile = finalOutputFile; var outputFileInfo = new FileInfo(tempOutputFile);
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
                try
                {
                    if (File.Exists(tempInputFile))
                        File.Delete(tempInputFile);
                    if (File.Exists(tempOutputFile))
                        File.Delete(tempOutputFile);
                }
                catch
                {
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

        /// <summary>
        /// Ensures FFmpeg is initialized and available
        /// </summary>
        private async Task EnsureFfmpegInitializedAsync()
        {
            if (_ffmpegInitialized)
                return;

            await _ffmpegSemaphore.WaitAsync();

            try
            {
                if (_ffmpegInitialized)
                    return;

                try
                {
                    var ffmpegDir = Path.Combine(Path.GetTempPath(), "SmartRAG", "ffmpeg");
                    if (!Directory.Exists(ffmpegDir))
                    {
                        Directory.CreateDirectory(ffmpegDir);
                    }

                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegDir);

                    GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegDir });

                    _ffmpegInitialized = true;
                    _logger.LogInformation("FFmpeg initialized successfully at {Path}", ffmpegDir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FFmpeg auto-download failed. Falling back to system FFmpeg.");
                    _ffmpegInitialized = true;
                }
            }
            finally
            {
                _ffmpegSemaphore.Release();
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
    }
}
