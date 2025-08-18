using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;

namespace SmartRAG.Services;

/// <summary>
/// AI Service that uses the configured AI provider
/// </summary>
public class AIService(IAIProviderFactory aiProviderFactory, IOptions<SmartRagOptions> options, IConfiguration configuration) : IAIService
{
	private readonly SmartRagOptions _options = options.Value;

	public async Task<string> GenerateResponseAsync(string query, IEnumerable<string> context)
	{
		try
		{
			var aiProvider = aiProviderFactory.CreateProvider(_options.AIProvider);

			var providerKey = _options.AIProvider.ToString();

			var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

			if (providerConfig == null)
				return $"AI provider configuration not found for '{providerKey}'.";

			string response;
			var attempt = 0;
			var maxAttempts = Math.Max(1, _options.MaxRetryAttempts);
			var delayMs = Math.Max(0, _options.RetryDelayMs);

			while (true)
			{
				try
				{
					// Build prompt with context and query and use full provider config
					var contextText = string.Join("\n\n", context);
					var prompt = $"Context:\n{contextText}\n\nQuestion: {query}\n\nAnswer:";
					response = await aiProvider.GenerateTextAsync(prompt, providerConfig);
					break;
				}
				catch (Exception) when (_options.RetryPolicy != RetryPolicy.None && attempt < maxAttempts - 1)
				{
					attempt++;
					var backoff = _options.RetryPolicy switch
					{
						RetryPolicy.FixedDelay => delayMs,
						RetryPolicy.LinearBackoff => delayMs * attempt,
						RetryPolicy.ExponentialBackoff => delayMs * (int)Math.Pow(2, attempt - 1),
						_ => delayMs
					};
					await Task.Delay(backoff);
				}
			}

			if (!string.IsNullOrEmpty(response))
			{
				return response;
			}

			// Try fallback providers if enabled
			if (_options.EnableFallbackProviders && _options.FallbackProviders.Count > 0)
			{
				return await TryFallbackProvidersAsync(query, context);
			}

			return "I'm sorry, I couldn't generate a response at this time.";
		}
		catch (Exception)
		{
			// Try fallback providers on error if enabled
			if (_options.EnableFallbackProviders && _options.FallbackProviders.Count > 0)
			{
				try
				{
					return await TryFallbackProvidersAsync(query, context);
				}
				catch (Exception)
				{
					// ignore
				}
			}

			return "I encountered an error while processing your request. Please try again later.";
		}
	}

	public async Task<List<float>> GenerateEmbeddingsAsync(string text)
	{
		try
		{
			var selectedProvider = _options.AIProvider;
			var aiProvider = aiProviderFactory.CreateProvider(selectedProvider);
			var providerKey = selectedProvider.ToString();
			var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();
			if (providerConfig == null)
				return [];

			var embeddings = await aiProvider.GenerateEmbeddingAsync(text, providerConfig);
			return embeddings;
		}
		catch (Exception)
		{
			throw;
		}
	}

	public async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts)
	{
		try
		{
			var selectedProvider = _options.AIProvider;
			var aiProvider = aiProviderFactory.CreateProvider(selectedProvider);
			var providerKey = selectedProvider.ToString();
			var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

			if (providerConfig == null)
				return [];

			// Use batch embedding if supported by the provider
			var embeddings = await aiProvider.GenerateEmbeddingsBatchAsync(texts, providerConfig);
			return embeddings.Where(e => e != null && e.Count > 0).ToList();
		}
		catch (Exception)
		{
			// Return empty list on error
			return [];
		}
	}

	private async Task<string> TryFallbackProvidersAsync(string query, IEnumerable<string> context)
	{
		foreach (var fallbackProvider in _options.FallbackProviders)
		{
			try
			{
				var aiProvider = aiProviderFactory.CreateProvider(fallbackProvider);
				var key = fallbackProvider.ToString();
				var config = configuration.GetSection($"AI:{key}").Get<AIProviderConfig>();
				if (config == null) continue;
				var contextText = string.Join("\n\n", context);
				var prompt = $"Context:\n{contextText}\n\nQuestion: {query}\n\nAnswer:";
				var response = await aiProvider.GenerateTextAsync(prompt, config);

				if (!string.IsNullOrEmpty(response))
				{
					return response;
				}
			}
			catch (Exception)
			{
				continue;
			}
		}

		return "I'm sorry, all AI providers are currently unavailable.";
	}
}
