---
layout: default
title: Audio & OCR
description: SmartRAG audio and OCR configuration - Google Speech-to-Text and Tesseract OCR settings
lang: en
---

## Audio & OCR Configuration

SmartRAG provides capabilities for converting audio files to text and extracting text from images:

---

## Google Speech-to-Text

### Configuration

```json
{
  "GoogleSpeech": {
    "CredentialsPath": "./path/to/google-credentials.json",
    "DefaultLanguageCode": "en-US",
    "EnableAutomaticPunctuation": true,
    "Model": "default"
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.GoogleSpeechConfig = new GoogleSpeechConfig
    {
        CredentialsPath = "./path/to/google-credentials.json",
        DefaultLanguageCode = "en-US",
        EnableAutomaticPunctuation = true,
        Model = "default"
    };
});
```

### Supported Languages

- `en-US` - English (United States)
- `tr-TR` - Turkish (Turkey)
- `de-DE` - German (Germany)
- `fr-FR` - French (France)
- `es-ES` - Spanish (Spain)
- `it-IT` - Italian (Italy)
- `ru-RU` - Russian (Russia)
- `ja-JP` - Japanese (Japan)
- `ko-KR` - Korean (South Korea)
- `zh-CN` - Chinese (China)
- 100+ languages supported - [View all](https://cloud.google.com/speech-to-text/docs/languages)

### Usage Example

```csharp
// Upload audio file
var document = await _documentService.UploadDocumentAsync(
    audioStream,
    "meeting-recording.mp3",
    "audio/mpeg",
    "user-id"
);

// Ask AI about audio file
var response = await _aiService.AskAsync(
    "What topics were discussed in this meeting?",
    "user-id"
);
```

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Privacy Note</h4>
    <p class="mb-0">
        Audio files are sent to Google Cloud for transcription. For complete data privacy, avoid uploading audio files or use alternative on-premise solutions.
    </p>
</div>

---

## OCR Configuration

### Tesseract Language Support

```csharp
// Specify language for OCR when uploading images
var document = await _documentService.UploadDocumentAsync(
    imageStream,
    "invoice.jpg",
    "image/jpeg",
    "user-id",
    language: "eng"  // English OCR
);

// Turkish OCR
language: "tur"

// Multi-language
language: "tur+eng"
```

### Supported OCR Languages

- `eng` - English
- `tur` - Turkish
- `deu` - German
- `fra` - French
- `spa` - Spanish
- `ita` - Italian
- `rus` - Russian
- `ara` - Arabic
- `chi` - Chinese
- `jpn` - Japanese
- `kor` - Korean
- `hin` - Hindi
- 100+ languages supported

### OCR Usage Examples

```csharp
// Invoice analysis
var invoice = await _documentService.UploadDocumentAsync(
    invoiceStream,
    "invoice-2024-01.pdf",
    "application/pdf",
    "user-id",
    language: "eng"
);

var analysis = await _aiService.AskAsync(
    "What products are in this invoice and what is the total amount?",
    "user-id"
);

// ID card analysis
var idCard = await _documentService.UploadDocumentAsync(
    idCardStream,
    "id-card.jpg",
    "image/jpeg",
    "user-id",
    language: "eng"
);

var info = await _aiService.AskAsync(
    "What is the person's name and birth date on this ID card?",
    "user-id"
);
```

---

## OCR Capabilities

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> OCR Capabilities</h4>
    <ul class="mb-0">
        <li><strong>✅ Works perfectly:</strong> Printed documents, scanned text, digital screenshots</li>
        <li><strong>⚠️ Limited support:</strong> Handwritten text (very low accuracy)</li>
        <li><strong>💡 Best results:</strong> High-quality scans of printed documents</li>
        <li><strong>🔒 100% On-Premise:</strong> No data sent to cloud - Tesseract runs on-premise</li>
    </ul>
</div>

### Supported File Formats

**Image Formats:**
- `image/jpeg` - JPEG images
- `image/png` - PNG images
- `image/tiff` - TIFF images
- `image/bmp` - BMP images
- `image/gif` - GIF images

**PDF Formats:**
- `application/pdf` - PDF documents (page-by-page OCR)

### OCR Quality Tips

1. **High Resolution:** At least 300 DPI scan quality
2. **Clean Image:** Avoid blurry or shadowy images
3. **Correct Language:** Specify the correct language of text in image
4. **Contrast:** Prefer high-contrast, black-and-white images

---

## Audio and OCR Comparison

| Feature | Google Speech-to-Text | Tesseract OCR |
|---------|----------------------|---------------|
| **Data Privacy** | ❌ Sent to cloud | ✅ 100% On-premise |
| **Accuracy** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Language Support** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Setup** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Cost** | 💰 Paid | 🆓 Free |
| **Performance** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |

---

## Security and Privacy

### Recommendations for Audio Files

```csharp
// Use on-premise solutions for sensitive audio files
if (isSensitiveAudio)
{
    // Alternative: Whisper.cpp or other on-premise solutions
    throw new NotSupportedException("Use on-premise solutions for sensitive audio files");
}
```

### OCR Security

```csharp
// OCR runs completely on-premise
var document = await _documentService.UploadDocumentAsync(
    sensitiveImageStream,
    "confidential-document.jpg",
    "image/jpeg",
    "user-id",
    language: "eng"
    // Data is never sent to cloud
);
```

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-cogs"></i>
            </div>
            <h3>Advanced Configuration</h3>
            <p>Fallback providers and best practices</p>
            <a href="{{ site.baseurl }}/en/configuration/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Advanced Configuration
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>Examples</h3>
            <p>Audio and OCR usage examples</p>
            <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm mt-3">
                View Examples
            </a>
        </div>
    </div>
</div>
