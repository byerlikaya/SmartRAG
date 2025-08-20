

using Microsoft.AspNetCore.Mvc;

using SmartRAG.Interfaces;

namespace SmartRAG.API.Controllers;

/// <summary>
/// Documents management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class DocumentsController(
    IDocumentService documentService,
    IDocumentParserService documentParser) : ControllerBase
{
    /// <summary>
    /// Gets supported file types and content types
    /// </summary>
    /// <returns>Supported file types and content types</returns>
    [HttpGet("supported-types")]
    public IActionResult GetSupportedTypes()
    {
        var result = new
        {
            SupportedFileTypes = documentParser.GetSupportedFileTypes(),
            SupportedContentTypes = documentParser.GetSupportedContentTypes(),
            MaxFileSize = 50 * 1024 * 1024,
            Description = "Supported document formats for text extraction and analysis"
        };

        return Ok(result);
    }

    /// <summary>
    /// Upload a document to the system
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<Entities.Document>> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        try
        {
            var document = await documentService.UploadDocumentAsync(
                file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                "system");

            // Return simple success response
            return Ok(new { 
                message = "Document uploaded successfully",
                documentId = document.Id,
                fileName = document.FileName,
                chunkCount = document.Chunks?.Count ?? 0
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Upload multiple documents to the system
    /// </summary>
    [HttpPost("upload-multiple")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    public async Task<ActionResult<List<Entities.Document>>> UploadDocuments([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files provided");

        try
        {
            var fileStreams = files.Select(f => f.OpenReadStream());
            var fileNames = files.Select(f => f.FileName);
            var contentTypes = files.Select(f => f.ContentType);

            var documents = await documentService.UploadDocumentsAsync(
                fileStreams,
                fileNames,
                contentTypes,
                "system");

            return CreatedAtAction(nameof(GetAllDocuments), documents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    /// <summary>
    /// Get a document by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Entities.Document>> GetDocument(Guid id)
    {
        var document = await documentService.GetDocumentAsync(id);

        if (document == null)
            return NotFound();

        return Ok(document);
    }

    /// <summary>
    /// Get all documents
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<Entities.Document>>> GetAllDocuments()
    {
        var documents = await documentService.GetAllDocumentsAsync();

        return Ok(documents);
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteDocument(Guid id)
    {
        var success = await documentService.DeleteDocumentAsync(id);

        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Regenerate embeddings for all existing documents
    /// </summary>
    [HttpPost("regenerate-embeddings")]
    public async Task<ActionResult> RegenerateAllEmbeddings()
    {
        try
        {
            Console.WriteLine("[API] Embedding regeneration requested");
            
            var success = await documentService.RegenerateAllEmbeddingsAsync();
            
            if (success)
            {
                return Ok(new { 
                    message = "Embedding regeneration completed successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(500, new { 
                    message = "Embedding regeneration failed",
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] Embedding regeneration failed: {ex.Message}");
            return StatusCode(500, new { 
                message = $"Internal server error: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Clear all embeddings from all documents
    /// </summary>
    [HttpPost("clear-embeddings")]
    public async Task<ActionResult> ClearAllEmbeddings()
    {
        try
        {
            Console.WriteLine("[API] Clear all embeddings requested");
            
            var success = await documentService.ClearAllEmbeddingsAsync();
            
            if (success)
            {
                return Ok(new { 
                    message = "All embeddings cleared successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(500, new { 
                    message = "Failed to clear embeddings",
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] Clear embeddings failed: {ex.Message}");
            return StatusCode(500, new { 
                message = $"Internal server error: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Clear all documents and their embeddings
    /// </summary>
    [HttpPost("clear-documents")]
    public async Task<ActionResult> ClearAllDocuments()
    {
        try
        {
            Console.WriteLine("[API] Clear all documents requested");
            
            var success = await documentService.ClearAllDocumentsAsync();
            
            if (success)
            {
                return Ok(new { 
                    message = "All documents and embeddings cleared successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(500, new { 
                    message = "Failed to clear documents",
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] Clear documents failed: {ex.Message}");
            return StatusCode(500, new { 
                message = $"Internal server error: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
