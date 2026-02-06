namespace SmartRAG.Models.Configuration;


/// <summary>
/// Configuration for Whisper audio transcription
/// </summary>
public class WhisperConfig
{
    /// <summary>
    /// Path to Whisper GGML model file (e.g., "ggml-base.bin", "ggml-medium.bin")
    /// Larger models provide better accuracy: tiny (75MB), base (142MB), small (466MB), medium (1.5GB), large-v3 (2.9GB)
    /// </summary>
    public string ModelPath { get; set; } = "models/ggml-base.bin";

    /// <summary>
    /// Default language for transcription (e.g., "en", "tr", "auto").
    /// This is the <em>source</em> language: Whisper transcribes in the same language (no translation).
    /// Use "auto" for automatic detection and transcribe-in-detected-language; no system-locale fallback is used.
    /// Set a concrete code (e.g. "tr") only when you want to pin the language for all uploads.
    /// </summary>
    public string DefaultLanguage { get; set; } = "auto";

    /// <summary>
    /// Minimum confidence threshold (0.0 - 1.0)
    /// Lower values accept more transcriptions but may include errors
    /// </summary>
    public double MinConfidenceThreshold { get; set; } = 0.3;

    /// <summary>
    /// Optional prompt/context hint for better transcription accuracy
    /// Example: "Natural conversation", "Business meeting", "Phone call"
    /// Providing context significantly improves accuracy (20-30% better)
    /// </summary>
    public string PromptHint { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of threads to use for processing.
    /// 0 = use (ProcessorCount - 1) to leave one core for system responsiveness and avoid 100% CPU.
    /// Set to a positive value (e.g. 4, 8) to cap or fix thread count.
    /// </summary>
    public int MaxThreads { get; set; } = 0;

    /// <summary>
    /// When true (default), only transcribe in the source language; never translate to English.
    /// Ensures output text is in the same language as the speech for any detected language.
    /// </summary>
    public bool ForceTranscribeOnly { get; set; } = true;

    /// <summary>
    /// When true, use GPU acceleration if the host app has the matching Whisper.net runtime installed.
    /// Windows/Linux: reference Whisper.net.Runtime.Cuda.Windows or Whisper.net.Runtime.Cuda.Linux (NVIDIA GPU).
    /// macOS: reference Whisper.net.Runtime.CoreML for Metal/Apple Silicon. Default is false (CPU only).
    /// </summary>
    public bool UseGpu { get; set; } = false;
}


