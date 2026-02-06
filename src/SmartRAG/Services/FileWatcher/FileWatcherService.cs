
namespace SmartRAG.Services.FileWatcher;


/// <summary>
/// Service for watching file system folders and automatically indexing documents
/// </summary>
public class FileWatcherService : IFileWatcherService
{
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 1000;

    private readonly ILogger<FileWatcherService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SmartRagOptions _options;
    private readonly Dictionary<string, System.IO.FileSystemWatcher> _watchers = new Dictionary<string, System.IO.FileSystemWatcher>();
    private readonly Dictionary<string, WatchedFolderConfig> _configs = new Dictionary<string, WatchedFolderConfig>();
    private readonly Lazy<HashSet<string>> _supportedExtensions;
    private static readonly Dictionary<string, string> ContentTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", "text/plain" },
        { ".md", "text/markdown" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },
        { ".csv", "text/csv" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".doc", "application/msword" },
        { ".pdf", "application/pdf" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".xls", "application/vnd.ms-excel" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".wav", "audio/wav" },
        { ".mp3", "audio/mpeg" },
        { ".m4a", "audio/mp4" },
        { ".db", "application/x-sqlite3" }
    };
    private bool _disposed = false;

    public FileWatcherService(
        ILogger<FileWatcherService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<SmartRagOptions> options)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;

        // Lazy-load supported file types to avoid creating scope in constructor
        _supportedExtensions = new Lazy<HashSet<string>>(() => LoadSupportedExtensions(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Starts watching a folder for file changes
    /// </summary>
    public Task StartWatchingAsync(WatchedFolderConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.FolderPath))
            throw new ArgumentException("FolderPath cannot be null or empty", nameof(config));

        var baseDirectory = Directory.GetCurrentDirectory();
        var sanitizedPath = PathSanitizer.SanitizePath(config.FolderPath, baseDirectory);

        if (!Directory.Exists(sanitizedPath))
        {
            try
            {
                Directory.CreateDirectory(sanitizedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {FolderPath}", sanitizedPath);
                throw new DirectoryNotFoundException($"Folder does not exist and could not be created: {sanitizedPath}", ex);
            }
        }

        if (_watchers.ContainsKey(sanitizedPath))
        {
            if (config.AutoUpload)
            {
                _ = Task.Run(async () => await ScanExistingFilesAsync(sanitizedPath, config));
            }
            return Task.CompletedTask;
        }

        var watcher = new System.IO.FileSystemWatcher(sanitizedPath)
        {
            IncludeSubdirectories = config.IncludeSubdirectories,
            NotifyFilter = System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime,
            Filter = "*.*" // Watch all files, filtering will be done in IsFileAllowed
        };

        watcher.Created += async (sender, e) => await OnFileCreatedAsync(e);
        watcher.Changed += async (sender, e) => await OnFileChangedAsync(e);
        watcher.Deleted += async (sender, e) => await OnFileDeletedAsync(e);
        watcher.Error += OnError;

        watcher.EnableRaisingEvents = true;

        _watchers[sanitizedPath] = watcher;
        _configs[sanitizedPath] = config;

        _logger.LogInformation("Started watching folder: {FolderPath}", sanitizedPath);

        if (config.AutoUpload)
        {
            _ = Task.Run(async () => await ScanExistingFilesAsync(sanitizedPath, config));
        }

        return Task.CompletedTask;
    }
   

    private Task StopAllWatchingAsync()
    {
        foreach (var kvp in _watchers)
        {
            kvp.Value.EnableRaisingEvents = false;
            kvp.Value.Dispose();
        }
        _watchers.Clear();
        _configs.Clear();
        _logger.LogInformation("Stopped watching all folders");
        return Task.CompletedTask;
    }
  

    private async Task OnFileCreatedAsync(System.IO.FileSystemEventArgs e)
    {
        try
        {
            await Task.Delay(1000);

            if (!IsFileAllowed(e.FullPath))
            {
                return;
            }

            if (await GetConfigForPath(e.FullPath) is WatchedFolderConfig config && config.AutoUpload)
            {
                _logger.LogInformation("Auto-uploading file: {FilePath}", e.FullPath);
                await UploadFileAsync(e.FullPath, config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file created event for {FilePath}", e.FullPath);
        }
    }

    private async Task OnFileChangedAsync(System.IO.FileSystemEventArgs e)
    {
        try
        {
            if (!IsFileAllowed(e.FullPath))
                return;

            if (await GetConfigForPath(e.FullPath) is WatchedFolderConfig config && config.AutoUpload)
            {
                await Task.Delay(500);
                await UploadFileAsync(e.FullPath, config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file changed event for {FilePath}", e.FullPath);
        }
    }

    private async Task OnFileDeletedAsync(System.IO.FileSystemEventArgs e)
    {
        try
        {
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file deleted event for {FilePath}", e.FullPath);
        }
    }

    private void OnError(object sender, System.IO.ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "File watcher error occurred");
    }

    private bool IsFileAllowed(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        if (!File.Exists(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        if (!_supportedExtensions.Value.Contains(extension))
        {
            return false;
        }

        var config = GetConfigForPathSync(filePath);
        if (config == null)
        {
            return false;
        }

        if (config.AllowedExtensions != null && config.AllowedExtensions.Count > 0)
        {
            var isAllowed = config.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            return isAllowed;
        }

        return true;
    }

    private WatchedFolderConfig GetConfigForPathSync(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath);

        foreach (var kvp in _configs)
        {
            var watchedPath = Path.GetFullPath(kvp.Key);
            if (kvp.Value.IncludeSubdirectories)
            {
                if (directory.StartsWith(watchedPath, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
            else
            {
                if (string.Equals(directory, watchedPath, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
        }

        return null;
    }

    private async Task<WatchedFolderConfig> GetConfigForPath(string filePath)
    {
        return await Task.FromResult(GetConfigForPathSync(filePath));
    }

    private async Task UploadFileAsync(string filePath, WatchedFolderConfig config)
    {
        var retryCount = 0;
        while (retryCount < MaxRetryAttempts)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File no longer exists: {FilePath}", filePath);
                    return;
                }

                var fileName = Path.GetFileName(filePath);
                var fileInfo = new FileInfo(filePath);
                var contentType = GetContentType(filePath);
                var fileHash = await ComputeFileHashAsync(filePath, CancellationToken.None);

                var existingDocuments = await documentService.GetAllDocumentsAsync();

                foreach (var doc in existingDocuments)
                {
                    object existingHash = null;
                    var hasFileHash = doc.Metadata?.TryGetValue("FileHash", out existingHash) == true;
                    var existingHashStr = existingHash?.ToString() ?? "null";

                    if (hasFileHash && existingHashStr == fileHash)
                    {
                        _logger.LogInformation("Skipping duplicate file: {FileName} (size: {Size} bytes, hash: {Hash}) - Found duplicate with ID: {DuplicateId}",
                            fileName, fileInfo.Length, fileHash, doc.Id);
                        return;
                    }
                }

                using var fileStream = File.OpenRead(filePath);
                var additionalMetadata = new Dictionary<string, object>
                {
                    ["FileHash"] = fileHash,
                    ["FilePath"] = filePath
                };

                var languageToUse = config.Language ?? _options.DefaultLanguage;
                var uploadRequest = new Models.RequestResponse.UploadDocumentRequest
                {
                    FileStream = fileStream,
                    FileName = fileName,
                    ContentType = contentType,
                    UploadedBy = config.UserId,
                    Language = languageToUse,
                    FileSize = fileInfo.Length,
                    AdditionalMetadata = additionalMetadata
                };
                var document = await documentService.UploadDocumentAsync(uploadRequest);

                _logger.LogInformation("Auto-uploaded file: {FilePath} (size: {Size} bytes, hash: {Hash})", filePath, fileInfo.Length, fileHash);
                return;
            }
            catch (SmartRAG.Exceptions.DocumentSkippedException ex)
            {
                _logger.LogInformation("Skipping file (no content to index): {FilePath}. {Message}", filePath, ex.Message);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= MaxRetryAttempts)
                {
                    _logger.LogError(ex, "Failed to upload file after {RetryCount} attempts: {FilePath}", MaxRetryAttempts, filePath);
                    throw;
                }

                _logger.LogWarning(ex, "Error uploading file (attempt {RetryCount}/{MaxRetries}): {FilePath}", retryCount, MaxRetryAttempts, filePath);
                await Task.Delay(RetryDelayMs * retryCount);
            }
        }
    }

    private async Task ScanExistingFilesAsync(string folderPath, WatchedFolderConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

        try
        {
            var existingDocuments = await documentService.GetAllDocumentsAsync();
            var existingHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var doc in existingDocuments)
            {
                if (doc.Metadata != null && doc.Metadata.TryGetValue("FileHash", out var hash) && hash != null && !string.IsNullOrEmpty(hash.ToString()))
                {
                    existingHashSet.Add(hash.ToString());
                }
            }

            var searchOption = config.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(folderPath, "*.*", searchOption);

            var processedCount = 0;
            var skippedCount = 0;
            var duplicateCount = 0;

            foreach (var filePath in files)
            {
                try
                {
                    if (!IsFileAllowed(filePath))
                    {
                        skippedCount++;
                        continue;
                    }

                    var fileName = Path.GetFileName(filePath);
                    var fileInfo = new FileInfo(filePath);
                    var fileHash = await ComputeFileHashAsync(filePath, CancellationToken.None);

                    if (existingHashSet.Contains(fileHash))
                    {
                        _logger.LogInformation("Skipping duplicate file: {FileName} (size: {Size} bytes, hash: {Hash})", fileName, fileInfo.Length, fileHash);
                        duplicateCount++;
                        continue;
                    }

                    await UploadFileAsync(filePath, config);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process existing file: {FilePath}", filePath);
                    skippedCount++;
                }
            }

            _logger.LogInformation("Initial scan completed for folder: {FolderPath}. Processed: {ProcessedCount}, Skipped: {SkippedCount}, Duplicates: {DuplicateCount}",
                folderPath, processedCount, skippedCount, duplicateCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning existing files in folder: {FolderPath}", folderPath);
        }
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
            return "application/octet-stream";

        if (ContentTypeMap.TryGetValue(extension, out var contentType))
            return contentType;

        return "application/octet-stream";
    }

    private HashSet<string> LoadSupportedExtensions()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var documentParserService = scope.ServiceProvider.GetRequiredService<IDocumentParserService>();
        return new HashSet<string>(documentParserService.GetSupportedFileTypes(), StringComparer.OrdinalIgnoreCase);
    }


    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var md5 = MD5.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var hash = md5.Hash;
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Use ConfigureAwait(false) to avoid deadlock in Dispose pattern
            // This is safe because Dispose is called synchronously and there's no SynchronizationContext
            StopAllWatchingAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _disposed = true;
        }
    }
}

