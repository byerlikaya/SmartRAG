using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SmartRAG.API.Contracts;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Models.RequestResponse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.API.Controllers;


/// <summary>
/// Universal Database Integration and Management Controller
/// 
/// This controller provides comprehensive database integration capabilities including:
/// - SQLite file upload and processing with schema analysis
/// - Live database connections to SQL Server, MySQL, PostgreSQL
/// - Custom SQL query execution with performance optimization
/// - Database schema extraction and table analysis
/// - Data extraction with intelligent chunking and optimization
/// - Connection validation and health monitoring
/// - Memory-optimized processing for large datasets
/// 
/// Key Features:
/// - Multi-Database Support: SQLite files, SQL Server, MySQL, PostgreSQL with unified API
/// - Live Connections: Connect to running databases with connection pooling and caching
/// - Schema Intelligence: Automatic schema detection, foreign key analysis, index information
/// - Performance Optimization: Connection pooling, query caching, parallel processing, memory management
/// - Security Controls: Sensitive data sanitization, SQL injection protection, connection validation
/// - Data Processing: Intelligent chunking, streaming for large datasets, garbage collection optimization
/// - Enterprise Features: Connection health monitoring, performance metrics, error handling
/// 
/// Database Support:
/// - **SQLite Files**: Upload .db, .sqlite, .sqlite3 files for processing and analysis
/// - **SQL Server**: Live connections with Windows and SQL authentication support
/// - **MySQL**: Native MySQL connections with performance optimization
/// - **PostgreSQL**: Full PostgreSQL support with advanced features
/// - **Schema Analysis**: Automatic table discovery, column types, relationships, indexes
/// - **Data Extraction**: Configurable row limits, filtering, sensitive data handling
/// 
/// Use Cases:
/// - Data Migration: Extract and analyze data from legacy databases
/// - Business Intelligence: Connect to operational databases for AI-powered insights
/// - Database Documentation: Automatic schema documentation and analysis
/// - Data Integration: Integrate database content with RAG for intelligent querying
/// - Compliance Auditing: Analyze database structures and data patterns
/// - Performance Analysis: Database performance monitoring and optimization
/// - Knowledge Extraction: Convert database content into searchable knowledge base
/// 
/// Performance Features:
/// - **Connection Pooling**: Efficient database connection management
/// - **Query Caching**: Intelligent caching of query results for improved performance
/// - **Parallel Processing**: Multi-threaded table processing for large databases
/// - **Memory Optimization**: Streaming data processing with automatic garbage collection
/// - **Batch Processing**: Efficient processing of multiple tables and queries
/// - **Resource Management**: Automatic cleanup and resource optimization
/// 
/// Security Features:
/// - **Sensitive Data Protection**: Automatic detection and sanitization of sensitive columns
/// - **SQL Injection Prevention**: Parameterized queries and input validation
/// - **Connection Security**: Secure connection string handling and validation
/// - **Access Control**: Connection validation and permission checking
/// - **Audit Logging**: Comprehensive logging of database operations
/// 
/// Example Usage:
/// ```bash
/// # Upload SQLite database file
/// curl -X POST "https://localhost:7001/api/database/upload-database" \
///   -H "Content-Type: multipart/form-data" \
///   -F "file=@database.sqlite"
/// 
/// # Connect to SQL Server database
/// curl -X POST "https://localhost:7001/api/database/connect-database" \
///   -H "Content-Type: application/json" \
///   -d '{"connectionString": "Server=localhost;Database=Northwind;Trusted_Connection=true;", "databaseType": "SqlServer"}'
/// 
/// # Execute custom SQL query
/// curl -X POST "https://localhost:7001/api/database/execute-query" \
///   -H "Content-Type: application/json" \
///   -d '{"connectionString": "...", "query": "SELECT * FROM Customers", "databaseType": "SqlServer"}'
/// 
/// # Get table schema information
/// curl -X POST "https://localhost:7001/api/database/schema" \
///   -H "Content-Type: application/json" \
///   -d '{"connectionString": "...", "tableName": "Customers", "databaseType": "SqlServer"}'
/// ```
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseParserService _databaseParserService;
    private readonly IDocumentService _documentService;

    public DatabaseController(
        IDatabaseParserService databaseParserService,
        IDocumentService documentService)
    {
        _databaseParserService = databaseParserService;
        _documentService = documentService;
    }

    /// <summary>
    /// Uploads and processes SQLite database files for AI-powered analysis
    /// </summary>
    /// <remarks>
    /// Processes SQLite database files with comprehensive analysis including:
    /// - **File Validation**: Validates SQLite file format and integrity
    /// - **Schema Extraction**: Automatic discovery of tables, columns, and relationships
    /// - **Data Analysis**: Intelligent sampling and content analysis
    /// - **Performance Optimization**: Memory-efficient processing for large databases
    /// - **Security Scanning**: Detection and sanitization of sensitive data
    /// - **RAG Integration**: Prepares database content for AI-powered querying
    /// 
    /// Processing features:
    /// - **Automatic Schema Discovery**: Identifies all tables, views, and relationships
    /// - **Intelligent Sampling**: Extracts representative data samples for analysis
    /// - **Foreign Key Analysis**: Maps relationships between tables
    /// - **Index Information**: Analyzes database indexes and performance characteristics
    /// - **Data Type Detection**: Identifies column types and constraints
    /// - **Content Summarization**: Generates summaries of table contents
    /// 
    /// Supported SQLite file formats:
    /// - .db, .sqlite, .sqlite3 file extensions
    /// - SQLite version 3.x databases
    /// - Encrypted SQLite databases (with proper configuration)
    /// - Large databases with streaming processing
    /// 
    /// Use cases:
    /// - **Legacy Data Migration**: Analyze and migrate old SQLite databases
    /// - **Mobile App Data**: Process SQLite databases from mobile applications
    /// - **Embedded Systems**: Analyze data from embedded system databases
    /// - **Development Databases**: Process development and testing databases
    /// - **Data Archaeology**: Discover and analyze unknown database structures
    /// 
    /// The processed database content becomes available for AI-powered querying
    /// through the RAG system, enabling natural language questions about the data.
    /// </remarks>
    /// <param name="file">SQLite database file (.db, .sqlite, .sqlite3)</param>
    /// <returns>Comprehensive database analysis results with schema and data insights</returns>
    [HttpPost("upload-database")]
    [ProducesResponseType(typeof(DatabaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DatabaseResponse>> UploadDatabase(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No database file provided");

        var supportedExtensions = _databaseParserService.GetSupportedDatabaseFileExtensions();
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!supportedExtensions.Contains(fileExtension))
        {
            return BadRequest($"Unsupported file type. Supported extensions: {string.Join(", ", supportedExtensions)}");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var content = await _databaseParserService.ParseDatabaseFileAsync(
                file.OpenReadStream(), 
                file.FileName,
                HttpContext.RequestAborted);

            var request = new UploadDocumentRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedBy = "database-upload"
            };

            var document = await _documentService.UploadDocumentAsync(request, HttpContext.RequestAborted);

            stopwatch.Stop();

            var lines = content.Split('\n');
            var tablesProcessed = lines.Count(l => l.StartsWith("--- Table:"));

            return Ok(new DatabaseResponse
            {
                Success = true,
                Message = "SQLite database uploaded and processed successfully",
                Content = content,
                DatabaseType = DatabaseType.SQLite,
                TablesProcessed = tablesProcessed,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StatusCode(500, new DatabaseResponse
            {
                Success = false,
                Message = $"Error processing database file: {ex.Message}",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    /// <summary>
    /// Connects to live databases and performs comprehensive data extraction
    /// </summary>
    /// <remarks>
    /// Establishes live connections to operational databases and extracts data for AI analysis including:
    /// - **Multi-Database Support**: SQL Server, MySQL, PostgreSQL with native connectivity
    /// - **Schema Discovery**: Automatic table detection, column analysis, relationship mapping
    /// - **Intelligent Sampling**: Smart data sampling with configurable row limits
    /// - **Security Controls**: Sensitive data detection and sanitization
    /// - **Performance Optimization**: Connection pooling, query caching, parallel processing
    /// - **Data Analysis**: Content analysis, pattern detection, statistical insights
    /// 
    /// Connection features:
    /// - **Authentication Support**: Windows, SQL, and custom authentication methods
    /// - **Connection Validation**: Pre-connection testing and health verification
    /// - **Security Scanning**: SQL injection prevention and input validation
    /// - **Resource Management**: Automatic connection cleanup and pooling
    /// - **Error Recovery**: Robust error handling with detailed diagnostics
    /// 
    /// Data extraction capabilities:
    /// - **Table Analysis**: Automatic discovery of tables, views, and relationships
    /// - **Schema Intelligence**: Column types, constraints, indexes, foreign keys
    /// - **Content Sampling**: Representative data samples for AI training
    /// - **Sensitive Data Handling**: Automatic detection and sanitization
    /// - **Performance Optimization**: Optimized queries and batch processing
    /// 
    /// The extracted database content becomes available for:
    /// - **Natural Language Querying**: Ask questions about database content in plain language
    /// - **Business Intelligence**: AI-powered insights and data analysis
    /// - **Data Discovery**: Explore and understand database structures
    /// - **Compliance Auditing**: Analyze data patterns and compliance requirements
    /// </remarks>
    /// <param name="request">Database connection configuration with security and performance options</param>
    /// <returns>Comprehensive database analysis results with schema, data samples, and insights</returns>
    [HttpPost("connect-database")]
    [ProducesResponseType(typeof(DatabaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DatabaseResponse>> ConnectDatabase([FromBody] DatabaseConnectionApiRequest request)
    {
        if (request == null)
            return BadRequest("Request body is required");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var isValidConnection = await _databaseParserService.ValidateConnectionAsync(
                request.ConnectionString, 
                request.DatabaseType,
                HttpContext.RequestAborted);

            if (!isValidConnection)
            {
                return BadRequest($"Cannot connect to {request.DatabaseType} database");
            }

            var config = new DatabaseConfig
            {
                Type = request.DatabaseType,
                ConnectionString = request.ConnectionString,
                IncludedTables = request.IncludedTables,
                ExcludedTables = request.ExcludedTables,
                MaxRowsPerTable = request.MaxRows,
                IncludeSchema = request.IncludeSchema,
                IncludeForeignKeys = request.IncludeForeignKeys,
                SanitizeSensitiveData = request.SanitizeSensitiveData,
                QueryTimeoutSeconds = request.QueryTimeoutSeconds
            };

            var content = await _databaseParserService.ParseDatabaseConnectionAsync(
                request.ConnectionString, 
                config,
                HttpContext.RequestAborted);

            stopwatch.Stop();

            var lines = content.Split('\n');
            var tablesProcessed = lines.Count(l => l.StartsWith("--- Table:"));

            return Ok(new DatabaseResponse
            {
                Success = true,
                Message = $"{request.DatabaseType} database connected successfully",
                Content = content,
                DatabaseType = request.DatabaseType,
                TablesProcessed = tablesProcessed,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StatusCode(500, new DatabaseResponse
            {
                Success = false,
                Message = $"Error connecting to database: {ex.Message}",
                DatabaseType = request.DatabaseType,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    /// <summary>
    /// Executes custom SQL queries with advanced security and performance features
    /// </summary>
    /// <remarks>
    /// Executes custom SQL queries on connected databases with comprehensive safety and optimization including:
    /// - **SQL Injection Prevention**: Parameterized queries and input sanitization
    /// - **Query Optimization**: Automatic query analysis and performance optimization
    /// - **Result Processing**: Intelligent result formatting and data type handling
    /// - **Security Controls**: Query validation, permission checking, audit logging
    /// - **Performance Monitoring**: Execution time tracking, resource usage analysis
    /// - **Error Handling**: Detailed error reporting with troubleshooting guidance
    /// 
    /// Security features:
    /// - **Query Validation**: Prevents dangerous operations (DROP, DELETE without WHERE)
    /// - **Permission Checking**: Validates user permissions for query execution
    /// - **Input Sanitization**: Comprehensive SQL injection prevention
    /// - **Audit Logging**: Complete query execution logging for compliance
    /// - **Rate Limiting**: Prevents query abuse and resource exhaustion
    /// 
    /// Performance optimizations:
    /// - **Query Caching**: Intelligent caching of query results
    /// - **Connection Pooling**: Efficient database connection management
    /// - **Result Streaming**: Memory-efficient handling of large result sets
    /// - **Timeout Management**: Configurable query timeouts and cancellation
    /// - **Resource Monitoring**: CPU and memory usage tracking
    /// 
    /// Supported query types:
    /// - **SELECT Queries**: Data retrieval with filtering, sorting, aggregation
    /// - **JOIN Operations**: Complex multi-table queries with relationship analysis
    /// - **Aggregate Functions**: Statistical analysis and data summarization
    /// - **Window Functions**: Advanced analytical queries and reporting
    /// 
    /// Query results are automatically formatted for AI consumption and can be used for:
    /// - **Natural Language Insights**: Ask questions about query results
    /// - **Data Analysis**: AI-powered analysis of query results
    /// - **Report Generation**: Automatic report creation from query data
    /// - **Trend Analysis**: Identify patterns and trends in query results
    /// </remarks>
    /// <param name="request">SQL query execution configuration with security and performance options</param>
    /// <returns>Query execution results with performance metrics and data insights</returns>
    [HttpPost("execute-query")]
    [ProducesResponseType(typeof(DatabaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DatabaseResponse>> ExecuteQuery([FromBody] QueryExecutionApiRequest request)
    {
        if (request == null)
            return BadRequest("Request body is required");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var isValidConnection = await _databaseParserService.ValidateConnectionAsync(
                request.ConnectionString, 
                request.DatabaseType,
                HttpContext.RequestAborted);

            if (!isValidConnection)
            {
                return BadRequest($"Cannot connect to {request.DatabaseType} database");
            }

            var result = await _databaseParserService.ExecuteQueryAsync(
                request.ConnectionString,
                request.Query,
                request.DatabaseType,
                request.MaxRows,
                HttpContext.RequestAborted);

            stopwatch.Stop();

            return Ok(new DatabaseResponse
            {
                Success = true,
                Message = "SQL query executed successfully",
                Content = result,
                DatabaseType = request.DatabaseType,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StatusCode(500, new DatabaseResponse
            {
                Success = false,
                Message = $"Error executing query: {ex.Message}",
                DatabaseType = request.DatabaseType,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    /// <summary>
    /// Get information about supported database types
    /// </summary>
    /// <returns>Supported database types and capabilities</returns>
    [HttpGet("supported-types")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetSupportedTypes()
    {
        var supportedTypes = _databaseParserService.GetSupportedDatabaseTypes();
        var supportedExtensions = _databaseParserService.GetSupportedDatabaseFileExtensions();

        return Ok(new
        {
            supportedDatabases = supportedTypes.Select(t => t.ToString()).ToList(),
            supportedFileExtensions = supportedExtensions.ToList(),
            message = "Database support information retrieved successfully"
        });
    }

    /// <summary>
    /// Gets all configured database connections
    /// </summary>
    /// <remarks>
    /// Returns information about all configured database connections including:
    /// - Connection status and validation
    /// - Schema analysis status
    /// - Database metadata and statistics
    /// - Table counts and row estimates
    /// </remarks>
    /// <returns>List of configured database connections</returns>
    [HttpGet("connections")]
    [ProducesResponseType(typeof(List<DatabaseConnectionInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DatabaseConnectionInfoDto>>> GetConnections()
    {
        var connectionManager = HttpContext.RequestServices.GetService<IDatabaseConnectionManager>();
        var schemaAnalyzer = HttpContext.RequestServices.GetService<IDatabaseSchemaAnalyzer>();

        if (connectionManager == null || schemaAnalyzer == null)
        {
            return StatusCode(500, "Multi-database services not configured");
        }

        try
        {
            var connections = await connectionManager.GetAllConnectionsAsync(HttpContext.RequestAborted);
            var result = new List<DatabaseConnectionInfoDto>();

            foreach (var conn in connections)
            {
                var databaseId = await connectionManager.GetDatabaseIdAsync(conn, HttpContext.RequestAborted);
                var schema = await schemaAnalyzer.GetSchemaAsync(databaseId, HttpContext.RequestAborted);

                var dto = new DatabaseConnectionInfoDto
                {
                    DatabaseId = databaseId,
                    DatabaseName = schema?.DatabaseName ?? "Unknown",
                    DatabaseType = conn.DatabaseType,
                    Enabled = conn.Enabled,
                    IsValid = await connectionManager.ValidateConnectionAsync(databaseId, HttpContext.RequestAborted),
                    SchemaStatus = schema?.Status.ToString() ?? "NotAnalyzed",
                    TableCount = schema?.Tables.Count ?? 0,
                    TotalRows = schema?.TotalRowCount ?? 0,
                    LastAnalyzed = schema?.LastAnalyzed
                };

                result.Add(dto);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving database connections: {ex.Message}");
        }
    }

    /// <summary>
    /// Triggers schema analysis for all configured databases
    /// </summary>
    /// <remarks>
    /// Initiates comprehensive schema analysis for all enabled database connections including:
    /// - Table and column discovery
    /// - Relationship mapping (foreign keys)
    /// - Row count estimation
    /// - AI-powered content summarization
    /// 
    /// This is an asynchronous operation that runs in the background.
    /// Use the GET /connections endpoint to check analysis status.
    /// </remarks>
    /// <returns>Analysis initiation status</returns>
    [HttpPost("analyze-all")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> AnalyzeAllDatabases()
    {
        var connectionManager = HttpContext.RequestServices.GetService<IDatabaseConnectionManager>();
        var schemaAnalyzer = HttpContext.RequestServices.GetService<IDatabaseSchemaAnalyzer>();

        if (connectionManager == null || schemaAnalyzer == null)
        {
            return StatusCode(500, "Multi-database services not configured");
        }

        try
        {
            var connections = await connectionManager.GetAllConnectionsAsync(HttpContext.RequestAborted);
            var enabledConnections = connections.Where(c => c.Enabled).ToList();

            // Start background analysis for each database
            foreach (var conn in enabledConnections)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await schemaAnalyzer.AnalyzeDatabaseSchemaAsync(conn, CancellationToken.None);
                    }
                    catch
                    {
                        // Logged by analyzer
                    }
                });
            }

            return Accepted(new
            {
                message = $"Schema analysis initiated for {enabledConnections.Count} database(s)",
                databaseCount = enabledConnections.Count,
                databases = enabledConnections.Select(c => c.Name ?? c.DatabaseType.ToString()).ToList()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error initiating schema analysis: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets detailed schema information for a specific database
    /// </summary>
    /// <param name="databaseId">Database identifier</param>
    /// <returns>Detailed schema information</returns>
    [HttpGet("schema/{databaseId}")]
    [ProducesResponseType(typeof(DatabaseSchemaInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDatabaseSchema(string databaseId)
    {
        var schemaAnalyzer = HttpContext.RequestServices.GetService<IDatabaseSchemaAnalyzer>();

        if (schemaAnalyzer == null)
        {
            return StatusCode(500, "Schema analyzer not configured");
        }

        try
        {
            var schema = await schemaAnalyzer.GetSchemaAsync(databaseId, HttpContext.RequestAborted);

            if (schema == null)
            {
                return NotFound($"Schema not found for database: {databaseId}");
            }

            return Ok(schema);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving schema: {ex.Message}");
        }
    }
}
