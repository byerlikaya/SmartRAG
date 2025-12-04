using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.FileWatcher.Events;
using SmartRAG.Helpers;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SmartRAG.FileWatcher
{
    /// <summary>
    /// Service for watching file system folders and automatically indexing documents
    /// </summary>
    public class FileWatcherService : IFileWatcherService
    {
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 1000;

        private readonly ILogger<FileWatcherService> _logger;
        private readonly IDocumentService _documentService;
        private readonly IDocumentParserService _documentParserService;
        private readonly SmartRagOptions _options;
        private readonly Dictionary<string, System.IO.FileSystemWatcher> _watchers = new Dictionary<string, System.IO.FileSystemWatcher>();
        private readonly Dictionary<string, WatchedFolderConfig> _configs = new Dictionary<string, WatchedFolderConfig>();
        private bool _disposed = false;

        public FileWatcherService(
            ILogger<FileWatcherService> logger,
            IDocumentService documentService,
            IDocumentParserService documentParserService,
            IOptions<SmartRagOptions> options)
        {
            _logger = logger;
            _documentService = documentService;
            _documentParserService = documentParserService;
            _options = options.Value;
        }

        public event EventHandler<FileWatcherEventArgs> FileCreated;
        public event EventHandler<FileWatcherEventArgs> FileChanged;
        public event EventHandler<FileWatcherEventArgs> FileDeleted;

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
                _logger.LogInformation("Folder does not exist: {FolderPath}. Creating directory...", sanitizedPath);
                try
                {
                    Directory.CreateDirectory(sanitizedPath);
                    _logger.LogInformation("Successfully created directory: {FolderPath}", sanitizedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create directory: {FolderPath}", sanitizedPath);
                    throw new DirectoryNotFoundException($"Folder does not exist and could not be created: {sanitizedPath}", ex);
                }
            }

            if (_watchers.ContainsKey(sanitizedPath))
            {
                _logger.LogInformation("Already watching folder: {FolderPath}", sanitizedPath);
                return Task.CompletedTask;
            }

            var watcher = new System.IO.FileSystemWatcher(sanitizedPath)
            {
                IncludeSubdirectories = config.IncludeSubdirectories,
                NotifyFilter = System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime
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

        /// <summary>
        /// Stops watching a folder
        /// </summary>
        public Task StopWatchingAsync(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("FolderPath cannot be null or empty", nameof(folderPath));

            var baseDirectory = Directory.GetCurrentDirectory();
            var sanitizedPath = PathSanitizer.SanitizePath(folderPath, baseDirectory);

            if (_watchers.TryGetValue(sanitizedPath, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(sanitizedPath);
                _configs.Remove(sanitizedPath);
                _logger.LogInformation("Stopped watching folder: {FolderPath}", sanitizedPath);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops watching all folders
        /// </summary>
        public Task StopAllWatchingAsync()
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

        /// <summary>
        /// Gets list of currently watched folders
        /// </summary>
        public List<WatchedFolderConfig> GetWatchedFolders()
        {
            return new List<WatchedFolderConfig>(_configs.Values);
        }

        private async Task OnFileCreatedAsync(System.IO.FileSystemEventArgs e)
        {
            try
            {
                if (!IsFileAllowed(e.FullPath))
                    return;

                var args = new FileWatcherEventArgs
                {
                    FilePath = e.FullPath,
                    FileName = e.Name,
                    EventType = "Created",
                    Timestamp = DateTime.UtcNow
                };

                FileCreated?.Invoke(this, args);

                if (await GetConfigForPath(e.FullPath) is WatchedFolderConfig config && config.AutoUpload)
                {
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

                var args = new FileWatcherEventArgs
                {
                    FilePath = e.FullPath,
                    FileName = e.Name,
                    EventType = "Changed",
                    Timestamp = DateTime.UtcNow
                };

                FileChanged?.Invoke(this, args);

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
                var args = new FileWatcherEventArgs
                {
                    FilePath = e.FullPath,
                    FileName = e.Name,
                    EventType = "Deleted",
                    Timestamp = DateTime.UtcNow
                };

                FileDeleted?.Invoke(this, args);
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
                return false;

            if (!File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var supportedExtensions = _documentParserService.GetSupportedFileTypes();

            if (string.IsNullOrEmpty(extension))
                return false;

            if (!supportedExtensions.Contains(extension))
                return false;

            var config = GetConfigForPathSync(filePath);
            if (config == null)
                return false;

            if (config.AllowedExtensions != null && config.AllowedExtensions.Count > 0)
            {
                return config.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
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
                    var fileHash = ComputeFileHash(filePath);

                    var existingDocuments = await _documentService.GetAllDocumentsAsync();

                    _logger.LogDebug("Checking for duplicates. FileName: {FileName}, Hash: {Hash}, Total existing documents: {Count}",
                        fileName, fileHash, existingDocuments.Count);

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

                    _logger.LogDebug("No duplicate found. Proceeding with upload. FileName: {FileName}, Hash: {Hash}", fileName, fileHash);

                    using var fileStream = File.OpenRead(filePath);
                    var additionalMetadata = new Dictionary<string, object>
                    {
                        ["FileHash"] = fileHash,
                        ["FilePath"] = filePath
                    };

                    var document = await _documentService.UploadDocumentAsync(fileStream, fileName, contentType, config.UserId, null, fileInfo.Length, additionalMetadata);

                    _logger.LogInformation("Auto-uploaded file: {FilePath} (size: {Size} bytes, hash: {Hash})", filePath, fileInfo.Length, fileHash);
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
            try
            {
                _logger.LogInformation("Scanning existing files in folder: {FolderPath}", folderPath);

                var existingDocuments = await _documentService.GetAllDocumentsAsync();
                var existingHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var doc in existingDocuments)
                {
                    if (doc.Metadata != null && doc.Metadata.TryGetValue("FileHash", out var hash) && hash != null && !string.IsNullOrEmpty(hash.ToString()))
                    {
                        existingHashSet.Add(hash.ToString());
                    }
                }

                _logger.LogDebug("ScanExistingFiles: Found {Count} documents with FileHash out of {Total} total documents", existingHashSet.Count, existingDocuments.Count);

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
                        var fileHash = ComputeFileHash(filePath);

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

        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var supportedContentTypes = _documentParserService.GetSupportedContentTypes();

            foreach (var contentType in supportedContentTypes)
            {
                if (contentType.Contains(extension.TrimStart('.'), StringComparison.OrdinalIgnoreCase))
                    return contentType;
            }

            return "application/octet-stream";
        }

        private static string ComputeFileHash(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
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
                StopAllWatchingAsync().GetAwaiter().GetResult();
                _disposed = true;
            }
        }
    }
}

