using Microsoft.Extensions.Logging;
using SmartRAG.Demo.Services.Console;
using SmartRAG.Interfaces;

namespace SmartRAG.Demo.Handlers.DocumentHandlers;

/// <summary>
/// Handler for document-related operations
/// </summary>
public class DocumentHandler(
    ILogger<DocumentHandler> logger,
    IConsoleService console,
    IDocumentService documentService) : IDocumentHandler
{
    #region Fields

    private readonly ILogger<DocumentHandler> _logger = logger;
    private readonly IConsoleService _console = console;
    private readonly IDocumentService _documentService = documentService;

    #endregion

    #region Public Methods

    public async Task UploadDocumentsAsync(string language)
    {
        _console.WriteSectionHeader("📄 Upload Documents");

        System.Console.WriteLine("Supported file types:");
        System.Console.WriteLine("  • PDF documents");
        System.Console.WriteLine("  • Word documents (.docx)");
        System.Console.WriteLine("  • Excel spreadsheets (.xlsx)");
        System.Console.WriteLine("  • Images (.jpg, .png, .bmp - OCR)");
        System.Console.WriteLine("  • Text files (.txt)");
        System.Console.WriteLine("  • Audio files (.mp3, .wav, .m4a)");
        System.Console.WriteLine();

        var filePath = _console.ReadLine("Enter file path (or drag & drop file here): ")?.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            _console.WriteError("File not found!");
            return;
        }

        try
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"⏳ Processing file: {Path.GetFileName(filePath)}");
            System.Console.WriteLine();

            using var fileStream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            var contentType = GetContentType(filePath);

            var languageToUse = language;
            if (IsAudioFile(fileName))
            {
                languageToUse = await SelectAudioLanguageAsync();
            }

            var document = await _documentService.UploadDocumentAsync(
                fileStream,
                fileName,
                contentType,
                "Demo",
                languageToUse);

            _console.WriteSuccess("Document uploaded successfully!");
            System.Console.WriteLine($"  ID: {document.Id}");
            System.Console.WriteLine($"  Name: {document.FileName}");
            System.Console.WriteLine($"  Size: {document.Content.Length:N0} bytes");
            System.Console.WriteLine($"  Chunks: {document.Chunks?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    public async Task ListDocumentsAsync()
    {
        _console.WriteSectionHeader("📚 Uploaded Documents");

        try
        {
            var documents = await _documentService.GetAllDocumentsAsync();

            if (!documents.Any())
            {
                _console.WriteWarning("No documents uploaded yet");
                System.Console.WriteLine();
                System.Console.WriteLine("💡 Use option 12 to upload documents");
                return;
            }

            System.Console.WriteLine($"Total documents: {documents.Count}");
            System.Console.WriteLine();

            foreach (var doc in documents)
            {
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine($"📄 {doc.FileName}");
                System.Console.ResetColor();
                System.Console.WriteLine($"   ID: {doc.Id}");
                System.Console.WriteLine($"   Type: {doc.ContentType}");
                System.Console.WriteLine($"   Size: {doc.Content.Length:N0} bytes");
                System.Console.WriteLine($"   Chunks: {doc.Chunks?.Count ?? 0}");
                System.Console.WriteLine($"   Uploaded: {doc.UploadedAt:yyyy-MM-dd HH:mm}");
                System.Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents");
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    public async Task ClearAllDocumentsAsync()
    {
        _console.WriteSectionHeader("🗑️ Clear All Documents");

        var documents = await _documentService.GetAllDocumentsAsync();
        System.Console.WriteLine($"Total documents in storage: {documents.Count}");
        System.Console.WriteLine();

        if (documents.Count == 0)
        {
            _console.WriteWarning("No documents to clear!");
            return;
        }

        _console.WriteWarning("WARNING: This will permanently delete ALL documents and their embeddings!");
        System.Console.WriteLine();

        var confirmation = _console.ReadLine("Are you sure? Type 'yes' to confirm: ");
        if (confirmation?.ToLower() != "yes")
        {
            _console.WriteInfo("Operation cancelled");
            return;
        }

        try
        {
            System.Console.WriteLine();
            System.Console.WriteLine("🗑️  Clearing all documents...");

            var success = await _documentService.ClearAllDocumentsAsync();

            if (success)
            {
                _console.WriteSuccess($"Successfully cleared {documents.Count} documents!");
            }
            else
            {
                _console.WriteError("Failed to clear documents!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing documents");
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".txt" => "text/plain",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            _ => "application/octet-stream"
        };
    }

    private static bool IsAudioFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".mp3" or ".wav" or ".m4a" or ".flac" or ".ogg";
    }

    private async Task<string> SelectAudioLanguageAsync()
    {
        await Task.CompletedTask;
        
        System.Console.WriteLine();
        System.Console.WriteLine("🎤 Audio Language Selection:");
        System.Console.WriteLine("1. 🌍 Auto-detect (recommended)");
        System.Console.WriteLine("2. 🇹🇷 Turkish");
        System.Console.WriteLine("3. 🇬🇧 English");
        System.Console.WriteLine("4. 🇩🇪 German");
        System.Console.WriteLine("5. 🇷🇺 Russian");
        
        var langChoice = _console.ReadLine("Select audio language (1-5): ")?.Trim();
        var languageCode = langChoice switch
        {
            "2" => "tr",
            "3" => "en",
            "4" => "de",
            "5" => "ru",
            _ => "auto"
        };

        System.Console.WriteLine($"Selected language: {languageCode}");
        System.Console.WriteLine();

        return languageCode;
    }

    #endregion
}

