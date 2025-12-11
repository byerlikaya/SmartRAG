namespace SmartRAG.Enums
{
    /// <summary>
    /// Schema analysis status
    /// </summary>
    public enum SchemaAnalysisStatus
    {
        /// <summary>
        /// Analysis has not started yet
        /// </summary>
        Pending,

        /// <summary>
        /// Analysis is currently in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Analysis completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Analysis failed with an error
        /// </summary>
        Failed,

        /// <summary>
        /// Schema refresh is needed
        /// </summary>
        RefreshNeeded
    }
}

