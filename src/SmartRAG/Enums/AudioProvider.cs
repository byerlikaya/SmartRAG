namespace SmartRAG.Enums
{
    /// <summary>
    /// Supported audio transcription providers
    /// </summary>
    public enum AudioProvider
    {
        /// <summary>
        /// Google Cloud Speech-to-Text (Cloud-based)
        /// </summary>
        GoogleCloud,
        
        /// <summary>
        /// Whisper.net (Local transcription)
        /// </summary>
        Whisper
    }
}

