/// <summary>
/// Storage management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class StorageController(IStorageFactory storageFactory, IDocumentService documentService) : ControllerBase
{
    /// <summary>
    /// Gets storage statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<Dictionary<string, object>>> GetStatistics()
    {
        try
        {
            var stats = await documentService.GetStorageStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets storage health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<StorageHealthInfo>> GetHealth()
    {
        try
        {
            var stats = await documentService.GetStorageStatisticsAsync();
            var provider = storageFactory.GetCurrentProvider();

            var health = new StorageHealthInfo
            {
                Provider = provider,
                IsHealthy = true,
                DocumentCount = stats.ContainsKey("DocumentCount") ? Convert.ToInt32(stats["DocumentCount"]) :
                                 stats.ContainsKey("TotalDocuments") ? Convert.ToInt32(stats["TotalDocuments"]) : 0,
                LastChecked = DateTime.UtcNow
            };

            // Add provider-specific health checks
            switch (provider)
            {
                case StorageProvider.Redis:
                    if (stats.ContainsKey("IsConnected"))
                    {
                        health.IsHealthy = Convert.ToBoolean(stats["IsConnected"]);
                    }
                    break;

                case StorageProvider.Sqlite:
                    if (stats.ContainsKey("DatabasePath"))
                    {
                        health.IsHealthy = System.IO.File.Exists(stats["DatabasePath"].ToString());
                    }
                    break;

                case StorageProvider.FileSystem:
                    if (stats.ContainsKey("StoragePath"))
                    {
                        health.IsHealthy = Directory.Exists(stats["StoragePath"].ToString());
                    }
                    break;
            }

            return Ok(health);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}


/// <summary>
/// Change storage provider request
/// </summary>
// Removed unused request classes (ChangeStorageProviderRequest, MigrateDataRequest)

/// <summary>
/// Storage health information
/// </summary>
public class StorageHealthInfo
{
    public StorageProvider Provider { get; set; }
    public bool IsHealthy { get; set; }
    public int DocumentCount { get; set; }
    public DateTime LastChecked { get; set; }
}
