namespace SmartRAG.Models;

/// <summary>
/// Redis storage configuration
/// </summary>
public class RedisConfig
{
    public string ConnectionString { get; set; } = "localhost:6379";

    public string? Password { get; set; }

    public string? Username { get; set; }

    public int Database { get; set; } = 0;

    public string KeyPrefix { get; set; } = "smartrag:doc:";

    public int ConnectionTimeout { get; set; } = 30;

    public bool EnableSsl { get; set; } = false;

    public int RetryCount { get; set; } = 3;

    public int RetryDelay { get; set; } = 1000;
}
