using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartRAG.API.Contracts;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.API.Controllers
{
    /// <summary>
    /// Database integration controller for connecting to and extracting data from various database types
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
        /// Upload and process SQLite database files
        /// </summary>
        /// <param name="file">SQLite database file to upload</param>
        /// <returns>Database processing results</returns>
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
                    file.FileName);

                var document = await _documentService.UploadDocumentAsync(
                    file.OpenReadStream(),
                    file.FileName,
                    file.ContentType,
                    "database-upload");

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
        /// Connect to live databases and extract data
        /// </summary>
        /// <param name="request">Database connection configuration</param>
        /// <returns>Database processing results</returns>
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
                    request.DatabaseType);

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
                    config);

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
        /// Execute custom SQL queries on databases
        /// </summary>
        /// <param name="request">SQL query execution configuration</param>
        /// <returns>Query execution results</returns>
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
                    request.DatabaseType);

                if (!isValidConnection)
                {
                    return BadRequest($"Cannot connect to {request.DatabaseType} database");
                }

                var result = await _databaseParserService.ExecuteQueryAsync(
                    request.ConnectionString,
                    request.Query,
                    request.DatabaseType,
                    request.MaxRows);

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
    }
}