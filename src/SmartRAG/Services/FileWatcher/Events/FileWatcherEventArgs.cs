using System;

namespace SmartRAG.Services.FileWatcher.Events
{
    /// <summary>
    /// Event arguments for file watcher events
    /// </summary>
    public class FileWatcherEventArgs : EventArgs
    {
        /// <summary>
        /// Full path to the file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Event type (Created, Changed, Deleted)
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
