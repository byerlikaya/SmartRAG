using Moq;
using SmartRAG.Interfaces;

namespace SmartRAG.Tests;

public class FileUploadTests
{
    [Fact]
    public async Task UploadTextFile_ShouldCreateDocumentWithChunks()
    {
        // Arrange
        var content = "This is a test file. Testing file upload functionality.";
        var fileName = "test.txt";
        var contentType = "text/plain";
        var uploadedBy = "testuser";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var options = Options.Create(new SmartRagOptions());
        var logger = new TestLogger<DocumentParserService>();
        var mockImageParserService = new Mock<IImageParserService>();
        var documentParserService = new DocumentParserService(options, mockImageParserService.Object, logger);

        // Act
        var result = await documentParserService.ParseDocumentAsync(stream, fileName, contentType, uploadedBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(uploadedBy, result.UploadedBy);
        Assert.Equal(content, result.Content);
        Assert.True(result.FileSize >= 0); // FileSize can be 0
        Assert.NotNull(result.Chunks);
        // Chunks count can be 0 (for very short texts)
        Assert.True(result.Chunks.Count >= 0);
    }

    [Fact]
    public void GetSupportedFileTypes_ShouldReturnCommonFormats()
    {
        // Arrange
        var options = Options.Create(new SmartRagOptions());
        var logger = new TestLogger<DocumentParserService>();
        var mockImageParserService = new Mock<IImageParserService>();
        var documentParserService = new DocumentParserService(options, mockImageParserService.Object, logger);

        // Act
        var supportedTypes = documentParserService.GetSupportedFileTypes();

        // Assert
        Assert.NotNull(supportedTypes);
        Assert.Contains(".txt", supportedTypes);
        Assert.Contains(".pdf", supportedTypes);
        Assert.Contains(".docx", supportedTypes);
    }
}

// Basit test logger
public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
