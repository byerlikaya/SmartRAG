---
layout: default
title: Real-World Use Cases
description: Production-ready examples from various industries
lang: en
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

## Related Examples

- [Examples Index]({{ site.baseurl }}/en/examples) - Back to Examples categories
