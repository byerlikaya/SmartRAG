
namespace SmartRAG.Services.Startup;


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
    /// Starts the service and initializes MCP connections, file watchers, and database schema analysis based on configuration
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.Features.EnableMcpSearch)
        {
            var mcpConnectionManager = _serviceProvider.GetService<IMcpConnectionManager>();
            if (mcpConnectionManager != null)
            {
                StartupLogMessages.LogMcpInit(_logger, null!);
                await mcpConnectionManager.ConnectAllAsync();
            }
            else
            {
                StartupLogMessages.LogMcpServiceNotFound(_logger, null!);
            }
        }
        else
        {
            StartupLogMessages.LogMcpDisabled(_logger, null!);
        }

        if (_options.Features.EnableFileWatcher)
        {
            var fileWatcherService = _serviceProvider.GetService<IFileWatcherService>();
            if (fileWatcherService != null)
            {
                StartupLogMessages.LogFileWatcherInit(_logger, null!);
                foreach (var folderConfig in _options.WatchedFolders)
                {
                    try
                    {
                        await fileWatcherService.StartWatchingAsync(folderConfig);
                    }
                    catch (Exception ex)
                    {
                        StartupLogMessages.LogFileWatcherFailed(_logger, folderConfig.FolderPath, ex);
                    }
                }
            }
        }

        if (_options.DatabaseConnections.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                await Task.Yield();
                using var dbScope = _serviceProvider.CreateScope();
                var databaseConnectionManager = dbScope.ServiceProvider.GetService<IDatabaseConnectionManager>();
                if (databaseConnectionManager != null)
                {
                    StartupLogMessages.LogDatabaseInitBackground(_logger, null!);
                    try
                    {
                        await databaseConnectionManager.InitializeAsync(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        StartupLogMessages.LogDatabaseInitFailed(_logger, ex);
                    }
                }
            }, CancellationToken.None);
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


