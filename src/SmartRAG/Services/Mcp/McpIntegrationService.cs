
namespace SmartRAG.Services.Mcp;


/// <summary>
/// Service for integrating MCP server results with SmartRAG queries
/// </summary>
public class McpIntegrationService : IMcpIntegrationService
{
    private const int MaxToolResults = 10;
    private static readonly string[] QueryParameterNames = { "query", "libraryName", "text", "input", "prompt", "question", "search" };
    private static readonly SemaphoreSlim ConnectLock = new(1, 1);

    private readonly ILogger<McpIntegrationService> _logger;
    private readonly IMcpClient _mcpClient;
    private readonly IMcpConnectionManager _mcpConnectionManager;

    public McpIntegrationService(
        ILogger<McpIntegrationService> logger,
        IMcpClient mcpClient,
        IMcpConnectionManager mcpConnectionManager)
    {
        _logger = logger;
        _mcpClient = mcpClient;
        _mcpConnectionManager = mcpConnectionManager;
    }

    /// <summary>
    /// Queries connected MCP servers and merges results with RAG response
    /// </summary>
    public async Task<List<McpToolResult>> QueryWithMcpAsync(string query, int maxResults = 5, string? conversationHistory = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        var enrichedQuery = EnrichQueryWithContext(query, conversationHistory);
        var results = new List<McpToolResult>();
        var connectedServers = _mcpClient.GetConnectedServers();

        if (connectedServers.Count == 0)
        {
            await EnsureMcpConnectedAsync(cancellationToken).ConfigureAwait(false);
            connectedServers = _mcpClient.GetConnectedServers();
        }

        if (connectedServers.Count == 0)
        {
            _logger.LogDebug("No MCP servers connected");
            return results;
        }

        foreach (var serverId in connectedServers)
        {
            try
            {
                var tools = await _mcpClient.DiscoverToolsAsync(serverId);

                var relevantTools = SelectRelevantTools(tools, enrichedQuery);

                foreach (var tool in relevantTools.Take(maxResults))
                {
                    try
                    {
                        var parameters = BuildToolParameters(tool, enrichedQuery);
                        var response = await _mcpClient.CallToolAsync(serverId, tool.Name, parameters);

                        if (response.IsSuccess)
                        {
                            var content = ExtractContentFromResponse(response);
                            results.Add(new McpToolResult
                            {
                                ServerId = serverId,
                                ToolName = tool.Name,
                                Content = content,
                                IsSuccess = true
                            });
                        }
                        else
                        {
                            results.Add(new McpToolResult
                            {
                                ServerId = serverId,
                                ToolName = tool.Name,
                                IsSuccess = false,
                                ErrorMessage = response.Error?.Message
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error calling tool on server");
                        results.Add(new McpToolResult
                        {
                            ServerId = serverId,
                            ToolName = tool.Name,
                            IsSuccess = false,
                            ErrorMessage = ex.Message
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying MCP server");
            }
        }

        return results.Take(MaxToolResults).ToList();
    }

    private async Task EnsureMcpConnectedAsync(CancellationToken cancellationToken)
    {
        await ConnectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mcpClient.GetConnectedServers().Count > 0)
                return;
            _logger.LogInformation("No MCP servers connected; attempting on-demand connect for MCP-tagged request.");
            await _mcpConnectionManager.ConnectAllAsync().ConfigureAwait(false);
        }
        finally
        {
            ConnectLock.Release();
        }
    }

    private Dictionary<string, object> BuildToolParameters(McpTool tool, string query)
    {
        var parameters = new Dictionary<string, object>();

        if (tool.InputSchema.Count > 0)
        {
            try
            {
                var schemaJson = JsonSerializer.Serialize(tool.InputSchema);
                var schemaElement = JsonSerializer.Deserialize<JsonElement>(schemaJson);

                if (schemaElement.TryGetProperty("properties", out var properties))
                {
                    var requiredProperties = new List<string>();
                    if (schemaElement.TryGetProperty("required", out var required))
                    {
                        if (required.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var req in required.EnumerateArray())
                            {
                                if (req.ValueKind == JsonValueKind.String)
                                {
                                    requiredProperties.Add(req.GetString() ?? string.Empty);
                                }
                            }
                        }
                    }

                    foreach (var prop in properties.EnumerateObject())
                    {
                        var propName = prop.Name;
                        var propSchema = prop.Value;

                        if (propSchema.ValueKind != JsonValueKind.Object)
                            continue;

                        var propType = propSchema.TryGetProperty("type", out var typeElement)
                            ? typeElement.GetString()
                            : "string";

                        if (propType != "string")
                            continue;

                        if (IsQueryParameter(propName) || requiredProperties.Contains(propName))
                        {
                            parameters[propName] = query;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing InputSchema for tool, using default parameters");
            }
        }

        if (parameters.Count == 0)
        {
            parameters["query"] = query;
        }

        return parameters;
    }

    private static bool IsQueryParameter(string paramName)
    {
        return QueryParameterNames.Any(name => paramName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string EnrichQueryWithContext(string query, string? conversationHistory)
    {
        if (string.IsNullOrWhiteSpace(conversationHistory))
            return query;

        var historyLines = conversationHistory.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var mentionedTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in historyLines.TakeLast(20))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.StartsWith("You:", StringComparison.OrdinalIgnoreCase))
            {
                var userQuery = trimmed[4..].Trim();
                ExtractKeywords(userQuery, mentionedTopics);
            }
            else if (trimmed.StartsWith("Assistant:", StringComparison.OrdinalIgnoreCase))
            {
                var assistantResponse = trimmed[10..].Trim();
                ExtractKeywords(assistantResponse, mentionedTopics);
            }
        }

        if (mentionedTopics.Count <= 0 || mentionedTopics.Contains(query, StringComparer.OrdinalIgnoreCase))
            return query;
        var topics = string.Join(", ", mentionedTopics.Take(3));
        return $"{query} (about: {topics})";

    }

    private static void ExtractKeywords(string text, HashSet<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var words = text.Split(new[] { ' ', '\t', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}' },
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var trimmed = word.Trim();
            if (trimmed.Length is >= 2 and <= 20 &&
                char.IsLetter(trimmed[0]) &&
                !IsCommonWord(trimmed))
            {
                keywords.Add(trimmed);
            }
        }
    }

    private static bool IsCommonWord(string word)
    {
        // Generic approach: Filter very short words (1-2 characters) and common function words
        // This approach works across all languages without hardcoding language-specific words
        if (word.Length <= 2)
        {
            return true;
        }

        // Filter common function words that appear in technical contexts
        // The length check above already filters most short function words in any language
        var commonFunctionWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "is", "are", "was", "were", "be", "been", "have", "has", "had",
            "do", "does", "did", "will", "would", "could", "should", "may", "might", "can",
            "this", "that", "these", "those", "a", "an", "and", "or", "but", "if", "then", "else",
            "when", "where", "why", "how", "what", "which", "who", "whom", "whose", "not", "no", "yes", "ok", "okay"
        };

        return commonFunctionWords.Contains(word);
    }

    /// <summary>
    /// Gets available tools from all connected MCP servers
    /// </summary>
    public async Task<List<McpTool>> GetAvailableToolsAsync()
    {
        var allTools = new List<McpTool>();
        var connectedServers = _mcpClient.GetConnectedServers();

        foreach (var serverId in connectedServers)
        {
            try
            {
                var tools = await _mcpClient.DiscoverToolsAsync(serverId);
                allTools.AddRange(tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering tools");
            }
        }

        return allTools;
    }

    /// <summary>
    /// Calls a specific MCP tool
    /// </summary>
    public async Task<McpToolResult> CallToolAsync(string serverId, string toolName, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrWhiteSpace(serverId))
            throw new ArgumentException("ServerId cannot be null or empty", nameof(serverId));

        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("ToolName cannot be null or empty", nameof(toolName));

        try
        {
            var response = await _mcpClient.CallToolAsync(serverId, toolName, parameters);

            if (!response.IsSuccess)
                return new McpToolResult
                {
                    ServerId = serverId,
                    ToolName = toolName,
                    IsSuccess = false,
                    ErrorMessage = response.Error?.Message ?? "Unknown error"
                };

            var content = ExtractContentFromResponse(response);
            return new McpToolResult
            {
                ServerId = serverId,
                ToolName = toolName,
                Content = content,
                IsSuccess = true
            };

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MCP tool");
            return new McpToolResult
            {
                ServerId = serverId,
                ToolName = toolName,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private List<McpTool> SelectRelevantTools(List<McpTool> tools, string query)
    {
        if (tools.Count == 0)
            return tools;

        var queryLower = query.ToLowerInvariant();
        var relevantTools = tools.Where(tool =>
            tool.Name.ToLowerInvariant().Contains(queryLower) ||
            tool.Description.ToLowerInvariant().Contains(queryLower)
        ).ToList();

        return relevantTools.Count > 0 ? relevantTools : tools;
    }

    private string ExtractContentFromResponse(McpResponse response)
    {
        switch (response.Result)
        {
            case string str:
                return str;
            case JsonElement jsonElement:
            {
                if (!jsonElement.TryGetProperty("content", out var contentElement))
                    return jsonElement.GetRawText();

                switch (contentElement.ValueKind)
                {
                    case JsonValueKind.Array:
                    {
                        var contents = new List<string>();
                        foreach (var item in contentElement.EnumerateArray())
                        {
                            switch (item.ValueKind)
                            {
                                case JsonValueKind.String:
                                    contents.Add(item.GetString() ?? string.Empty);
                                    break;
                                case JsonValueKind.Object when item.TryGetProperty("text", out var textElement):
                                    contents.Add(textElement.GetString() ?? string.Empty);
                                    break;
                            }
                        }
                        return string.Join("\n", contents);
                    }
                    case JsonValueKind.String:
                        return contentElement.GetString() ?? string.Empty;
                }

                return jsonElement.GetRawText();
            }
            default:
                return response.Result.ToString() ?? string.Empty;
        }
    }
}


