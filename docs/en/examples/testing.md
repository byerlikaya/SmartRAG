---
layout: default
title: Testing Examples
description: Testing strategies and examples
lang: en
---

## Testing Examples

### Unit Test Example

```csharp
using Xunit;
using Moq;

public class DocumentServiceTests
{
    [Fact]
    public async Task UploadDocumentAsync_ValidPdf_ReturnsDocument()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DocumentService>>();
        var mockParser = new Mock<IDocumentParserService>();
        var mockConversationManager = new Mock<IConversationManagerService>();
        
        var service = new DocumentService(mockLogger.Object, mockParser.Object, mockRepository.Object);
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
        
        // Act
        var result = await service.UploadDocumentAsync(
            stream, 
            "test.pdf", 
            "application/pdf", 
            "user-test"
        );
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.pdf", result.FileName);
    }
}
```

---

## Related Examples

- [Examples Index]({{ site.baseurl }}/en/examples) - Back to Examples categories
