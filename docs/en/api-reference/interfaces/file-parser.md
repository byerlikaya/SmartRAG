---
layout: default
title: IFileParser
description: IFileParser interface documentation
lang: en
---
## IFileParser

**Purpose:** Strategy for parsing specific file formats

**Namespace:** `SmartRAG.Interfaces.Parser.Strategies`

Enables custom file format parsers.

#### Methods

##### ParseAsync

Parse a file and extract content.

```csharp
Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
```

##### CanParse

Check if this parser can handle the given file.

```csharp
bool CanParse(string fileName, string contentType)
```

#### Built-in Implementations

- `PdfFileParser` - PDF documents
- `WordFileParser` - Word documents (.docx)
- `ExcelFileParser` - Excel spreadsheets (.xlsx)
- `TextFileParser` - Plain text files
- `ImageFileParser` - Images with OCR
- `AudioFileParser` - Audio transcription
- `DatabaseFileParser` - SQLite databases

#### Custom Implementation Example

```csharp
public class MarkdownFileParser : IFileParser
{
    public bool CanParse(string fileName, string contentType)
    {
        return fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
               contentType == "text/markdown";
    }
    
    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
    {
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync();
        
        // Strip markdown syntax for plain text
        var plainText = StripMarkdownSyntax(content);
        
        return new FileParserResult
        {
            Content = plainText,
            Success = true
        };
    }
    
    private string StripMarkdownSyntax(string markdown)
    {
        // Remove markdown formatting
        return Regex.Replace(markdown, @"[#*`\[\]()]", "");
    }
}
```


## Related Interfaces

- [Strategy Interfaces]({{ site.baseurl }}/en/api-reference/strategies) - Browse all strategy interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

