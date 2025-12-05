using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.FileWatcher;
using SmartRAG.Interfaces.Mcp;
using SmartRAG.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SmartRAG.Services.Startup
{
    /// <summary>
    /// Hosted service for automatically initializing SmartRAG features on startup
    /// </summary>
    public class SmartRagStartupService : IHostedService
    {
        private readonly ILogger<SmartRagStartupService> _logger;
        private readonly SmartRagOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public SmartRagStartupService(
            ILogger<SmartRagStartupService> logger,
            IOptions<SmartRagOptions> options,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Starts the service and initializes MCP connections and file watchers based on configuration
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SmartRagStartupService.StartAsync called. EnableMcpClient: {EnableMcpClient}, EnableFileWatcher: {EnableFileWatcher}",
                _options.Features.EnableMcpClient, _options.Features.EnableFileWatcher);

            if (_options.Features.EnableMcpClient)
            {
                var mcpConnectionManager = _serviceProvider.GetService<IMcpConnectionManager>();
                if (mcpConnectionManager != null)
                {
                    _logger.LogInformation("Initializing MCP connections...");
                    await mcpConnectionManager.ConnectAllAsync();
                }
                else
                {
                    _logger.LogWarning("IMcpConnectionManager service not found. MCP client may not be properly registered.");
                }
            }
            else
            {
                _logger.LogInformation("MCP client is disabled in configuration");
            }

            if (_options.Features.EnableFileWatcher && _options.WatchedFolders != null)
            {
                var fileWatcherService = _serviceProvider.GetService<IFileWatcherService>();
                if (fileWatcherService != null)
                {
                    _logger.LogInformation("Initializing file watchers...");
                    foreach (var folderConfig in _options.WatchedFolders)
                    {
                        try
                        {
                            await fileWatcherService.StartWatchingAsync(folderConfig);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to start watching folder: {FolderPath}", folderConfig.FolderPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stops the service
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

