namespace SmartRAG.Enums
{
    /// <summary>
    /// Retry policy options for AI provider requests
    /// </summary>
    public enum RetryPolicy
    {
        /// <summary>
        /// No retry policy - fail immediately on first error
        /// </summary>
        None,

        /// <summary>
        /// Fixed delay retry policy - wait the same amount of time between retries
        /// </summary>
        FixedDelay,

        /// <summary>
        /// Linear backoff retry policy - increase delay linearly with each retry
        /// </summary>
        LinearBackoff,

        /// <summary>
        /// Exponential backoff retry policy - exponentially increase delay with each retry (recommended for AI providers)
        /// </summary>
        ExponentialBackoff
    }
}
