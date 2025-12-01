---
layout: default
title: Advanced Examples
description: Advanced features and customization examples
lang: en
---

## Advanced Examples

### Conversation Context Management

```csharp
// First question
var q1 = await _searchService.QueryIntelligenceAsync(
    "What is the company's refund policy?"
);

// Follow-up - AI remembers context
var q2 = await _searchService.QueryIntelligenceAsync(
    "What about international orders?"
);

// Another follow-up - maintains full context
var q3 = await _searchService.QueryIntelligenceAsync(
    "How do I initiate a refund?"
);

// Start new conversation
var newConv = await _searchService.QueryIntelligenceAsync(
    "Let's talk about shipping",
    startNewConversation: true
);
```

---

### Batch Document Processing

```csharp
var fileStreams = new List<Stream> { stream1, stream2, stream3 };
var fileNames = new List<string> { "doc1.pdf", "doc2.pdf", "doc3.pdf" };
var contentTypes = new List<string> { "application/pdf", "application/pdf", "application/pdf" };

var documents = await _documentService.UploadDocumentsAsync(
    fileStreams,
    fileNames,
    contentTypes,
    "user-123"
);

Console.WriteLine($"Uploaded {documents.Count} documents");
```

---

### Custom SQL Execution

```csharp
// Execute custom SQL query
var result = await _databaseService.ExecuteQueryAsync(
    connectionString: "Server=localhost;Database=Sales;Trusted_Connection=true;",
    query: @"
        SELECT 
            c.CustomerName, 
            SUM(o.TotalAmount) as TotalSales,
            COUNT(o.OrderID) as OrderCount
        FROM Customers c
        JOIN Orders o ON c.CustomerID = o.CustomerID
        WHERE o.OrderDate >= '2024-01-01'
        GROUP BY c.CustomerName
        ORDER BY TotalSales DESC
    ",
    databaseType: DatabaseType.SqlServer,
    maxRows: 100
);

Console.WriteLine(result);
```

---

### Storage Statistics

```csharp
var stats = await _documentService.GetStorageStatisticsAsync();

Console.WriteLine($"Total Documents: {stats["TotalDocuments"]}");
Console.WriteLine($"Total Chunks: {stats["TotalChunks"]}");
Console.WriteLine($"Total Size: {stats["TotalSizeBytes"]} bytes");
Console.WriteLine($"Storage Provider: {stats["StorageProvider"]}");
```

---

### Regenerate Embeddings

Useful after changing AI provider:

```csharp
// Switch from OpenAI to Anthropic
// Need to regenerate embeddings with new provider

bool success = await _documentService.RegenerateAllEmbeddingsAsync();

if (success)
{
    Console.WriteLine("All embeddings regenerated successfully!");
}
```

---

## Related Examples

- [Examples Index]({{ site.baseurl }}/en/examples) - Back to Examples categories
