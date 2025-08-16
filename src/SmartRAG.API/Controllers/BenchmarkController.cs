using Microsoft.AspNetCore.Mvc;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Diagnostics;
using System.Text;

namespace SmartRAG.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BenchmarkController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<BenchmarkController> _logger;

        public BenchmarkController(IDocumentService documentService, ILogger<BenchmarkController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpPost("performance-test")]
        public async Task<IActionResult> RunPerformanceTest([FromBody] BenchmarkRequest request)
        {
            var results = new BenchmarkResults();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Test 1: Document Upload Performance
                if (request.TestDocumentUpload)
                {
                    var uploadResult = await TestDocumentUpload(request.DocumentSizeKB);
                    results.DocumentUpload = uploadResult;
                }

                // Test 2: Search Performance
                if (request.TestSearch)
                {
                    var searchResult = await TestSearchPerformance(request.SearchQuery, request.MaxResults);
                    results.Search = searchResult;
                }

                // Test 3: AI Response Generation
                if (request.TestAIResponse)
                {
                    var aiResult = await TestAIResponseGeneration(request.SearchQuery, request.MaxResults);
                    results.AIResponse = aiResult;
                }

                // Test 4: End-to-End RAG Performance
                if (request.TestEndToEnd)
                {
                    var endToEndResult = await TestEndToEndRAG(request.SearchQuery, request.MaxResults);
                    results.EndToEnd = endToEndResult;
                }

                stopwatch.Stop();
                results.TotalExecutionTime = stopwatch.ElapsedMilliseconds;
                results.Timestamp = DateTime.UtcNow;

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Benchmark test failed");
                return StatusCode(500, new { error = "Benchmark test failed", details = ex.Message });
            }
        }

        private async Task<BenchmarkMetric> TestDocumentUpload(int documentSizeKB)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Generate test document content
            var testContent = GenerateTestContent(documentSizeKB);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            
            try
            {
                var document = await _documentService.UploadDocumentAsync(
                    stream,
                    $"benchmark-test-{documentSizeKB}kb.txt",
                    "text/plain",
                    "benchmark-user"
                );

                stopwatch.Stop();
                return new BenchmarkMetric
                {
                    Operation = "Document Upload",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    DocumentSizeKB = documentSizeKB,
                    ChunksCreated = document.Chunks?.Count ?? 0,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkMetric
                {
                    Operation = "Document Upload",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    DocumentSizeKB = documentSizeKB,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<BenchmarkMetric> TestSearchPerformance(string query, int maxResults)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var response = await _documentService.GenerateRagAnswerAsync(query, maxResults);
                
                stopwatch.Stop();
                return new BenchmarkMetric
                {
                    Operation = "Search Performance",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryLength = query.Length,
                    MaxResults = maxResults,
                    SourcesFound = response.Sources?.Count ?? 0,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkMetric
                {
                    Operation = "Search Performance",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryLength = query.Length,
                    MaxResults = maxResults,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<BenchmarkMetric> TestAIResponseGeneration(string query, int maxResults)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var response = await _documentService.GenerateRagAnswerAsync(query, maxResults);
                
                stopwatch.Stop();
                return new BenchmarkMetric
                {
                    Operation = "AI Response Generation",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryLength = query.Length,
                    ResponseLength = response.Answer?.Length ?? 0,
                    MaxResults = maxResults,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkMetric
                {
                    Operation = "AI Response Generation",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryLength = query.Length,
                    MaxResults = maxResults,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<BenchmarkMetric> TestEndToEndRAG(string query, int maxResults)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var response = await _documentService.GenerateRagAnswerAsync(query, maxResults);
                
                stopwatch.Stop();
                return new BenchmarkMetric
                {
                    Operation = "End-to-End RAG",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryLength = query.Length,
                    ResponseLength = response.Answer?.Length ?? 0,
                    MaxResults = maxResults,
                    SourcesFound = response.Sources?.Count ?? 0,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkMetric
                {
                    Operation = "End-to-End RAG",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryLength = query.Length,
                    MaxResults = maxResults,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private string GenerateTestContent(int sizeKB)
        {
            var content = new StringBuilder();
            var words = new[] { "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua" };
            var random = new Random();
            
            var targetSize = sizeKB * 1024; // Convert KB to bytes
            var currentSize = 0;
            
            while (currentSize < targetSize)
            {
                var word = words[random.Next(words.Length)];
                content.Append(word).Append(" ");
                currentSize += word.Length + 1; // +1 for space
                
                // Add some variety
                if (random.Next(10) == 0)
                {
                    content.Append(". ");
                    currentSize += 2;
                }
            }
            
            return content.ToString();
        }

        [HttpGet("system-info")]
        public IActionResult GetSystemInfo()
        {
            var info = new
            {
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Framework = Environment.Version.ToString(),
                OS = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet / (1024 * 1024) + " MB",
                Timestamp = DateTime.UtcNow
            };
            
            return Ok(info);
        }
    }

    public class BenchmarkRequest
    {
        public bool TestDocumentUpload { get; set; } = true;
        public bool TestSearch { get; set; } = true;
        public bool TestAIResponse { get; set; } = true;
        public bool TestEndToEnd { get; set; } = true;
        public int DocumentSizeKB { get; set; } = 100; // 100KB test document
        public string SearchQuery { get; set; } = "What are the main topics discussed in the documents?";
        public int MaxResults { get; set; } = 5;
    }

    public class BenchmarkResults
    {
        public BenchmarkMetric? DocumentUpload { get; set; }
        public BenchmarkMetric? Search { get; set; }
        public BenchmarkMetric? AIResponse { get; set; }
        public BenchmarkMetric? EndToEnd { get; set; }
        public long TotalExecutionTime { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BenchmarkMetric
    {
        public string Operation { get; set; } = string.Empty;
        public long ExecutionTimeMs { get; set; }
        public int? DocumentSizeKB { get; set; }
        public int? QueryLength { get; set; }
        public int? ResponseLength { get; set; }
        public int? MaxResults { get; set; }
        public int? ChunksCreated { get; set; }
        public int? SourcesFound { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
