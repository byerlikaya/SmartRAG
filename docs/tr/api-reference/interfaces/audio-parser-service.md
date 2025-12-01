---
layout: default
title: IAudioParserService
description: IAudioParserService arayüz dokümantasyonu
lang: tr
---

## IAudioParserService

**Amaç:** Whisper.net ile ses transkripsiyonu (%100 yerel işleme)

**Namespace:** `SmartRAG.Interfaces.Parser`

Whisper.net kullanarak yerel ses-metin transkripsiyonu sağlar. Tüm işlem lokalde yapılır.

<div class="alert alert-success">
    <h4><i class="fas fa-lock me-2"></i> Gizlilik Notu</h4>
    <p class="mb-0">
        Ses transkripsiyonu %100 yerel işleme için <strong>Whisper.net</strong> kullanır. 
        Hiçbir ses verisi hiçbir zaman harici servislere gönderilmez. GDPR/KVKK/HIPAA uyumlu.
    </p>
</div>

#### Metodlar

##### TranscribeAudioAsync

Bir akıştan ses içeriğini metne transcribe eder.

```csharp
Task<AudioTranscriptionResult> TranscribeAudioAsync(
    Stream audioStream, 
    string fileName, 
    string language = null
)
```

**Parametreler:**
- `audioStream` (Stream): Transcribe edilecek ses akışı
- `fileName` (string): Format tespiti için ses dosyası adı
- `language` (string, isteğe bağlı): Transkripsiyon için dil kodu (örn. "tr", "en", "auto")

**Döndürür:** Transcribe edilmiş metin, güven skoru ve metadata ile `AudioTranscriptionResult`

**Örnek:**

```csharp
using var audioStream = File.OpenRead("toplanti.mp3");

var result = await _audioParser.TranscribeAudioAsync(
    audioStream, 
    "toplanti.mp3", 
    language: "tr"
);

Console.WriteLine($"Transkripsiyon: {result.Text}");
Console.WriteLine($"Güven: {result.Confidence:P}");
```


## İlgili Arayüzler

- [Gelişmiş Arayüzler]({{ site.baseurl }}/tr/api-reference/advanced) - Tüm gelişmiş arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

