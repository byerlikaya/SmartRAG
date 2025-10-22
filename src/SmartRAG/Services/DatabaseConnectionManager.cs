using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services
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

            _logger.LogInformation("Initializing database connections...");

            if (_options.DatabaseConnections == null || _options.DatabaseConnections.Count == 0)
            {
                _logger.LogInformation("No database connections configured");
                _initialized = true;
                return;
            }

            var enabledConnections = _options.DatabaseConnections
                .Where(c => c.Enabled)
                .ToList();

            _logger.LogInformation("Found {Count} enabled database connections", enabledConnections.Count);

            foreach (var config in enabledConnections)
            {
                try
                {
                    var databaseId = await GetDatabaseIdAsync(config);
                    _connections[databaseId] = config;
                    
                    _logger.LogInformation("Registered database connection: {DatabaseId}", databaseId);

                    // Perform initial schema analysis if enabled
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
                    _logger.LogError(ex, "Failed to initialize database connection: {ConnectionString}", 
                        MaskConnectionString(config.ConnectionString));
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

        public async Task<Dictionary<string, bool>> ValidateAllConnectionsAsync()
        {
            var results = new Dictionary<string, bool>();

            foreach (var kvp in _connections)
            {
                var databaseId = kvp.Key;
                var config = kvp.Value;

                try
                {
                    var isValid = await _databaseParserService.ValidateConnectionAsync(
                        config.ConnectionString,
                        config.DatabaseType);
                    
                    results[databaseId] = isValid;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Validation failed for database: {DatabaseId}", databaseId);
                    results[databaseId] = false;
                }
            }

            return results;
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

            // Auto-generate from database name
            try
            {
                var dbName = await ExtractDatabaseNameAsync(connectionConfig);
                return $"{connectionConfig.DatabaseType}_{dbName}_{Guid.NewGuid():N}".Substring(0, 50);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract database name, using GUID");
                return $"DB_{Guid.NewGuid():N}";
            }
        }

        public async Task<string> AddConnectionAsync(DatabaseConnectionConfig connectionConfig)
        {
            var databaseId = await GetDatabaseIdAsync(connectionConfig);

            if (_connections.ContainsKey(databaseId))
            {
                throw new InvalidOperationException($"Database connection already exists: {databaseId}");
            }

            _connections[databaseId] = connectionConfig;
            _logger.LogInformation("Added new database connection: {DatabaseId}", databaseId);

            // Trigger schema analysis if enabled
            if (_options.EnableAutoSchemaAnalysis)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(connectionConfig);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Schema analysis failed for new connection: {DatabaseId}", databaseId);
                    }
                });
            }

            return databaseId;
        }

        public async Task RemoveConnectionAsync(string databaseId)
        {
            if (_connections.TryRemove(databaseId, out _))
            {
                _logger.LogInformation("Removed database connection: {DatabaseId}", databaseId);
            }
            else
            {
                _logger.LogWarning("Database connection not found for removal: {DatabaseId}", databaseId);
            }

            await Task.CompletedTask;
        }

        #region Private Helper Methods

        private async Task<string> ExtractDatabaseNameAsync(DatabaseConnectionConfig config)
        {
            // Try to extract database name from connection string
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

        private string MaskConnectionString(string connectionString)
        {
            // Mask sensitive information in connection string
            var masked = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(password|pwd)=([^;]+)",
                "$1=****",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            masked = System.Text.RegularExpressions.Regex.Replace(
                masked,
                @"(user id|uid)=([^;]+)",
                "$1=****",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return masked;
        }

        #endregion
    }
}

