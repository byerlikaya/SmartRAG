using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services
{

    public class DocumentSearchService : IDocumentSearchService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IAIService _aiService;
        private readonly IAIProviderFactory _aiProviderFactory;


        public DocumentSearchService(
            IDocumentRepository documentRepository,
            IAIService aiService,
            IAIProviderFactory aiProviderFactory,
            SemanticSearchService semanticSearchService,
            IConfiguration configuration,
            IOptions<SmartRagOptions> options,
            ILogger<DocumentSearchService> logger)
        {
            _documentRepository = documentRepository;
            _aiService = aiService;
            _aiProviderFactory = aiProviderFactory;
            _semanticSearchService = semanticSearchService;
            _configuration = configuration;
            _options = options.Value;
            _logger = logger;
        }

        #region Constants

        // Scoring weights
        private const double FullNameMatchScoreBoost = 200.0;
        private const double PartialNameMatchScoreBoost = 100.0;
        private const double WordMatchScore = 2.0;
        private const double WordCountScoreBoost = 5.0;
        private const double PunctuationScoreBoost = 2.0;
        private const double NumberScoreBoost = 2.0;

        // Thresholds
        private const int WordCountMin = 10;
        private const int WordCountMax = 100;
        private const int PunctuationCountThreshold = 3;
        private const int NumberCountThreshold = 2;
        private const double RelevanceThreshold = 0.1;
        private const int ChunkPreviewLength = 100;

        // Selection multipliers and minimums
        private const int InitialSearchMultiplier = 2;
        private const int CandidateMultiplier = 3;
        private const int CandidateMinCount = 30;
        private const int FinalTakeMultiplier = 2;
        private const int FinalMinCount = 20;

        // Query processing constants
        private const int MinWordLength = 2;
        private const int MinNameCount = 2;
        private const int DefaultScore = 0;
        private const double HybridSemanticWeight = 0.8;
        private const double HybridKeywordWeight = 0.2;
        private const int EmptyEmbeddingCount = 0;
        private const int MinQueryWordsCount = 0;
        private const double DefaultScoreValue = 0.0;
        private const double NormalizedScoreMax = 1.0;
        private const int MinVectorCount = 0;
        private const int MinChunksWithEmbeddingsCount = 0;
        private const int MinSearchResultsCount = 0;
        private const int MinRelevantChunksCount = 0;
        private const int MinNameChunksCount = 0;
        private const int MinPotentialNamesCount = 2;
        private const int MinWordCountThreshold = 0;
        // Fallback search and content
        private const int FallbackSearchMaxResults = 5;
        private const int MinSubstantialContentLength = 50;
        private const int MaxConversationLength = 2000;
        private const int MinConversationLines = 6;
        private const int MinExchanges = 3;
        private const int LinesPerExchange = 2;

        // Generic messages
        private const string ChatUnavailableMessage = "Sorry, I cannot chat right now. Please try again later.";

        #endregion

        #region Fields

        private readonly SmartRagOptions _options;
        private readonly SemanticSearchService _semanticSearchService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentSearchService> _logger;

        // Conversation management using existing storage
        private readonly ConcurrentDictionary<string, string> _conversationCache = new ConcurrentDictionary<string, string>();

        #endregion

        #region Public Methods

        public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            // Use our integrated search algorithm with diversity selection
            var searchResults = await PerformBasicSearchAsync(query, maxResults * InitialSearchMultiplier);

            if (searchResults.Count > 0)
            {
                ServiceLogMessages.LogSearchResults(_logger, searchResults.Count, searchResults.Select(c => c.DocumentId).Distinct().Count(), null);

                // Apply diversity selection to ensure chunks from different documents
                var diverseResults = ApplyDiversityAndSelect(searchResults, maxResults);

                ServiceLogMessages.LogDiverseResults(_logger, diverseResults.Count, diverseResults.Select(c => c.DocumentId).Distinct().Count(), null);

                return diverseResults;
            }

            return searchResults;
        }

        public async Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            // Check for new conversation command or parameter
            if (startNewConversation || IsNewConversationCommand(query))
            {
                await StartNewConversationAsync();
                return new RagResponse
                {
                    Query = query,
                    Answer = "New conversation started. How can I help you?",
                    Sources = new List<SearchSource>(),
                    SearchedAt = DateTime.UtcNow,
                    Configuration = GetRagConfiguration()
                };
            }

            // Generate or retrieve session ID automatically
            var sessionId = await GetOrCreateSessionIdAsync();

            // Get conversation history from session
            var conversationHistory = await GetConversationHistoryAsync(sessionId);

            // Universal approach: Check if documents contain relevant information for the query
            // This approach is language-agnostic and doesn't rely on specific word patterns
            var canAnswerFromDocuments = await CanAnswerFromDocumentsAsync(query);

            RagResponse response;

            if (!canAnswerFromDocuments)
            {
                ServiceLogMessages.LogGeneralConversationQuery(_logger, null);
                var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory);
                response = new RagResponse
                {
                    Query = query,
                    Answer = chatResponse,
                    Sources = new List<SearchSource>(),
                    SearchedAt = DateTime.UtcNow,
                    Configuration = GetRagConfiguration()
                };
            }
            else
            {
                // Document search query - use our integrated RAG implementation
                response = await GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory);
            }

            // Add to conversation history
            await AddToConversationAsync(sessionId, query, response.Answer);

            return response;
        }

        private async Task<string> StartNewConversationAsync()
        {
            try
            {
                // Clear current session from cache
                var currentSession = _conversationCache.Keys.FirstOrDefault();
                if (!string.IsNullOrEmpty(currentSession))
                {
                    _conversationCache.TryRemove(currentSession, out _);
                }

                // Clear persistent session key
                const string PersistentSessionKey = "smartrag-current-session";
                try
                {
                    await _documentRepository.ClearConversationAsync(PersistentSessionKey);
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogConversationStorageFailed(_logger, PersistentSessionKey, ex);
                }

                // Create new session
                var newSessionId = $"session-{Guid.NewGuid():N}";

                // Store the new session ID in persistent storage
                await _documentRepository.AddToConversationAsync(PersistentSessionKey, "", $"session-id:{newSessionId}");
                await _documentRepository.AddToConversationAsync(newSessionId, "", "");

                // Add to cache
                _conversationCache.TryAdd(newSessionId, string.Empty);

                ServiceLogMessages.LogSessionCreated(_logger, newSessionId, null);

                return newSessionId;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, "new-session", ex);
                // Fallback: create session without persistence
                var fallbackSessionId = $"session-{Guid.NewGuid():N}";
                _conversationCache.TryAdd(fallbackSessionId, string.Empty);
                return fallbackSessionId;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets or creates a session ID automatically for conversation continuity
        /// Uses a persistent session key that survives application restarts
        /// </summary>
        private async Task<string> GetOrCreateSessionIdAsync()
        {
            const string PersistentSessionKey = "smartrag-current-session";

            // First, try to get existing session from storage
            try
            {
                var existingSessionData = await _documentRepository.GetConversationHistoryAsync(PersistentSessionKey);
                if (!string.IsNullOrEmpty(existingSessionData))
                {
                    // Extract session ID from stored data (format: "session-id:actual-session-id")
                    var lines = existingSessionData.Split('\n');
                    var sessionLine = lines.FirstOrDefault(l => l.StartsWith("session-id:"));
                    if (sessionLine != null)
                    {
                        var sessionId = sessionLine.Substring("session-id:".Length).Trim();

                        // Verify session still exists and has conversation data
                        var sessionExists = await _documentRepository.SessionExistsAsync(sessionId);
                        if (sessionExists)
                        {
                            // Get the actual conversation history from the session
                            var conversationHistory = await _documentRepository.GetConversationHistoryAsync(sessionId);

                            // Add to cache for faster access
                            _conversationCache.TryAdd(sessionId, conversationHistory ?? string.Empty);
                            ServiceLogMessages.LogSessionRetrieved(_logger, sessionId, null);
                            return sessionId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationRetrievalFailed(_logger, PersistentSessionKey, ex);
            }

            // Create new session ID
            var newSessionId = $"session-{Guid.NewGuid():N}";

            // Store the session ID in persistent storage for future retrieval
            try
            {
                await _documentRepository.AddToConversationAsync(PersistentSessionKey, "", $"session-id:{newSessionId}");
                await _documentRepository.AddToConversationAsync(newSessionId, "", "");

                // Add to cache
                _conversationCache.TryAdd(newSessionId, string.Empty);

                ServiceLogMessages.LogSessionCreated(_logger, newSessionId, null);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, newSessionId, ex);
            }

            return newSessionId;
        }

        /// <summary>
        /// Check if the query is a new conversation command
        /// </summary>
        private static bool IsNewConversationCommand(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            var lowerQuery = query.ToLowerInvariant().Trim();

            // English commands only
            if (lowerQuery == "/new" || lowerQuery == "/reset" || lowerQuery == "/clear")
                return true;

            // Commands with parameters
            if (lowerQuery.StartsWith("/new ") || lowerQuery.StartsWith("/reset ") || lowerQuery.StartsWith("/clear "))
                return true;

            return false;
        }

        /// <summary>
        /// Gets the conversation storage provider (uses ConversationStorageProvider if specified, otherwise StorageProvider with fallback)
        /// </summary>
        private StorageProvider GetConversationStorageProvider()
        {
            if (_options.ConversationStorageProvider.HasValue)
            {
                // Convert ConversationStorageProvider to StorageProvider
                switch (_options.ConversationStorageProvider.Value)
                {
                    case ConversationStorageProvider.Redis:
                        return StorageProvider.Redis;
                    case ConversationStorageProvider.Sqlite:
                        return StorageProvider.Sqlite;
                    case ConversationStorageProvider.FileSystem:
                        return StorageProvider.FileSystem;
                    case ConversationStorageProvider.InMemory:
                        return StorageProvider.InMemory;
                    default:
                        return StorageProvider.InMemory; // Fallback
                }
            }

            // If not specified, use StorageProvider but exclude Qdrant
            switch (_options.StorageProvider)
            {
                case StorageProvider.Qdrant:
                    return StorageProvider.InMemory; // Fallback for Qdrant
                default:
                    return _options.StorageProvider;
            }
        }

        /// <summary>
        /// Sanitizes user input for safe logging by removing newlines and carriage returns.
        /// </summary>
        private static string SanitizeForLog(string input)
        {
            if (input == null) return string.Empty;
            return input.Replace("\r", "").Replace("\n", "");
        }

        /// <summary>
        /// Enhanced search with intelligent filtering and name detection
        /// </summary>
        private async Task<List<DocumentChunk>> PerformBasicSearchAsync(string query, int maxResults)
        {
            var allDocuments = await _documentRepository.GetAllAsync();
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            ServiceLogMessages.LogSearchInDocuments(_logger, allDocuments.Count, allChunks.Count, null);

            // Try embedding-based search first if available
            try
            {
                var embeddingResults = await TryEmbeddingBasedSearchAsync(query, allChunks, maxResults);
                if (embeddingResults.Count > 0)
                {
                    ServiceLogMessages.LogEmbeddingSearchSuccessful(_logger, embeddingResults.Count, null);
                    return embeddingResults;
                }
            }
            catch (Exception)
            {
                ServiceLogMessages.LogEmbeddingSearchFailed(_logger, null);
            }

            // Enhanced keyword-based fallback for global content
            var queryWords = query.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength)
                .ToList();

            // Extract potential names from ORIGINAL query (not lowercase) - language agnostic
            var potentialNames = query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength && char.IsUpper(w[0]))
                .ToList();

            ServiceLogMessages.LogQueryWords(_logger, string.Join(", ", queryWords.Select(SanitizeForLog)), null);
            ServiceLogMessages.LogPotentialNames(_logger, string.Join(", ", potentialNames.Select(SanitizeForLog)), null);

            var scoredChunks = allChunks.Select(chunk =>
            {
                var score = DefaultScoreValue;
                var content = chunk.Content.ToLowerInvariant();

                // Special handling for names like "John Smith" - HIGHEST PRIORITY (language agnostic)
                if (potentialNames.Count >= MinPotentialNamesCount)
                {
                    var fullName = string.Join(" ", potentialNames);
                    if (ContainsNormalizedName(content, fullName))
                    {
                        score += FullNameMatchScoreBoost;
                        ServiceLogMessages.LogFullNameMatch(_logger, SanitizeForLog(fullName), chunk.Content.Substring(0, Math.Min(ChunkPreviewLength, chunk.Content.Length)), null);
                    }
                    else if (potentialNames.Any(name => ContainsNormalizedName(content, name)))
                    {
                        score += PartialNameMatchScoreBoost;
                        var foundNames = potentialNames.Where(name => ContainsNormalizedName(content, name)).ToList();
                        ServiceLogMessages.LogPartialNameMatches(_logger, string.Join(", ", foundNames.Select(SanitizeForLog)), chunk.Content.Substring(0, Math.Min(ChunkPreviewLength, chunk.Content.Length)), null);
                    }
                }

                // Exact word matches
                foreach (var word in queryWords)
                {
                    if (content.ToLowerInvariant().Contains(word.ToLowerInvariant()))
                        score += WordMatchScore;
                }

                // Generic content quality scoring (language and content agnostic)
                var wordCount = content.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount >= WordCountMin && wordCount <= WordCountMax) score += WordCountScoreBoost;

                // Bonus for chunks with punctuation (indicates structured content)
                var punctuationCount = content.Count(c => ".,;:!?()[]{}".Contains(c));
                if (punctuationCount >= PunctuationCountThreshold) score += PunctuationScoreBoost;

                // Bonus for chunks with numbers (often indicates factual information)
                var numberCount = content.Count(c => char.IsDigit(c));
                if (numberCount >= NumberCountThreshold) score += NumberScoreBoost;

                chunk.RelevanceScore = score;
                return chunk;
            }).ToList();

            var relevantChunks = scoredChunks
                .Where(c => c.RelevanceScore > MinWordCountThreshold)
                .OrderByDescending(c => c.RelevanceScore)
                .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                .ToList();

            ServiceLogMessages.LogRelevantChunksFound(_logger, relevantChunks.Count, null);

            // If we found chunks with names, prioritize them
            if (potentialNames.Count >= MinPotentialNamesCount)
            {
                var nameChunks = relevantChunks.Where(c =>
                    potentialNames.Any(name => c.Content.ToLowerInvariant().Contains(name.ToLowerInvariant()))).ToList();

                if (nameChunks.Count > MinNameChunksCount)
                {
                    ServiceLogMessages.LogNameChunksFound(_logger, nameChunks.Count, null);
                    return nameChunks.Take(maxResults).ToList();
                }
            }

            return relevantChunks.Take(maxResults).ToList();
        }

        private async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults, string conversationHistory = null)
        {
            var chunks = await SearchDocumentsAsync(query, maxResults);
            var context = string.Join("\n\n", chunks.Select(c => c.Content));

            // Build prompt with conversation history if available
            var historyContext = !string.IsNullOrEmpty(conversationHistory)
                ? $"\n\nPrevious conversation:\n{conversationHistory}\n"
                : "";

            // Enhanced prompt for better AI understanding with conversation history
            var enhancedPrompt = $@"You are a helpful document analysis assistant. Answer questions based on the provided document context.

IMPORTANT: 
- Carefully analyze the context
- Look for specific information that answers the question
- If you find the information, provide a clear answer
- If you cannot find it, say 'I cannot find this specific information in the provided documents'
- Be precise and use exact information from documents
- Consider the conversation history when answering follow-up questions
- If the question refers to previous information, use that context to provide a more relevant answer

{historyContext}Question: {query}

Context: {context}

Answer:";

            var answer = await _aiService.GenerateResponseAsync(enhancedPrompt, new List<string> { context });

            return new RagResponse
            {
                Query = query,
                Answer = answer,
                Sources = chunks.Select(c => new SearchSource
                {
                    DocumentId = c.DocumentId,
                    FileName = "Document",
                    RelevantContent = c.Content,
                    RelevanceScore = c.RelevanceScore ?? 0.0
                }).ToList(),
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration()
            };
        }

        private static List<DocumentChunk> ApplyDiversityAndSelect(List<DocumentChunk> chunks, int maxResults)
        {
            return chunks.Take(maxResults).ToList();
        }

        private RagConfiguration GetRagConfiguration()
        {
            return new RagConfiguration
            {
                AIProvider = _options.AIProvider.ToString(),
                StorageProvider = _options.StorageProvider.ToString(),
                Model = _configuration["AI:OpenAI:Model"] ?? "gpt-3.5-turbo"
            };
        }

        /// <summary>
        /// Try embedding-based search using configured AI provider
        /// </summary>
        private async Task<List<DocumentChunk>> TryEmbeddingBasedSearchAsync(string query, List<DocumentChunk> allChunks, int maxResults)
        {
            try
            {
                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);
                var providerKey = _options.AIProvider.ToString();
                var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    return new List<DocumentChunk>();
                }

                // Generate embedding for query
                var queryEmbedding = await aiProvider.GenerateEmbeddingAsync(query, providerConfig);
                if (queryEmbedding == null || queryEmbedding.Count == EmptyEmbeddingCount)
                {
                    return new List<DocumentChunk>();
                }

                // Calculate similarity for all chunks that have embeddings
                var chunksWithEmbeddings = allChunks.Where(c => c.Embedding != null && c.Embedding.Count > EmptyEmbeddingCount).ToList();

                if (chunksWithEmbeddings.Count == MinChunksWithEmbeddingsCount)
                {
                    return new List<DocumentChunk>();
                }

                // Enhanced semantic search with hybrid scoring
                var scoredChunks = await Task.WhenAll(chunksWithEmbeddings.Select(async chunk =>
                {
                    var semanticSimilarity = CalculateCosineSimilarity(queryEmbedding, chunk.Embedding);
                    var enhancedSemanticScore = await _semanticSearchService.CalculateEnhancedSemanticSimilarityAsync(query, chunk.Content);

                    // Hybrid scoring: Combine enhanced semantic similarity with keyword matching
                    var keywordScore = CalculateKeywordRelevanceScore(query, chunk.Content);
                    var hybridScore = (enhancedSemanticScore * HybridSemanticWeight) + (keywordScore * HybridKeywordWeight);

                    chunk.RelevanceScore = hybridScore;
                    return chunk;
                }));

                // Get top chunks based on hybrid scoring
                var relevantChunks = scoredChunks.ToList()
                    .Where(c => c.RelevanceScore > RelevanceThreshold)
                    .OrderByDescending(c => c.RelevanceScore)
                    .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                    .ToList();

                return relevantChunks
                    .OrderByDescending(c => c.RelevanceScore)
                    .Take(Math.Max(maxResults * FinalTakeMultiplier, FinalMinCount))
                    .ToList();
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogEmbeddingSearchFailedError(_logger, ex);
                return new List<DocumentChunk>();
            }
        }

        /// <summary>
        /// Calculate keyword relevance score for better hybrid search
        /// </summary>
        private static double CalculateKeywordRelevanceScore(string query, string content)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(content))
                return DefaultScoreValue;

            var queryWords = query.ToLowerInvariant()
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength)
                .ToList();

            if (queryWords.Count == MinQueryWordsCount)
                return DefaultScoreValue;

            var contentLower = content.ToLowerInvariant();
            var score = DefaultScoreValue;

            foreach (var word in queryWords)
            {
                // Exact word match (highest score)
                if (contentLower.Contains($" {word} ") || contentLower.StartsWith($"{word} ", StringComparison.OrdinalIgnoreCase) || contentLower.EndsWith($" {word}", StringComparison.OrdinalIgnoreCase))
                {
                    score += WordMatchScore;
                }
                // Partial word match (medium score)
                else if (contentLower.Contains(word))
                {
                    score += WordMatchScore / 2;
                }
            }

            // Normalize score
            return Math.Min(score / queryWords.Count, NormalizedScoreMax);
        }

        /// <summary>
        /// Calculate cosine similarity between two vectors
        /// </summary>
        private static double CalculateCosineSimilarity(List<float> a, List<float> b)
        {
            if (a == null || b == null || a.Count == MinVectorCount || b.Count == MinVectorCount) return DefaultScoreValue;

            var n = Math.Min(a.Count, b.Count);
            double dot = DefaultScore, na = DefaultScore, nb = DefaultScore;

            for (int i = 0; i < n; i++)
            {
                double va = a[i];
                double vb = b[i];
                dot += va * vb;
                na += va * va;
                nb += vb * vb;
            }

            if (na == DefaultScore || nb == DefaultScore) return DefaultScoreValue;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }

        /// <summary>
        /// Normalize text for better search matching (handles Unicode encoding issues)
        /// </summary>
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Decode Unicode escape sequences
            var decoded = System.Text.RegularExpressions.Regex.Unescape(text);

            // Normalize Unicode characters
            var normalized = decoded.Normalize(System.Text.NormalizationForm.FormC);

            // Handle common character variations for multiple languages (Turkish, German, etc.)
            var characterMappings = new Dictionary<string, string>
        {
            {"ı", "i"}, {"İ", "I"}, {"ğ", "g"}, {"Ğ", "G"},
            {"ü", "u"}, {"Ü", "U"}, {"ş", "s"}, {"Ş", "S"},
            {"ö", "o"}, {"Ö", "O"}, {"ç", "c"}, {"Ç", "C"}
        };

            foreach (var mapping in characterMappings)
            {
                normalized = normalized.Replace(mapping.Key, mapping.Value);
            }

            return normalized;
        }

        /// <summary>
        /// Check if content contains normalized name (handles encoding issues)
        /// </summary>
        private static bool ContainsNormalizedName(string content, string searchName)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(searchName))
                return false;

            var normalizedContent = NormalizeText(content);
            var normalizedSearchName = NormalizeText(searchName);

            // Try exact match first
            if (normalizedContent.ToLowerInvariant().Contains(normalizedSearchName.ToLowerInvariant()))
                return true;

            // Try partial matches for each word
            var searchWords = normalizedSearchName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var contentWords = normalizedContent.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if all search words are present in content
            return searchWords.All(searchWord =>
                contentWords.Any(contentWord =>
                    contentWord.ToLowerInvariant().Contains(searchWord.ToLowerInvariant())));
        }

        /// <summary>
        /// Ultimate language-agnostic approach: ONLY check if documents contain relevant information
        /// No word patterns, no language detection, no grammar analysis, no greeting detection
        /// Pure content-based decision making
        /// </summary>
        private async Task<bool> CanAnswerFromDocumentsAsync(string query)
        {
            try
            {
                // Step 1: Search documents for any content related to the query
                // This works regardless of the language of the query
                var searchResults = await PerformBasicSearchAsync(query, FallbackSearchMaxResults);

                if (searchResults.Count == MinSearchResultsCount)
                {
                    // No content found that matches the query in any way
                    return false;
                }

                // Step 2: Check if we found meaningful content with decent relevance
                var hasRelevantContent = searchResults.Any(chunk =>
                    chunk.RelevanceScore > RelevanceThreshold);

                if (!hasRelevantContent)
                {
                    // Found some content but it's not relevant enough
                    return false;
                }

                // Step 3: Check if the total content is substantial enough to potentially answer
                var totalContentLength = searchResults
                    .Where(c => c.RelevanceScore > RelevanceThreshold)
                    .Sum(c => c.Content.Length);

                var hasSubstantialContent = totalContentLength > MinSubstantialContentLength;

                // Final decision: If we have relevant and substantial content, use document search
                // No other checks - let the content decide!
                return hasRelevantContent && hasSubstantialContent;
            }
            catch (Exception ex)
            {
                // If there's an error, be conservative and assume it's document search
                ServiceLogMessages.LogCanAnswerFromDocumentsError(_logger, ex);
                return true;
            }
        }

        /// <summary>
        /// Handle general conversation queries with conversation history
        /// </summary>
        private async Task<string> HandleGeneralConversationAsync(string query, string conversationHistory = null)
        {
            try
            {
                // Use the configured AI provider from options
                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);
                var providerKey = _options.AIProvider.ToString();
                var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    return ChatUnavailableMessage;
                }

                // Build prompt with conversation history if available
                var historyContext = !string.IsNullOrEmpty(conversationHistory)
                    ? $"\n\nPrevious conversation:\n{conversationHistory}\n"
                    : "";

                var prompt = $@"You are a helpful AI assistant. Answer the user's question naturally and friendly.

{historyContext}User: {query}

Answer:";

                return await aiProvider.GenerateTextAsync(prompt, providerConfig);
            }
            catch (Exception)
            {
                // Log error using structured logging
                return ChatUnavailableMessage;
            }
        }

        #endregion

        #region Conversation Management

        /// <summary>
        /// Get conversation history for a session using existing storage provider
        /// </summary>
        private async Task<string> GetConversationHistoryAsync(string sessionId)
        {
            try
            {
                // Always get fresh data from storage to ensure conversation continuity
                var history = await GetConversationFromStorageAsync(sessionId);

                // Update cache with fresh data
                _conversationCache.AddOrUpdate(sessionId, history, (key, oldValue) => history);

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
                return string.Empty;
            }
        }

        /// <summary>
        /// Add conversation to storage using existing storage provider
        /// </summary>
        private async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            try
            {
                // Get current history from storage (not cache)
                var currentHistory = await GetConversationFromStorageAsync(sessionId);

                // Build new conversation entry
                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                // Limit conversation length
                if (newEntry.Length > MaxConversationLength)
                {
                    newEntry = TruncateConversation(newEntry);
                }

                // Store in persistent storage first
                await StoreConversationToStorageAsync(sessionId, newEntry);

                // Then update cache
                _conversationCache.AddOrUpdate(sessionId, newEntry, (key, oldValue) => newEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation for session {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// Get conversation from storage based on conversation storage provider
        /// </summary>
        private async Task<string> GetConversationFromStorageAsync(string sessionId)
        {
            try
            {
                var conversationStorageProvider = GetConversationStorageProvider();

                switch (conversationStorageProvider)
                {
                    case StorageProvider.Redis:
                    case StorageProvider.Sqlite:
                    case StorageProvider.InMemory:
                    case StorageProvider.FileSystem:
                        // Use the existing document repository for conversation storage
                        return await _documentRepository.GetConversationHistoryAsync(sessionId);

                    case StorageProvider.Qdrant:
                        // Qdrant doesn't support conversation history yet
                        return string.Empty;

                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationRetrievalFailed(_logger, sessionId, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Store conversation to storage based on conversation storage provider
        /// </summary>
        private async Task StoreConversationToStorageAsync(string sessionId, string conversation)
        {
            try
            {
                var conversationStorageProvider = GetConversationStorageProvider();

                switch (conversationStorageProvider)
                {
                    case StorageProvider.Redis:
                    case StorageProvider.Sqlite:
                    case StorageProvider.InMemory:
                    case StorageProvider.FileSystem:
                        // Use the existing document repository for conversation storage
                        await _documentRepository.AddToConversationAsync(sessionId, "", conversation);
                        break;

                    case StorageProvider.Qdrant:
                        // Qdrant doesn't support conversation history yet
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, sessionId, ex);
            }
        }

        /// <summary>
        /// Truncate conversation to keep only recent exchanges
        /// </summary>
        private static string TruncateConversation(string conversation)
        {
            var lines = conversation.Split('\n');
            if (lines.Length <= MinConversationLines) // Keep at least 3 exchanges (6 lines)
                return conversation;

            // Keep last 6 lines (3 exchanges)
            var result = new List<string>();
            for (int i = lines.Length - MinConversationLines; i < lines.Length; i++)
            {
                result.Add(lines[i]);
            }
            return string.Join("\n", result);
        }

        #endregion
    }
}
