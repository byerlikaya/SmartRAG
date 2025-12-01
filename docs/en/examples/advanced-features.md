---
layout: default
title: Advanced Features
description: Advanced features and customization examples
lang: en
---

## Advanced Features

<p>SmartRAG provides extensibility and customization through strategy patterns and advanced interfaces.</p>

### Conversation Management

Dedicated conversation management with `IConversationManagerService`.

```csharp
public class ChatController : ControllerBase
{
    private readonly IConversationManagerService _conversationManager;
    private readonly IDocumentSearchService _searchService;
    
    public ChatController(
        IConversationManagerService conversationManager,
        IDocumentSearchService searchService)
    {
        _conversationManager = conversationManager;
        _searchService = searchService;
    }
    
    // Start new conversation
    [HttpPost("conversations/new")]
    public async Task<IActionResult> StartNewConversation()
    {
        var sessionId = await _conversationManager.StartNewConversationAsync();
        return Ok(new { sessionId });
    }
    
    // Chat with conversation context
    [HttpPost("conversations/{sessionId}/chat")]
    public async Task<IActionResult> Chat(string sessionId, [FromBody] ChatRequest request)
    {
        // Get conversation history for context
        var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
        
        // Query with context
        var response = await _searchService.QueryIntelligenceAsync(request.Message);
        
        // Save to conversation history
        await _conversationManager.AddToConversationAsync(
            sessionId,
            request.Message,
            response.Answer
        );
        
        return Ok(new
        {
            answer = response.Answer,
            sources = response.Sources,
            sessionId
        });
    }
    
    // Get conversation history
    [HttpGet("conversations/{sessionId}/history")]
    public async Task<IActionResult> GetHistory(string sessionId)
    {
        var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
        return Ok(new { history });
    }
}
```

### Custom SQL Dialect Strategy

Add support for custom database dialects with `ISqlDialectStrategy`. SmartRAG supports SQLite, SQL Server, MySQL, and PostgreSQL out of the box. You can extend these or create custom variants.

```csharp
// Enhanced PostgreSQL dialect strategy
public class EnhancedPostgreSqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    
    public override string GetDialectName() => "Enhanced PostgreSQL";
    
    public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("You are a PostgreSQL SQL expert. Generate PostgreSQL-specific SQL.");
        prompt.AppendLine($"User Query: {userQuery}");
        prompt.AppendLine($"Database Schema: {JsonSerializer.Serialize(schema)}");
        prompt.AppendLine("Rules:");
        prompt.AppendLine("- Use PostgreSQL syntax (LIMIT/OFFSET for pagination)");
        prompt.AppendLine("- Use PostgreSQL-specific functions when appropriate (e.g., ARRAY_AGG, JSON functions)");
        prompt.AppendLine("- Use proper PostgreSQL data types");
        prompt.AppendLine("- Return only the SQL query, no explanations");
        
        return prompt.ToString();
    }
    
    public override bool ValidateSyntax(string sql, out string errorMessage)
    {
        errorMessage = null;
        
        // PostgreSQL-specific validation
        if (sql.Contains("TOP", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "PostgreSQL uses LIMIT, not TOP";
            return false;
        }
        
        if (sql.Contains("FETCH FIRST", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "PostgreSQL uses LIMIT, not FETCH FIRST";
            return false;
        }
        
        return true;
    }
    
    public override string FormatSql(string sql)
    {
        // PostgreSQL-specific formatting (optional)
        return sql;
    }
    
    public override string GetLimitClause(int limit)
    {
        return $"LIMIT {limit}";
    }
}

// Register in DI
services.AddSingleton<ISqlDialectStrategy, EnhancedPostgreSqlDialectStrategy>();
```

### Custom Scoring Strategy

Implement custom relevance scoring with `IScoringStrategy`.

```csharp
// Semantic-only scoring (100% embedding-based)
public class SemanticOnlyScoringStrategy : IScoringStrategy
{
    public async Task<double> CalculateScoreAsync(
        string query,
        DocumentChunk chunk,
        List<float> queryEmbedding)
    {
        if (chunk.Embedding == null || chunk.Embedding.Count == 0)
            return 0.0;
        
        // Pure cosine similarity
        return CosineSimilarity(queryEmbedding, chunk.Embedding);
    }
    
    private double CosineSimilarity(List<float> a, List<float> b)
    {
        if (a.Count != b.Count) return 0.0;
        
        double dotProduct = 0;
        double normA = 0;
        double normB = 0;
        
        for (int i = 0; i < a.Count; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        
        if (normA == 0 || normB == 0) return 0.0;
        
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

// Register in DI
services.AddSingleton<IScoringStrategy, SemanticOnlyScoringStrategy>();
```

### Custom File Parser

Add support for custom file formats with `IFileParser`.

```csharp
// Markdown file parser
public class MarkdownFileParser : IFileParser
{
    public bool CanParse(string fileName, string contentType)
    {
        return fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);
    }
    
    public async Task<string> ParseAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        
        // Remove markdown syntax, keep text
        var text = Regex.Replace(content, @"[#*_`\[\]()]", "");
        
        return text;
    }
}

// Register in DI
services.AddSingleton<IFileParser, MarkdownFileParser>();
```

## Related Examples

- [Examples Index]({{ site.baseurl }}/en/examples) - Back to Examples categories

