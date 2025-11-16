using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database
{
    /// <summary>
    /// Coordinates intelligent multi-database queries using AI
    /// </summary>
    public class MultiDatabaseQueryCoordinator : IMultiDatabaseQueryCoordinator
    {     

        #region Fields

        private readonly IDatabaseConnectionManager _connectionManager;
        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IDatabaseParserService _databaseParser;
        private readonly IAIService _aiService;
        private readonly ILogger<MultiDatabaseQueryCoordinator> _logger;
        private readonly IQueryIntentAnalyzer _queryIntentAnalyzer;
        private readonly ISQLQueryGenerator _sqlQueryGenerator;
        private readonly IDatabaseQueryExecutor _databaseQueryExecutor;
        private readonly IResultMerger _resultMerger;

        #endregion

        #region Constructor

        public MultiDatabaseQueryCoordinator(
            IDatabaseConnectionManager connectionManager,
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            IDatabaseParserService databaseParser,
            IAIService aiService,
            ILogger<MultiDatabaseQueryCoordinator> logger,
            IQueryIntentAnalyzer queryIntentAnalyzer,
            ISQLQueryGenerator sqlQueryGenerator,
            IDatabaseQueryExecutor databaseQueryExecutor,
            IResultMerger resultMerger)
        {
            _connectionManager = connectionManager;
            _schemaAnalyzer = schemaAnalyzer;
            _databaseParser = databaseParser;
            _aiService = aiService;
            _logger = logger;
            _queryIntentAnalyzer = queryIntentAnalyzer ?? throw new ArgumentNullException(nameof(queryIntentAnalyzer), 
                "QueryIntentAnalyzer must be provided. Register IQueryIntentAnalyzer in DI container.");
            _sqlQueryGenerator = sqlQueryGenerator ?? throw new ArgumentNullException(nameof(sqlQueryGenerator),
                "SQLQueryGenerator must be provided. Register ISQLQueryGenerator in DI container.");
            _databaseQueryExecutor = databaseQueryExecutor ?? throw new ArgumentNullException(nameof(databaseQueryExecutor),
                "DatabaseQueryExecutor must be provided. Register IDatabaseQueryExecutor in DI container.");
            _resultMerger = resultMerger ?? throw new ArgumentNullException(nameof(resultMerger),
                "ResultMerger must be provided. Register IResultMerger in DI container.");
        }

        #endregion

        #region Public Methods

        public async Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
        {
            return await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(userQuery);
        }

        public async Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent)
        {
            return await _databaseQueryExecutor.ExecuteMultiDatabaseQueryAsync(queryIntent);
        }

        public async Task<RagResponse> QueryMultipleDatabasesAsync(string userQuery, int maxResults = 5)
        {
            try
            {
                // Step 1: Analyze query intent
                var queryIntent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(userQuery);

                if (queryIntent.DatabaseQueries.Count == 0)
                {
                    return CreateNoDatabaseMatchResponse();
                }
                
                // Step 1.5: Validate intent - remove invalid database/table combinations
                queryIntent = await ValidateAndCorrectQueryIntentAsync(queryIntent);

                // Step 2: Generate SQL queries
                queryIntent = await GenerateDatabaseQueriesAsync(queryIntent);

                // Step 3: Execute queries
                var queryResults = await ExecuteMultiDatabaseQueryAsync(queryIntent);

                if (!queryResults.Success)
                {
                    return CreateQueryExecutionErrorResponse(queryResults.Errors);
                }

                // Step 4: Merge and generate final response
                var mergedData = await _resultMerger.MergeResultsAsync(queryResults, userQuery);

                // Step 5: Generate AI answer from merged data
                var finalAnswer = await _resultMerger.GenerateFinalAnswerAsync(userQuery, mergedData, queryResults);

                return finalAnswer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing multi-database query");
                return CreateExceptionResponse(ex);
            }
        }

        public async Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
        {
            return await _sqlQueryGenerator.GenerateDatabaseQueriesAsync(queryIntent);
        }

        #endregion   

        #region Private Helper Methods (Legacy - Kept for Reference Only)      

        /// <summary>
        /// Creates a RagResponse with no database matches
        /// </summary>
        /// <returns>RagResponse indicating no databases matched</returns>
        private RagResponse CreateNoDatabaseMatchResponse()
        {
            return new RagResponse
            {
                Answer = "I couldn't determine which databases to query for your question. Please try rephrasing your query.",
                Sources = new List<SearchSource>
                {
                    new SearchSource
                    {
                        SourceType = "System",
                        FileName = "No databases matched",
                        RelevantContent = "No database match for query intent",
                        Location = "System notification"
                    }
                }
            };
        }

        /// <summary>
        /// Creates a RagResponse with query execution errors
        /// </summary>
        /// <param name="errors">List of error messages</param>
        /// <returns>RagResponse with error information</returns>
        private RagResponse CreateQueryExecutionErrorResponse(List<string> errors)
        {
            return new RagResponse
            {
                Answer = $"Some database queries failed: {string.Join(", ", errors)}",
                Sources = new List<SearchSource>
                {
                    new SearchSource
                    {
                        SourceType = "System",
                        FileName = "Error in query execution",
                        RelevantContent = string.Join("; ", errors),
                        Location = "System notification"
                    }
                }
            };
        }

        /// <summary>
        /// Creates a RagResponse with exception information
        /// </summary>
        /// <param name="ex">Exception that occurred</param>
        /// <returns>RagResponse with error information</returns>
        private RagResponse CreateExceptionResponse(Exception ex)
        {
            return new RagResponse
            {
                Answer = $"An error occurred while processing your query: {ex.Message}",
                Sources = new List<SearchSource>
                {
                    new SearchSource
                    {
                        SourceType = "System",
                        FileName = "Error",
                        RelevantContent = ex.Message,
                        Location = "System notification"
                    }
                }
            };
        }

        private async Task<QueryIntent> ValidateAndCorrectQueryIntentAsync(QueryIntent queryIntent)
        {
            var validQueries = new List<DatabaseQueryIntent>();
            var missingTables = new Dictionary<string, List<string>>(); // table -> databases that need it
            var allSchemas = await _schemaAnalyzer.GetAllSchemasAsync();
            
            foreach (var dbQuery in queryIntent.DatabaseQueries)
            {
                var schema = await _schemaAnalyzer.GetSchemaAsync(dbQuery.DatabaseId);
                if (schema == null)
                {
                    _logger.LogWarning("Schema not found for {DatabaseId}, removing from query list", dbQuery.DatabaseId);
                    continue;
                }
                
                // Check which tables actually exist in this database
                var validTables = new List<string>();
                var invalidTables = new List<string>();
                
                foreach (var tableName in dbQuery.RequiredTables)
                {
                    if (schema.Tables.Any(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Use exact casing from schema
                        var exactTableName = schema.Tables.First(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)).TableName;
                        validTables.Add(exactTableName);
                    }
                    else
                    {
                        invalidTables.Add(tableName);
                        
                        // Track this missing table
                        if (!missingTables.ContainsKey(tableName))
                        {
                            missingTables[tableName] = new List<string>();
                        }
                        missingTables[tableName].Add(dbQuery.DatabaseId);
                    }
                }
                
                if (invalidTables.Count > 0)
                {
                    _logger.LogWarning("AI selected invalid tables for {DatabaseName}: {InvalidTables}",
                        dbQuery.DatabaseName, string.Join(", ", invalidTables));
                }
                
                if (validTables.Count > 0)
                {
                    dbQuery.RequiredTables = validTables;
                    validQueries.Add(dbQuery);
                }
                else
                {
                    _logger.LogWarning("✗ {DatabaseName}: No valid tables, removing", dbQuery.DatabaseName);
                }
            }
            
            // Auto-add missing databases for tables AI requested but put in wrong database
            foreach (var kvp in missingTables)
            {
                var tableName = kvp.Key;
                
                // Find which database actually has this table
                var correctSchema = allSchemas.FirstOrDefault(s => 
                    s.Tables.Any(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)));
                
                if (correctSchema == null)
                {
                    _logger.LogWarning("Table '{Table}' not found in any database", tableName);
                    continue;
                }
                
                if (validQueries.Any(q => q.DatabaseId == correctSchema.DatabaseId))
                {
                    var existingQuery = validQueries.First(q => q.DatabaseId == correctSchema.DatabaseId);
                    if (!existingQuery.RequiredTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                    {
                        var exactTableName = correctSchema.Tables.First(t => 
                            t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)).TableName;
                        
                        existingQuery.RequiredTables.Add(exactTableName);
                    }
                }
                else
                {
                    var exactTableName = correctSchema.Tables.First(t => 
                        t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)).TableName;
                    
                    validQueries.Add(new DatabaseQueryIntent
                    {
                        DatabaseId = correctSchema.DatabaseId,
                        DatabaseName = correctSchema.DatabaseName,
                        RequiredTables = new List<string> { exactTableName },
                        Purpose = $"Get data from {exactTableName} table",
                        Priority = 2
                    });
                }
            }
            
            queryIntent.DatabaseQueries = validQueries;
            
            return queryIntent;
        }      

        #endregion    
    }
}