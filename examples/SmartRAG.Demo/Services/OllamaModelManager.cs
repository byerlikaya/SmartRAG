using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SmartRAG.Demo.Services;

/// <summary>
/// Manages Ollama models - downloads, lists and verifies model availability
/// </summary>
public class OllamaModelManager
{
    #region Constants

    private const string DefaultOllamaEndpoint = "http://localhost:11434";
    private const int DefaultTimeoutMinutes = 30;
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMilliseconds = 5000;

    #endregion

    #region Fields

    private readonly HttpClient _httpClient;
    private readonly string _ollamaEndpoint;
    private readonly ILogger<OllamaModelManager>? _logger;

    #endregion

    #region Constructor

    public OllamaModelManager(string? ollamaEndpoint = null, ILogger<OllamaModelManager>? logger = null)
    {
        _ollamaEndpoint = ollamaEndpoint ?? DefaultOllamaEndpoint;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(DefaultTimeoutMinutes)
        };
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Downloads and installs a model from Ollama with retry mechanism
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    public async Task DownloadModelAsync(string modelName, Action<string>? progressCallback = null)
    {
        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                progressCallback?.Invoke($"Starting download of model: {modelName} (Attempt {attempt}/{MaxRetryAttempts})");

                var payload = new { name = modelName };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_ollamaEndpoint}/api/pull", content);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        try
                        {
                            var json = JsonSerializer.Deserialize<JsonElement>(line);
                            if (json.TryGetProperty("status", out var status))
                            {
                                progressCallback?.Invoke(status.GetString() ?? "Downloading...");
                            }
                        }
                        catch (JsonException)
                        {
                        }
                    }
                }

                progressCallback?.Invoke($"Model {modelName} downloaded successfully");
                return;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                progressCallback?.Invoke($"Download timeout on attempt {attempt}/{MaxRetryAttempts}. Retrying in 5 seconds...");
                
                if (attempt < MaxRetryAttempts)
                {
                    await Task.Delay(RetryDelayMilliseconds);
                    continue;
                }
                else
                {
                    throw new InvalidOperationException($"Model download failed after {MaxRetryAttempts} attempts due to timeout. The model might be too large for your connection. Try a smaller model like 'llama3.2:1b' or 'phi3'.", ex);
                }
            }
            catch (HttpRequestException ex)
            {
                progressCallback?.Invoke($"Network error on attempt {attempt}/{MaxRetryAttempts}: {ex.Message}");
                
                if (attempt < MaxRetryAttempts)
                {
                    await Task.Delay(RetryDelayMilliseconds);
                    continue;
                }
                else
                {
                    _logger?.LogError(ex, "Model download failed after {MaxRetries} attempts due to network issues", MaxRetryAttempts);
                    throw new InvalidOperationException($"Model download failed after {MaxRetryAttempts} attempts due to network issues. Please check your internet connection and Ollama service.", ex);
                }
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke($"Unexpected error on attempt {attempt}/{MaxRetryAttempts}: {ex.Message}");
                
                if (attempt < MaxRetryAttempts)
                {
                    await Task.Delay(RetryDelayMilliseconds);
                    continue;
                }
                else
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Lists all installed models
    /// </summary>
    /// <returns>List of installed model names</returns>
    public async Task<List<string>> ListInstalledModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_ollamaEndpoint}/api/tags");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var models = new List<string>();

            if (json.TryGetProperty("models", out var modelsArray))
            {
                foreach (var model in modelsArray.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name))
                    {
                        var modelName = name.GetString();
                        if (!string.IsNullOrEmpty(modelName))
                        {
                            models.Add(modelName);
                        }
                    }
                }
            }

            return models;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to list installed Ollama models");
            return new List<string>();
        }
    }

    /// <summary>
    /// Checks if Ollama service is running and accessible
    /// </summary>
    /// <returns>True if service is available, false otherwise</returns>
    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_ollamaEndpoint}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets recommended models for SmartRAG Demo (Optimized for 32GB+ RAM systems)
    /// </summary>
    /// <returns>Dictionary of model names and descriptions</returns>
    public static Dictionary<string, string> GetRecommendedModels()
    {
        return new Dictionary<string, string>
        {
            // 🏆 Top 5 Models for High-End Systems (32GB+ RAM) - SQL Generation Focus
            { "deepseek-coder-v2:16b", "⭐ DeepSeek-Coder-V2 16B - Best SQL generation, requires ~12GB RAM" },
            { "qwen2.5-coder:32b", "🚀 Qwen2.5-Coder 32B - Most powerful for complex SQL, requires ~20GB RAM" },
            { "codellama:13b-instruct", "💡 CodeLlama 13B Instruct - Meta's SQL specialist, requires ~8GB RAM" },
            { "qwen2.5-coder:14b", "⚡ Qwen2.5-Coder 14B - Balanced power and speed, requires ~9GB RAM" },
            { "llama3.1:8b", "📦 Llama 3.1 8B - Lightweight but capable, requires ~5GB RAM" },
            
            // Required for RAG (Embeddings)
            { "nomic-embed-text", "📊 Nomic Embed - Text embedding model (Required for RAG, ~300MB)" }
        };
    }

    /// <summary>
    /// Gets small models recommended for slow connections
    /// </summary>
    /// <returns>List of small model names</returns>
    public static List<string> GetSmallModels()
    {
        return new List<string> { "llama3.2:1b", "phi3", "nomic-embed-text" };
    }

    #endregion
}

