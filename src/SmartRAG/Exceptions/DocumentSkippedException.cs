using System;

namespace SmartRAG.Exceptions
{
    /// <summary>
    /// Thrown when a document is intentionally skipped (e.g. no content to index, transcription unavailable).
    /// Callers such as FileWatcher should not retry the same file.
    /// </summary>
    public sealed class DocumentSkippedException : InvalidOperationException
    {
        public DocumentSkippedException(string message) : base(message) { }

        public DocumentSkippedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
