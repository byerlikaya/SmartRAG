using SmartRAG.Interfaces.Database;
using SmartRAG.Models;
using System.Collections.Concurrent;

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
            _logger.LogWarning("DatabaseConnectionManager already initialized");
            return;
        }
        if (_options.DatabaseConnections == null || _options.DatabaseConnections.Count == 0)
        {
            _logger.LogInformation("No database connections configured");
            _initialized = true;
            return;
        }

        var enabledConnections = _options.DatabaseConnections
            .Where(c => c.Enabled)
            .ToList(); foreach (var config in enabledConnections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            string? databaseId = null;
            try
            {
                databaseId = await GetDatabaseIdAsync(config, cancellationToken);
                _connections[databaseId] = config;

                _logger.LogInformation("Registered database connection");

                if (ShouldPerformSchemaAnalysis())
                {
                    _logger.LogInformation("Starting schema analysis");
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(config, cancellationToken);
                        _logger.LogInformation("Schema analysis completed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Schema analysis failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database connection");
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
                _logger.LogWarning(ex, "Failed to detect cross-database mappings");
            }
        }

        if (ShouldPerformSchemaAnalysis() && _schemaMigrationService != null)
        {
            try
            {
                _logger.LogInformation("Starting schema migration to vector store");
                var migratedCount = await _schemaMigrationService.MigrateAllSchemasAsync(cancellationToken);
                _logger.LogInformation("Schema migration completed: {MigratedCount} schemas migrated", migratedCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Schema migration failed, continuing without schema chunks");
            }
        }

        _initialized = true;
        _logger.LogInformation("Database connection manager initialized with {Count} connections",
            _connections.Count);
    }

    private async Task DetectAndApplyCrossDatabaseMappingsAsync(
        List<DatabaseConnectionConfig> connections,
        CancellationToken cancellationToken)
    {
        var manualMappingCount = connections.Sum(c => c.CrossDatabaseMappings?.Count ?? 0);
        if (manualMappingCount > 0)
        {
            _logger.LogInformation("Found {Count} manually configured cross-database mappings in appsettings.json", manualMappingCount);
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
                    _logger.LogInformation(
                        "Auto-detected and added cross-database mapping: {SourceDB}.{SourceTable}.{SourceColumn} -> {TargetDB}.{TargetTable}.{TargetColumn}",
                        mapping.SourceDatabase, mapping.SourceTable, mapping.SourceColumn,
                        mapping.TargetDatabase, mapping.TargetTable, mapping.TargetColumn);
                }
                else
                {
                    _logger.LogDebug(
                        "Skipped auto-detected mapping (already exists in appsettings.json): {SourceDB}.{SourceColumn} -> {TargetDB}.{TargetColumn}",
                        mapping.SourceDatabase, mapping.SourceColumn,
                        mapping.TargetDatabase, mapping.TargetColumn);
                }
            }
        }

        var totalMappingCount = connections.Sum(c => c.CrossDatabaseMappings?.Count ?? 0);
        _logger.LogInformation("Total cross-database mappings: {Total} ({Manual} manual + {Auto} auto-detected)",
            totalMappingCount, manualMappingCount, totalMappingCount - manualMappingCount);
    }

    public Task<List<DatabaseConnectionConfig>> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_connections.Values.ToList());
    }

    public Task<DatabaseConnectionConfig?> GetConnectionAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _connections.TryGetValue(databaseId, out var config);
        return Task.FromResult<DatabaseConnectionConfig?>(config);
    }

    public async Task<bool> ValidateConnectionAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(databaseId, out var config))
        {
            _logger.LogWarning("Database connection not found");
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
            _logger.LogError(ex, "Validation failed for database");
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
            _logger.LogWarning(ex, "Could not extract database name, using GUID");
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


