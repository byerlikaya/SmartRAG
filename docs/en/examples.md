---
layout: default
title: Examples
description: Practical code examples and real-world use cases for SmartRAG
lang: en
---


## Quick Examples

### 1. Simple Document Search

Upload a document and search it:

```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Upload document
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123"
        );
        
        return Ok(new { 
            id = document.Id, 
            fileName = document.FileName,
            chunks = document.Chunks.Count 
        });
    }
    
    // Search documents
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Query,
            maxResults: request.MaxResults
        );
        
        return Ok(response);
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}
```

---

### 2. Multi-Database Query

Query multiple databases simultaneously:

```csharp
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseParserService _databaseService;
    private readonly IMultiDatabaseQueryCoordinator _multiDbCoordinator;
    
    // Connect to SQL Server database
    [HttpPost("connect/sqlserver")]
    public async Task<IActionResult> ConnectSqlServer([FromBody] ConnectionRequest request)
    {
        var config = new DatabaseConfig
        {
            Type = DatabaseType.SqlServer,
            ConnectionString = request.ConnectionString,
            IncludedTables = request.Tables,
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true
        };
        
        var content = await _databaseService.ParseDatabaseConnectionAsync(
            request.ConnectionString,
            config
        );
        
        return Ok(new { message = "Connected successfully", tables = request.Tables });
    }
    
    // Connect to MySQL database
    [HttpPost("connect/mysql")]
    public async Task<IActionResult> ConnectMySQL([FromBody] ConnectionRequest request)
    {
        var config = new DatabaseConfig
        {
            Type = DatabaseType.MySQL,
            ConnectionString = request.ConnectionString,
            MaxRowsPerTable = 1000
        };
        
        var content = await _databaseService.ParseDatabaseConnectionAsync(
            request.ConnectionString,
            config
        );
        
        return Ok(new { message = "Connected successfully" });
    }
    
    // Query across multiple databases
    [HttpPost("query")]
    public async Task<IActionResult> QueryDatabases([FromBody] MultiDbQueryRequest request)
    {
        var response = await _multiDbCoordinator.ExecuteQueryAsync(request.Query);
        
        return Ok(response);
    }
}

public class ConnectionRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public List<string> Tables { get; set; } = new();
}

public class MultiDbQueryRequest
{
    public string Query { get; set; } = string.Empty;
}
```

**Example Query:**
```
"Show me total sales from SQL Server with current inventory levels from MySQL"
```

SmartRAG will:
1. Analyze query intent
2. Identify relevant databases and tables
3. Generate appropriate SQL for each database
4. Execute queries in parallel
5. Merge results intelligently
6. Return unified AI-powered answer

---

### 3. OCR Document Processing

Process images with Tesseract OCR:

```csharp
public class OcrController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Upload image for OCR processing
    [HttpPost("upload/image")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string language = "eng")
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123",
            language: language  // OCR language: eng, tur, deu, etc.
        );
        
        return Ok(new { 
            id = document.Id,
            extractedText = document.Content,
            confidence = "OCR completed successfully"
        });
    }
    
    // Query OCR-processed documents
    [HttpPost("query/image-content")]
    public async Task<IActionResult> QueryImageContent([FromBody] string query)
    {
        var response = await _searchService.QueryIntelligenceAsync(query);
        return Ok(response);
    }
}
```

**Supported Image Formats:**
- JPEG/JPG, PNG, GIF, BMP, TIFF, WebP

**Example Usage:**
```bash
# Upload invoice image
curl -X POST "http://localhost:5000/api/ocr/upload/image?language=eng" \
  -F "file=@invoice.jpg"

# Query: "What is the total amount on this invoice?"
```

---

### 4. Audio Transcription

Transcribe audio files with Google Speech-to-Text:

```csharp
public class AudioController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Upload audio for transcription
    [HttpPost("upload/audio")]
    public async Task<IActionResult> UploadAudio(IFormFile file, [FromQuery] string language = "en-US")
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123",
            language: language  // Speech language: en-US, tr-TR, etc.
        );
        
        return Ok(new { 
            id = document.Id,
            transcription = document.Content,
            message = "Audio transcribed successfully"
        });
    }
    
    // Query transcription content
    [HttpPost("query/audio-content")]
    public async Task<IActionResult> QueryAudioContent([FromBody] string query)
    {
        var response = await _searchService.QueryIntelligenceAsync(query);
        return Ok(response);
    }
}
```

**Supported Audio Formats:**
- MP3, WAV, M4A, AAC, OGG, FLAC, WMA

**Language Codes:**
- `en-US` - English
- `tr-TR` - Turkish
- `de-DE` - German
- `fr-FR` - French
- 100+ languages

<div class="alert alert-warning">
    <h4><i class="fas fa-cloud me-2"></i> Privacy Note</h4>
    <p class="mb-0">
        Audio files are sent to Google Cloud for transcription. All other formats (PDF, Word, Excel, Images, Databases) are processed 100% locally.
    </p>
                    </div>

---

### 5. Conversation History

Natural multi-turn conversations:

```csharp
public class ConversationController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Message,
            maxResults: 5,
            startNewConversation: request.StartNew
        );
        
        return Ok(new {
            answer = response.Answer,
            sources = response.Sources.Count,
            timestamp = response.SearchedAt
        });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public bool StartNew { get; set; } = false;
}
```

**Conversation Flow Example:**

```
User: "What is machine learning?"
AI: "Machine learning is a subset of artificial intelligence..."

User: "Can you explain supervised learning?"  // AI remembers context
AI: "Based on our previous discussion about machine learning, supervised learning is..."

User: "What are some common algorithms?"  // Maintains conversation context
AI: "Common supervised learning algorithms include..."

User: "/new"  // Start fresh conversation
AI: "Started new conversation. How can I help you?"
```

---

## Real-World Use Cases

### 1. Medical Records Intelligence System

Unify patient data across multiple systems:

```csharp
// Connect to multiple databases
var postgresConfig = new DatabaseConfig
{
    Name = "Patient Records",
    Type = DatabaseType.PostgreSql,
    ConnectionString = "Host=localhost;Database=Hospital;...",
    IncludedTables = new List<string> { "Patients", "Admissions", "Discharges" }
};

var content1 = await _databaseService.ParseDatabaseConnectionAsync(
    postgresConfig.ConnectionString, 
    postgresConfig
);

// Upload Excel lab results
await _documentService.UploadDocumentAsync(labResultsStream, "labs.xlsx", "application/vnd.ms-excel", "lab-tech");

// Upload scanned prescriptions (OCR)
await _documentService.UploadDocumentAsync(prescriptionImage, "prescription.jpg", "image/jpeg", "doctor", language: "eng");

// Upload doctor's voice notes (Audio transcription)
await _documentService.UploadDocumentAsync(audioStream, "notes.mp3", "audio/mpeg", "doctor", language: "en-US");

// Query across all data sources
var response = await _searchService.QueryIntelligenceAsync(
    "Show me Emily Davis's complete medical history for the past year"
);

// AI combines: PostgreSQL + Excel + OCR + Audio → Complete patient timeline
```

**Power:** 4 data sources unified (PostgreSQL + Excel + OCR + Audio) → Complete patient timeline from disconnected systems.

---

### 2. Banking Credit Limit Evaluation

Comprehensive financial profile analysis:

```csharp
// Connect to 4 different databases
var sqlServerConfig = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Transactions", "BillPayments", "SalaryDeposits" }
};

var mySqlConfig = new DatabaseConfig
{
    Type = DatabaseType.MySQL,
    ConnectionString = "...",
    IncludedTables = new List<string> { "CreditCards", "Spending", "PaymentHistory" }
};

var postgresConfig = new DatabaseConfig
{
    Type = DatabaseType.PostgreSql,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Loans", "Mortgages", "CreditScores" }
};

var sqliteConfig = new DatabaseConfig
{
    Type = DatabaseType.Sqlite,
    ConnectionString = "Data Source=./branches.db",
    IncludedTables = new List<string> { "Visits", "Interactions", "Complaints" }
};

// Upload OCR scanned documents
await _documentService.UploadDocumentAsync(taxReturnImage, "tax.jpg", "image/jpeg", "rm", language: "eng");

// Upload PDF account statements
await _documentService.UploadDocumentAsync(statementPdf, "statement.pdf", "application/pdf", "rm");

// Comprehensive query
var response = await _multiDbCoordinator.ExecuteQueryAsync(
    "Should we increase John Smith's credit card limit from $8K to $18K?"
);

// AI analyzes: 36 months transactions + credit behavior + assets + visit history + OCR docs + PDFs
```

**Power:** 6 data sources coordinated (4 databases + OCR + PDF) → 360° financial intelligence for risk-free decisions.

---

### 3. Legal Precedent Discovery Engine

Find winning strategies from case history:

```csharp
// Upload 1,000+ legal PDFs
foreach (var legalDoc in legalDocuments)
{
    await _documentService.UploadDocumentAsync(
        legalDoc.Stream,
        legalDoc.FileName,
        "application/pdf",
        "legal-team"
    );
}

// Connect to case database
var caseDbConfig = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Cases", "Outcomes", "Judges", "Clients" }
};

await _databaseService.ParseDatabaseConnectionAsync(caseDbConfig.ConnectionString, caseDbConfig);

// Upload OCR scanned court orders
await _documentService.UploadDocumentAsync(courtOrderImage, "order.jpg", "image/jpeg", "clerk", language: "eng");

// Query for winning patterns
var response = await _searchService.QueryIntelligenceAsync(
    "What arguments won our contract dispute cases in the last 5 years?"
);

// AI discovers patterns from 1,000+ cases that would take weeks manually
```

**Power:** 1,000+ PDFs + SQL Server + OCR → AI discovers winning legal patterns in minutes.

---

### 4. Predictive Inventory Intelligence

Prevent stockouts with cross-database analytics:

```csharp
// Setup configuration with 4 databases
builder.Services.AddSmartRag(configuration, options =>
{
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        new() { Name = "Catalog", Type = DatabaseType.Sqlite, ConnectionString = "Data Source=./catalog.db" },
        new() { Name = "Sales", Type = DatabaseType.SqlServer, ConnectionString = "..." },
        new() { Name = "Inventory", Type = DatabaseType.MySQL, ConnectionString = "..." },
        new() { Name = "Suppliers", Type = DatabaseType.PostgreSql, ConnectionString = "..." }
    };
});

// Query across all databases
var response = await _multiDbCoordinator.ExecuteQueryAsync(
    "Which products will run out of stock in the next 2 weeks?"
);

// AI coordinates: SQLite (10K SKUs) + SQL Server (2M transactions) + 
//                  MySQL (real-time stock) + PostgreSQL (supplier lead times)
// Result: Predictive analytics preventing stockouts
```

**Power:** 4 databases coordinated → Cross-database predictive analytics impossible with single-DB queries.

---

### 5. Manufacturing Root Cause Analysis

Find production quality issues:

```csharp
// Upload Excel production reports
await _documentService.UploadDocumentAsync(
    excelStream,
    "production-report.xlsx",
    "application/vnd.ms-excel",
    "quality-manager"
);

// Connect to PostgreSQL sensor database
var sensorConfig = new DatabaseConfig
{
    Type = DatabaseType.PostgreSql,
    ConnectionString = "...",
    IncludedTables = new List<string> { "SensorReadings", "MachineStatus" },
    MaxRowsPerTable = 100000  // Large sensor data
};

await _databaseService.ParseDatabaseConnectionAsync(sensorConfig.ConnectionString, sensorConfig);

// Upload OCR quality control photos
await _documentService.UploadDocumentAsync(
    photoStream,
    "defect-photo.jpg",
    "image/jpeg",
    "inspector",
    language: "eng"
);

// Upload PDF maintenance logs
await _documentService.UploadDocumentAsync(
    maintenancePdf,
    "maintenance.pdf",
    "application/pdf",
    "technician"
);

// Root cause analysis query
var response = await _searchService.QueryIntelligenceAsync(
    "Why did we have 47 defects in last week's production batch?"
);

// AI correlates: Excel reports + PostgreSQL 100K sensor readings + OCR photos + PDF logs
```

**Power:** 4 data sources unified → AI finds temperature anomalies causing defects across millions of data points.

---

### 6. AI Resume Screening at Scale

Screen hundreds of candidates efficiently:

```csharp
// Upload 500+ resume PDFs
foreach (var resume in resumeFiles)
{
    await _documentService.UploadDocumentAsync(
        resume.Stream,
        resume.FileName,
        "application/pdf",
        "hr-team"
    );
}

// Connect to applicant database
var applicantDbConfig = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Applicants", "Skills", "Experience", "Education" }
};

await _databaseService.ParseDatabaseConnectionAsync(applicantDbConfig.ConnectionString, applicantDbConfig);

// Upload OCR scanned certificates
await _documentService.UploadDocumentAsync(
    certificateImage,
    "aws-cert.jpg",
    "image/jpeg",
    "hr-team",
    language: "eng"
);

// Upload audio interview transcripts
await _documentService.UploadDocumentAsync(
    interviewAudio,
    "interview.mp3",
    "audio/mpeg",
    "hr-team",
    language: "en-US"
);

// Find best candidates
var response = await _searchService.QueryIntelligenceAsync(
    "Find senior React developers with Python skills and AWS certifications"
);

// AI screens: 500+ PDFs + SQL Server + OCR certificates + Audio interviews
```

**Power:** 4 data sources unified → AI screens and ranks candidates in minutes vs. days.

---

### 7. Financial Audit Automation

Detect expense anomalies:

```csharp
// Upload Excel financial reports
await _documentService.UploadDocumentAsync(
    excelStream,
    "expenses-q3.xlsx",
    "application/vnd.ms-excel",
    "finance-team"
);

// Connect to transaction database
var transactionConfig = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Transactions", "Approvals", "Vendors" },
    MaxRowsPerTable = 500000
};

await _databaseService.ParseDatabaseConnectionAsync(transactionConfig.ConnectionString, transactionConfig);

// Upload OCR vendor invoices
await _documentService.UploadDocumentAsync(invoiceImage, "invoice.jpg", "image/jpeg", "accountant", language: "eng");

// Upload PDF approval workflows
await _documentService.UploadDocumentAsync(approvalPdf, "approvals.pdf", "application/pdf", "cfo");

// Audit query
var response = await _searchService.QueryIntelligenceAsync(
    "Show me all expenses over $10K in July-September with approval status"
);

// AI cross-validates: Excel (15K line items) + SQL Server (500K transactions) + OCR invoices + PDF approvals
```

**Power:** 4 data sources cross-validated → AI detects policy violations humans would miss.

---

### 8. Smart Government Services

Process citizen applications efficiently:

```csharp
// Connect to citizen database
var citizenConfig = new DatabaseConfig
{
    Type = DatabaseType.PostgreSql,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Citizens", "Applications", "Permits" },
    MaxRowsPerTable = 15000000  // Large citizen database
};

// Upload OCR application forms
await _documentService.UploadDocumentAsync(formImage, "building-permit.jpg", "image/jpeg", "clerk", language: "tur");

// Upload audio call center recordings
await _documentService.UploadDocumentAsync(callAudio, "citizen-call.wav", "audio/wav", "agent", language: "tr-TR");

// Upload PDF regulation documents
await _documentService.UploadDocumentAsync(regulationPdf, "zoning-law.pdf", "application/pdf", "legal");

// Government analytics query
var response = await _searchService.QueryIntelligenceAsync(
    "How many building permits in Istanbul, September 2024? Average processing time?"
);

// AI combines: PostgreSQL (15M records) + OCR forms + Audio calls + PDF regulations
```

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

## Testing Examples

### Unit Test Example

```csharp
using Xunit;
using Moq;

public class DocumentServiceTests
{
    [Fact]
    public async Task UploadDocumentAsync_ValidPdf_ReturnsDocument()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DocumentService>>();
        var mockParser = new Mock<IDocumentParserService>();
        var mockRepository = new Mock<IDocumentRepository>();
        
        var service = new DocumentService(mockLogger.Object, mockParser.Object, mockRepository.Object);
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
        
        // Act
        var result = await service.UploadDocumentAsync(
            stream, 
            "test.pdf", 
            "application/pdf", 
            "user-test"
        );
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.pdf", result.FileName);
    }
}
```

---

## Best Practices

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="alert alert-success">
            <h4><i class="fas fa-check-circle me-2"></i> Do's</h4>
            <ul class="mb-0">
                <li>Use dependency injection for services</li>
                <li>Handle exceptions properly</li>
                <li>Use async/await consistently</li>
                <li>Validate user input</li>
                <li>Set reasonable maxResults limits</li>
                <li>Use conversation history for natural interactions</li>
                <li>Test database connections before deployment</li>
            </ul>
        </div>
                    </div>

    <div class="col-md-6">
        <div class="alert alert-warning">
            <h4><i class="fas fa-times-circle me-2"></i> Don'ts</h4>
            <ul class="mb-0">
                <li>Don't use .Result or .Wait() on async methods</li>
                <li>Don't commit API keys to source control</li>
                <li>Don't use InMemory storage in production</li>
                <li>Don't skip error handling</li>
                <li>Don't query databases without row limits</li>
                <li>Don't upload sensitive data without sanitization</li>
                <li>Don't forget to dispose streams</li>
            </ul>
                    </div>
                </div>
            </div>

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-rocket"></i>
                    </div>
            <h3>Getting Started</h3>
            <p>Quick installation and setup guide</p>
            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Get Started
            </a>
                </div>
            </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-cog"></i>
            </div>
            <h3>Configuration</h3>
            <p>Complete configuration reference</p>
            <a href="{{ site.baseurl }}/en/configuration" class="btn btn-outline-primary btn-sm mt-3">
                Configure
            </a>
                    </div>
                </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-history"></i>
            </div>
            <h3>Changelog</h3>
            <p>Version history and updates</p>
            <a href="{{ site.baseurl }}/en/changelog" class="btn btn-outline-primary btn-sm mt-3">
                View Changelog
            </a>
            </div>
    </div>
</div>

