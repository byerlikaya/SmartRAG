using System;

namespace SmartRAG.Models
{

    /// <summary>
    /// Represents a search result source with document information and relevance score
    /// </summary>
    public class SearchSource
    {
        /// <summary>
        /// Unique identifier of the source document
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Name of the source document file
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Relevant content excerpt from the document
        /// </summary>
        public string RelevantContent { get; set; } = string.Empty;

        /// <summary>
        /// Relevance score indicating how well this source matches the search query
        /// </summary>
        public double RelevanceScore { get; set; }
    }
}
