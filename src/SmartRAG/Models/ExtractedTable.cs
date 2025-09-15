using System.Collections.Generic;

namespace SmartRAG.Models
{
    /// <summary>
    /// Represents an extracted table from an image or document.
    /// </summary>
    public class ExtractedTable
    {
        /// <summary>
        /// Table content as structured text
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Number of rows in the table
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Number of columns in the table
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// Confidence score for the table extraction
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Structured table data
        /// </summary>
        public List<List<string>> Data { get; set; } = new List<List<string>>();
    }
}
