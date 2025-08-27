namespace SmartRAG.Enums {

/// <summary>
/// Retry policy options for AI provider requests
/// </summary>
public enum RetryPolicy
{
    None,
    FixedDelay,
    LinearBackoff,
    ExponentialBackoff
}
}
