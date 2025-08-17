

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
public class DocumentsController(IDocumentService documentService, IDocumentParserService documentParser) : ControllerBase
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

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
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
}
