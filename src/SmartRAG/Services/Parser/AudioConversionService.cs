using FFMpegCore;
using Xabe.FFmpeg.Downloader;

using SmartRAG.Services.Shared;

namespace SmartRAG.Services.Parser;


/// <summary>
/// Service for converting audio files to WAV format using FFMpeg.
/// Supports multiple audio formats including M4A, AAC, MP3, OGG, FLAC, and WMA.
/// </summary>
public class AudioConversionService
{
    private const string FfmpegDownloadUrl = "https://ffmpeg.org/download.html";

    private readonly ILogger<AudioConversionService> _logger;
    private static bool _ffmpegInitialized;
    private static readonly SemaphoreSlim FfmpegSemaphore = new(1, 1);

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
            ServiceLogMessages.LogAudioConversionStarting(_logger, extension, null);

            await EnsureFfmpegInitializedAsync();

            await using (var tempFileStream = File.Create(tempInputFile))
            {
                await audioStream.CopyToAsync(tempFileStream).ConfigureAwait(false);
            }

            ServiceLogMessages.LogAudioConversionInputSaved(_logger, tempInputFile, new FileInfo(tempInputFile).Length, null);

            try
            {
                ServiceLogMessages.LogAudioConversionFfmpegRunning(_logger, tempInputFile, tempOutputFile, null);

                var conversionStrategies = new[]
                {
                new { Codec = "pcm_s16le", Args = new[] { "-ac 1", "-f wav" }, Ext = ".wav", Name = "WAV" },
                new { Codec = "flac", Args = new[] { "-ac 1" }, Ext = ".flac", Name = "FLAC" },
                new { Codec = "mp3", Args = new[] { "-ac 1", "-b:a 128k" }, Ext = ".mp3", Name = "MP3" }
            };

                var conversionSuccessful = false;
                var finalOutputFile = tempOutputFile;

                foreach (var strategy in conversionStrategies)
                {
                    try
                    {
                        var strategyOutputFile = Path.GetTempFileName() + strategy.Ext;
                        ServiceLogMessages.LogAudioConversionStrategyTrying(_logger, strategy.Name, strategy.Codec, null);

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
                            ServiceLogMessages.LogAudioConversionStrategySuccess(_logger, strategy.Name, fileInfo.Length, null);
                            finalOutputFile = strategyOutputFile;
                            conversionSuccessful = true;
                            break;
                        }

                        ServiceLogMessages.LogAudioConversionStrategyEmptyFile(_logger, strategy.Name, null);
                        if (File.Exists(strategyOutputFile))
                            File.Delete(strategyOutputFile);
                    }
                    catch (Exception ex)
                    {
                        ServiceLogMessages.LogAudioConversionStrategyFailed(_logger, strategy.Name, ex.Message, null);
                    }
                }

                if (!conversionSuccessful)
                {
                    throw new InvalidOperationException("All conversion strategies failed. The audio file may be corrupted or incompatible.");
                }

                tempOutputFile = finalOutputFile; var outputFileInfo = new FileInfo(tempOutputFile);
                ServiceLogMessages.LogAudioConversionConvertedFileCreated(_logger, outputFileInfo.Length, tempOutputFile, null);
            }
            catch (Exception ffmpegEx)
            {
                ServiceLogMessages.LogAudioConversionFfmpegFailed(_logger, ffmpegEx);
                throw new InvalidOperationException(
                    $"FFmpeg is required for audio format conversion but not found or failed to execute. " +
                    $"Please install FFmpeg from {FfmpegDownloadUrl} or convert your audio to WAV format manually.",
                    ffmpegEx);
            }

            var outputStream = new MemoryStream();
            await using (var wavFileStream = File.OpenRead(tempOutputFile))
            {
                await wavFileStream.CopyToAsync(outputStream).ConfigureAwait(false);
            }

            outputStream.Position = 0;

            ServiceLogMessages.LogAudioConversionCompleted(_logger, new FileInfo(tempInputFile).Length, outputStream.Length, null);

            return (outputStream, tempOutputFile);
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogAudioConversionFailed(_logger, SanitizeFileName(fileName), ex);
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
                // ignored
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

        await FfmpegSemaphore.WaitAsync();

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
                ServiceLogMessages.LogFfmpegInitialized(_logger, ffmpegDir, null);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogFfmpegAutoDownloadFailed(_logger, ex);
                _ffmpegInitialized = true;
            }
        }
        finally
        {
            FfmpegSemaphore.Release();
        }
    }

    /// <summary>
    /// Sanitizes file name for logging to prevent log injection
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        return string.IsNullOrEmpty(fileName) ? "unknown" : fileName.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
    }
}

