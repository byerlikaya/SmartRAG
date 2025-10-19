using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SmartRAG.LocalDemo
{
    /// <summary>
    /// Manages Ollama models - downloads, lists and verifies model availability
    /// </summary>
    public class OllamaModelManager
    {
        #region Constants

        private const string DefaultOllamaEndpoint = "http://localhost:11434";
        private const int DefaultTimeoutSeconds = 300;

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
                Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds)
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
        /// Downloads and installs a model from Ollama
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task DownloadModelAsync(string modelName, Action<string>? progressCallback = null)
        {
            progressCallback?.Invoke($"Starting download of model: {modelName}");

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
        /// Gets recommended models for SmartRAG Local Demo
        /// </summary>
        /// <returns>Dictionary of model names and descriptions</returns>
        public static Dictionary<string, string> GetRecommendedModels()
        {
            return new Dictionary<string, string>
            {
                { "llama3.2", "Meta's Llama 3.2 - Fast and efficient (Recommended)" },
                { "llama3.2:1b", "Llama 3.2 1B - Ultra lightweight version" },
                { "nomic-embed-text", "Text embedding model (Required for RAG)" },
                { "phi3", "Microsoft Phi-3 - Compact and fast" },
                { "mistral", "Mistral 7B - High quality responses" },
                { "qwen2.5", "Alibaba Qwen 2.5 - Multilingual support" }
            };
        }

        #endregion
    }
}

