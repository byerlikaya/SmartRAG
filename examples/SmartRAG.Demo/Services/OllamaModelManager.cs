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
        private const int DefaultTimeoutMinutes = 30; // 30 dakika timeout
        private const int MaxRetryAttempts = 3;

        #endregion

        #region Fields

        private readonly HttpClient _httpClient;
        private readonly string _ollamaEndpoint;

        #endregion

        #region Constructor

        public OllamaModelManager(string? ollamaEndpoint = null)
        {
            _ollamaEndpoint = ollamaEndpoint ?? DefaultOllamaEndpoint;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(DefaultTimeoutMinutes)
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if a specific model is installed
        /// </summary>
        /// <returns>True if model is installed, false otherwise</returns>
        public async Task<bool> IsModelInstalledAsync(string modelName)
        {
            try
            {
                var installedModels = await ListInstalledModelsAsync();
                return installedModels.Any(m => m.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

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
                            catch
                            {
                                // Ignore JSON parsing errors
                            }
                        }
                    }

                    progressCallback?.Invoke($"Model {modelName} downloaded successfully");
                    return; // Success, exit retry loop
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    progressCallback?.Invoke($"Download timeout on attempt {attempt}/{MaxRetryAttempts}. Retrying in 5 seconds...");
                    
                    if (attempt < MaxRetryAttempts)
                    {
                        await Task.Delay(5000); // 5 saniye bekle
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
                        await Task.Delay(5000); // 5 saniye bekle
                        continue;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Model download failed after {MaxRetryAttempts} attempts due to network issues. Please check your internet connection and Ollama service.", ex);
                    }
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Unexpected error on attempt {attempt}/{MaxRetryAttempts}: {ex.Message}");
                    
                    if (attempt < MaxRetryAttempts)
                    {
                        await Task.Delay(5000); // 5 saniye bekle
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
            catch
            {
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
        /// Gets recommended models for SmartRAG Demo
        /// </summary>
        /// <returns>Dictionary of model names and descriptions</returns>
        public static Dictionary<string, string> GetRecommendedModels()
        {
            return new Dictionary<string, string>
            {
                // SQL Generation Models (Best for Database Queries)
                { "deepseek-coder:6.7b", "DeepSeek Coder 6.7B - Best for SQL generation (4.1GB) ⭐ Recommended" },
                { "qwen2.5-coder:7b", "Qwen2.5 Coder 7B - Multilingual SQL support (4.7GB)" },
                { "codellama:7b", "CodeLlama 7B - Excellent code generation including SQL (3.8GB)" },
                
                // General Purpose Models
                { "llama3.1:8b", "Llama 3.1 8B - General purpose, good SQL support (4.7GB)" },
                { "llama3.2:1b", "Llama 3.2 1B - Ultra lightweight (Recommended for slow connections)" },
                { "llama3.2", "Meta's Llama 3.2 - Fast and efficient (Large download)" },
                
                // Compact Models
                { "phi3", "Microsoft Phi-3 - Compact and fast (Good for testing)" },
                { "mistral", "Mistral 7B - High quality responses (Very large download)" },
                { "qwen2.5", "Alibaba Qwen 2.5 - Multilingual support (Large download)" },
                
                // Required for RAG
                { "nomic-embed-text", "Text embedding model (Required for RAG)" }
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

