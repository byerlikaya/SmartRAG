using System.Collections.Generic;

namespace SmartRAG.Models
{
    /// <summary>
    /// Result of file parsing operation containing extracted content and metadata
    /// </summary>
    public class FileParserResult
    {
        /// <summary>
        /// Extracted text content from the file
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Additional metadata extracted from the file (page counts, creation date, etc.)
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}

