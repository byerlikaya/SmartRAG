namespace SmartRAG.Interfaces.Health;

/// <summary>
/// Service for checking health status of SmartRAG components (AI, storage, databases)
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Runs a comprehensive health check on all configured services
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Aggregated health status for AI, storage, and databases</returns>
    Task<HealthCheckResult> RunFullHealthCheckAsync(CancellationToken cancellationToken = default);
}
