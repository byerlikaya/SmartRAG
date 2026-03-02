using Whisper.net;
using Whisper.net.Ggml;

using SmartRAG.Services.Shared;

namespace SmartRAG.Services.Parser;


/// <summary>
/// Service for parsing audio files using Whisper.net for local transcription.
/// Supports multiple audio formats with 99+ languages and hardware acceleration.
/// </summary>
public class WhisperAudioParserService : IAudioParserService
{
    private static readonly SemaphoreSlim DownloadLock = new(1, 1);
    private static readonly SemaphoreSlim TranscriptionLock = new(1, 1);
    private static readonly SemaphoreSlim FactoryInitLock = new(1, 1);

    private readonly ILogger<WhisperAudioParserService> _logger;
    private readonly WhisperConfig _config;
    private readonly AudioConversionService _audioConversionService;
    private WhisperFactory? _whisperFactory;
    private bool _disposed;
    private bool _whisperUnavailable;
    private bool _whisperUnavailableLogged;

    public WhisperAudioParserService(ILogger<WhisperAudioParserService> logger, IOptions<SmartRagOptions> options, AudioConversionService audioConversionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = options.Value.WhisperConfig;
        _audioConversionService = audioConversionService ?? throw new ArgumentNullException(nameof(audioConversionService));
    }

    /// <summary>
    /// [AI Query] Transcribes audio content from a stream using Whisper.net
    /// </summary>
    public async Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string language = null)
    {
        if (audioStream == null)
            throw new ArgumentNullException(nameof(audioStream));

        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

        if (_whisperUnavailable)
        {
            if (_whisperUnavailableLogged)
                return new AudioTranscriptionResult
                {
                    Language = language ?? _config?.DefaultLanguage,
                    Confidence = 0.0,
                    Text = string.Empty,
                    Metadata = new Dictionary<string, object> { ["TranscriptionService"] = "Whisper.net (unavailable)" }
                };
            _whisperUnavailableLogged = true;
            ServiceLogMessages.LogWhisperUnavailable(_logger, null);
            return new AudioTranscriptionResult
            {
                Language = language ?? _config?.DefaultLanguage,
                Confidence = 0.0,
                Text = string.Empty,
                Metadata = new Dictionary<string, object> { ["TranscriptionService"] = "Whisper.net (unavailable)" }
            };
        }

        try
        {
            ServiceLogMessages.LogWhisperTranscriptionStarting(_logger, SanitizeFileName(fileName), audioStream.Length, null);

            ValidateAudioStream(audioStream);

            await EnsureWhisperFactoryInitializedAsync();

            var result = await PerformTranscriptionAsync(audioStream, fileName, language);

            ServiceLogMessages.LogWhisperTranscriptionCompleted(_logger, result.Text?.Length ?? 0, result.Confidence, null);

            return result;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogWhisperTranscriptionFailed(_logger, SanitizeFileName(fileName), ex);
            throw;
        }
    }

    /// <summary>
    /// Resolves model path to absolute so it works regardless of current directory.
    /// </summary>
    private static string ResolveModelPath(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || Path.IsPathRooted(modelPath))
            return modelPath;
        var baseDir = AppContext.BaseDirectory ?? ".";
        return Path.GetFullPath(Path.Combine(baseDir, modelPath));
    }

    /// <summary>
    /// Ensures Whisper factory is initialized and model is loaded
    /// </summary>
    private async Task EnsureWhisperFactoryInitializedAsync()
    {
        if (_whisperFactory != null)
            return;

        await FactoryInitLock.WaitAsync();
        try
        {
            if (_whisperFactory != null)
                return;

            Services.Startup.WhisperNativeBootstrap.EnsureMacOsWhisperNativeLibraries();

            var resolvedPath = ResolveModelPath(_config.ModelPath);
            ServiceLogMessages.LogWhisperFactoryInitializing(_logger, resolvedPath, null);

            await EnsureModelExistsAsync(resolvedPath);

            var modelType = DetermineModelType(resolvedPath);
            var fileInfo = new FileInfo(resolvedPath);
            var minSize = GetMinimumModelSizeBytes(modelType);
            if (fileInfo.Length < minSize)
            {
                var minMb = minSize / (1024.0 * 1024);
                var actualMb = fileInfo.Length / (1024.0 * 1024);
                throw new InvalidOperationException(
                    $"Whisper model file is too small for {modelType}. File size: {fileInfo.Length} bytes ({actualMb:F1} MB). " +
                    $"Expected at least {minSize} bytes (~{minMb:F0} MB). The file may be truncated or corrupted. " +
                    "Re-download the model or use a smaller model (e.g. ggml-base.bin or ggml-tiny.bin).");
            }

            var useGpu = _config.UseGpu;
            var factoryOptions = new WhisperFactoryOptions { UseGpu = useGpu };
            if (useGpu)
            {
                ServiceLogMessages.LogWhisperGpuEnabled(_logger, null);
            }
            else
            {
                ServiceLogMessages.LogWhisperUsingCpu(_logger, null);
            }

            const long twoGb = 2L * 1024 * 1024 * 1024;
            var usePathOnMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && fileInfo.Length >= twoGb;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !usePathOnMac)
            {
                var modelBytes = await File.ReadAllBytesAsync(resolvedPath).ConfigureAwait(false);
                ServiceLogMessages.LogWhisperModelLoadedToMemory(_logger, modelBytes.Length, null);
                _whisperFactory = WhisperFactory.FromBuffer(modelBytes, factoryOptions);
            }
            else
            {
                ServiceLogMessages.LogWhisperInitializingFromPath(_logger, fileInfo.Length, null);
                _whisperFactory = WhisperFactory.FromPath(resolvedPath, factoryOptions);
            }
        }
        catch (Exception ex)
        {
            var path = ResolveModelPath(_config.ModelPath);
            ServiceLogMessages.LogWhisperModelLoadFailed(_logger, path, ex);
            throw new InvalidOperationException(
                $"Failed to load the Whisper model at '{path}'. Check that the file exists and that Whisper native runtime libraries are installed and loadable.",
                ex);
        }
        finally
        {
            FactoryInitLock.Release();
        }
    }

    /// <summary>
    /// Ensures Whisper model file exists at the resolved path, downloads if necessary.
    /// Download is serialized process-wide to avoid concurrent writes and truncated files.
    /// Writes to a temp file then moves atomically so partial downloads are never used.
    /// </summary>
    private async Task EnsureModelExistsAsync(string resolvedPath)
    {
        await DownloadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var modelType = DetermineModelType(resolvedPath);
            var minSize = GetMinimumModelSizeBytes(modelType);

            if (File.Exists(resolvedPath))
            {
                var length = new FileInfo(resolvedPath).Length;
                if (length >= minSize)
                {
                    ServiceLogMessages.LogWhisperModelFound(_logger, resolvedPath, length, null);
                    return;
                }
                try
                {
                    File.Delete(resolvedPath);
                    ServiceLogMessages.LogWhisperRemovingTruncatedModel(_logger, length, minSize, null);
                }
                catch (IOException ex)
                {
                    ServiceLogMessages.LogWhisperCouldNotRemoveTruncatedModel(_logger, ex);
                    return;
                }
            }

            var modelDirectory = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(modelDirectory) && !Directory.Exists(modelDirectory))
            {
                Directory.CreateDirectory(modelDirectory);
                ServiceLogMessages.LogWhisperCreatedModelDirectory(_logger, modelDirectory, null);
            }

            var tempPath = resolvedPath + ".tmp";
            try
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }

                ServiceLogMessages.LogWhisperDownloadingModel(_logger, modelType.ToString(), resolvedPath, null);

                await using (var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType).ConfigureAwait(false))
                await using (var fileWriter = File.Create(tempPath))
                {
                    await modelStream.CopyToAsync(fileWriter).ConfigureAwait(false);
                }

                if (new FileInfo(tempPath).Length < minSize)
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // ignored
                    }

                    throw new InvalidOperationException(
                        $"Downloaded Whisper model is too small (expected at least {minSize} bytes). Source may be unavailable or rate-limited.");
                }

                if (File.Exists(resolvedPath))
                    File.Delete(resolvedPath);
                File.Move(tempPath, resolvedPath);

                ServiceLogMessages.LogWhisperModelDownloaded(_logger, resolvedPath, null);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
        finally
        {
            DownloadLock.Release();
        }
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
        if (fileName.Contains("large-v3") || fileName.Contains("large")) return GgmlType.LargeV3;

        return GgmlType.Base;
    }

    /// <summary>
    /// Minimum expected file size in bytes for each GGML model type (whisper.cpp reference).
    /// Used to detect truncated or wrong model files before native load.
    /// </summary>
    private static long GetMinimumModelSizeBytes(GgmlType modelType)
    {
        const long mib = 1024L * 1024;
        switch (modelType)
        {
            case GgmlType.Tiny: return 70 * mib;
            case GgmlType.Base: return 130 * mib;
            case GgmlType.Small: return 450 * mib;
            case GgmlType.Medium: return 1400 * mib;
            case GgmlType.LargeV1:
            case GgmlType.LargeV2:
            case GgmlType.LargeV3: return 2800 * mib;
            default: return 70 * mib;
        }
    }

    /// <summary>
    /// Performs the actual audio transcription using Whisper.net
    /// </summary>
    private async Task<AudioTranscriptionResult> PerformTranscriptionAsync(Stream audioStream, string fileName, string language)
    {
        var result = new AudioTranscriptionResult
        {
            Language = language,
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
                : Math.Max(1, Environment.ProcessorCount - 1);

            var languageToUse = GetLanguageForWhisper(language);

            WhisperProcessorBuilder builder;
            try
            {
                builder = _whisperFactory.CreateBuilder();
            }
            catch (Exception ex)
            {
                _whisperUnavailable = true;
                var resolvedPath = ResolveModelPath(_config.ModelPath);
                var modelFileSize = File.Exists(resolvedPath) ? new FileInfo(resolvedPath).Length : 0L;
                ServiceLogMessages.LogWhisperCreateBuilderFailed(_logger, modelFileSize, ex);
                return new AudioTranscriptionResult
                {
                    Language = language,
                    Confidence = 0.0,
                    Text = string.Empty,
                    Metadata = new Dictionary<string, object>
                    {
                        ["TranscriptionService"] = "Whisper.net (unavailable)",
                        ["Timestamp"] = DateTime.UtcNow,
                        ["FileName"] = fileName,
                        ["ModelPath"] = _config.ModelPath,
                        ["Error"] = ex.Message
                    }
                };
            }

            // Do not call WithTranslate(); we always transcribe in the source language, never translate to English.
            var builderWithOptions = builder
                .WithLanguage(languageToUse)
                .WithThreads(threadCount)
                .WithProbabilities();

            if (!string.IsNullOrEmpty(_config.PromptHint))
            {
                builderWithOptions = builderWithOptions.WithPrompt(_config.PromptHint);
            }

            await using var processor = builderWithOptions.Build();
            Stream waveStream = null;
            var needsConversion = AudioConversionService.RequiresConversion(fileName);

            try
            {
                if (needsConversion)
                {
                    ServiceLogMessages.LogWhisperConvertingToWav(_logger, SanitizeFileName(fileName), null);
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

                await TranscriptionLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    ServiceLogMessages.LogWhisperInferenceStarted(_logger, SanitizeFileName(fileName), null);

                    var segments = processor.ProcessAsync(waveStream);
                    var enumerator = segments.GetAsyncEnumerator();
                    try
                    {
                        while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var segment = enumerator.Current;
                            var segmentText = segment.Text.Trim();

                            if (segment.Probability < _config.MinConfidenceThreshold)
                            {
                                skippedLowConfidence++;
                                continue;
                            }

                            if (segmentText == lastSegmentText)
                            {
                                duplicateCount++;
                                if (duplicateCount > 2)
                                {
                                    skippedDuplicates++;
                                    continue;
                                }
                            }
                            else
                            {
                                duplicateCount = 0;
                                lastSegmentText = segmentText;
                            }

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

                        ServiceLogMessages.LogWhisperProcessingCompleted(_logger, segmentCount, skippedLowConfidence, skippedDuplicates, null);
                    }
                    finally
                    {
                        await enumerator.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    TranscriptionLock.Release();
                }

                result.Text = transcriptionText.Trim();
                result.Confidence = segmentCount > 0 ? totalConfidence / segmentCount : 0.0;

                if (!(result.Confidence < _config.MinConfidenceThreshold))
                    return result;
                ServiceLogMessages.LogWhisperConfidenceBelowThreshold(_logger, result.Confidence, _config.MinConfidenceThreshold, null);
                result.Text = string.Empty;
                result.Confidence = 0.0;

                return result;
            }
            finally
            {
                if (needsConversion && waveStream != null && waveStream != audioStream)
                {
                    await waveStream.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogWhisperTranscriptionProcessingFailed(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Converts language parameter for Whisper.net. Pass "auto" explicitly for auto-detect
    /// so the native layer does not default to English (null can be interpreted as "en").
    /// </summary>
    private static string GetLanguageForWhisper(string language)
    {
        if (string.IsNullOrEmpty(language) || language.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return "auto";
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
    /// Sanitizes file name for logging to prevent log injection
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        return string.IsNullOrEmpty(fileName) ? "unknown" : fileName.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _whisperFactory?.Dispose();
        _whisperFactory = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}


