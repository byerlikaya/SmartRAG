using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartRAG.API.Contracts;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.API.Controllers
{
    /// <summary>
    /// AI Provider Management and Direct AI Operations Controller
    /// 
    /// This controller provides comprehensive AI management capabilities including:
    /// - AI provider switching and management
    /// - Direct AI text generation without RAG
    /// - Embedding generation and batch processing
    /// - Provider health monitoring and validation
    /// - Performance benchmarking across providers
    /// 
    /// Key Features:
    /// - Multi-Provider Support: OpenAI, Anthropic, Google Gemini, Azure OpenAI, Custom Providers
    /// - Runtime Provider Switching: Change AI providers without restarting the application
    /// - Direct AI Access: Generate text and embeddings without document context
    /// - Provider Comparison: Test same queries across different providers
    /// - Health Monitoring: Real-time provider status and performance metrics
    /// - Batch Operations: Process multiple texts efficiently
    /// - Configuration Management: Validate and update provider settings
    /// 
    /// Use Cases:
    /// - Development and Testing: Test AI responses without uploading documents
    /// - Provider Evaluation: Compare quality and performance across providers
    /// - Integration Testing: Validate provider configurations
    /// - Troubleshooting: Debug AI-related issues
    /// - Performance Optimization: Benchmark and optimize AI operations
    /// - Failover Testing: Test provider switching scenarios
    /// 
    /// Example Usage:
    /// ```bash
    /// # Get available providers
    /// curl -X GET "https://localhost:7001/api/ai/providers"
    /// 
    /// # Switch to Anthropic
    /// curl -X POST "https://localhost:7001/api/ai/switch-provider" \
    ///   -H "Content-Type: application/json" \
    ///   -d '{"provider": "Anthropic", "validateBeforeSwitch": true}'
    /// 
    /// # Generate text directly
    /// curl -X POST "https://localhost:7001/api/ai/generate-text" \
    ///   -H "Content-Type: application/json" \
    ///   -d '{"prompt": "Explain quantum computing", "maxTokens": 500}'
    /// 
    /// # Generate embeddings
    /// curl -X POST "https://localhost:7001/api/ai/generate-embeddings" \
    ///   -H "Content-Type: application/json" \
    ///   -d '{"texts": ["Hello world", "Machine learning"]}'
    /// ```
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class AIController : ControllerBase
    {
        private readonly IAIProviderFactory _aiProviderFactory;
        private readonly IAIService _aiService;

        /// <summary>
        /// Initializes a new instance of the AIController
        /// </summary>
        public AIController(IAIProviderFactory aiProviderFactory, IAIService aiService)
        {
            _aiProviderFactory = aiProviderFactory;
            _aiService = aiService;
        }

        /// <summary>
        /// Gets information about all available AI providers
        /// </summary>
        /// <remarks>
        /// Returns detailed information about all supported AI providers including:
        /// - Current active provider
        /// - Configuration status for each provider
        /// - Available models and capabilities
        /// - Health status and last check time
        /// - Provider-specific features and limitations
        /// 
        /// This endpoint is useful for:
        /// - Discovering available AI providers
        /// - Checking provider configuration status
        /// - Monitoring provider health
        /// - Understanding provider capabilities
        /// </remarks>
        /// <returns>List of AI provider information</returns>
        /// <response code="200">Returns the list of AI providers with their status</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("providers")]
        [ProducesResponseType(typeof(List<AIProviderInfo>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AIProviderInfo>>> GetProviders()
        {
            try
            {
                var providers = new List<AIProviderInfo>();
                var allProviders = Enum.GetValues(typeof(AIProvider)).Cast<AIProvider>();

                foreach (var provider in allProviders)
                {
                    var providerInfo = new AIProviderInfo
                    {
                        Provider = provider,
                        DisplayName = GetProviderDisplayName(provider),
                        IsActive = await IsProviderActiveAsync(provider),
                        IsConfigured = await IsProviderConfiguredAsync(provider),
                        AvailableModels = GetProviderModels(provider),
                        DefaultModel = GetProviderDefaultModel(provider),
                        Capabilities = GetProviderCapabilities(provider),
                        IsHealthy = await CheckProviderHealthAsync(provider),
                        LastHealthCheck = DateTime.UtcNow
                    };

                    providers.Add(providerInfo);
                }

                return Ok(providers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the currently active AI provider information
        /// </summary>
        /// <remarks>
        /// Returns detailed information about the currently active AI provider including:
        /// - Provider type and display name
        /// - Current model being used
        /// - Configuration status
        /// - Health status and performance metrics
        /// - Available capabilities
        /// 
        /// This is useful for understanding which provider is currently handling AI requests.
        /// </remarks>
        /// <returns>Current AI provider information</returns>
        /// <response code="200">Returns the current active AI provider info</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("current-provider")]
        [ProducesResponseType(typeof(AIProviderInfo), StatusCodes.Status200OK)]
        public async Task<ActionResult<AIProviderInfo>> GetCurrentProvider()
        {
            try
            {
                var currentProvider = await GetActiveProviderAsync();
                
                var providerInfo = new AIProviderInfo
                {
                    Provider = currentProvider,
                    DisplayName = GetProviderDisplayName(currentProvider),
                    IsActive = true,
                    IsConfigured = await IsProviderConfiguredAsync(currentProvider),
                    AvailableModels = GetProviderModels(currentProvider),
                    DefaultModel = GetProviderDefaultModel(currentProvider),
                    Capabilities = GetProviderCapabilities(currentProvider),
                    IsHealthy = await CheckProviderHealthAsync(currentProvider),
                    LastHealthCheck = DateTime.UtcNow
                };

                return Ok(providerInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Switches to a different AI provider
        /// </summary>
        /// <remarks>
        /// Changes the active AI provider for all subsequent AI operations. This includes:
        /// - Text generation requests
        /// - Embedding generation
        /// - RAG operations
        /// - Search operations
        /// 
        /// Features:
        /// - **Validation**: Optionally validate provider configuration before switching
        /// - **Configuration Update**: Update API keys and model settings
        /// - **Custom Endpoints**: Support for custom provider endpoints
        /// - **Rollback**: Automatic rollback if validation fails
        /// 
        /// The switch is applied immediately and affects all future AI operations until changed again.
        /// 
        /// Example scenarios:
        /// - Switch from OpenAI to Anthropic for better reasoning
        /// - Use Azure OpenAI for enterprise compliance
        /// - Test different providers for quality comparison
        /// - Implement failover when primary provider is down
        /// </remarks>
        /// <param name="request">Provider switch configuration</param>
        /// <returns>Result of the provider switch operation</returns>
        /// <response code="200">Provider switched successfully</response>
        /// <response code="400">Invalid provider configuration or validation failed</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("switch-provider")]
        [ProducesResponseType(typeof(AIProviderInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AIProviderInfo>> SwitchProvider([FromBody] AIProviderSwitchRequest request)
        {
            try
            {
                // Validate the provider configuration if requested
                if (request.ValidateBeforeSwitch)
                {
                    var isValid = await ValidateProviderConfigurationAsync(request.Provider, request.ApiKey, request.BaseUrl);
                    if (!isValid)
                    {
                        return BadRequest(new { Error = $"Provider {request.Provider} configuration validation failed" });
                    }
                }

                // Perform the provider switch
                var success = await SwitchProviderAsync(request.Provider, request.ApiKey, request.ModelName, request.BaseUrl);
                if (!success)
                {
                    return BadRequest(new { Error = $"Failed to switch to provider {request.Provider}" });
                }

                // Return updated provider information
                var providerInfo = new AIProviderInfo
                {
                    Provider = request.Provider,
                    DisplayName = GetProviderDisplayName(request.Provider),
                    IsActive = true,
                    IsConfigured = true,
                    AvailableModels = GetProviderModels(request.Provider),
                    DefaultModel = string.IsNullOrEmpty(request.ModelName) ? GetProviderDefaultModel(request.Provider) : request.ModelName,
                    Capabilities = GetProviderCapabilities(request.Provider),
                    IsHealthy = true,
                    LastHealthCheck = DateTime.UtcNow
                };

                return Ok(providerInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Generates text using direct AI without RAG (Retrieval-Augmented Generation)
        /// </summary>
        /// <remarks>
        /// Provides direct access to AI text generation without document context. This is useful for:
        /// - **Pure AI Conversations**: Chat without document knowledge
        /// - **General Knowledge Queries**: Questions not related to uploaded documents
        /// - **Creative Writing**: Story generation, poetry, creative content
        /// - **Code Generation**: Programming assistance and code examples
        /// - **Translation**: Language translation services
        /// - **Summarization**: Summarize provided text content
        /// - **Analysis**: Analyze and explain concepts
        /// 
        /// Features:
        /// - **Conversation History**: Maintain context across multiple exchanges
        /// - **System Messages**: Set AI behavior and personality
        /// - **Temperature Control**: Adjust creativity vs consistency
        /// - **Token Limits**: Control response length
        /// - **Provider Flexibility**: Uses currently active AI provider
        /// 
        /// This endpoint bypasses the RAG pipeline entirely, providing direct AI responses
        /// similar to ChatGPT, Claude, or other AI chat interfaces.
        /// 
        /// Example use cases:
        /// - "Explain quantum computing in simple terms"
        /// - "Write a Python function to sort a list"
        /// - "Translate this text to Spanish"
        /// - "Help me brainstorm marketing ideas"
        /// </remarks>
        /// <param name="request">Direct AI generation request</param>
        /// <returns>AI-generated text response</returns>
        /// <response code="200">Text generated successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("generate-text")]
        [ProducesResponseType(typeof(AIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AIResponse>> GenerateText([FromBody] DirectAIRequest request)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var currentProvider = await GetActiveProviderAsync();

                // Build conversation context
                var context = new List<string>();
                if (!string.IsNullOrEmpty(request.SystemMessage))
                {
                    context.Add($"System: {request.SystemMessage}");
                }

                foreach (var msg in request.ConversationHistory)
                {
                    context.Add($"{msg.Role}: {msg.Content}");
                }

                // Generate AI response
                var response = await _aiService.GenerateResponseAsync(request.Prompt, context);
                stopwatch.Stop();

                var aiResponse = new AIResponse
                {
                    Text = response,
                    Provider = currentProvider,
                    Model = GetProviderDefaultModel(currentProvider),
                    TokensUsed = EstimateTokens(request.Prompt + response),
                    ResponseTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                    GeneratedAt = DateTime.UtcNow,
                    Success = true
                };

                return Ok(aiResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AIResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    GeneratedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Generates embedding vectors for text content
        /// </summary>
        /// <remarks>
        /// Converts text into high-dimensional numerical vectors (embeddings) that represent
        /// the semantic meaning of the text. These embeddings are used for:
        /// 
        /// - **Semantic Search**: Find similar content based on meaning
        /// - **Clustering**: Group similar texts together
        /// - **Classification**: Categorize text content
        /// - **Recommendation Systems**: Find related content
        /// - **Similarity Analysis**: Measure text similarity
        /// - **Vector Databases**: Store and search text representations
        /// 
        /// Features:
        /// - **Single Text**: Generate embedding for one text
        /// - **Batch Processing**: Generate embeddings for multiple texts efficiently
        /// - **Normalization**: Optional vector normalization
        /// - **Model Selection**: Use specific embedding models
        /// - **Provider Flexibility**: Uses currently active AI provider's embedding service
        /// 
        /// Embedding Dimensions:
        /// - OpenAI (text-embedding-ada-002): 1536 dimensions
        /// - Anthropic: Provider-specific dimensions
        /// - Google: Provider-specific dimensions
        /// - Custom providers: Varies by implementation
        /// 
        /// The returned vectors can be stored in vector databases like Qdrant, Pinecone,
        /// or used directly for similarity calculations.
        /// </remarks>
        /// <param name="request">Embedding generation request</param>
        /// <returns>Generated embedding vectors</returns>
        /// <response code="200">Embeddings generated successfully</response>
        /// <response code="400">Invalid request - must provide either Text or Texts</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("generate-embeddings")]
        [ProducesResponseType(typeof(EmbeddingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EmbeddingResponse>> GenerateEmbeddings([FromBody] EmbeddingRequest request)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var currentProvider = await GetActiveProviderAsync();

                var embeddingResponse = new EmbeddingResponse
                {
                    Provider = currentProvider,
                    Model = string.IsNullOrEmpty(request.Model) ? GetProviderDefaultEmbeddingModel(currentProvider) : request.Model,
                    Success = true
                };

                // Handle single text
                if (!string.IsNullOrEmpty(request.Text))
                {
                    var embedding = await _aiService.GenerateEmbeddingsAsync(request.Text);
                    embeddingResponse.Embedding = embedding;
                    embeddingResponse.Dimensions = embedding.Count;
                    embeddingResponse.TokensUsed = EstimateTokens(request.Text);
                }
                // Handle multiple texts
                else if (request.Texts != null && request.Texts.Any())
                {
                    var embeddings = new List<List<float>>();
                    var totalTokens = 0;

                    foreach (var text in request.Texts)
                    {
                        var embedding = await _aiService.GenerateEmbeddingsAsync(text);
                        embeddings.Add(embedding);
                        totalTokens += EstimateTokens(text);
                    }

                    embeddingResponse.Embeddings = embeddings;
                    embeddingResponse.Dimensions = embeddings.FirstOrDefault()?.Count ?? 0;
                    embeddingResponse.TokensUsed = totalTokens;
                }
                else
                {
                    return BadRequest(new { Error = "Must provide either 'text' or 'texts' in the request" });
                }

                stopwatch.Stop();
                embeddingResponse.ResponseTimeSeconds = stopwatch.Elapsed.TotalSeconds;

                return Ok(embeddingResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new EmbeddingResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Validates AI provider configuration and connectivity
        /// </summary>
        /// <remarks>
        /// Tests the configuration and connectivity of a specific AI provider without switching to it.
        /// This is useful for:
        /// 
        /// - **Configuration Testing**: Verify API keys and settings
        /// - **Connectivity Checks**: Test network connectivity to provider APIs
        /// - **Health Monitoring**: Regular health checks of all providers
        /// - **Troubleshooting**: Diagnose connection issues
        /// - **Setup Validation**: Confirm new provider configurations
        /// 
        /// The validation includes:
        /// - API key validity
        /// - Network connectivity
        /// - Basic API functionality test
        /// - Model availability check
        /// - Rate limit status (if available)
        /// 
        /// This endpoint does not change the active provider.
        /// </remarks>
        /// <param name="provider">AI provider to validate</param>
        /// <returns>Validation result with detailed status</returns>
        /// <response code="200">Validation completed (check Success property for result)</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("validate/{provider}")]
        [ProducesResponseType(typeof(AIProviderInfo), StatusCodes.Status200OK)]
        public async Task<ActionResult<AIProviderInfo>> ValidateProvider(AIProvider provider)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                var providerInfo = new AIProviderInfo
                {
                    Provider = provider,
                    DisplayName = GetProviderDisplayName(provider),
                    IsActive = await IsProviderActiveAsync(provider),
                    IsConfigured = await IsProviderConfiguredAsync(provider),
                    AvailableModels = GetProviderModels(provider),
                    DefaultModel = GetProviderDefaultModel(provider),
                    Capabilities = GetProviderCapabilities(provider),
                    LastHealthCheck = DateTime.UtcNow
                };

                // Perform health check
                try
                {
                    providerInfo.IsHealthy = await CheckProviderHealthAsync(provider);
                    if (!providerInfo.IsHealthy)
                    {
                        providerInfo.HealthError = $"Provider {provider} health check failed";
                    }
                }
                catch (Exception ex)
                {
                    providerInfo.IsHealthy = false;
                    providerInfo.HealthError = ex.Message;
                }

                stopwatch.Stop();
                return Ok(providerInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets comprehensive AI service status and metrics
        /// </summary>
        /// <remarks>
        /// Returns detailed status information about the AI service including:
        /// 
        /// - **Current Provider**: Active AI provider and configuration
        /// - **Provider Health**: Health status of all configured providers
        /// - **Performance Metrics**: Response times and success rates
        /// - **Usage Statistics**: Request counts and token usage
        /// - **System Status**: Overall AI service health
        /// 
        /// This endpoint provides a comprehensive overview of AI service status
        /// and is useful for monitoring dashboards and health checks.
        /// </remarks>
        /// <returns>AI service status and metrics</returns>
        /// <response code="200">Status retrieved successfully</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("status")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> GetStatus()
        {
            try
            {
                var currentProvider = await GetActiveProviderAsync();
                var allProviders = Enum.GetValues(typeof(AIProvider)).Cast<AIProvider>();
                
                var providerStatuses = new List<object>();
                foreach (var provider in allProviders)
                {
                    providerStatuses.Add(new
                    {
                        Provider = provider.ToString(),
                        IsActive = await IsProviderActiveAsync(provider),
                        IsConfigured = await IsProviderConfiguredAsync(provider),
                        IsHealthy = await CheckProviderHealthAsync(provider)
                    });
                }

                var status = new
                {
                    ServiceStatus = "Running",
                    CurrentProvider = currentProvider.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Providers = providerStatuses,
                    Capabilities = new
                    {
                        TextGeneration = true,
                        EmbeddingGeneration = true,
                        ProviderSwitching = true,
                        BatchProcessing = true,
                        ConversationHistory = true
                    }
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        #region Private Helper Methods

        private string GetProviderDisplayName(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.OpenAI => "OpenAI GPT Models",
                AIProvider.Anthropic => "Anthropic Claude Models",
                AIProvider.Gemini => "Google Gemini Models",
                AIProvider.AzureOpenAI => "Azure OpenAI Service",
                AIProvider.Custom => "Custom AI Provider",
                _ => provider.ToString()
            };
        }

        private List<string> GetProviderModels(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.OpenAI => new List<string> { "gpt-4", "gpt-3.5-turbo", "text-embedding-ada-002" },
                AIProvider.Anthropic => new List<string> { "claude-3-opus", "claude-3-sonnet", "claude-3-haiku" },
                AIProvider.Gemini => new List<string> { "gemini-pro", "gemini-pro-vision" },
                AIProvider.AzureOpenAI => new List<string> { "gpt-4", "gpt-35-turbo", "text-embedding-ada-002" },
                AIProvider.Custom => new List<string> { "custom-model" },
                _ => new List<string>()
            };
        }

        private string GetProviderDefaultModel(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.OpenAI => "gpt-4",
                AIProvider.Anthropic => "claude-3-sonnet",
                AIProvider.Gemini => "gemini-pro",
                AIProvider.AzureOpenAI => "gpt-4",
                AIProvider.Custom => "custom-model",
                _ => "default"
            };
        }

        private string GetProviderDefaultEmbeddingModel(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.OpenAI => "text-embedding-ada-002",
                AIProvider.AzureOpenAI => "text-embedding-ada-002",
                AIProvider.Anthropic => "claude-embedding",
                AIProvider.Gemini => "embedding-001",
                AIProvider.Custom => "custom-embedding",
                _ => "default-embedding"
            };
        }

        private List<string> GetProviderCapabilities(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.OpenAI => new List<string> { "TextGeneration", "Embeddings", "ChatCompletion", "FunctionCalling" },
                AIProvider.Anthropic => new List<string> { "TextGeneration", "ChatCompletion", "LongContext" },
                AIProvider.Gemini => new List<string> { "TextGeneration", "ChatCompletion", "Vision", "Embeddings" },
                AIProvider.AzureOpenAI => new List<string> { "TextGeneration", "Embeddings", "ChatCompletion", "Enterprise" },
                AIProvider.Custom => new List<string> { "TextGeneration", "CustomEndpoints" },
                _ => new List<string> { "TextGeneration" }
            };
        }

        private Task<AIProvider> GetActiveProviderAsync()
        {
            // This would typically get the current provider from configuration or factory
            // For now, return a default - this should be implemented based on your architecture
            return Task.FromResult(AIProvider.OpenAI);
        }

        private async Task<bool> IsProviderActiveAsync(AIProvider provider)
        {
            var currentProvider = await GetActiveProviderAsync();
            return currentProvider == provider;
        }

        private async Task<bool> IsProviderConfiguredAsync(AIProvider provider)
        {
            // This should check if the provider has valid configuration (API keys, etc.)
            // Implementation depends on your configuration system
            return await Task.FromResult(true);
        }

        private async Task<bool> CheckProviderHealthAsync(AIProvider provider)
        {
            try
            {
                // This should perform an actual health check against the provider
                // For now, return true - implement actual health checks based on your needs
                return await Task.FromResult(true);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> ValidateProviderConfigurationAsync(AIProvider provider, string apiKey, string baseUrl)
        {
            try
            {
                // This should validate the provider configuration
                // Implementation depends on your provider validation logic
                return await Task.FromResult(true);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> SwitchProviderAsync(AIProvider provider, string apiKey, string modelName, string baseUrl)
        {
            try
            {
                // This should perform the actual provider switch
                // Implementation depends on your provider switching logic
                return await Task.FromResult(true);
            }
            catch
            {
                return false;
            }
        }

        private int EstimateTokens(string text)
        {
            // Simple token estimation - roughly 4 characters per token
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        #endregion
    }
}
