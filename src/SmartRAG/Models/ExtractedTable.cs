namespace SmartRAG.Models
{
    /// <summary>
    /// Represents an extracted table from an image
    /// </summary>
    public class ExtractedTable
    {
        /// <summary>
        /// Raw text content of the table
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
        /// Confidence score of the table extraction (0-1)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Structured table data as rows and columns
        /// </summary>
        public List<List<string>> Data { get; set; } = new List<List<string>>();

        /// <summary>
        /// Indicates if the table extraction is successful
        /// </summary>
        public bool IsSuccessful => !string.IsNullOrWhiteSpace(Content) && RowCount > 0 && ColumnCount > 0;

        /// <summary>
        /// Indicates if the table has valid data structure
        /// </summary>
        public bool HasValidData => Data.Count > 0 && Data.All(row => row.Count > 0);
    }
}
