using SmartRAG.Entities;
using SmartRAG.Interfaces.AI;
using SmartRAG.Models;
using System.Text;

namespace SmartRAG.Services.Database;


/// <summary>
/// Service for converting database schema information into vectorized chunks
/// </summary>
public class SchemaChunkService
{
    private const long TransactionalTableThreshold = 10000;
    private const long LookupTableThreshold = 1000;

    private readonly IAIService _aiService;
    private readonly ILogger<SchemaChunkService> _logger;

    public SchemaChunkService(
        IAIService aiService,
        ILogger<SchemaChunkService> logger)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts database schema information into Document entity with chunks and embeddings
    /// </summary>
    /// <param name="schema">Database schema information to convert</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Document entity containing schema chunks with embeddings</returns>
    public async Task<Entities.Document> ConvertSchemaToDocumentAsync(
        DatabaseSchemaInfo schema,
        CancellationToken cancellationToken = default)
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        if (schema.Tables == null || schema.Tables.Count == 0)
        {
            _logger.LogWarning("Schema {DatabaseName} has no tables to convert", schema.DatabaseName);
            return null;
        }

        var documentId = Guid.NewGuid();
        var chunks = new List<DocumentChunk>();

        foreach (var table in schema.Tables)
        {
            var chunk = CreateChunkForTable(table, schema, documentId, chunks.Count);
            chunks.Add(chunk);
        }

        _logger.LogInformation("Created {Count} schema chunks for database {DatabaseName}", chunks.Count, schema.DatabaseName);

        var chunkContents = chunks.Select(c => c.Content).ToList();
        var embeddings = await _aiService.GenerateEmbeddingsBatchAsync(chunkContents, cancellationToken);

        if (embeddings == null || embeddings.Count != chunks.Count)
        {
            _logger.LogError("Failed to generate embeddings for schema chunks. Expected {Expected}, got {Actual}",
                chunks.Count, embeddings?.Count ?? 0);
            throw new InvalidOperationException("Failed to generate embeddings for schema chunks");
        }

        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Embedding = embeddings[i];
        }

        _logger.LogInformation("Generated embeddings for {Count} schema chunks", chunks.Count);

        var content = string.Join("\n\n", chunks.Select(c => c.Content));
        var fileSize = System.Text.Encoding.UTF8.GetByteCount(content);

        var document = new Entities.Document
        {
            Id = documentId,
            FileName = $"{schema.DatabaseName}_schema",
            ContentType = "application/schema",
            Content = content,
            UploadedBy = "system",
            UploadedAt = DateTime.UtcNow,
            Chunks = chunks,
            FileSize = fileSize,
            Metadata = new Dictionary<string, object>
            {
                { "databaseId", schema.DatabaseId },
                { "databaseName", schema.DatabaseName },
                { "databaseType", schema.DatabaseType.ToString() },
                { "documentType", "Schema" }
            }
        };

        return document;
    }

    private DocumentChunk CreateChunkForTable(
        TableSchemaInfo table,
        DatabaseSchemaInfo schema,
        Guid documentId,
        int chunkIndex)
    {
        var content = BuildTableChunkContent(table, schema);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            FileName = $"{schema.DatabaseName}_schema",
            Content = content,
            ChunkIndex = chunkIndex,
            CreatedAt = DateTime.UtcNow,
            DocumentType = "Schema",
            StartPosition = 0,
            EndPosition = content.Length
        };

        return chunk;
    }

    private string BuildTableChunkContent(TableSchemaInfo table, DatabaseSchemaInfo schema)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Table: {table.TableName}");
        var tableKeywords = ExtractSemanticKeywords(table.TableName);
        if (tableKeywords.Any())
        {
            sb.AppendLine($"Semantic Keywords: {string.Join(", ", tableKeywords)}");
            sb.AppendLine($"  → These keywords help match user queries in ANY language to this table");
        }
        sb.AppendLine($"Database: {schema.DatabaseName}");
        sb.AppendLine($"Database Type: {schema.DatabaseType}");
        sb.AppendLine($"Type: {DetermineTableType(table.RowCount)}");
        sb.AppendLine($"Row Count: {table.RowCount:N0}");

        if (table.PrimaryKeys != null && table.PrimaryKeys.Count > 0)
        {
            sb.AppendLine($"Primary Key(s): {string.Join(", ", table.PrimaryKeys)}");
        }

        if (table.Columns != null && table.Columns.Count > 0)
        {
            sb.AppendLine("Available Columns in this table (use these exact column names in SQL):");
            var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            var fkColumns = table.Columns.Where(c => c.IsForeignKey && !c.IsPrimaryKey).ToList();
            var regularColumns = table.Columns.Where(c => !c.IsPrimaryKey && !c.IsForeignKey).ToList();

            if (pkColumns.Any())
            {
                sb.AppendLine("  Primary Key columns:");
                foreach (var column in pkColumns)
                {
                    var columnName = FormatColumnNameForDatabase(column.ColumnName, schema.DatabaseType);
                    sb.AppendLine($"    • {columnName}({column.DataType}) [PK]");
                }
            }

            if (fkColumns.Any())
            {
                sb.AppendLine("  Foreign Key columns:");
                foreach (var column in fkColumns)
                {
                    var fkInfo = table.ForeignKeys?.FirstOrDefault(fk => fk.ColumnName.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase));
                    var fkRef = fkInfo != null ? $" → {FormatColumnNameForDatabase(fkInfo.ReferencedTable, schema.DatabaseType)}.{FormatColumnNameForDatabase(fkInfo.ReferencedColumn, schema.DatabaseType)}" : "";
                    var columnName = FormatColumnNameForDatabase(column.ColumnName, schema.DatabaseType);
                    sb.AppendLine($"    • {columnName}({column.DataType}) [FK]{fkRef}");
                }
            }

            if (regularColumns.Any())
            {
                sb.AppendLine("  Regular columns:");
                foreach (var column in regularColumns)
                {
                    var columnName = FormatColumnNameForDatabase(column.ColumnName, schema.DatabaseType);
                    var columnInfo = $"{columnName}({column.DataType})";
                    if (!column.IsNullable)
                        columnInfo += " [NOT NULL]";
                    if (column.MaxLength.HasValue)
                        columnInfo += $" [MaxLength: {column.MaxLength}]";
                    sb.AppendLine($"    • {columnInfo}");
                }
            }
        }

        if (table.ForeignKeys != null && table.ForeignKeys.Count > 0)
        {
            sb.AppendLine("Outgoing Foreign Keys (this table references other tables):");
            foreach (var fk in table.ForeignKeys)
            {
                var columnName = FormatColumnNameForDatabase(fk.ColumnName, schema.DatabaseType);
                var refTable = FormatColumnNameForDatabase(fk.ReferencedTable, schema.DatabaseType);
                var refColumn = FormatColumnNameForDatabase(fk.ReferencedColumn, schema.DatabaseType);
                sb.AppendLine($"  • {columnName} → references {refTable}.{refColumn}");
            }
        }

        var reverseForeignKeys = FindReverseForeignKeys(table.TableName, schema);
        if (reverseForeignKeys.Any())
        {
            sb.AppendLine("Incoming Foreign Keys (other tables reference this table):");
            foreach (var reverseFk in reverseForeignKeys)
            {
                var refTable = FormatColumnNameForDatabase(reverseFk.ReferencingTable, schema.DatabaseType);
                var refColumn = FormatColumnNameForDatabase(reverseFk.ReferencingColumn, schema.DatabaseType);
                var refCol = FormatColumnNameForDatabase(reverseFk.ReferencedColumn, schema.DatabaseType);
                sb.AppendLine($"  • {refTable}.{refColumn} → references this table.{refCol}");
            }
        }

        if (schema.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
        {
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️⚠️ POSTGRESQL CRITICAL: Use DOUBLE QUOTES for ALL identifiers! ⚠️⚠️⚠️");
            sb.AppendLine("  ✓ CORRECT: SELECT \"ColumnName1\", \"ColumnName2\" FROM \"SchemaName\".\"TableName\"");
            sb.AppendLine("  ✗ WRONG: SELECT ColumnName1, ColumnName2 FROM SchemaName.TableName  -- Will fail!");
            sb.AppendLine();
            sb.AppendLine("🚨🚨🚨 POSTGRESQL ALIAS RULE - CRITICAL! 🚨🚨🚨");
            sb.AppendLine("  → Alias WITHOUT quotes, column WITH quotes!");
            sb.AppendLine("  ✓ CORRECT: SELECT T1.\"ColumnName\" FROM \"SchemaName\".\"TableName\" T1");
            sb.AppendLine("  ✓ CORRECT: SELECT T1.\"ColumnName\", T2.\"OtherColumn\" FROM \"SchemaName\".\"TableName\" T1 JOIN \"SchemaName\".\"OtherTable\" T2 ON T1.\"FK\" = T2.\"PK\"");
            sb.AppendLine("  ✓ CORRECT: SELECT T1.\"ColumnName\" FROM \"SchemaName\".\"TableName\" T1 WHERE T1.\"ID\" IN (1, 2, 3)");
            sb.AppendLine("  ✗ WRONG: SELECT \"T1\".\"ColumnName\" FROM \"SchemaName\".\"TableName\" \"T1\"  -- SYNTAX ERROR!");
            sb.AppendLine("  ✗ WRONG: SELECT \"T1\".\"ColumnName\" FROM \"SchemaName\".\"TableName\" T1  -- SYNTAX ERROR (alias mismatch)!");
            sb.AppendLine("  ✗ WRONG: WHERE \"T1\".\"ID\" IN (...)  -- SYNTAX ERROR! Use WHERE T1.\"ID\" IN (...)");
            sb.AppendLine("  Rule: Alias NEVER in quotes - T1, T2, T3 (NOT \"T1\", \"T2\", \"T3\")");
            sb.AppendLine("  Rule: Column ALWAYS in quotes when using alias - T1.\"ColumnName\" (NOT T1.ColumnName)");
        }

        return sb.ToString();
    }

    private static string FormatColumnNameForDatabase(string identifier, SmartRAG.Enums.DatabaseType databaseType)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return identifier;

        if (databaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
        {
            if (identifier.Contains('.'))
            {
                var parts = identifier.Split('.', 2);
                var schemaPart = HasUpperCase(parts[0]) ? $"\"{parts[0]}\"" : parts[0];
                var tablePart = HasUpperCase(parts[1]) ? $"\"{parts[1]}\"" : parts[1];
                return $"{schemaPart}.{tablePart}";
            }
            
            return HasUpperCase(identifier) ? $"\"{identifier}\"" : identifier;
        }

        return identifier;
    }

    private static bool HasUpperCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Any(c => char.IsUpper(c));
    }

    private static List<string> ExtractSemanticKeywords(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return new List<string>();

        var keywords = new List<string>();
        var parts = new List<string>();

        if (identifier.Contains('.'))
        {
            var dotParts = identifier.Split('.');
            foreach (var part in dotParts)
            {
                parts.AddRange(SplitCamelCase(part));
            }
        }
        else
        {
            parts.AddRange(SplitCamelCase(identifier));
        }

        foreach (var part in parts)
        {
            var lower = part.ToLowerInvariant();
            if (lower.Length > 2 && !keywords.Contains(lower, StringComparer.OrdinalIgnoreCase))
            {
                keywords.Add(lower);
            }
        }

        return keywords;
    }

    private static List<string> SplitCamelCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var parts = new List<string>();
        var currentWord = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsUpper(c) && currentWord.Length > 0)
            {
                parts.Add(currentWord.ToString());
                currentWord.Clear();
            }
            currentWord.Append(c);
        }

        if (currentWord.Length > 0)
        {
            parts.Add(currentWord.ToString());
        }

        if (parts.Count == 0)
        {
            parts.Add(text);
        }

        return parts;
    }

    private List<(string ReferencingTable, string ReferencingColumn, string ReferencedColumn)> FindReverseForeignKeys(string tableName, DatabaseSchemaInfo schema)
    {
        var reverseFks = new List<(string ReferencingTable, string ReferencingColumn, string ReferencedColumn)>();
        
        if (schema.Tables == null)
            return reverseFks;

        foreach (var otherTable in schema.Tables)
        {
            if (otherTable.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (otherTable.ForeignKeys == null)
                continue;

            foreach (var fk in otherTable.ForeignKeys)
            {
                if (fk.ReferencedTable.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                {
                    reverseFks.Add((otherTable.TableName, fk.ColumnName, fk.ReferencedColumn));
                }
            }
        }

        return reverseFks;
    }

    private string DetermineTableType(long rowCount)
    {
        if (rowCount > TransactionalTableThreshold)
            return "TRANSACTIONAL";
        if (rowCount > LookupTableThreshold)
            return "LOOKUP";
        return "MASTER";
    }
}

