using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Database;
using SmartRAG.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database
{
    /// <summary>
    /// Executes queries across multiple databases
    /// </summary>
    public class DatabaseQueryExecutor : IDatabaseQueryExecutor
    {
        #region Constants

        private const int DefaultMaxRows = 100;

        #endregion

        #region Fields

        private readonly IDatabaseConnectionManager _connectionManager;
        private readonly IDatabaseParserService _databaseParser;
        private readonly ILogger<DatabaseQueryExecutor> _logger;

        #endregion

        #region Constructor

        public DatabaseQueryExecutor(
            IDatabaseConnectionManager connectionManager,
            IDatabaseParserService databaseParser,
            ILogger<DatabaseQueryExecutor> logger)
        {
            _connectionManager = connectionManager;
            _databaseParser = databaseParser;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// [DB Query] Executes queries across multiple databases based on query intent
        /// </summary>
        public async Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new MultiDatabaseQueryResult
            {
                Success = true
            };

            _logger.LogInformation("Executing multi-database query across {Count} databases",
                queryIntent.DatabaseQueries.Count);

            // Execute queries in parallel
            var tasks = queryIntent.DatabaseQueries.Select(async dbQuery =>
            {
                var dbResult = await ExecuteSingleDatabaseQueryAsync(dbQuery);
                return (dbQuery.DatabaseId, dbResult);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (databaseId, dbResult) in results)
            {
                result.DatabaseResults[databaseId] = dbResult;
                
                if (!dbResult.Success)
                {
                    result.Success = false;
                    result.Errors.Add($"Database {databaseId}: {dbResult.ErrorMessage}");
                }
            }

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Multi-database query completed in {Ms}ms. Success: {Success}",
                result.ExecutionTimeMs, result.Success);

            return result;
        }

        #endregion

        #region Private Helper Methods

        private async Task<DatabaseQueryResult> ExecuteSingleDatabaseQueryAsync(DatabaseQueryIntent dbQuery)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new DatabaseQueryResult
            {
                DatabaseId = dbQuery.DatabaseId,
                DatabaseName = dbQuery.DatabaseName,
                ExecutedQuery = dbQuery.GeneratedQuery
            };

            try
            {
                var connection = await _connectionManager.GetConnectionAsync(dbQuery.DatabaseId);
                if (connection == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Database connection not found";
                    return result;
                }

                if (string.IsNullOrEmpty(dbQuery.GeneratedQuery))
                {
                    result.Success = false;
                    result.ErrorMessage = "No query generated";
                    return result;
                }

                // Execute the query
                var maxRows = connection.MaxRowsPerQuery > 0 ? connection.MaxRowsPerQuery : DefaultMaxRows;
                var queryResult = await _databaseParser.ExecuteQueryAsync(
                    connection.ConnectionString,
                    dbQuery.GeneratedQuery,
                    connection.DatabaseType,
                    maxRows);

                result.ResultData = queryResult;
                result.RowCount = CountRowsInResult(queryResult);
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query on database: {DatabaseId}", dbQuery.DatabaseId);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        private int CountRowsInResult(string resultData)
        {
            if (string.IsNullOrEmpty(resultData))
            {
                return 0;
            }

            var lines = resultData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("Rows extracted:"))
                {
                    if (int.TryParse(line.Substring("Rows extracted:".Length).Trim(), out int count))
                    {
                        return count;
                    }
                }
            }

            // Fallback: Count lines that look like data (not metadata)
            // This is an approximation if "Rows extracted" is missing
            int dataRows = 0;
            bool headerFound = false;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("===") || line.StartsWith("Query:") || line.StartsWith("Rows"))
                    continue;
                    
                if (!headerFound)
                {
                    headerFound = true; // Skip header
                    continue;
                }
                
                dataRows++;
            }
            
            return dataRows;
        }

        #endregion
    }
}

