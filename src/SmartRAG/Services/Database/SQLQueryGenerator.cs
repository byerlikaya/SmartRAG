using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Models;
using SmartRAG.Services.Database.Strategies;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database
{
    /// <summary>
    /// Generates optimized SQL queries for databases based on query intent
    /// </summary>
    public class SQLQueryGenerator : ISqlQueryGenerator
    {
        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IAIService _aiService;
        private readonly ISqlDialectStrategyFactory _strategyFactory;
        private readonly ISqlValidator _validator;
        private readonly ISqlPromptBuilder _promptBuilder;
        private readonly ILogger<SQLQueryGenerator> _logger;

        /// <summary>
        /// Initializes a new instance of the SQLQueryGenerator
        /// </summary>
        /// <param name="schemaAnalyzer">Database schema analyzer</param>
        /// <param name="aiService">AI service for query generation</param>
        /// <param name="strategyFactory">SQL dialect strategy factory</param>
        /// <param name="validator">SQL validator</param>
        /// <param name="promptBuilder">SQL prompt builder</param>
        /// <param name="logger">Logger instance</param>
        public SQLQueryGenerator(
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            IAIService aiService,
            ISqlDialectStrategyFactory strategyFactory,
            ISqlValidator validator,
            ISqlPromptBuilder promptBuilder,
            ILogger<SQLQueryGenerator> logger)
        {
            _schemaAnalyzer = schemaAnalyzer ?? throw new ArgumentNullException(nameof(schemaAnalyzer));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
                        _logger.LogWarning("Schema not found for database");
                        continue;
                    }

                    var strategy = _strategyFactory.GetStrategy(schema.DatabaseType);

                    var systemPrompt = _promptBuilder.Build(queryIntent.OriginalQuery, dbQuery, schema, strategy, queryIntent);

                    var sql = await _aiService.GenerateResponseAsync(systemPrompt, new List<string>());

                    _logger.LogDebug("AI raw response received");

                    var extractedSql = ExtractSQLFromAIResponse(sql);

                    extractedSql = strategy.FormatSql(extractedSql);

                    _logger.LogDebug("Extracted SQL");

                    if (!ValidateSql(extractedSql, schema, dbQuery.RequiredTables, strategy, out var validationErrors))
                    {
                        _logger.LogWarning("Generated SQL failed validation. Errors: {Errors}", string.Join(", ", validationErrors));
                        dbQuery.GeneratedQuery = null;
                        continue;
                    }

                    dbQuery.GeneratedQuery = extractedSql;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating SQL for database");
                }
            }

            return queryIntent;
        }

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
    }
}
