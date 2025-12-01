---
layout: default
title: Gelişmiş Özellikler
description: Gelişmiş özellikler ve özelleştirme örnekleri
lang: tr
---

## Gelişmiş Özellikler

<p>SmartRAG, strateji desenleri ve gelişmiş arayüzler aracılığıyla genişletilebilirlik ve özelleştirme sağlar.</p>

### Konuşma Yönetimi

`IConversationManagerService` ile özel konuşma yönetimi.

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
    
    // Yeni konuşma başlat
    [HttpPost("conversations/new")]
    public async Task<IActionResult> StartNewConversation()
    {
        var sessionId = await _conversationManager.StartNewConversationAsync();
        return Ok(new { sessionId });
    }
    
    // Konuşma context'i ile sohbet
    [HttpPost("conversations/{sessionId}/chat")]
    public async Task<IActionResult> Chat(string sessionId, [FromBody] ChatRequest request)
    {
        // Context için konuşma geçmişini al
        var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
        
        // Context ile sorgu
        var response = await _searchService.QueryIntelligenceAsync(request.Message);
        
        // Konuşma geçmişine kaydet
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
    
    // Konuşma geçmişini al
    [HttpGet("conversations/{sessionId}/history")]
    public async Task<IActionResult> GetHistory(string sessionId)
    {
        var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
        return Ok(new { history });
    }
}
```

### Özel SQL Diyalekt Stratejisi

`ISqlDialectStrategy` ile özel veritabanı desteği ekleyin. SmartRAG varsayılan olarak SQLite, SQL Server, MySQL ve PostgreSQL'i destekler. Bunları genişletebilir veya özel varyantlar oluşturabilirsiniz.

```csharp
// Gelişmiş PostgreSQL diyalekt stratejisi
public class EnhancedPostgreSqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    
    public override string GetDialectName() => "Gelişmiş PostgreSQL";
    
    public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("PostgreSQL SQL uzmanısınız. PostgreSQL'e özgü SQL oluşturun.");
        prompt.AppendLine($"Kullanıcı Sorgusu: {userQuery}");
        prompt.AppendLine($"Veritabanı Şeması: {JsonSerializer.Serialize(schema)}");
        prompt.AppendLine("Kurallar:");
        prompt.AppendLine("- PostgreSQL sözdizimi kullanın (sayfalama için LIMIT/OFFSET)");
        prompt.AppendLine("- Uygun olduğunda PostgreSQL'e özgü fonksiyonlar kullanın (örn. ARRAY_AGG, JSON fonksiyonları)");
        prompt.AppendLine("- Doğru PostgreSQL veri tiplerini kullanın");
        prompt.AppendLine("- Sadece SQL sorgusunu döndürün, açıklama yapmayın");
        
        return prompt.ToString();
    }
    
    public override bool ValidateSyntax(string sql, out string errorMessage)
    {
        errorMessage = null;
        
        // PostgreSQL'e özgü doğrulama
        if (sql.Contains("TOP", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "PostgreSQL TOP değil LIMIT kullanır";
            return false;
        }
        
        if (sql.Contains("FETCH FIRST", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "PostgreSQL FETCH FIRST değil LIMIT kullanır";
            return false;
        }
        
        return true;
    }
    
    public override string FormatSql(string sql)
    {
        // PostgreSQL'e özgü biçimlendirme (opsiyonel)
        return sql;
    }
    
    public override string GetLimitClause(int limit)
    {
        return $"LIMIT {limit}";
    }
}

// DI'a kaydet
services.AddSingleton<ISqlDialectStrategy, EnhancedPostgreSqlDialectStrategy>();
```

### Özel Skorlama Stratejisi

`IScoringStrategy` ile özel ilgililik skorlaması uygulayın.

```csharp
// Sadece semantik skorlama (%100 embedding tabanlı)
public class SemanticOnlyScoringStrategy : IScoringStrategy
{
    public async Task<double> CalculateScoreAsync(
        string query,
        DocumentChunk chunk,
        List<float> queryEmbedding)
    {
        if (chunk.Embedding == null || chunk.Embedding.Count == 0)
            return 0.0;
        
        // Saf kosinüs benzerliği
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

// DI'a kaydet
services.AddSingleton<IScoringStrategy, SemanticOnlyScoringStrategy>();
```

### Özel Dosya Ayrıştırıcı

`IFileParser` ile özel dosya formatları için destek ekleyin.

```csharp
// Markdown dosya ayrıştırıcı
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
        
        // Markdown sözdizimini kaldır, metni koru
        var text = Regex.Replace(content, @"[#*_`\[\]()]", "");
        
        return text;
    }
}

// DI'a kaydet
services.AddSingleton<IFileParser, MarkdownFileParser>();
```

## İlgili Örnekler

- [Örnekler Ana Sayfası]({{ site.baseurl }}/tr/examples) - Örnekler kategorilerine dön

