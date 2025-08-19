

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
    /// Delete ALL documents (use with extreme caution!)
    /// </summary>
    [HttpDelete("all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteAllDocuments()
    {
        try
        {
            var success = await documentService.DeleteAllDocumentsAsync();
            if (success)
                return Ok("All documents deleted successfully");
            else
                return StatusCode(500, "Failed to delete all documents");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting all documents: {ex.Message}");
        }
    }


}
