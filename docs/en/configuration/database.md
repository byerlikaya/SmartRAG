---
layout: default
title: Database Configuration
description: SmartRAG database configuration - multi-database connections, schema analysis and security settings
lang: en
---

## Database Configuration

SmartRAG can perform intelligent cross-database queries with multi-database support:

## Multi-Database Connections

<p>Configure multiple database connections to enable cross-database queries:</p>

Configure databases in `appsettings.json`:

```json
{
  "SmartRAG": {
    "DatabaseConnections": [
      {
        "Name": "Sales Database",
        "ConnectionString": "Server=localhost;Database=Sales;...",
        "DatabaseType": "SqlServer",
        "IncludedTables": ["Orders", "Customers"],
        "MaxRowsPerQuery": 1000,
        "Enabled": true
      },
      {
        "Name": "Inventory Database",
        "ConnectionString": "Server=localhost;Database=Inventory;...",
        "DatabaseType": "MySQL",
        "MaxRowsPerQuery": 1000,
        "Enabled": true
      }
    ]
  }
}
```

## DatabaseConnectionConfig Parameters

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Parameter</th>
                <th>Type</th>
                <th>Default</th>
                <th>Description</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>Name</code></td>
                <td><code>string</code></td>
                <td><code>-</code></td>
                <td>Friendly name for database connection (auto-generated if not provided)</td>
            </tr>
            <tr>
                <td><code>ConnectionString</code></td>
                <td><code>string</code></td>
                <td><code>-</code></td>
                <td>Database connection string (required)</td>
            </tr>
            <tr>
                <td><code>DatabaseType</code></td>
                <td><code>DatabaseType</code></td>
                <td><code>-</code></td>
                <td>Database type (SqlServer, MySql, PostgreSql, Sqlite) (required)</td>
            </tr>
            <tr>
                <td><code>Description</code></td>
                <td><code>string</code></td>
                <td><code>-</code></td>
                <td>Optional description to help AI understand the database content</td>
            </tr>
            <tr>
                <td><code>Enabled</code></td>
                <td><code>bool</code></td>
                <td><code>true</code></td>
                <td>Whether this connection is enabled</td>
            </tr>
            <tr>
                <td><code>MaxRowsPerQuery</code></td>
                <td><code>int</code></td>
                <td><code>0</code></td>
                <td>Maximum rows to retrieve per query (0 = use default)</td>
            </tr>
            <tr>
                <td><code>QueryTimeoutSeconds</code></td>
                <td><code>int</code></td>
                <td><code>0</code></td>
                <td>Query timeout in seconds (0 = use default)</td>
            </tr>
            <tr>
                <td><code>SchemaRefreshIntervalMinutes</code></td>
                <td><code>int</code></td>
                <td><code>0</code></td>
                <td>Auto-refresh interval in minutes (0 = no auto-refresh)</td>
            </tr>
            <tr>
                <td><code>IncludedTables</code></td>
                <td><code>string[]</code></td>
                <td><code>[]</code></td>
                <td>Specific tables to include (empty = all tables)</td>
            </tr>
            <tr>
                <td><code>ExcludedTables</code></td>
                <td><code>string[]</code></td>
                <td><code>[]</code></td>
                <td>Tables to exclude from analysis</td>
            </tr>
        </tbody>
    </table>
</div>

## Supported Databases

<p>SmartRAG supports the following database types:</p>

### SQL Server

```json
{
  "DatabaseConnections": [
    {
      "Name": "SQL Server DB",
      "Type": "SqlServer",
      "ConnectionString": "Server=localhost;Database=MyDb;User Id=user;Password=pass;TrustServerCertificate=true;"
    }
  ]
}
```

### MySQL

```json
{
  "DatabaseConnections": [
    {
      "Name": "MySQL DB",
      "Type": "MySql",
      "ConnectionString": "Server=localhost;Database=MyDb;Uid=user;Pwd=pass;"
    }
  ]
}
```

### PostgreSQL

```json
{
  "DatabaseConnections": [
    {
      "Name": "PostgreSQL DB",
      "Type": "PostgreSql",
      "ConnectionString": "Host=localhost;Database=MyDb;Username=postgres;Password=password;"
    }
  ]
}
```

### SQLite

```json
{
  "DatabaseConnections": [
    {
      "Name": "SQLite DB",
      "Type": "Sqlite",
      "ConnectionString": "Data Source=./mydb.db;"
    }
  ]
}
```

## Security and Sensitive Data Sanitization

SmartRAG automatically detects and sanitizes sensitive data types:

**Automatically Sanitized Sensitive Data Types:**
- `password`, `pwd`, `pass`
- `ssn`, `social_security`
- `credit_card`, `creditcard`, `cc_number`
- `email`, `mail`
- `phone`, `telephone`
- `salary`, `compensation`

```csharp
// Disable sensitive data sanitization
new DatabaseConnectionConfig
{
    Name = "Secure Database",
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=SecureDB;...",
    SanitizeSensitiveData = false  // Use with caution!
}
```

## Schema Analysis and Refresh

### SmartRAG Options for Schema Management

These global options control schema analysis behavior for all databases:

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Automatically analyze database schemas on startup
    options.EnableAutoSchemaAnalysis = true;
});
```

**Configuration in appsettings.json:**

```json
{
  "SmartRAG": {
    "EnableAutoSchemaAnalysis": true
  }
}
```

### Manual Schema Refresh

```csharp
// Custom refresh interval for specific database
new DatabaseConnectionConfig
{
    Name = "Frequently Changing Database",
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=DynamicDB;...",
    SchemaRefreshIntervalMinutes = 15  // Refresh every 15 minutes
}
```

## Performance Optimization

### Table Filtering

```csharp
// Include only specific tables
new DatabaseConnectionConfig
{
    Name = "Main Tables Only",
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=LargeDB;...",
    IncludedTables = new List<string> { "Users", "Orders", "Products" },
    ExcludedTables = new List<string> { "Logs", "TempData", "Cache" }
}
```

### Row Limits

```csharp
// Row limit for large tables
new DatabaseConnectionConfig
{
    Name = "Large Table",
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=BigDB;...",
    MaxRowsPerTable = 5000  // Maximum 5000 rows per table
}
```

## Error Handling

```csharp
// Handle connection errors
try
{
    var result = await _multiDatabaseQueryCoordinator.QueryMultipleDatabasesAsync(query);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Database connection error");
    // Implement fallback strategy
}
```

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>Audio & OCR</h3>
            <p>Whisper.net and Tesseract OCR configuration</p>
            <a href="{{ site.baseurl }}/en/configuration/audio-ocr" class="btn btn-outline-primary btn-sm mt-3">
                Audio & OCR
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cogs"></i>
            </div>
            <h3>Advanced Configuration</h3>
            <p>Fallback providers and best practices</p>
            <a href="{{ site.baseurl }}/en/configuration/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Advanced Configuration
            </a>
        </div>
    </div>
</div>
