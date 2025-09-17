using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartRAG.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.API.Controllers
{
    /// <summary>
    /// Document management controller with multi-format support
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentParserService _documentParser;

        public DocumentsController(
            IDocumentService documentService,
            IDocumentParserService documentParser)
        {
            _documentService = documentService;
            _documentParser = documentParser;
        }

        /// <summary>
        /// Get supported file types and content types
        /// </summary>
        /// <returns>Supported file types information</returns>
        [HttpGet("supported-types")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetSupportedTypes()
        {
            var result = new
            {
                SupportedFileTypes = _documentParser.GetSupportedFileTypes(),
                SupportedContentTypes = _documentParser.GetSupportedContentTypes(),
                MaxFileSize = 50 * 1024 * 1024,
                Description = "Supported document formats for text extraction and analysis"
            };

            return Ok(result);
        }

        /// <summary>
        /// Upload a document to the system
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="language">Language code for audio files</param>
        /// <returns>Upload result</returns>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> UploadDocument(IFormFile file, [FromQuery] string? language = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            try
            {
                var document = await _documentService.UploadDocumentAsync(
                    file.OpenReadStream(),
                    file.FileName,
                    file.ContentType,
                    "system",
                    language);

                return Ok(new
                {
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
        /// Get a document by ID
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>Document information</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetDocument(Guid id)
        {
            var document = await _documentService.GetDocumentAsync(id);

            if (document == null)
                return NotFound();

            return Ok(document);
        }

        /// <summary>
        /// Get all documents
        /// </summary>
        /// <returns>List of all documents</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllDocuments()
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            return Ok(documents);
        }

        /// <summary>
        /// Delete a document
        /// </summary>
        /// <param name="id">Document ID to delete</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteDocument(Guid id)
        {
            var success = await _documentService.DeleteDocumentAsync(id);

            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}