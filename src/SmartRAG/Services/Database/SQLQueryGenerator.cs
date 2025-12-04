using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Models;
using SmartRAG.Services.Database.Prompts;
using SmartRAG.Services.Database.Strategies;
using SmartRAG.Services.Database.Validation;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database
{
    /// <summary>
    /// Generates optimized SQL queries for databases based on query intent
    /// </summary>
    public class SQLQueryGenerator : ISQLQueryGenerator
    {
        #region Fields

        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IAIService _aiService;
        private readonly ISqlDialectStrategyFactory _strategyFactory;
        private readonly SqlValidator _validator;
        private readonly SqlPromptBuilder _promptBuilder;
        private readonly ILogger<SQLQueryGenerator> _logger;

        #endregion

        #region Constructor

        public SQLQueryGenerator(
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            IAIService aiService,
            ISqlDialectStrategyFactory strategyFactory,
            ILogger<SQLQueryGenerator> logger)
        {
            _schemaAnalyzer = schemaAnalyzer;
            _aiService = aiService;
            _strategyFactory = strategyFactory;
            _logger = logger;
            _validator = new SqlValidator(logger);
            _promptBuilder = new SqlPromptBuilder();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// [AI Query] Generates optimized SQL queries for each database based on intent
        /// </summary>
        public async Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
        {
            _logger.LogInformation("Generating SQL queries for {Count} databases", queryIntent.DatabaseQueries.Count);

            foreach (var dbQuery in queryIntent.DatabaseQueries)
            {
                try
                {
                    var schema = await _schemaAnalyzer.GetSchemaAsync(dbQuery.DatabaseId);
                    if (schema == null)
                    {
                        _logger.LogWarning("Schema not found for database: {DatabaseId}", dbQuery.DatabaseId);
                        continue;
                    }

                    var strategy = _strategyFactory.GetStrategy(schema.DatabaseType);

                    var systemPrompt = _promptBuilder.Build(queryIntent.OriginalQuery, dbQuery, schema, strategy, queryIntent);

                    var sql = await _aiService.GenerateResponseAsync(systemPrompt, new List<string>());

                    _logger.LogDebug("AI raw response for {DatabaseId}: {RawSQL}", dbQuery.DatabaseId, sql);

                    var extractedSql = ExtractSQLFromAIResponse(sql);

                    extractedSql = strategy.FormatSql(extractedSql);

                    _logger.LogDebug("Extracted SQL for {DatabaseId}: {ExtractedSQL}", dbQuery.DatabaseId, extractedSql);

                    if (!ValidateSql(extractedSql, schema, dbQuery.RequiredTables, strategy, out var validationErrors))
                    {
                        _logger.LogWarning("Generated SQL failed validation for {DatabaseId}. Errors: {Errors}", dbQuery.DatabaseId, string.Join(", ", validationErrors));
                        dbQuery.GeneratedQuery = null;
                        continue;
                    }

                    dbQuery.GeneratedQuery = extractedSql;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating SQL for database: {DatabaseId}", dbQuery.DatabaseId);
                }
            }

            return queryIntent;
        }

        #endregion

        #region Private Helper Methods

        private bool ValidateSql(string sql, DatabaseSchemaInfo schema, List<string> requiredTables, ISqlDialectStrategy strategy, out List<string> errors)
        {
            errors = new List<string>();

            if (!strategy.ValidateSyntax(sql, out var syntaxError))
            {
                errors.Add(syntaxError);
            }

            var schemaErrors = _validator.ValidateQuery(sql, schema, requiredTables);
            errors.AddRange(schemaErrors);

            return errors.Count == 0;
        }

        private string ExtractSQLFromAIResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return string.Empty;

            var match = Regex.Match(response, @"```sql\s*(.*?)\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            match = Regex.Match(response, @"```\s*(.*?)\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            var trimmed = response.Trim();
            if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return response;
        }

        #endregion
    }
}
