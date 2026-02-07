namespace SmartRAG.Services.Database;


/// <summary>
/// Manages database connections from configuration
/// </summary>
public class DatabaseConnectionManager : IDatabaseConnectionManager
{
    private readonly SmartRagOptions _options;
    private readonly IDatabaseParserService _databaseParserService;
    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
    private readonly ISchemaMigrationService? _schemaMigrationService;
    private readonly ILogger<DatabaseConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, DatabaseConnectionConfig> _connections;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public DatabaseConnectionManager(
        IOptions<SmartRagOptions> options,
        IDatabaseParserService databaseParserService,
        IDatabaseSchemaAnalyzer schemaAnalyzer,
        ISchemaMigrationService? schemaMigrationService,
        ILogger<DatabaseConnectionManager> logger)
    {
        _options = options.Value;
        _databaseParserService = databaseParserService;
        _schemaAnalyzer = schemaAnalyzer;
        _schemaMigrationService = schemaMigrationService;
        _logger = logger;
        _connections = new ConcurrentDictionary<string, DatabaseConnectionConfig>();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            DatabaseLogMessages.LogConnectionManagerAlreadyInitialized(_logger, null!);
            return;
        }
        if (_options.DatabaseConnections == null || _options.DatabaseConnections.Count == 0)
        {
            DatabaseLogMessages.LogNoDatabaseConnectionsConfigured(_logger, null!);
            _initialized = true;
            return;
        }

        var enabledConnections = _options.DatabaseConnections
            .Where(c => c.Enabled)
            .ToList();

        foreach (var config in enabledConnections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            string? databaseId = null;
            try
            {
                databaseId = await GetDatabaseIdAsync(config, cancellationToken);
                _connections[databaseId] = config;

                DatabaseLogMessages.LogRegisteredDatabaseConnection(_logger, null!);

                if (ShouldPerformSchemaAnalysis())
                {
                    DatabaseLogMessages.LogSchemaAnalysisStarted(_logger, null!);
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(config, cancellationToken);
                        DatabaseLogMessages.LogSchemaAnalysisCompleted(_logger, null!);
                    }
                    catch (Exception ex)
                    {
                        DatabaseLogMessages.LogSchemaAnalysisFailed(_logger, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                DatabaseLogMessages.LogDatabaseConnectionInitFailed(_logger, ex);
            }
        }

        if (enabledConnections.Count >= 2 && ShouldPerformSchemaAnalysis())
        {
            try
            {
                await DetectAndApplyCrossDatabaseMappingsAsync(enabledConnections, cancellationToken);
            }
            catch (Exception ex)
            {
                DatabaseLogMessages.LogCrossDatabaseMappingsDetectFailed(_logger, ex);
            }
        }

        if (ShouldPerformSchemaAnalysis() && _schemaMigrationService != null)
        {
            try
            {
                DatabaseLogMessages.LogSchemaMigrationStarted(_logger, null!);
                var migratedCount = await _schemaMigrationService.MigrateAllSchemasAsync(cancellationToken);
                DatabaseLogMessages.LogSchemaMigrationCompleted(_logger, migratedCount, null!);
            }
            catch (Exception ex)
            {
                DatabaseLogMessages.LogSchemaMigrationFailed(_logger, ex);
            }
        }

        _initialized = true;
        DatabaseLogMessages.LogConnectionManagerInitialized(_logger, _connections.Count, null!);
    }

    private async Task DetectAndApplyCrossDatabaseMappingsAsync(
        List<DatabaseConnectionConfig> connections,
        CancellationToken cancellationToken)
    {
        var manualMappingCount = connections.Sum(c => c.CrossDatabaseMappings?.Count ?? 0);
        if (manualMappingCount > 0)
        {
            DatabaseLogMessages.LogManualCrossDatabaseMappingsFound(_logger, manualMappingCount, null!);
        }

        var detectorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CrossDatabaseMappingDetector>.Instance;
        var detector = new CrossDatabaseMappingDetector(_schemaAnalyzer, detectorLogger);
        var autoDetectedMappings = await detector.DetectMappingsAsync(connections, cancellationToken);

        foreach (var mapping in autoDetectedMappings)
        {
            var sourceConfig = connections.FirstOrDefault(c =>
                (c.Name ?? string.Empty).Equals(mapping.SourceDatabase, StringComparison.OrdinalIgnoreCase));
            if (sourceConfig != null)
            {
                if (sourceConfig.CrossDatabaseMappings == null)
                    sourceConfig.CrossDatabaseMappings = new List<CrossDatabaseMapping>();

                var exists = sourceConfig.CrossDatabaseMappings.Any(m =>
                    m.SourceColumn.Equals(mapping.SourceColumn, StringComparison.OrdinalIgnoreCase) &&
                    m.TargetColumn.Equals(mapping.TargetColumn, StringComparison.OrdinalIgnoreCase) &&
                    m.TargetDatabase.Equals(mapping.TargetDatabase, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrEmpty(m.SourceTable) || m.SourceTable.Equals(mapping.SourceTable, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(m.TargetTable) || m.TargetTable.Equals(mapping.TargetTable, StringComparison.OrdinalIgnoreCase)));

                if (!exists)
                {
                    sourceConfig.CrossDatabaseMappings.Add(mapping);
                    DatabaseLogMessages.LogCrossDatabaseMappingAdded(_logger,
                        mapping.SourceDatabase, mapping.SourceTable ?? string.Empty, mapping.SourceColumn,
                        mapping.TargetDatabase, mapping.TargetTable ?? string.Empty, mapping.TargetColumn, null!);
                }
                else
                {
                    DatabaseLogMessages.LogCrossDatabaseMappingSkipped(_logger,
                        mapping.SourceDatabase, mapping.SourceColumn,
                        mapping.TargetDatabase, mapping.TargetColumn, null!);
                }
            }
        }

        var totalMappingCount = connections.Sum(c => c.CrossDatabaseMappings?.Count ?? 0);
        DatabaseLogMessages.LogTotalCrossDatabaseMappings(_logger, totalMappingCount, manualMappingCount, totalMappingCount - manualMappingCount, null!);
    }

    public async Task<List<DatabaseConnectionConfig>> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureConnectionsPopulatedAsync(cancellationToken).ConfigureAwait(false);
        return _connections.Values.ToList();
    }

    private async Task EnsureConnectionsPopulatedAsync(CancellationToken cancellationToken)
    {
        if (_initialized || _connections.Count > 0)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized || _connections.Count > 0)
            {
                return;
            }

            if (_options.DatabaseConnections == null || _options.DatabaseConnections.Count == 0)
            {
                _initialized = true;
                return;
            }

            foreach (var config in _options.DatabaseConnections.Where(c => c.Enabled))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var databaseId = await GetDatabaseIdAsync(config, cancellationToken).ConfigureAwait(false);
                    _connections[databaseId] = config;
                }
                catch (Exception ex)
                {
                    DatabaseLogMessages.LogDatabaseConnectionRegisterFailed(_logger, config.Name, ex);
                }
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<DatabaseConnectionConfig?> GetConnectionAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureConnectionsPopulatedAsync(cancellationToken).ConfigureAwait(false);
        _connections.TryGetValue(databaseId, out var config);
        return config;
    }

    public async Task<bool> ValidateConnectionAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectionsPopulatedAsync(cancellationToken).ConfigureAwait(false);
        if (!_connections.TryGetValue(databaseId, out var config))
        {
            DatabaseLogMessages.LogDatabaseConnectionNotFound(_logger, null!);
            return false;
        }

        try
        {
            return await _databaseParserService.ValidateConnectionAsync(
                config.ConnectionString,
                config.DatabaseType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogDatabaseValidationFailed(_logger, ex);
            return false;
        }
    }

    public async Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig connectionConfig, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(connectionConfig.Name))
        {
            return connectionConfig.Name;
        }

        try
        {
            var dbName = await ExtractDatabaseNameAsync(connectionConfig, cancellationToken);
            return $"{connectionConfig.DatabaseType}_{dbName}_{Guid.NewGuid():N}"[..50];
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogDatabaseNameExtractFailed(_logger, ex);
            return $"DB_{Guid.NewGuid():N}";
        }
    }

    private Task<string> ExtractDatabaseNameAsync(DatabaseConnectionConfig config, CancellationToken cancellationToken = default)
    {
        var connectionString = config.ConnectionString.ToLower();

        if (connectionString.Contains("database="))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                config.ConnectionString,
                @"database=([^;]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return Task.FromResult(match.Groups[1].Value.Trim());
            }
        }

        if (connectionString.Contains("data source=") && config.DatabaseType == Enums.DatabaseType.SQLite)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                config.ConnectionString,
                @"data source=([^;]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var path = match.Groups[1].Value.Trim();
                return Task.FromResult(System.IO.Path.GetFileNameWithoutExtension(path));
            }
        }

        return Task.FromResult("UnknownDB");
    }

    private bool ShouldPerformSchemaAnalysis()
    {
        return _options.EnableAutoSchemaAnalysis;
    }
}


