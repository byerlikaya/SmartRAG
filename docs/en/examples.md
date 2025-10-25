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

Configure databases in `appsettings.json`, then query them:

**Configuration (appsettings.json):**
```json
{
  "SmartRAG": {
    "DatabaseConnections": [
      {
        "Name": "Sales",
        "ConnectionString": "Server=localhost;Database=Sales;...",
        "DatabaseType": "SqlServer"
      },
      {
        "Name": "Inventory",
        "ConnectionString": "Server=localhost;Database=Inventory;...",
        "DatabaseType": "MySQL"
      }
    ]
  }
}
```

**Query Controller:**
```csharp
public class DatabaseController : ControllerBase
{
    private readonly IMultiDatabaseQueryCoordinator _multiDbCoordinator;
    
    public DatabaseController(IMultiDatabaseQueryCoordinator multiDbCoordinator)
    {
        _multiDbCoordinator = multiDbCoordinator;
    }
    
    // Query across multiple databases
    [HttpPost("query")]
    public async Task<IActionResult> QueryDatabases([FromBody] MultiDbQueryRequest request)
    {
        var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(
            request.Query,
            maxResults: request.MaxResults
        );
        
        return Ok(response);
    }
}

public class MultiDbQueryRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
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

Transcribe audio files with Whisper.net:

```csharp
public class AudioController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Upload audio for transcription
    [HttpPost("upload/audio")]
    public async Task<IActionResult> UploadAudio(IFormFile file, [FromQuery] string language = "en")
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123",
            language: language  // Speech language: en, tr, auto, etc.
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
- `en` - English
- `tr` - Turkish
- `de` - German
- `fr` - French
- `auto` - Automatic detection (recommended)
- 100+ languages supported

<div class="alert alert-success">
    <h4><i class="fas fa-lock me-2"></i> Privacy Note</h4>
    <p class="mb-0">
        All processing is done 100% locally. Audio transcription uses Whisper.net, OCR uses Tesseract. No data is sent to external services.
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

**Configuration (appsettings.json):**
```json
{
  "SmartRAG": {
    "DatabaseConnections": [
      {
        "Name": "Patient Records",
        "ConnectionString": "Host=localhost;Database=Hospital;...",
        "DatabaseType": "PostgreSQL",
        "IncludedTables": ["Patients", "Admissions", "Discharges"]
      }
    ]
  }
}
```

**Code:**
```csharp
// Upload Excel lab results
await _documentService.UploadDocumentAsync(labResultsStream, "labs.xlsx", "application/vnd.ms-excel", "lab-tech");

// Upload scanned prescriptions (OCR)
await _documentService.UploadDocumentAsync(prescriptionImage, "prescription.jpg", "image/jpeg", "doctor", language: "eng");

// Upload doctor's voice notes (Audio transcription)
await _documentService.UploadDocumentAsync(audioStream, "notes.mp3", "audio/mpeg", "doctor", language: "en");

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

**Configuration (appsettings.json):**
```json
{
  "SmartRAG": {
    "DatabaseConnections": [
      {"Name": "Transactions", "ConnectionString": "...", "DatabaseType": "SqlServer", "IncludedTables": ["Transactions", "BillPayments", "SalaryDeposits"]},
      {"Name": "Credit", "ConnectionString": "...", "DatabaseType": "MySQL", "IncludedTables": ["CreditCards", "Spending", "PaymentHistory"]},
      {"Name": "Loans", "ConnectionString": "...", "DatabaseType": "PostgreSQL", "IncludedTables": ["Loans", "Mortgages", "CreditScores"]},
      {"Name": "Branches", "ConnectionString": "Data Source=./branches.db", "DatabaseType": "Sqlite", "IncludedTables": ["Visits", "Interactions", "Complaints"]}
    ]
  }
}
```

**Code:**
```csharp
// Upload OCR scanned documents
await _documentService.UploadDocumentAsync(taxReturnImage, "tax.jpg", "image/jpeg", "rm", language: "eng");

// Upload PDF account statements
await _documentService.UploadDocumentAsync(statementPdf, "statement.pdf", "application/pdf", "rm");

// Comprehensive query
var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(
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

// Database configured in appsettings.json:
// {"Name": "Cases", "ConnectionString": "...", "DatabaseType": "SqlServer", "IncludedTables": ["Cases", "Outcomes", "Judges", "Clients"]}

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
// Databases configured in appsettings.json (4 databases: Catalog, Sales, Inventory, Suppliers)

// Query across all databases
var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(
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

// Database configured in appsettings.json:
// {"Name": "Sensors", "ConnectionString": "...", "DatabaseType": "PostgreSQL", "IncludedTables": ["SensorReadings", "MachineStatus"], "MaxRowsPerQuery": 100000}

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

// Database configured in appsettings.json:
// {"Name": "Applicants", "ConnectionString": "...", "DatabaseType": "SqlServer", "IncludedTables": ["Applicants", "Skills", "Experience", "Education"]}

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
    language: "en"
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

// Database configured in appsettings.json:
// {"Name": "Transactions", "ConnectionString": "...", "DatabaseType": "SqlServer", "IncludedTables": ["Transactions", "Approvals", "Vendors"], "MaxRowsPerQuery": 500000}

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
// Database configured in appsettings.json:
// {"Name": "Citizens", "ConnectionString": "...", "DatabaseType": "PostgreSQL", "IncludedTables": ["Citizens", "Applications", "Permits"], "MaxRowsPerQuery": 15000000}

// Upload OCR application forms
await _documentService.UploadDocumentAsync(formImage, "building-permit.jpg", "image/jpeg", "clerk", language: "tur");

// Upload audio call center recordings
await _documentService.UploadDocumentAsync(callAudio, "citizen-call.wav", "audio/wav", "agent", language: "tr");

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

