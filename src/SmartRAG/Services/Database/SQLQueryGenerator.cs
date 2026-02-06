using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using SmartRAG.Services.Database.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database;


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
    private readonly IDatabaseConnectionManager _connectionManager;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<SQLQueryGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of the SQLQueryGenerator
    /// </summary>
    /// <param name="schemaAnalyzer">Database schema analyzer</param>
    /// <param name="aiService">AI service for query generation</param>
    /// <param name="strategyFactory">SQL dialect strategy factory</param>
    /// <param name="validator">SQL validator</param>
    /// <param name="promptBuilder">SQL prompt builder</param>
    /// <param name="connectionManager">Database connection manager</param>
    /// <param name="documentRepository">Document repository for RAG-based schema retrieval</param>
    /// <param name="logger">Logger instance</param>
    public SQLQueryGenerator(
        IDatabaseSchemaAnalyzer schemaAnalyzer,
        IAIService aiService,
        ISqlDialectStrategyFactory strategyFactory,
        ISqlValidator validator,
        ISqlPromptBuilder promptBuilder,
        IDatabaseConnectionManager connectionManager,
        IDocumentRepository documentRepository,
        ILogger<SQLQueryGenerator> logger)
    {
        _schemaAnalyzer = schemaAnalyzer ?? throw new ArgumentNullException(nameof(schemaAnalyzer));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// [AI Query] Generates optimized SQL queries for each database based on intent
    /// </summary>
    public async Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent, CancellationToken cancellationToken = default)
    {
        queryIntent.DatabaseQueries = queryIntent.DatabaseQueries.OrderBy(q => q.Priority).ToList();
        
        _logger.LogInformation("Executing {Count} database queries in priority order: {Order}",
            queryIntent.DatabaseQueries.Count,
            string.Join(" → ", queryIntent.DatabaseQueries.Select(q => $"{q.DatabaseName}(priority:{q.Priority})")));
        
        var schemas = new Dictionary<string, DatabaseSchemaInfo>();

        var strategies = new Dictionary<string, ISqlDialectStrategy>();
        var requiredMappingColumns = new Dictionary<string, List<string>>();

        foreach (var dbQuery in queryIntent.DatabaseQueries)
        {
            var schema = await _schemaAnalyzer.GetSchemaAsync(dbQuery.DatabaseId, cancellationToken);
            if (schema == null)
            {
                _logger.LogWarning("Schema not found for database {DatabaseId}", dbQuery.DatabaseId);
                dbQuery.GeneratedQuery = null;
                continue;
            }

            schemas[dbQuery.DatabaseId] = schema;
            strategies[dbQuery.DatabaseId] = _strategyFactory.GetStrategy(schema.DatabaseType);
            requiredMappingColumns[dbQuery.DatabaseId] = await GetRequiredMappingColumnsAsync(schema.DatabaseName, dbQuery.RequiredTables);

            if (requiredMappingColumns[dbQuery.DatabaseId].Any())
            {
                _logger.LogInformation("Database {DatabaseName} requires mapping columns: {Columns}",
                    schema.DatabaseName, string.Join(", ", requiredMappingColumns[dbQuery.DatabaseId]));
            }
        }

        if (schemas.Count == 0)
        {
            _logger.LogError("No valid schemas found for any database");
            return queryIntent;
        }

        var schemaChunksMap = new Dictionary<string, List<Entities.DocumentChunk>>();
        var allDocumentsCache = new Dictionary<Guid, Entities.Document>();
        
        try
        {
            var allDocuments = await _documentRepository.GetAllAsync(cancellationToken);
            foreach (var doc in allDocuments)
            {
                allDocumentsCache[doc.Id] = doc;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load documents cache, schema chunk filtering may be limited");
        }

        foreach (var dbQuery in queryIntent.DatabaseQueries)
        {
            if (schemas.TryGetValue(dbQuery.DatabaseId, out var schema))
            {
                try
                {
                    var schemaDoc = allDocumentsCache.Values.FirstOrDefault(d =>
                        d?.Metadata != null &&
                        d.Metadata.TryGetValue("documentType", out var dt) && string.Equals(dt?.ToString(), "Schema", StringComparison.OrdinalIgnoreCase) &&
                        d.Metadata.TryGetValue("databaseId", out var id) && id?.ToString() == dbQuery.DatabaseId);

                    if (schemaDoc != null)
                    {
                        var fullDoc = await _documentRepository.GetByIdAsync(schemaDoc.Id, cancellationToken);
                        if (fullDoc?.Chunks != null && fullDoc.Chunks.Count > 0)
                        {
                            schemaChunksMap[dbQuery.DatabaseId] = fullDoc.Chunks.OrderBy(c => c.ChunkIndex).ToList();
                            _logger.LogInformation("Using {Count} schema chunks for database {DatabaseName} (from stored schema document)",
                                fullDoc.Chunks.Count, schema.DatabaseName);
                        }
                        else
                        {
                            _logger.LogDebug("Schema document for database {DatabaseName} has no chunks, using DatabaseSchemaInfo fallback",
                                schema.DatabaseName);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("No schema document found for database {DatabaseName}, using DatabaseSchemaInfo fallback",
                            schema.DatabaseName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load schema chunks for database {DatabaseName}, falling back to DatabaseSchemaInfo",
                        schema.DatabaseName);
                }
            }
        }

        var promptParts = _promptBuilder.BuildMultiDatabaseSeparated(
            queryIntent.OriginalQuery,
            queryIntent,
            schemas,
            strategies,
            schemaChunksMap,
            requiredMappingColumns);
        
        var additionalInstructions = new StringBuilder();
        foreach (var kvp in requiredMappingColumns.Where(k => k.Value.Any()))
        {
            additionalInstructions.AppendLine($"\n🚨 CRITICAL FOR {schemas[kvp.Key].DatabaseName}:");
            additionalInstructions.AppendLine("MAPPING COLUMNS REQUIRED - MUST include in SELECT:");
            foreach (var col in kvp.Value)
            {
                additionalInstructions.AppendLine($"  • {col}");
            }
        }
        
        if (additionalInstructions.Length > 0)
        {
            promptParts.UserMessage += "\n" + additionalInstructions.ToString();
        }

        _logger.LogInformation("Sending separated multi-database prompt to AI for {DatabaseCount} databases", queryIntent.DatabaseQueries.Count);
        
        var context = new List<string> { promptParts.SystemMessage };
        var aiResponse = await _aiService.GenerateResponseAsync(promptParts.UserMessage, context, cancellationToken);
        _logger.LogInformation("AI response received for multi-database SQL generation");
        
        var databaseSqls = ExtractMultiDatabaseSQL(aiResponse, queryIntent.DatabaseQueries, schemas);

        foreach (var dbQuery in queryIntent.DatabaseQueries)
        {
            if (!databaseSqls.TryGetValue(dbQuery.DatabaseId, out var extractedSql) || string.IsNullOrEmpty(extractedSql))
            {
                _logger.LogError("Failed to extract SQL for database {DatabaseId}", dbQuery.DatabaseId);
                dbQuery.GeneratedQuery = null;
                continue;
            }

            var schema = schemas[dbQuery.DatabaseId];
            var strategy = strategies[dbQuery.DatabaseId];
            
            extractedSql = strategy.FormatSql(extractedSql);
            
            extractedSql = DetectAndFixCrossDatabaseReferences(extractedSql, schema, dbQuery.RequiredTables);
            
            extractedSql = FixAmbiguousColumnsInJoin(extractedSql, schema);
            
            _logger.LogDebug("AI generated SQL for database {DatabaseName}: {Sql}", schema.DatabaseName, extractedSql);

            var allDatabaseNames = schemas.Values.Select(s => s.DatabaseName).Distinct().ToList();
            if (!ValidateSql(extractedSql, schema, dbQuery.RequiredTables, strategy, allDatabaseNames, out var validationErrors))
            {
                _logger.LogError("AI generated invalid SQL for database {DatabaseName}. SQL: {Sql}. Errors: {Errors}",
                    schema.DatabaseName, extractedSql, string.Join("; ", validationErrors));
                dbQuery.GeneratedQuery = null;
                continue;
            }

            var mappingErrors = await ValidateCrossDatabaseMappingColumnsAsync(extractedSql, schema.DatabaseName, dbQuery.RequiredTables);
            if (mappingErrors.Any())
            {
                _logger.LogWarning("Generated SQL missing required mapping columns for database {DatabaseName}. SQL: {Sql}. Missing: {Missing}",
                    schema.DatabaseName, extractedSql, string.Join(", ", mappingErrors));
            }

            dbQuery.GeneratedQuery = extractedSql;
        }

        return queryIntent;
    }

    private Dictionary<string, string> ExtractMultiDatabaseSQL(string response, List<DatabaseQueryIntent> databaseQueries, Dictionary<string, DatabaseSchemaInfo> schemas)
    {
        var result = new Dictionary<string, string>();
        
        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogWarning("AI response is empty, cannot extract SQL");
            return result;
        }

        response = Regex.Replace(response, @"```sql\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        response = Regex.Replace(response, @"```\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string currentDatabase = null;
        var currentSql = new List<string>();
        bool inSql = false;
        bool waitingForConfirmed = true;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;
            
            if (trimmedLine.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                continue;
            
            if (trimmedLine.StartsWith("###", StringComparison.OrdinalIgnoreCase))
            {
                var dbHeaderMatch = Regex.Match(trimmedLine, @"###\s+DATABASE\s+(\d+):\s*(.+)", RegexOptions.IgnoreCase);
                if (dbHeaderMatch.Success)
                {
                    if (currentDatabase != null && currentSql.Any())
                    {
                        var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
                        if (!string.IsNullOrWhiteSpace(sql))
                        {
                            result[currentDatabase] = sql;
                        }
                    }
                    
                    var dbIndex = int.Parse(dbHeaderMatch.Groups[1].Value) - 1;
                    if (dbIndex >= 0 && dbIndex < databaseQueries.Count)
                    {
                        currentDatabase = databaseQueries[dbIndex].DatabaseId;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                        _logger.LogDebug("Found database header: {DatabaseId}", currentDatabase);
                    }
                    else
                    {
                        currentDatabase = null;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                        _logger.LogDebug("Skipping database header at index {DbIndex} (out of range, expected databases: {ExpectedCount})", dbIndex, databaseQueries.Count);
                    }
                }
                continue;
            }
            
            var dbMatch = Regex.Match(trimmedLine, @"^DATABASE\s+(\d+):\s*(.+)$", RegexOptions.IgnoreCase);
            if (dbMatch.Success)
            {
                    if (currentDatabase != null && currentSql.Any())
                    {
                        var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
                        if (!string.IsNullOrWhiteSpace(sql))
                        {
                            result[currentDatabase] = sql;
                        }
                }
                
                var dbIndex = int.Parse(dbMatch.Groups[1].Value) - 1;
                var dbNameFromResponse = dbMatch.Groups[2].Value.Trim();
                
                if (dbIndex >= 0 && dbIndex < databaseQueries.Count)
                {
                    currentDatabase = databaseQueries[dbIndex].DatabaseId;
                    currentSql.Clear();
                    inSql = false;
                    waitingForConfirmed = true;
                    _logger.LogDebug("Found database marker: {DatabaseId} (response had: {DbName})", currentDatabase, dbNameFromResponse);
                }
                else if (!string.IsNullOrWhiteSpace(dbNameFromResponse) && !dbNameFromResponse.Equals("DatabaseName", StringComparison.OrdinalIgnoreCase))
                {
                    var matchingDbQuery = databaseQueries.FirstOrDefault(q => 
                        schemas[q.DatabaseId].DatabaseName.Equals(dbNameFromResponse, StringComparison.OrdinalIgnoreCase));
                    if (matchingDbQuery != null)
                    {
                        currentDatabase = matchingDbQuery.DatabaseId;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                        _logger.LogDebug("Found database by name match: {DatabaseId} (from response: {DbName})", currentDatabase, dbNameFromResponse);
                    }
                }
                else
                {
                    currentDatabase = null;
                    currentSql.Clear();
                    inSql = false;
                    waitingForConfirmed = true;
                }
                continue;
            }

            if (trimmedLine.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase))
            {
                if (currentDatabase != null)
                {
                    inSql = true;
                    waitingForConfirmed = false;
                }
                continue;
            }
            
            if (currentDatabase != null && waitingForConfirmed && 
                (trimmedLine.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                 trimmedLine.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)))
            {
                inSql = true;
                waitingForConfirmed = false;
                currentSql.Clear();
                currentSql.Add(trimmedLine);
                continue;
            }

            if (trimmedLine.StartsWith("CORRECTION:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            if (trimmedLine.StartsWith("Explanation:", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("Note:", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("Note that", StringComparison.OrdinalIgnoreCase))
            {
                if (currentDatabase != null && currentSql.Any())
                {
                    inSql = false;
                }
                continue;
            }

            if (currentDatabase != null && (inSql || !waitingForConfirmed))
            {
                if (trimmedLine.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                {
                    inSql = true;
                    waitingForConfirmed = false;
                    currentSql.Clear();
                    currentSql.Add(trimmedLine);
                }
                else if (inSql && currentSql.Any())
                {
                    var lowerLine = trimmedLine.ToLowerInvariant();
                    if (lowerLine.StartsWith("select") || 
                        lowerLine.StartsWith("from") || 
                        lowerLine.StartsWith("where") ||
                        lowerLine.StartsWith("join") ||
                        lowerLine.StartsWith("inner") ||
                        lowerLine.StartsWith("left") ||
                        lowerLine.StartsWith("right") ||
                        lowerLine.StartsWith("on") ||
                        lowerLine.StartsWith("group") ||
                        lowerLine.StartsWith("order") ||
                        lowerLine.StartsWith("having") ||
                        lowerLine.StartsWith("limit") ||
                        lowerLine.StartsWith("top") ||
                        lowerLine.StartsWith("as") ||
                        lowerLine.StartsWith("and") ||
                        lowerLine.StartsWith("or") ||
                        lowerLine.Contains("(") ||
                        lowerLine.Contains(")") ||
                        lowerLine.Contains(",") ||
                        lowerLine.Contains("=") ||
                        lowerLine.Contains("'") ||
                        trimmedLine.EndsWith(";") ||
                        char.IsDigit(trimmedLine[0]))
                    {
                        currentSql.Add(trimmedLine);
                    }
                    else if (trimmedLine.Length > 50 && !lowerLine.Contains("database") && !lowerLine.Contains("explanation"))
                    {
                        currentSql.Add(trimmedLine);
                    }
                    else
                    {
                        var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
                        if (!string.IsNullOrWhiteSpace(sql) && sql.Length > 20)
                        {
                            result[currentDatabase] = sql;
                        }
                        inSql = false;
                    }
                }
            }
        }

        if (currentDatabase != null && currentSql.Any())
        {
            var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
            if (!string.IsNullOrWhiteSpace(sql))
            {
                result[currentDatabase] = sql;
            }
        }

        if (result.Count == 0)
        {
            result = ExtractSqlBlocksWithoutDatabaseMarkers(response, databaseQueries, schemas);
            if (result.Count > 0)
                _logger.LogDebug("Extracted {Count} SQL block(s) via fallback (no DATABASE N: markers)", result.Count);
            else
                _logger.LogWarning("Failed to extract any SQL from AI response. Response preview: {Preview}", 
                    response?.Substring(0, Math.Min(500, response?.Length ?? 0)) ?? "null");
        }

        return result;
    }

    private Dictionary<string, string> ExtractSqlBlocksWithoutDatabaseMarkers(string response, List<DatabaseQueryIntent> databaseQueries, Dictionary<string, DatabaseSchemaInfo> schemas)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(response) || databaseQueries == null || databaseQueries.Count == 0)
            return result;

        response = Regex.Replace(response, @"```sql\s*", "", RegexOptions.IgnoreCase);
        response = Regex.Replace(response, @"```\s*", "", RegexOptions.IgnoreCase);

        var parts = Regex.Split(response, @"\s*;\s*", RegexOptions.Multiline);
        var selectBlocks = new List<string>();
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                var sql = ExtractCompleteSQL(trimmed);
                if (!string.IsNullOrWhiteSpace(sql) && sql.Length > 20)
                    selectBlocks.Add(sql);
            }
        }

        if (selectBlocks.Count == 0)
        {
            var single = ExtractCompleteSQL(response);
            if (!string.IsNullOrWhiteSpace(single) && single.Length > 20)
                selectBlocks.Add(single);
        }

        for (int i = 0; i < selectBlocks.Count && i < databaseQueries.Count; i++)
        {
            var dbQuery = databaseQueries[i];
            var sql = selectBlocks[i];
            var dbId = dbQuery.DatabaseId;

            var bestSchema = schemas.GetValueOrDefault(dbId);
            var bestScore = bestSchema != null
                ? bestSchema.Tables.Count(t => sql.IndexOf(t.TableName, StringComparison.OrdinalIgnoreCase) >= 0)
                : 0;

            foreach (var kv in schemas)
            {
                if (kv.Key == dbId || result.ContainsKey(kv.Key)) continue;
                var score = kv.Value.Tables.Count(t => sql.IndexOf(t.TableName, StringComparison.OrdinalIgnoreCase) >= 0);
                if (score > bestScore)
                {
                    bestScore = score;
                    dbId = kv.Key;
                }
            }

            if (!result.ContainsKey(dbId))
                result[dbId] = sql;
        }

        return result;
    }

    private async Task<List<string>> GetRequiredMappingColumnsAsync(string databaseName, List<string> requiredTables)
    {
        var requiredColumns = new List<string>();
        
        try
        {
            var connections = await _connectionManager.GetAllConnectionsAsync();
            var connection = connections.FirstOrDefault(c =>
                (c.Name ?? string.Empty).Equals(databaseName, StringComparison.OrdinalIgnoreCase));
            
            if (connection?.CrossDatabaseMappings == null)
                return requiredColumns;

            foreach (var mapping in connection.CrossDatabaseMappings)
            {
                if (!mapping.SourceDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (requiredTables != null && requiredTables.Count > 0)
                {
                    var tableName = mapping.SourceTable;
                    if (tableName.Contains('.'))
                    {
                        var parts = tableName.Split('.');
                        tableName = parts.Length > 1 ? parts[1] : parts[0];
                    }
                    
                    if (!requiredTables.Any(t => t.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                                                 t.Equals(mapping.SourceTable, StringComparison.OrdinalIgnoreCase) ||
                                                 mapping.SourceTable.Contains(t, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                }

                if (!requiredColumns.Contains($"{mapping.SourceTable}.{mapping.SourceColumn}", StringComparer.OrdinalIgnoreCase))
                {
                    requiredColumns.Add($"{mapping.SourceTable}.{mapping.SourceColumn}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting required mapping columns for database {DatabaseName}", databaseName);
        }

        return requiredColumns;
    }


    private async Task<List<string>> ValidateCrossDatabaseMappingColumnsAsync(string sql, string databaseName, List<string> requiredTables)
    {
        var missingColumns = new List<string>();
        
        try
        {
            var connections = await _connectionManager.GetAllConnectionsAsync();
            var connection = connections.FirstOrDefault(c =>
                (c.Name ?? string.Empty).Equals(databaseName, StringComparison.OrdinalIgnoreCase));
            
            if (connection?.CrossDatabaseMappings == null)
                return missingColumns;

            var sqlUpper = sql.ToUpperInvariant();
            
            foreach (var mapping in connection.CrossDatabaseMappings)
            {
                if (!mapping.SourceDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (requiredTables != null && requiredTables.Count > 0)
                {
                    var tableName = mapping.SourceTable;
                    if (tableName.Contains('.'))
                    {
                        var parts = tableName.Split('.');
                        tableName = parts.Length > 1 ? parts[1] : parts[0];
                    }
                    
                    if (!requiredTables.Any(t => t.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                                                 t.Equals(mapping.SourceTable, StringComparison.OrdinalIgnoreCase) ||
                                                 mapping.SourceTable.Contains(t, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                }

                var columnName = mapping.SourceColumn;
                var columnPattern = $@"\b{Regex.Escape(columnName)}\b";
                if (!Regex.IsMatch(sql, columnPattern, RegexOptions.IgnoreCase))
                {
                    missingColumns.Add($"{mapping.SourceTable}.{mapping.SourceColumn}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cross-database mapping columns for database {DatabaseName}", databaseName);
        }

        return missingColumns;
    }

    private bool ValidateSql(string sql, DatabaseSchemaInfo schema, List<string> requiredTables, ISqlDialectStrategy strategy, List<string> allDatabaseNames, out List<string> errors)
    {
        errors = new List<string>();

        if (!strategy.ValidateSyntax(sql, out var syntaxError))
        {
            errors.Add(syntaxError);
        }

        var schemaErrors = _validator.ValidateQuery(sql, schema, requiredTables, allDatabaseNames);
        errors.AddRange(schemaErrors);

        return errors.Count == 0;
    }

    private string ExtractCompleteSQL(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText)) return string.Empty;

        sqlText = Regex.Replace(sqlText, @"```sql\s*", "", RegexOptions.IgnoreCase);
        sqlText = Regex.Replace(sqlText, @"```\s*", "", RegexOptions.IgnoreCase);
        sqlText = Regex.Replace(sqlText, @"###\s+.*", "", RegexOptions.IgnoreCase);
        sqlText = Regex.Replace(sqlText, @"Explanation:.*", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var lines = sqlText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var sqlLines = new List<string>();
        bool foundSelect = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;
            
            if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("###", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Explanation:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                foundSelect = true;
                sqlLines.Add(trimmed);
            }
            else if (foundSelect)
            {
                if (trimmed.StartsWith("--") || trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
                    continue;

                var sqlKeywords = new[] { "FROM", "WHERE", "GROUP", "ORDER", "HAVING", "LIMIT", "TOP", "JOIN", "INNER", "LEFT", "RIGHT", "ON", "AND", "OR", "AS", "UNION", "EXCEPT", "INTERSECT" };
                var isSQLKeyword = Array.Exists(sqlKeywords, kw => trimmed.StartsWith(kw, StringComparison.OrdinalIgnoreCase));
                
                if (!isSQLKeyword && trimmed.Length > 0 && char.IsLetter(trimmed[0]) && !trimmed.Contains("(") && !trimmed.Contains(")") && !trimmed.Contains(",") && !trimmed.Contains("=") && !trimmed.Contains("'") && !trimmed.Contains("\""))
                {
                    break;
                }

                sqlLines.Add(trimmed);
            }
        }

        return string.Join(" ", sqlLines).Trim();
    }

    private string DetectAndFixCrossDatabaseReferences(string sql, DatabaseSchemaInfo schema, List<string> requiredTables)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var fixedSql = sql;
        var validTableNames = schema.Tables.Select(t => t.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        var databasePrefixPatterns = new[]
        {
            @"\b(\w+)\.(\w+)\.(\w+)\b",
            @"\""([^\""]+)\""\.\""([^\""]+)\""\.\""([^\""]+)\""",
            @"\""([^\""]+)\""\.\""([^\""]+)\""\.(\w+)",
            @"\""([^\""]+)\""\.(\w+)\.(\w+)",
            @"(\w+)\.(\w+)\.(\w+)",
            @"\[(\w+)\]\.\[(\w+)\]\.\[(\w+)\]",
            @"(\w+)\.""(\w+)""\.(\w+)"
        };
        
        var replacements = new List<(string Original, string Replacement)>();
        
        foreach (var pattern in databasePrefixPatterns)
        {
            var matches = Regex.Matches(fixedSql, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
        {
            var fullMatch = match.Value;
            var databaseName = match.Groups[1].Value.Trim('"', '[', ']');
            var schemaName = match.Groups[2].Value.Trim('"', '[', ']');
            var tableName = match.Groups[3].Value.Trim('"', '[', ']');
            var twoPartName = $"{schemaName}.{tableName}";
            
            if (!schema.DatabaseName.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
            {
                string replacement = null;
                
                var foundValidTable = validTableNames.FirstOrDefault(vt => 
                    vt.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                    vt.Equals(twoPartName, StringComparison.OrdinalIgnoreCase) ||
                    (vt.Contains('.') && vt.Split('.').Last().Equals(tableName, StringComparison.OrdinalIgnoreCase)) ||
                    (vt.Contains('.') && vt.Equals(twoPartName, StringComparison.OrdinalIgnoreCase)));
                
                if (foundValidTable != null)
                {
                    _logger.LogWarning("Removing database prefix from table reference: {Full} -> {Table}", fullMatch, foundValidTable);
                    replacement = foundValidTable;
                }
                else
                {
                    var requiredTableMatch = requiredTables.FirstOrDefault(rt => 
                        rt.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                        rt.Equals(twoPartName, StringComparison.OrdinalIgnoreCase) ||
                        rt.EndsWith($".{tableName}", StringComparison.OrdinalIgnoreCase) ||
                        rt.EndsWith($"\".{tableName}\"", StringComparison.OrdinalIgnoreCase) ||
                        (rt.Contains('.') && rt.Split('.').Last().Equals(tableName, StringComparison.OrdinalIgnoreCase)));
                    
                    if (requiredTableMatch != null)
                    {
                        _logger.LogWarning("Removing database prefix from table reference: {Full} -> {Table}", fullMatch, requiredTableMatch);
                        replacement = requiredTableMatch;
                    }
                    else
                    {
                        _logger.LogWarning("Removing entire cross-database table reference: {Full} (table not found in {Database})", fullMatch, schema.DatabaseName);
                        replacement = string.Empty;
                    }
                }
                
                if (replacement != null && !replacements.Any(r => r.Original.Equals(fullMatch, StringComparison.OrdinalIgnoreCase)))
                {
                    replacements.Add((fullMatch, replacement));
                }
            }
        }
        }
        
        foreach (var (original, replacement) in replacements.OrderByDescending(r => r.Original.Length))
        {
            if (string.IsNullOrEmpty(replacement))
            {
                fixedSql = Regex.Replace(fixedSql, Regex.Escape(original), string.Empty, RegexOptions.IgnoreCase);
            }
            else
            {
                fixedSql = Regex.Replace(fixedSql, Regex.Escape(original), replacement, RegexOptions.IgnoreCase);
            }
        }

        fixedSql = FixInvalidFromClauses(fixedSql);

        return fixedSql;
    }

    private string FixInvalidFromClauses(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var fixedSql = sql;

        var invalidPatterns = new[]
        {
            @"FROM\s+GROUP\s+BY",
            @"FROM\s+WHERE\s+",
            @"FROM\s+ORDER\s+BY",
            @"FROM\s+HAVING\s+",
            @"FROM\s+LIMIT\s+",
            @"FROM\s+\)",
            @"FROM\s*$",
            @"FROM\s+\s+GROUP"
        };

        foreach (var pattern in invalidPatterns)
        {
            if (Regex.IsMatch(fixedSql, pattern, RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("Detected invalid FROM clause after removing cross-database reference. Attempting to fix SQL.");
                
                fixedSql = Regex.Replace(fixedSql, @"FROM\s+GROUP\s+BY", "WHERE 1=0 GROUP BY", RegexOptions.IgnoreCase);
                fixedSql = Regex.Replace(fixedSql, @"FROM\s+WHERE\s+", "FROM (SELECT NULL) AS InvalidTable WHERE ", RegexOptions.IgnoreCase);
                fixedSql = Regex.Replace(fixedSql, @"FROM\s+ORDER\s+BY", "FROM (SELECT NULL) AS InvalidTable ORDER BY", RegexOptions.IgnoreCase);
                fixedSql = Regex.Replace(fixedSql, @"FROM\s+HAVING\s+", "FROM (SELECT NULL) AS InvalidTable HAVING ", RegexOptions.IgnoreCase);
                fixedSql = Regex.Replace(fixedSql, @"FROM\s+LIMIT\s+", "FROM (SELECT NULL) AS InvalidTable LIMIT ", RegexOptions.IgnoreCase);
                
                var fromSubqueryMatch = Regex.Match(fixedSql, @"\(([^)]*FROM\s*)\)", RegexOptions.IgnoreCase);
                if (fromSubqueryMatch.Success)
                {
                    fixedSql = fixedSql.Replace(fromSubqueryMatch.Value, "(SELECT NULL) AS InvalidSubquery");
                }
                
                break;
            }
        }

        return fixedSql;
    }

    /// <summary>
    /// Fixes ambiguous column names in JOIN queries by adding table aliases
    /// </summary>
    private string FixAmbiguousColumnsInJoin(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        if (!Regex.IsMatch(sql, @"\b(?:INNER|LEFT|RIGHT|FULL|CROSS)\s+JOIN\b", RegexOptions.IgnoreCase))
        {
            return sql;
        }

        var aliasToTable = ExtractTableAliases(sql);
        if (aliasToTable.Count == 0)
        {
            return sql;
        }

        var columnToTables = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var table in schema.Tables)
        {
            foreach (var column in table.Columns)
            {
                if (!columnToTables.ContainsKey(column.ColumnName))
                {
                    columnToTables[column.ColumnName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                columnToTables[column.ColumnName].Add(table.TableName);
            }
        }

        var ambiguousColumns = columnToTables
            .Where(kvp => kvp.Value.Count > 1)
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (ambiguousColumns.Count == 0)
        {
            return sql;
        }

        var usedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in aliasToTable.Keys)
        {
            usedTables.Add(aliasToTable[alias]);
        }

        var fixedSql = sql;

        var selectPattern = @"SELECT\s+(.*?)\s+FROM";
        var selectMatch = Regex.Match(fixedSql, selectPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (selectMatch.Success)
        {
            var selectClause = selectMatch.Groups[1].Value;
            var fixedSelectClause = FixColumnReferences(selectClause, ambiguousColumns, aliasToTable, usedTables);
            if (fixedSelectClause != selectClause)
            {
                fixedSql = fixedSql.Substring(0, selectMatch.Groups[1].Index) + 
                           fixedSelectClause + 
                           fixedSql.Substring(selectMatch.Groups[1].Index + selectMatch.Groups[1].Length);
                _logger.LogDebug("Fixed ambiguous columns in SELECT clause");
            }
        }

        var groupByPattern = @"GROUP\s+BY\s+(.*?)(?:\s+ORDER|\s+HAVING|$)";
        var groupByMatch = Regex.Match(fixedSql, groupByPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (groupByMatch.Success)
        {
            var groupByClause = groupByMatch.Groups[1].Value;
            var fixedGroupByClause = FixColumnReferences(groupByClause, ambiguousColumns, aliasToTable, usedTables);
            if (fixedGroupByClause != groupByClause)
            {
                fixedSql = fixedSql.Substring(0, groupByMatch.Groups[1].Index) + 
                           fixedGroupByClause + 
                           fixedSql.Substring(groupByMatch.Groups[1].Index + groupByMatch.Groups[1].Length);
                _logger.LogDebug("Fixed ambiguous columns in GROUP BY clause");
            }
        }

        var orderByPattern = @"ORDER\s+BY\s+(.*?)(?:\s+LIMIT|$)";
        var orderByMatch = Regex.Match(fixedSql, orderByPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (orderByMatch.Success)
        {
            var orderByClause = orderByMatch.Groups[1].Value;
            var fixedOrderByClause = FixColumnReferences(orderByClause, ambiguousColumns, aliasToTable, usedTables);
            if (fixedOrderByClause != orderByClause)
            {
                fixedSql = fixedSql.Substring(0, orderByMatch.Groups[1].Index) + 
                           fixedOrderByClause + 
                           fixedSql.Substring(orderByMatch.Groups[1].Index + orderByMatch.Groups[1].Length);
                _logger.LogDebug("Fixed ambiguous columns in ORDER BY clause");
            }
        }

        return fixedSql;
    }

    private string FixColumnReferences(string clause, HashSet<string> ambiguousColumns, Dictionary<string, string> aliasToTable, HashSet<string> usedTables)
    {
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        var parenDepth = 0;
        var inQuotes = false;
        var quoteChar = '\0';

        for (int i = 0; i < clause.Length; i++)
        {
            var ch = clause[i];

            if (!inQuotes && (ch == '\'' || ch == '"'))
            {
                inQuotes = true;
                quoteChar = ch;
                currentPart.Append(ch);
            }
            else if (inQuotes && ch == quoteChar)
            {
                inQuotes = false;
                quoteChar = '\0';
                currentPart.Append(ch);
            }
            else if (!inQuotes)
            {
                if (ch == '(')
                {
                    parenDepth++;
                    currentPart.Append(ch);
                }
                else if (ch == ')')
                {
                    parenDepth--;
                    currentPart.Append(ch);
                }
                else if (ch == ',' && parenDepth == 0)
                {
                    var part = currentPart.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        parts.Add(FixSingleColumnReference(part, ambiguousColumns, aliasToTable, usedTables));
                    }
                    currentPart.Clear();
                }
                else
                {
                    currentPart.Append(ch);
                }
            }
            else
            {
                currentPart.Append(ch);
            }
        }

        var lastPart = currentPart.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastPart))
        {
            parts.Add(FixSingleColumnReference(lastPart, ambiguousColumns, aliasToTable, usedTables));
        }

        return string.Join(", ", parts);
    }

    private string FixSingleColumnReference(string columnRef, HashSet<string> ambiguousColumns, Dictionary<string, string> aliasToTable, HashSet<string> usedTables)
    {
        columnRef = columnRef.Trim();

        if (string.IsNullOrWhiteSpace(columnRef))
            return columnRef;

        var trimmed = columnRef.Trim();

        if (trimmed.Contains("."))
        {
            return columnRef;
        }

        if (Regex.IsMatch(trimmed, @"\b(?:COUNT|SUM|AVG|MAX|MIN|CONCAT|UPPER|LOWER|SUBSTRING|CAST|CONVERT)\s*\(.*\)", RegexOptions.IgnoreCase))
        {
            var functionMatch = Regex.Match(trimmed, @"\b(?:COUNT|SUM|AVG|MAX|MIN|CONCAT|UPPER|LOWER|SUBSTRING|CAST|CONVERT)\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
            if (functionMatch.Success)
            {
                var innerColumn = functionMatch.Groups[1].Value.Trim();
                if (ambiguousColumns.Contains(innerColumn, StringComparer.OrdinalIgnoreCase) && !innerColumn.Contains("."))
                {
                    var functionBestAlias = FindBestAliasForColumn(innerColumn, aliasToTable, usedTables);
                    if (!string.IsNullOrEmpty(functionBestAlias))
                    {
                        var fixedInner = $"{functionBestAlias}.{innerColumn}";
                        var fixedRef = columnRef.Replace(innerColumn, fixedInner);
                        _logger.LogTrace("Fixed ambiguous column '{Column}' in function to '{Fixed}'", innerColumn, fixedRef);
                        return fixedRef;
                    }
                }
            }
            return columnRef;
        }

        if (trimmed.Contains(" AS ", StringComparison.OrdinalIgnoreCase))
        {
            var asMatch = Regex.Match(trimmed, @"^(.+?)\s+AS\s+", RegexOptions.IgnoreCase);
            if (asMatch.Success)
            {
                var beforeAs = asMatch.Groups[1].Value.Trim();
                return FixSingleColumnReference(beforeAs, ambiguousColumns, aliasToTable, usedTables) + " " + trimmed.Substring(asMatch.Index + asMatch.Length);
            }
        }

        var columnMatch = Regex.Match(trimmed, @"\b([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase);
        if (!columnMatch.Success)
            return columnRef;

        var columnName = columnMatch.Groups[1].Value;

        if (!ambiguousColumns.Contains(columnName))
            return columnRef;

        var sqlKeywords = new[] { "SELECT", "FROM", "WHERE", "JOIN", "ON", "AND", "OR", "GROUP", "BY", "ORDER", "HAVING", "AS", "COUNT", "SUM", "AVG", "MAX", "MIN", "TOP", "LIMIT" };
        if (sqlKeywords.Contains(columnName.ToUpperInvariant()))
            return columnRef;

        var bestAlias = FindBestAliasForColumn(columnName, aliasToTable, usedTables);
        if (!string.IsNullOrEmpty(bestAlias))
        {
            var fixedRef = Regex.Replace(columnRef, $@"\b{Regex.Escape(columnName)}\b", $"{bestAlias}.{columnName}", RegexOptions.IgnoreCase);
            _logger.LogTrace("Fixed ambiguous column '{Column}' to '{Fixed}'", columnName, fixedRef);
            return fixedRef;
        }

        return columnRef;
    }

    private string FindBestAliasForColumn(string columnName, Dictionary<string, string> aliasToTable, HashSet<string> usedTables)
    {
        foreach (var kvp in aliasToTable)
        {
            var alias = kvp.Key;
            var tableName = kvp.Value;

            if (!usedTables.Contains(tableName))
                continue;

            var tableMatch = Regex.Match(tableName, @"\.([^.]+)$");
            var shortTableName = tableMatch.Success ? tableMatch.Groups[1].Value : tableName;

            if (alias.Equals(shortTableName, StringComparison.OrdinalIgnoreCase) ||
                alias.Equals(tableName.Replace(".", "_"), StringComparison.OrdinalIgnoreCase))
            {
                return alias;
            }
        }

        if (aliasToTable.Count > 0)
        {
            return aliasToTable.Keys.First();
        }

        return string.Empty;
    }

    private Dictionary<string, string> ExtractTableAliases(string sql)
    {
        var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var fromPattern = @"FROM\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?(?:\s|$|JOIN)";
        var fromMatch = Regex.Match(sql, fromPattern, RegexOptions.IgnoreCase);
        if (fromMatch.Success)
        {
            var tableName = NormalizeTableName(fromMatch.Groups[1].Value);
            var alias = fromMatch.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(alias))
            {
                var tableMatch = Regex.Match(tableName, @"\.([^.]+)$");
                alias = tableMatch.Success ? tableMatch.Groups[1].Value : tableName;
            }
            if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
            {
                aliasToTable[alias] = tableName;
            }
        }

        var joinPattern = @"(?:INNER|LEFT|RIGHT|FULL|CROSS)\s+JOIN\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?(?:\s|$|ON)";
        var joinMatches = Regex.Matches(sql, joinPattern, RegexOptions.IgnoreCase);
        foreach (Match match in joinMatches)
        {
            var tableName = NormalizeTableName(match.Groups[1].Value);
            var alias = match.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(alias))
            {
                var tableMatch = Regex.Match(tableName, @"\.([^.]+)$");
                alias = tableMatch.Success ? tableMatch.Groups[1].Value : tableName;
            }
            if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
            {
                aliasToTable[alias] = tableName;
            }
        }

        return aliasToTable;
    }

    private string NormalizeTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return tableName;

        return tableName
            .Replace("[", "")
            .Replace("]", "")
            .Replace("\"", "")
            .Replace("`", "")
            .Trim();
    }

    private bool IsSqlKeyword(string word)
    {
        var keywords = new[] { "SELECT", "FROM", "WHERE", "JOIN", "ON", "AND", "OR", "GROUP", "BY", "ORDER", "LIMIT", "TOP", "AS", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS", "HAVING" };
        return keywords.Contains(word.ToUpperInvariant());
    }

}

