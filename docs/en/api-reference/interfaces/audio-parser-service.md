---
layout: default
title: IAudioParserService
description: IAudioParserService interface documentation
lang: en
---
## IAudioParserService

**Purpose:** Audio transcription with Whisper.net (100% local processing)

**Namespace:** `SmartRAG.Interfaces.Parser`

Provides local audio-to-text transcription using Whisper.net. All processing is done on-premise - no data is sent to external services.

<div class="alert alert-success">
    <h4><i class="fas fa-lock me-2"></i> Privacy Note</h4>
    <p class="mb-0">
        Audio transcription uses <strong>Whisper.net</strong> for 100% local processing. 
        No audio data is ever sent to external services. GDPR/KVKK/HIPAA compliant.
    </p>
</div>

#### Methods

##### TranscribeAudioAsync

Transcribes audio content from a stream to text.

```csharp
Task<AudioTranscriptionResult> TranscribeAudioAsync(
    Stream audioStream, 
    string fileName, 
    string language = null
)
```

**Parameters:**
- `audioStream` (Stream): The audio stream to transcribe
- `fileName` (string): The name of the audio file for format detection
- `language` (string, optional): Language code for transcription (e.g., "en", "tr", "auto")

**Returns:** `AudioTranscriptionResult` containing transcribed text, confidence score, and metadata

**Example:**

```csharp
using var audioStream = File.OpenRead("meeting.mp3");

var result = await _audioParser.TranscribeAudioAsync(
    audioStream, 
    "meeting.mp3", 
    language: "en"
);

Console.WriteLine($"Transcription: {result.Text}");
Console.WriteLine($"Confidence: {result.Confidence:P}");
Console.WriteLine($"Language: {result.Language}");
```

**Supported Audio Formats:**
- MP3, WAV, M4A, AAC, OGG, FLAC, WMA

**Whisper Models:**
- `tiny` (75MB) - Fastest, lowest accuracy
- `base` (142MB) - Good balance (recommended)
- `small` (466MB) - Better accuracy
- `medium` (1.5GB) - High accuracy
- `large-v3` (2.9GB) - Highest accuracy


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

