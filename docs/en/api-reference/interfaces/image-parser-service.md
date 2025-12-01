---
layout: default
title: IImageParserService
description: IImageParserService interface documentation
lang: en
---
## IImageParserService

**Purpose:** OCR text extraction from images using Tesseract

**Namespace:** `SmartRAG.Interfaces.Parser`

Provides optical character recognition (OCR) for extracting text from images. All processing is local using Tesseract.

#### Methods

##### ExtractTextFromImageAsync

Extracts text from an image using OCR.

```csharp
Task<string> ExtractTextFromImageAsync(
    Stream imageStream, 
    string language = "eng"
)
```

**Parameters:**
- `imageStream` (Stream): The image stream to process
- `language` (string, optional): Language code for OCR (default: "eng")
  - English: "eng"
  - Turkish: "tur"
  - German: "deu"
  - Multiple: "eng+tur"

**Returns:** Extracted text as string

**Example:**

```csharp
using var imageStream = File.OpenRead("document.png");

var text = await _imageParser.ExtractTextFromImageAsync(
    imageStream, 
    language: "eng"
);

Console.WriteLine($"Extracted Text: {text}");
```

##### ExtractTextWithConfidenceAsync

Extracts text from an image with confidence scores.

```csharp
Task<OcrResult> ExtractTextWithConfidenceAsync(
    Stream imageStream, 
    string language = "eng"
)
```

**Parameters:**
- `imageStream` (Stream): The image stream to process
- `language` (string, optional): Language code for OCR (default: "eng")

**Returns:** `OcrResult` with extracted text, confidence scores, and text blocks

**Example:**

```csharp
using var imageStream = File.OpenRead("invoice.jpg");

var result = await _imageParser.ExtractTextWithConfidenceAsync(
    imageStream, 
    language: "eng"
);

Console.WriteLine($"Text: {result.ExtractedText}");
Console.WriteLine($"Confidence: {result.Confidence:P}");

foreach (var block in result.TextBlocks)
{
    Console.WriteLine($"Block: {block.Text} (Confidence: {block.Confidence:P})");
}
```

##### PreprocessImageAsync

Preprocesses an image for better OCR results.

```csharp
Task<Stream> PreprocessImageAsync(Stream imageStream)
```

**Parameters:**
- `imageStream` (Stream): The input image stream

**Returns:** Preprocessed image stream

**Preprocessing Steps:**
- Grayscale conversion
- Contrast enhancement
- Noise reduction
- Binarization

**Example:**

```csharp
using var originalStream = File.OpenRead("low-quality.jpg");
using var preprocessedStream = await _imageParser.PreprocessImageAsync(originalStream);

var text = await _imageParser.ExtractTextFromImageAsync(
    preprocessedStream, 
    language: "eng"
);

Console.WriteLine($"Text from preprocessed image: {text}");
```

**Supported Image Formats:**
- JPEG, PNG, GIF, BMP, TIFF, WEBP


## Related Interfaces

- [Advanced Interfaces]({{ site.baseurl }}/en/api-reference/advanced) - Browse all advanced interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

