using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.Database;
using SmartRAG.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database
{
    /// <summary>
    /// Manages database connections from configuration
    /// </summary>
    public class DatabaseConnectionManager : IDatabaseConnectionManager
    {
        private readonly SmartRagOptions _options;
        private readonly IDatabaseParserService _databaseParserService;
        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly ILogger<DatabaseConnectionManager> _logger;
        private readonly ConcurrentDictionary<string, DatabaseConnectionConfig> _connections;
        private bool _initialized;

        public DatabaseConnectionManager(
            IOptions<SmartRagOptions> options,
            IDatabaseParserService databaseParserService,
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            ILogger<DatabaseConnectionManager> logger)
        {
            _options = options.Value;
            _databaseParserService = databaseParserService;
            _schemaAnalyzer = schemaAnalyzer;
            _logger = logger;
            _connections = new ConcurrentDictionary<string, DatabaseConnectionConfig>();
        }

        public async Task InitializeAsync()
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
                string databaseId = null;
                try
                {
                    databaseId = await GetDatabaseIdAsync(config);
                    _connections[databaseId] = config;

                    _logger.LogInformation("Registered database connection: {DatabaseId}", databaseId);

                    if (_options.EnableAutoSchemaAnalysis)
                    {
                        _logger.LogInformation("Starting schema analysis for: {DatabaseId}", databaseId);
                        try
                        {
                            await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(config);
                            _logger.LogInformation("Schema analysis completed for: {DatabaseId}", databaseId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Schema analysis failed for: {DatabaseId}", databaseId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (databaseId != null)
                    {
                        _logger.LogError(ex, "Failed to initialize database connection: {DatabaseId}", databaseId);
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to initialize database connection");
                    }
                }
            }

            _initialized = true;
            _logger.LogInformation("Database connection manager initialized with {Count} connections",
                _connections.Count);
        }

        public async Task<List<DatabaseConnectionConfig>> GetAllConnectionsAsync()
        {
            return await Task.FromResult(_connections.Values.ToList());
        }

        public async Task<DatabaseConnectionConfig> GetConnectionAsync(string databaseId)
        {
            _connections.TryGetValue(databaseId, out var config);
            return await Task.FromResult(config);
        }

        public async Task<bool> ValidateConnectionAsync(string databaseId)
        {
            if (!_connections.TryGetValue(databaseId, out var config))
            {
                _logger.LogWarning("Database connection not found: {DatabaseId}", databaseId);
                return false;
            }

            try
            {
                return await _databaseParserService.ValidateConnectionAsync(
                    config.ConnectionString,
                    config.DatabaseType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation failed for database: {DatabaseId}", databaseId);
                return false;
            }
        }

        public async Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig connectionConfig)
        {
            if (!string.IsNullOrEmpty(connectionConfig.Name))
            {
                return connectionConfig.Name;
            }

            try
            {
                var dbName = await ExtractDatabaseNameAsync(connectionConfig);
                return $"{connectionConfig.DatabaseType}_{dbName}_{Guid.NewGuid():N}"[..50];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract database name, using GUID");
                return $"DB_{Guid.NewGuid():N}";
            }
        }

        private async Task<string> ExtractDatabaseNameAsync(DatabaseConnectionConfig config)
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
                    return match.Groups[1].Value.Trim();
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
                    return System.IO.Path.GetFileNameWithoutExtension(path);
                }
            }

            return await Task.FromResult("UnknownDB");
        }
    }
}

