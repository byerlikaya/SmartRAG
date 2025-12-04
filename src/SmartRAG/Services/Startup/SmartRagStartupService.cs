using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.FileWatcher;
using SmartRAG.Mcp.Client.Services;
using SmartRAG.Models;
using System.Threading.Tasks;

#nullable enable

namespace SmartRAG.Services.Startup
{
    /// <summary>
    /// Service for initializing SmartRAG features on startup
    /// </summary>
    public class SmartRagStartupService : ISmartRagStartupService
    {
        private readonly ILogger<SmartRagStartupService> _logger;
        private readonly SmartRagOptions _options;
        private readonly IMcpConnectionManager? _mcpConnectionManager;
        private readonly IFileWatcherService? _fileWatcherService;

        public SmartRagStartupService(
            ILogger<SmartRagStartupService> logger,
            IOptions<SmartRagOptions> options,
            IMcpConnectionManager? mcpConnectionManager = null,
            IFileWatcherService? fileWatcherService = null)
        {
            _logger = logger;
            _options = options.Value;
            _mcpConnectionManager = mcpConnectionManager;
            _fileWatcherService = fileWatcherService;
        }

        /// <summary>
        /// Initializes MCP connections and file watchers based on configuration
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_options.Features.EnableMcpClient && _mcpConnectionManager != null)
            {
                _logger.LogInformation("Initializing MCP connections...");
                await _mcpConnectionManager.ConnectAllAsync();
            }

            if (_options.Features.EnableFileWatcher && _fileWatcherService != null && _options.WatchedFolders != null)
            {
                _logger.LogInformation("Initializing file watchers...");
                foreach (var folderConfig in _options.WatchedFolders)
                {
                    try
                    {
                        await _fileWatcherService.StartWatchingAsync(folderConfig);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start watching folder: {FolderPath}", folderConfig.FolderPath);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Interface for SmartRAG startup service
    /// </summary>
    public interface ISmartRagStartupService
    {
        /// <summary>
        /// Initializes MCP connections and file watchers based on configuration
        /// </summary>
        Task InitializeAsync();
    }
}

