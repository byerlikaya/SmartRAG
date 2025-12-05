---
layout: default
title: Audio & OCR
description: SmartRAG audio and OCR configuration - Whisper.net and Tesseract OCR settings
lang: en
---

## Audio & OCR Configuration

SmartRAG provides capabilities for converting audio files to text and extracting text from images:

## Whisper.net (Local Audio Transcription)

<p>Whisper.net provides local, on-premise audio transcription with support for 99+ languages:</p>

### WhisperConfig Parameters

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Parameter</th>
                <th>Type</th>
                <th>Default</th>
                <th>Description</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>ModelPath</code></td>
                <td><code>string</code></td>
                <td><code>"models/ggml-large-v3.bin"</code></td>
                <td>Path to Whisper model file</td>
            </tr>
            <tr>
                <td><code>DefaultLanguage</code></td>
                <td><code>string</code></td>
                <td><code>"auto"</code></td>
                <td>Language code for transcription</td>
            </tr>
            <tr>
                <td><code>MinConfidenceThreshold</code></td>
                <td><code>double</code></td>
                <td><code>0.3</code></td>
                <td>Minimum confidence score (0.0-1.0)</td>
            </tr>
            <tr>
                <td><code>PromptHint</code></td>
                <td><code>string</code></td>
                <td><code>""</code></td>
                <td>Context hint for better accuracy</td>
            </tr>
            <tr>
                <td><code>MaxThreads</code></td>
                <td><code>int</code></td>
                <td><code>0</code></td>
                <td>CPU threads (0 = auto-detect)</td>
            </tr>
        </tbody>
    </table>
</div>

### Whisper Model Sizes

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Model</th>
                <th>Size</th>
                <th>Speed</th>
                <th>Accuracy</th>
                <th>Use Case</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>tiny</code></td>
                <td>75MB</td>
                <td>⭐⭐⭐⭐⭐</td>
                <td>⭐⭐</td>
                <td>Fast prototyping</td>
            </tr>
            <tr>
                <td><code>base</code></td>
                <td>142MB</td>
                <td>⭐⭐⭐⭐</td>
                <td>⭐⭐⭐</td>
                <td>Balanced performance</td>
            </tr>
            <tr>
                <td><code>small</code></td>
                <td>244MB</td>
                <td>⭐⭐⭐</td>
                <td>⭐⭐⭐⭐</td>
                <td>Good accuracy</td>
            </tr>
            <tr>
                <td><code>medium</code></td>
                <td>769MB</td>
                <td>⭐⭐</td>
                <td>⭐⭐⭐⭐⭐</td>
                <td>High accuracy</td>
            </tr>
            <tr>
                <td><code>large-v3</code></td>
                <td>1.5GB</td>
                <td>⭐</td>
                <td>⭐⭐⭐⭐⭐</td>
                <td>Best accuracy</td>
            </tr>
        </tbody>
    </table>
</div>

### Model Download

Whisper.net automatically downloads GGML models from Hugging Face on first use. Models are saved to the path specified in `ModelPath` configuration:

**Automatic Download:**
- Models are downloaded automatically when first used via `WhisperGgmlDownloader`
- Downloaded from Hugging Face repository
- Saved to the path specified in `ModelPath` (default: `models/ggml-large-v3.bin`)
- No manual download required

**Model Files:**
- Format: `ggml-{model-name}.bin` (e.g., `ggml-base.bin`, `ggml-large-v3.bin`)
- Available models: `tiny`, `base`, `small`, `medium`, `large-v3`
- First use downloads the model automatically (~5-10 minutes depending on connection and model size)

**Configuration:**
```json
{
  "SmartRAG": {
    "WhisperConfig": {
      "ModelPath": "models/ggml-large-v3.bin"
    }
  }
}
```

**Important Notes:**
- Whisper.net uses its own GGML model format and download system
- This is **independent** of Ollama, LM Studio, or cloud services
- Models are stored locally at the `ModelPath` location
- For on-premise deployments, ensure the application has write access to the model directory
- For cloud deployments, consider pre-downloading models or using persistent storage volumes

### Configuration Example

```json
{
  "SmartRAG": {
    "WhisperConfig": {
      "ModelPath": "models/ggml-large-v3.bin",
      "DefaultLanguage": "auto",
      "MinConfidenceThreshold": 0.3,
      "PromptHint": "",
      "MaxThreads": 0
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.WhisperConfig = new WhisperConfig
    {
        ModelPath = "models/ggml-large-v3.bin",
        DefaultLanguage = "auto",
        MinConfidenceThreshold = 0.3,
        PromptHint = "",
        MaxThreads = 0
    };
});
```

- `auto` - Automatic language detection (recommended)
- `en` - English
- `tr` - Turkish
- `de` - German
- `fr` - French
- `es` - Spanish
- `it` - Italian
- `ru` - Russian
- `ja` - Japanese
- `ko` - Korean
- `zh` - Chinese
- 99+ languages supported

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

<div class="alert alert-success">
    <h4><i class="fas fa-shield-alt me-2"></i> Privacy First</h4>
    <p class="mb-0">
        Audio files are processed locally using Whisper.net. No data leaves your machine - perfect for GDPR/KVKK/HIPAA compliance.
    </p>
</div>

## OCR Configuration

<p>Tesseract OCR enables text extraction from images and PDFs with support for 100+ languages:</p>

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

**Audio Formats:**
- `audio/mpeg` - MP3 files
- `audio/wav` - WAV files
- `audio/m4a` - M4A files
- `audio/flac` - FLAC files
- `audio/ogg` - OGG files

**Image Formats:**
- `image/jpeg` - JPEG images
- `image/png` - PNG images
- `image/tiff` - TIFF images
- `image/bmp` - BMP images
- `image/gif` - GIF images

**PDF Formats:**
- `application/pdf` - PDF documents (page-by-page OCR)

### Audio Quality Tips

1. **Clear Audio:** Avoid background noise and echo
2. **Good Microphone:** Use quality recording equipment
3. **Correct Language:** Specify the correct language of speech
4. **File Format:** MP3, WAV, M4A formats work best

### OCR Quality Tips

1. **High Resolution:** At least 300 DPI scan quality
2. **Clean Image:** Avoid blurry or shadowy images
3. **Correct Language:** Specify the correct language of text in image
4. **Contrast:** Prefer high-contrast, black-and-white images

## Audio and OCR Comparison

<p>Compare Whisper.net and Tesseract OCR capabilities:</p>

<div class="table-responsive">
<table class="table">
<thead>
<tr>
<th>Feature</th>
<th>Whisper.net</th>
<th>Tesseract OCR</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Data Privacy</strong></td>
<td><span class="badge bg-success">100% On-premise</span></td>
<td><span class="badge bg-success">100% On-premise</span></td>
</tr>
<tr>
<td><strong>Accuracy</strong></td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐</td>
</tr>
<tr>
<td><strong>Language Support</strong></td>
<td>⭐⭐⭐⭐⭐ (99+ languages)</td>
<td>⭐⭐⭐⭐ (100+ languages)</td>
</tr>
<tr>
<td><strong>Setup</strong></td>
<td>⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐⭐</td>
</tr>
<tr>
<td><strong>Cost</strong></td>
<td><span class="badge bg-secondary">Free</span></td>
<td><span class="badge bg-secondary">Free</span></td>
</tr>
<tr>
<td><strong>Performance</strong></td>
<td>⭐⭐⭐⭐</td>
<td>⭐⭐⭐</td>
</tr>
</tbody>
</table>
</div>

## Security and Privacy

### Audio Security

```csharp
// Whisper.net runs completely on-premise
var document = await _documentService.UploadDocumentAsync(
    sensitiveAudioStream,
    "confidential-meeting.mp3",
    "audio/mpeg",
    "user-id"
    // Data is never sent to cloud
);
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

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>Examples</h3>
            <p>Audio and OCR usage examples</p>
            <a href="{{ site.baseurl }}/en/examples/quick" class="btn btn-outline-primary btn-sm mt-3">
                View Examples
            </a>
        </div>
    </div>
</div>