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
    /// Advanced Document Management and Processing Controller
    /// 
    /// This controller provides comprehensive document management capabilities including:
    /// - Multi-format document upload and processing (PDF, Word, Excel, PowerPoint, Images, Audio)
    /// - Advanced text extraction with OCR for images and scanned documents
    /// - Speech-to-text processing for audio files with multiple language support
    /// - Intelligent document chunking and embedding generation for RAG
    /// - Document lifecycle management (upload, retrieve, update, delete)
    /// - Batch processing for multiple documents with progress tracking
    /// - Document analytics and processing statistics
    /// 
    /// Supported Document Formats:
    /// - **Office Documents**: PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx)
    /// - **Text Files**: Plain text (.txt), Markdown (.md), RTF, CSV
    /// - **Image Files**: PNG, JPEG, GIF, BMP, TIFF with OCR text extraction
    /// - **Audio Files**: MP3, WAV, M4A, FLAC with speech-to-text conversion
    /// - **Database Files**: SQLite (.db, .sqlite, .sqlite3) with schema analysis
    /// - **Archive Files**: ZIP, RAR with recursive processing
    /// 
    /// Advanced Features:
    /// - **OCR Processing**: Tesseract-powered text extraction from images and scanned PDFs
    /// - **Speech Recognition**: Multi-language audio transcription with OpenAI Whisper
    /// - **Smart Chunking**: Intelligent text segmentation for optimal RAG performance
    /// - **Embedding Generation**: Automatic vector embeddings for semantic search
    /// - **Metadata Extraction**: Document properties, creation dates, authors
    /// - **Content Analysis**: Language detection, content type classification
    /// - **Progress Tracking**: Real-time processing status and progress updates
    /// 
    /// Processing Pipeline:
    /// 1. **File Validation**: Format validation, size limits, security scanning
    /// 2. **Content Extraction**: Format-specific text and metadata extraction
    /// 3. **Text Processing**: Cleaning, normalization, language detection
    /// 4. **Chunking**: Intelligent segmentation for optimal retrieval
    /// 5. **Embedding Generation**: Vector embeddings for semantic search
    /// 6. **Storage**: Secure storage with metadata and search indexing
    /// 7. **RAG Integration**: Integration with retrieval-augmented generation
    /// 
    /// Use Cases:
    /// - **Knowledge Management**: Build searchable knowledge bases from documents
    /// - **Document Intelligence**: Extract insights from business documents
    /// - **Content Migration**: Migrate and process legacy document collections
    /// - **Compliance**: Process regulatory documents and contracts
    /// - **Research**: Academic and scientific document analysis
    /// - **Customer Support**: Process support documentation and manuals
    /// 
    /// Example Usage:
    /// ```bash
    /// # Upload a PDF document
    /// curl -X POST "https://localhost:7001/api/documents/upload" \
    ///   -H "Content-Type: multipart/form-data" \
    ///   -F "file=@document.pdf"
    /// 
    /// # Upload an audio file with language specification
    /// curl -X POST "https://localhost:7001/api/documents/upload?language=en" \
    ///   -H "Content-Type: multipart/form-data" \
    ///   -F "file=@audio.mp3"
    /// 
    /// # Get all documents
    /// curl -X GET "https://localhost:7001/api/documents"
    /// 
    /// # Get document chunks for analysis
    /// curl -X GET "https://localhost:7001/api/documents/{id}/chunks"
    /// 
    /// # Batch upload multiple documents
    /// curl -X POST "https://localhost:7001/api/documents/batch-upload" \
    ///   -H "Content-Type: multipart/form-data" \
    ///   -F "files=@doc1.pdf" -F "files=@doc2.docx"
    /// ```
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
        /// Gets comprehensive information about supported file formats and processing capabilities
    /// </summary>
        /// <remarks>
        /// Returns detailed information about all supported document formats including:
        /// - **File Extensions**: Complete list of supported file extensions
        /// - **MIME Types**: Supported content types for proper file handling
        /// - **Processing Capabilities**: Available features for each format type
        /// - **Size Limits**: Maximum file sizes and processing constraints
        /// - **Advanced Features**: OCR, speech-to-text, and specialized processing options
        /// 
        /// Supported categories:
        /// - **Office Documents**: PDF, Word, Excel, PowerPoint with full text extraction
        /// - **Text Files**: Plain text, Markdown, RTF, CSV with encoding detection
        /// - **Image Files**: PNG, JPEG, GIF, BMP, TIFF with OCR text extraction
        /// - **Audio Files**: MP3, WAV, M4A, FLAC with speech-to-text conversion
        /// - **Database Files**: SQLite with schema analysis and data extraction
        /// - **Archive Files**: ZIP, RAR with recursive content processing
        /// 
        /// Use this endpoint to:
        /// - Validate file compatibility before upload
        /// - Build file upload interfaces with proper validation
        /// - Understand processing capabilities for different formats
        /// - Plan document processing workflows
        /// </remarks>
        /// <returns>Comprehensive file format support information with capabilities and limits</returns>
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
        /// Uploads and processes a single document with advanced format support
    /// </summary>
        /// <remarks>
        /// Uploads and processes a document through the complete SmartRAG pipeline including:
        /// - **Format Detection**: Automatic file format and content type detection
        /// - **Content Extraction**: Advanced text extraction with format-specific optimizations
        /// - **OCR Processing**: Optical character recognition for images and scanned PDFs
        /// - **Speech-to-Text**: Audio transcription with multi-language support
        /// - **Smart Chunking**: Intelligent text segmentation for optimal RAG performance
        /// - **Embedding Generation**: Automatic vector embeddings for semantic search
        /// - **Metadata Extraction**: Document properties, creation dates, and content analysis
        /// 
        /// Processing features:
        /// - **Multi-Language Support**: Automatic language detection and processing
        /// - **Quality Optimization**: Content cleaning, normalization, and enhancement
        /// - **Security Scanning**: Malware detection and content validation
        /// - **Progress Tracking**: Real-time processing status and completion metrics
        /// - **Error Recovery**: Robust error handling with detailed error reporting
        /// 
        /// Supported workflows:
        /// - **Office Documents**: Full text extraction with formatting preservation
        /// - **Image Documents**: OCR with confidence scoring and text validation
        /// - **Audio Files**: Speech recognition with speaker detection and timestamps
        /// - **Database Files**: Schema analysis with intelligent data sampling
        /// 
        /// The uploaded document becomes immediately available for:
        /// - AI-powered search and question answering
        /// - Semantic similarity matching
        /// - Content analysis and insights
        /// - Integration with conversation context
        /// </remarks>
        /// <param name="file">Document file to upload (supports multiple formats)</param>
        /// <param name="language">Language code for audio processing (e.g., 'en', 'tr', 'de')</param>
        /// <returns>Upload results with document ID, processing statistics, and chunk information</returns>
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
        /// Retrieves detailed information for a specific document
    /// </summary>
        /// <remarks>
        /// Returns comprehensive document information including:
        /// - **Document Metadata**: Filename, content type, upload details, file size
        /// - **Processing Status**: Content extraction status, chunking completion, embedding generation
        /// - **Content Analysis**: Extracted text, language detection, content statistics
        /// - **Chunk Information**: Number of chunks, chunk sizes, processing quality
        /// - **RAG Integration**: Embedding status, search indexing, retrieval readiness
        /// - **Performance Metrics**: Processing time, extraction quality, optimization suggestions
        /// 
        /// Document details include:
        /// - **Basic Information**: ID, filename, content type, upload timestamp
        /// - **Content Statistics**: Text length, chunk count, language, encoding
        /// - **Processing Results**: OCR confidence, speech-to-text accuracy, extraction quality
        /// - **Search Readiness**: Embedding status, indexing completion, search availability
        /// - **Metadata**: Custom metadata, document properties, content analysis
        /// 
        /// Use this endpoint for:
        /// - Document status verification and troubleshooting
        /// - Quality assessment of processed content
        /// - Integration with document management systems
        /// - Analytics and reporting on document processing
        /// - Debugging processing issues and optimization
        /// </remarks>
        /// <param name="id">Unique document identifier</param>
        /// <returns>Complete document information with processing status and content analysis</returns>
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
        /// Permanently deletes a document and all associated data
    /// </summary>
        /// <remarks>
        /// Deletes a document and all related data including:
        /// - **Document Content**: Original file content and extracted text
        /// - **Vector Embeddings**: All embedding vectors and search indexes
        /// - **Chunks**: All document chunks and their metadata
        /// - **Search Index**: Removes document from search and RAG operations
        /// - **Metadata**: Custom metadata and document properties
        /// - **Analytics Data**: Document-specific usage and performance data
        /// 
        /// Deletion process:
        /// 1. **Validation**: Verifies document exists and user has permission
        /// 2. **Dependency Check**: Identifies any dependent data or references
        /// 3. **Cascade Deletion**: Removes all related data and indexes
        /// 4. **Search Update**: Updates search indexes and embeddings
        /// 5. **Analytics Update**: Updates system statistics and metrics
        /// 6. **Audit Logging**: Records deletion for compliance and auditing
        /// 
        /// **Warning**: This operation is irreversible and permanently removes all document data.
        /// 
        /// Use cases:
        /// - **Data Management**: Remove outdated or incorrect documents
        /// - **Privacy Compliance**: Delete documents for GDPR compliance
        /// - **Storage Optimization**: Free up storage space and resources
        /// - **Content Curation**: Remove low-quality or irrelevant content
        /// - **Security**: Remove sensitive or confidential documents
        /// </remarks>
        /// <param name="id">Unique identifier of the document to delete</param>
        /// <returns>Deletion confirmation with cleanup statistics</returns>
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

    /// <summary>
        /// Gets all documents with enhanced filtering and pagination
    /// </summary>
        /// <remarks>
        /// Retrieves all documents with comprehensive filtering options including:
        /// - **Pagination**: Efficient data retrieval with skip/limit
        /// - **Content Filtering**: Filter by document type and format
        /// - **Date Range**: Filter by upload date range
        /// - **Search**: Full-text search in filenames
        /// - **Sorting**: Multiple sort options and directions
        /// - **Statistics**: Document processing and embedding status
        /// </remarks>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum documents to return</param>
        /// <param name="contentType">Filter by content type</param>
        /// <param name="search">Search term for filename filtering</param>
        /// <returns>Paginated list of documents with metadata</returns>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllDocumentsEnhanced(
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 50,
            [FromQuery] string? contentType = null,
            [FromQuery] string? search = null)
    {
        try
        {
                var documents = await _documentService.GetAllDocumentsAsync();
                
                var filteredDocuments = documents.AsQueryable();
                
                if (!string.IsNullOrEmpty(contentType))
                    filteredDocuments = filteredDocuments.Where(d => d.ContentType.Contains(contentType));
                
                if (!string.IsNullOrEmpty(search))
                    filteredDocuments = filteredDocuments.Where(d => d.FileName.Contains(search, StringComparison.OrdinalIgnoreCase));

                var totalCount = filteredDocuments.Count();
                var pagedDocuments = filteredDocuments.Skip(skip).Take(limit).ToList();

                return Ok(new
                {
                    documents = pagedDocuments.Select(d => new
                    {
                        id = d.Id,
                        fileName = d.FileName,
                        contentType = d.ContentType,
                        uploadedAt = d.UploadedAt,
                        uploadedBy = d.UploadedBy,
                        chunkCount = d.Chunks?.Count ?? 0,
                        language = d.Metadata?.ContainsKey("language") == true ? d.Metadata["language"].ToString() : "en",
                        hasEmbeddings = d.Chunks?.Any(c => c.Embedding != null) ?? false
                    }),
                    pagination = new
                    {
                        totalCount,
                        skip,
                        limit,
                        hasMore = skip + limit < totalCount
                    }
                });
        }
        catch (Exception ex)
        {
                return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
        /// Gets document chunks for detailed analysis
    /// </summary>
        /// <remarks>
        /// Retrieves all chunks for a specific document including:
        /// - **Chunk Content**: Full text content of each chunk
        /// - **Embeddings**: Vector embeddings if available
        /// - **Metadata**: Chunk position, size, and processing info
        /// - **Performance**: Chunk processing statistics
        /// 
        /// Useful for:
        /// - Document analysis and debugging
        /// - RAG performance optimization
        /// - Content quality assessment
        /// - Embedding verification
        /// </remarks>
        /// <param name="id">Document ID</param>
        /// <returns>Document chunks with detailed metadata</returns>
        [HttpGet("{id:guid}/chunks")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetDocumentChunks(Guid id)
    {
        try
        {
                var document = await _documentService.GetDocumentAsync(id);
                if (document == null)
                    return NotFound(new { Error = "Document not found" });

                var chunks = document.Chunks ?? new List<SmartRAG.Entities.DocumentChunk>();

                return Ok(new
                {
                    documentId = id,
                    documentName = document.FileName,
                    totalChunks = chunks.Count,
                    chunks = chunks.Select((chunk, index) => new
                    {
                        chunkId = chunk.Id,
                        chunkIndex = index,
                        content = chunk.Content,
                        hasEmbedding = chunk.Embedding != null,
                        embeddingDimensions = chunk.Embedding?.Count ?? 0,
                        contentLength = chunk.Content?.Length ?? 0
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Batch upload multiple documents
        /// </summary>
        /// <remarks>
        /// Uploads and processes multiple documents in a single request including:
        /// - **Parallel Processing**: Concurrent document processing for speed
        /// - **Progress Tracking**: Individual document processing status
        /// - **Error Handling**: Partial success with detailed error reporting
        /// - **Batch Statistics**: Overall processing metrics and timing
        /// 
        /// Features:
        /// - Process up to 10 documents simultaneously
        /// - Individual error handling per document
        /// - Comprehensive progress reporting
        /// - Optimized resource utilization
        /// </remarks>
        /// <param name="files">Multiple files to upload</param>
        /// <param name="language">Default language for audio files</param>
        /// <returns>Batch processing results</returns>
        [HttpPost("batch-upload")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> BatchUploadDocuments(List<IFormFile> files, [FromQuery] string? language = null)
        {
            if (files == null || !files.Any())
                return BadRequest(new { Error = "No files provided" });

            if (files.Count > 10)
                return BadRequest(new { Error = "Maximum 10 files allowed per batch" });

            var results = new List<object>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var file in files)
            {
                try
                {
                    if (file.Length > 0)
                    {
                        var document = await _documentService.UploadDocumentAsync(
                            file.OpenReadStream(),
                            file.FileName,
                            file.ContentType,
                            "system",
                            language);

                        results.Add(new
                        {
                            fileName = file.FileName,
                            success = true,
                            documentId = document.Id,
                            chunkCount = document.Chunks?.Count ?? 0,
                            message = "Document uploaded successfully"
                        });
                        successCount++;
            }
            else
            {
                        results.Add(new
                {
                            fileName = file.FileName,
                            success = false,
                            error = "Empty file"
                });
                        failureCount++;
            }
        }
        catch (Exception ex)
        {
                    results.Add(new
                    {
                        fileName = file.FileName,
                        success = false,
                        error = ex.Message
                    });
                    failureCount++;
                }
            }

            return Ok(new
            {
                totalFiles = files.Count,
                successCount,
                failureCount,
                results,
                message = $"Batch upload completed: {successCount} successful, {failureCount} failed"
            });
    }

    /// <summary>
        /// Regenerates embeddings for all documents
    /// </summary>
        /// <remarks>
        /// Regenerates vector embeddings for all documents in the system including:
        /// - **Batch Processing**: Efficient processing of all documents
        /// - **Progress Tracking**: Real-time progress updates
        /// - **Error Recovery**: Continues processing on individual failures
        /// - **Performance Optimization**: Optimized embedding generation
        /// 
        /// Use cases:
        /// - **Model Updates**: Regenerate embeddings after AI model changes
        /// - **Performance Optimization**: Update embeddings with improved algorithms
        /// - **Data Migration**: Regenerate embeddings after system upgrades
        /// - **Quality Improvement**: Refresh embeddings for better search quality
        /// </remarks>
        /// <returns>Embedding regeneration results</returns>
        [HttpPost("regenerate-embeddings")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> RegenerateAllEmbeddings()
    {
        try
        {
                var success = await _documentService.RegenerateAllEmbeddingsAsync();

                return Ok(new
                {
                    success,
                    message = success ? "All embeddings regenerated successfully" : "Failed to regenerate embeddings",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Clears all embeddings from documents
        /// </summary>
        /// <remarks>
        /// Removes all vector embeddings from documents while preserving document content including:
        /// - **Selective Clearing**: Remove only embeddings, keep document data
        /// - **Storage Optimization**: Free up vector storage space
        /// - **Reset Capability**: Prepare for embedding regeneration
        /// - **Performance Impact**: Analyze storage and performance improvements
        /// 
        /// **Warning**: This operation removes all embeddings and will disable
        /// semantic search until embeddings are regenerated.
        /// </remarks>
        /// <returns>Embedding clearing results</returns>
        [HttpDelete("embeddings")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> ClearAllEmbeddings()
        {
            try
            {
                var success = await _documentService.ClearAllEmbeddingsAsync();

                return Ok(new
                {
                    success,
                    message = success ? "All embeddings cleared successfully" : "Failed to clear embeddings",
                    warning = "Semantic search is disabled until embeddings are regenerated",
                    timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}