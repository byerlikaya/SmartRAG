using Microsoft.Extensions.Configuration;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;

namespace SmartRAG.Services;

/// <summary>
/// Enhanced search service using configured AI provider (Anthropic) with Redis storage
/// </summary>
public class EnhancedSearchService
{
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly IDocumentRepository _documentRepository;
    private readonly IConfiguration _configuration;

    public EnhancedSearchService(
        IAIProviderFactory aiProviderFactory,
        IDocumentRepository documentRepository,
        IConfiguration configuration)
    {
        _aiProviderFactory = aiProviderFactory;
        _documentRepository = documentRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// Simple semantic search using configured AI provider (Anthropic)
    /// </summary>
    public async Task<List<DocumentChunk>> EnhancedSemanticSearchAsync(string query, int maxResults = 5)
    {
        try
        {
            var allDocuments = await _documentRepository.GetAllAsync();
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            Console.WriteLine($"[DEBUG] EnhancedSearchService: Searching in {allDocuments.Count} documents with {allChunks.Count} chunks");

            // Use configured AI provider (Anthropic)
            var anthropicConfig = _configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig == null || string.IsNullOrEmpty(anthropicConfig.ApiKey))
            {
                Console.WriteLine($"[ERROR] Anthropic configuration not found");
                return await FallbackSearchAsync(query, maxResults);
            }

            var aiProvider = _aiProviderFactory.CreateProvider(AIProvider.Anthropic);

            // Create simple search prompt
            var searchPrompt = $@"You are a search assistant. Find the most relevant document chunks for this query.

Query: {query}

Available chunks (showing first 200 characters of each):
{string.Join("\n\n", allChunks.Select((c, i) => $"Chunk {i}: {c.Content.Substring(0, Math.Min(200, c.Content.Length))}..."))}

Instructions:
1. Look for chunks that contain information related to the query
2. Focus on key names, dates, companies, and facts mentioned in the query
3. Return ONLY the chunk numbers (0, 1, 2, etc.) that are relevant, separated by commas

Return format: 0,3,7 (chunk numbers, not IDs)";

            // Try with retry logic for rate limiting
            string aiResponse = null;
            var maxRetries = 3;
            var retryDelayMs = 2000; // Start with 2 seconds

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    aiResponse = await aiProvider.GenerateTextAsync(searchPrompt, anthropicConfig);
                    break; // Success, exit retry loop
                }
                catch (Exception ex) when (ex.Message.Contains("TooManyRequests") || ex.Message.Contains("rate limit"))
                {
                    if (attempt < maxRetries - 1)
                    {
                        var delay = retryDelayMs * (int)Math.Pow(2, attempt); // Exponential backoff
                        Console.WriteLine($"[DEBUG] EnhancedSearchService: Rate limited by Anthropic, retrying in {delay}ms (attempt {attempt + 1}/{maxRetries})");
                        await Task.Delay(delay);
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] EnhancedSearchService: Anthropic rate limited after {maxRetries} attempts, using fallback");
                        throw; // Re-throw to use fallback
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] EnhancedSearchService: Anthropic failed with error: {ex.Message}");
                    throw; // Re-throw to use fallback
                }
            }

            if (!string.IsNullOrEmpty(aiResponse))
            {
                Console.WriteLine($"[DEBUG] EnhancedSearchService: AI response: {aiResponse}");

                // Parse AI response and return relevant chunks
                var parsedResults = ParseAISearchResults(aiResponse, allChunks, maxResults, query);

                if (parsedResults.Count > 0)
                {
                    Console.WriteLine($"[DEBUG] EnhancedSearchService: Successfully parsed {parsedResults.Count} chunks");
                    return parsedResults;
                }

                Console.WriteLine($"[DEBUG] EnhancedSearchService: Failed to parse results, using fallback");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] EnhancedSearchService failed: {ex.Message}, using fallback");
        }

        // Fallback to basic search
        return await FallbackSearchAsync(query, maxResults);
    }

    /// <summary>
    /// Simple RAG using configured AI provider (Anthropic)
    /// </summary>
    public async Task<RagResponse> MultiStepRAGAsync(string query, int maxResults = 5)
    {
        try
        {
            // Check if this is a general conversation query
            if (IsGeneralConversationQuery(query))
            {
                Console.WriteLine($"[DEBUG] MultiStepRAGAsync: Detected general conversation query: '{query}'");
                var chatResponse = await HandleGeneralConversationAsync(query);

                return new RagResponse
                {
                    Query = query,
                    Answer = chatResponse,
                    Sources = new List<SearchSource>(), // No sources for chat
                    SearchedAt = DateTime.UtcNow,
                    Configuration = new RagConfiguration
                    {
                        //AIProvider = "Anthropic",
                        //StorageProvider = "Chat Mode",
                        //Model = "Claude + Chat"
                    }
                };
            }

            // Step 1: Simple search for document-related queries
            var relevantChunks = await EnhancedSemanticSearchAsync(query, maxResults);

            if (relevantChunks.Count == 0)
            {
                // Last resort: basic keyword search
                relevantChunks = await FallbackSearchAsync(query, maxResults);
            }

            // Step 2: Answer Generation using Anthropic
            var answer = await GenerateAnswerWithAnthropic(query, relevantChunks);

            // Step 3: Simple Source Attribution
            var sources = relevantChunks.Select(c => new SearchSource
            {
                DocumentId = c.DocumentId,
                FileName = "Document",
                RelevantContent = c.Content.Substring(0, Math.Min(200, c.Content.Length)),
                RelevanceScore = c.RelevanceScore ?? 0.0
            }).ToList();

            return new RagResponse
            {
                Query = query,
                Answer = answer,
                Sources = sources,
                SearchedAt = DateTime.UtcNow,
                Configuration = new RagConfiguration
                {
                    //AIProvider = "Anthropic",
                    //StorageProvider = "Redis",
                    //Model = "Claude + VoyageAI"
                }
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"RAG failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate answer using Anthropic
    /// </summary>
    private async Task<string> GenerateAnswerWithAnthropic(string query, List<DocumentChunk> context)
    {
        try
        {
            var anthropicConfig = _configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig == null || string.IsNullOrEmpty(anthropicConfig.ApiKey))
            {
                throw new InvalidOperationException("Anthropic configuration not found");
            }

            var aiProvider = _aiProviderFactory.CreateProvider(AIProvider.Anthropic);

            var contextText = string.Join("\n\n---\n\n",
                context.Select(c => $"[Document Chunk]\n{c.Content}"));

            var prompt = $@"You are a helpful AI assistant. Answer the user's question based on the provided context.

Question: {query}

Context:
{contextText}

Instructions:
1. Answer the question comprehensively using information from the context
2. If information is missing, state it clearly
3. Provide structured, easy-to-understand response in the same language as the question
4. Cite specific parts of the context when possible

Answer:";

            // Try with retry logic for rate limiting
            var maxRetries = 3;
            var retryDelayMs = 2000; // Start with 2 seconds

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    return await aiProvider.GenerateTextAsync(prompt, anthropicConfig);
                }
                catch (Exception ex) when (ex.Message.Contains("TooManyRequests") || ex.Message.Contains("rate limit"))
                {
                    if (attempt < maxRetries - 1)
                    {
                        var delay = retryDelayMs * (int)Math.Pow(2, attempt); // Exponential backoff
                        Console.WriteLine($"[DEBUG] GenerateAnswerWithAnthropic: Rate limited, retrying in {delay}ms (attempt {attempt + 1}/{maxRetries})");
                        await Task.Delay(delay);
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] GenerateAnswerWithAnthropic: Rate limited after {maxRetries} attempts");
                        throw; // Re-throw to use fallback
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] GenerateAnswerWithAnthropic: Failed with error: {ex.Message}");
                    throw; // Re-throw to use fallback
                }
            }

            throw new InvalidOperationException("Unexpected error in retry loop");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to generate answer: {ex.Message}");
            return "Sorry, unable to generate answer. Please try again.";
        }
    }

    /// <summary>
    /// Parse AI search results from AI provider response
    /// </summary>
    private static List<DocumentChunk> ParseAISearchResults(string response, List<DocumentChunk> allChunks, int maxResults, string query)
    {
        try
        {
            Console.WriteLine($"[DEBUG] ParseAISearchResults: Raw response: '{response}'");

            // Try to parse chunk numbers from response
            var chunkNumbers = response.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => int.TryParse(s, out var num) ? num : -1)
                .Where(num => num >= 0 && num < allChunks.Count)
                .Take(maxResults)
                .ToList();

            Console.WriteLine($"[DEBUG] ParseAISearchResults: Parsed chunk numbers: {string.Join(", ", chunkNumbers)}");

            var results = new List<DocumentChunk>();

            foreach (var number in chunkNumbers)
            {
                if (number >= 0 && number < allChunks.Count)
                {
                    var chunk = allChunks[number];
                    results.Add(chunk);
                    Console.WriteLine($"[DEBUG] ParseAISearchResults: Found chunk {number} from document {chunk.DocumentId}");
                }
            }

            if (results.Count > 0)
            {
                Console.WriteLine($"[DEBUG] ParseAISearchResults: Successfully parsed {results.Count} chunks");
                return results;
            }

            Console.WriteLine($"[DEBUG] ParseAISearchResults: No chunks parsed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] ParseAISearchResults failed: {ex.Message}");
        }

        return new List<DocumentChunk>();
    }

    /// <summary>
    /// Fallback search when AI search fails
    /// </summary>
    private async Task<List<DocumentChunk>> FallbackSearchAsync(string query, int maxResults)
    {
        var allDocuments = await _documentRepository.GetAllAsync();
        var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

        Console.WriteLine($"[DEBUG] FallbackSearchAsync: Searching in {allDocuments.Count} documents with {allChunks.Count} chunks");

        // Try embedding-based search first if available
        try
        {
            var embeddingResults = await TryEmbeddingBasedSearchAsync(query, allChunks, maxResults);
            if (embeddingResults.Count > 0)
            {
                Console.WriteLine($"[DEBUG] FallbackSearchAsync: Embedding search successful, found {embeddingResults.Count} chunks");
                return embeddingResults;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] FallbackSearchAsync: Embedding search failed: {ex.Message}, using keyword search");
        }

        // Enhanced keyword-based fallback for global content
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToList();

        // Extract potential names from ORIGINAL query (not lowercase) - language agnostic
        var potentialNames = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && char.IsUpper(w[0]))
            .ToList();

        Console.WriteLine($"[DEBUG] FallbackSearchAsync: Query words: [{string.Join(", ", queryWords)}]");
        Console.WriteLine($"[DEBUG] FallbackSearchAsync: Potential names: [{string.Join(", ", potentialNames)}]");

        var scoredChunks = allChunks.Select(chunk =>
        {
            var score = 0.0;
            var content = chunk.Content.ToLowerInvariant();

            // Special handling for names like "John Smith" - HIGHEST PRIORITY (language agnostic)
            if (potentialNames.Count >= 2)
            {
                var fullName = string.Join(" ", potentialNames);
                if (ContainsNormalizedName(content, fullName))
                {
                    score += 200.0; // Very high weight for full name matches
                    Console.WriteLine($"[DEBUG] FallbackSearchAsync: Found FULL NAME match: '{fullName}' in chunk: {chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length))}...");
                }
                else if (potentialNames.Any(name => ContainsNormalizedName(content, name)))
                {
                    score += 100.0; // High weight for partial name matches
                    var foundNames = potentialNames.Where(name => ContainsNormalizedName(content, name)).ToList();
                    Console.WriteLine($"[DEBUG] FallbackSearchAsync: Found PARTIAL name matches: [{string.Join(", ", foundNames)}] in chunk: {chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length))}...");
                }
            }

            // Exact word matches
            foreach (var word in queryWords)
            {
                if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
                    score += 2.0; // Higher weight for word matches
            }

            // Phrase matches (for longer queries)
            var queryPhrases = query.ToLowerInvariant().Split('.', '?', '!')
                .Where(p => p.Length > 5)
                .ToList();

            foreach (var phrase in queryPhrases)
            {
                var phraseWords = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3)
                    .ToList();

                if (phraseWords.Count >= 2)
                {
                    var phraseText = string.Join(" ", phraseWords);
                    if (content.Contains(phraseText, StringComparison.OrdinalIgnoreCase))
                        score += 10.0; // Higher weight for phrase matches
                }
            }

            // Penalty for very short content (global rule)
            if (content.Length < 50)
                score -= 20.0;

            // Generic content quality scoring (language and content agnostic)
            // Score based on content structure and information density, not specific keywords

            // Bonus for chunks with good information density
            var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var avgWordLength = content.Length / Math.Max(wordCount, 1);

            // Prefer chunks with reasonable word length and count
            if (wordCount >= 10 && wordCount <= 100) score += 5.0;
            if (avgWordLength >= 4.0 && avgWordLength <= 8.0) score += 3.0;

            // Bonus for chunks with punctuation (indicates structured content)
            var punctuationCount = content.Count(c => ".,;:!?()[]{}".Contains(c));
            if (punctuationCount >= 3) score += 2.0;

            // Bonus for chunks with numbers (often indicates factual information)
            var numberCount = content.Count(c => char.IsDigit(c));
            if (numberCount >= 2) score += 2.0;

            // Bonus for chunks with mixed case (indicates proper formatting)
            var hasUpper = content.Any(c => char.IsUpper(c));
            var hasLower = content.Any(c => char.IsLower(c));
            if (hasUpper && hasLower) score += 1.0;

            chunk.RelevanceScore = score;
            return chunk;
        }).ToList();

        var relevantChunks = scoredChunks
            .Where(c => c.RelevanceScore > 0)
            .OrderByDescending(c => c.RelevanceScore)
            .Take(Math.Max(maxResults * 3, 30)) // Take more for better context
            .ToList();

        Console.WriteLine($"[DEBUG] FallbackSearchAsync: Found {relevantChunks.Count} relevant chunks with keyword search");

        // If we found chunks with names, prioritize them
        if (potentialNames.Count >= 2)
        {
            var nameChunks = relevantChunks.Where(c =>
                potentialNames.Any(name => c.Content.Contains(name, StringComparison.OrdinalIgnoreCase))).ToList();

            if (nameChunks.Count > 0)
            {
                Console.WriteLine($"[DEBUG] FallbackSearchAsync: Found {nameChunks.Count} chunks containing names, prioritizing them");
                return nameChunks.Take(maxResults).ToList();
            }
        }

        return relevantChunks;
    }

    /// <summary>
    /// Try embedding-based search using VoyageAI with intelligent filtering
    /// </summary>
    private async Task<List<DocumentChunk>> TryEmbeddingBasedSearchAsync(string query, List<DocumentChunk> allChunks, int maxResults)
    {
        try
        {
            var anthropicConfig = _configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig == null || string.IsNullOrEmpty(anthropicConfig.EmbeddingApiKey))
            {
                Console.WriteLine($"[DEBUG] Embedding search: No VoyageAI API key found");
                return new List<DocumentChunk>();
            }

            var aiProvider = _aiProviderFactory.CreateProvider(AIProvider.Anthropic);

            // Generate embedding for query
            var queryEmbedding = await aiProvider.GenerateEmbeddingAsync(query, anthropicConfig);
            if (queryEmbedding == null || queryEmbedding.Count == 0)
            {
                Console.WriteLine($"[DEBUG] Embedding search: Failed to generate query embedding");
                return new List<DocumentChunk>();
            }

            // Check which chunks already have embeddings (cached)
            var chunksWithEmbeddings = allChunks.Where(c => c.Embedding != null && c.Embedding.Count > 0).ToList();
            var chunksWithoutEmbeddings = allChunks.Where(c => c.Embedding == null || c.Embedding.Count == 0).ToList();

            Console.WriteLine($"[DEBUG] Embedding search: {chunksWithEmbeddings.Count} chunks already have embeddings, {chunksWithoutEmbeddings.Count} need new embeddings");

            // Process chunks without embeddings in batches to avoid rate limiting
            if (chunksWithoutEmbeddings.Count > 0)
            {
                var batchSize = 10;
                var totalBatches = (chunksWithoutEmbeddings.Count + batchSize - 1) / batchSize;

                Console.WriteLine($"[DEBUG] Embedding search: Processing {chunksWithoutEmbeddings.Count} chunks in {totalBatches} batches of {batchSize}");

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var batch = chunksWithoutEmbeddings.Skip(batchIndex * batchSize).Take(batchSize).ToList();

                    var batchTasks = batch.Select(async chunk =>
                    {
                        try
                        {
                            var chunkEmbedding = await aiProvider.GenerateEmbeddingAsync(chunk.Content, anthropicConfig);
                            if (chunkEmbedding != null && chunkEmbedding.Count > 0)
                            {
                                chunk.Embedding = chunkEmbedding;
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DEBUG] Failed to generate embedding for chunk {chunk.Id}: {ex.Message}");
                        }
                        return false;
                    });

                    var batchResults = await Task.WhenAll(batchTasks);
                    var successfulEmbeddings = batchResults.Count(r => r);

                    Console.WriteLine($"[DEBUG] Embedding search: Batch {batchIndex + 1}/{totalBatches}: {successfulEmbeddings}/{batchSize} successful");

                    if (batchIndex < totalBatches - 1)
                    {
                        var waitTime = 1500;
                        Console.WriteLine($"[DEBUG] Embedding search: Waiting {waitTime}ms before next batch to respect rate limits");
                        await Task.Delay(waitTime);
                    }
                }
            }

            // Calculate similarity for all chunks
            var scoredChunks = allChunks.Select(chunk =>
            {
                var similarity = 0.0;
                if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                {
                    similarity = CalculateCosineSimilarity(queryEmbedding, chunk.Embedding);
                }

                chunk.RelevanceScore = similarity;
                return chunk;
            }).ToList();

            // INTELLIGENT FILTERING: Focus on chunks that actually contain the query terms
            var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            // Extract potential names from ORIGINAL query (not lowercase) - language agnostic
            var potentialNames = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && char.IsUpper(w[0]))
                .ToList();

            Console.WriteLine($"[DEBUG] Embedding search: Query words: [{string.Join(", ", queryWords)}]");
            Console.WriteLine($"[DEBUG] Embedding search: Potential names: [{string.Join(", ", potentialNames)}]");

            // Filter chunks that actually contain query terms
            var relevantChunks = scoredChunks.Where(chunk =>
            {
                var content = chunk.Content.ToLowerInvariant();

                // Must contain at least one query word
                var hasQueryWord = queryWords.Any(word => content.Contains(word, StringComparison.OrdinalIgnoreCase));

                // If query has names, prioritize chunks with names
                if (potentialNames.Count >= 2)
                {
                    var fullName = string.Join(" ", potentialNames);
                    var hasFullName = ContainsNormalizedName(content, fullName);
                    var hasPartialName = potentialNames.Any(name => ContainsNormalizedName(content, name));

                    if (hasFullName || hasPartialName)
                    {
                        Console.WriteLine($"[DEBUG] Embedding search: Found name match in chunk: {chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length))}...");
                    }

                    return hasQueryWord && (hasFullName || hasPartialName);
                }

                return hasQueryWord;
            }).ToList();

            Console.WriteLine($"[DEBUG] Embedding search: Found {relevantChunks.Count} chunks containing query terms");

            if (relevantChunks.Count == 0)
            {
                Console.WriteLine($"[DEBUG] Embedding search: No chunks contain query terms, using similarity only");
                relevantChunks = scoredChunks.Where(c => c.RelevanceScore > 0.01).ToList();
            }

            // Sort by relevance score and take top results
            var topChunks = relevantChunks
                .OrderByDescending(c => c.RelevanceScore)
                .Take(Math.Max(maxResults * 2, 20))
                .ToList();

            Console.WriteLine($"[DEBUG] Embedding search: Selected {topChunks.Count} most relevant chunks");

            // Debug: Show what we actually found
            foreach (var chunk in topChunks.Take(5))
            {
                Console.WriteLine($"[DEBUG] Top chunk content: {chunk.Content.Substring(0, Math.Min(150, chunk.Content.Length))}...");
            }

            return topChunks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Embedding search failed: {ex.Message}");
            return new List<DocumentChunk>();
        }
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private static double CalculateCosineSimilarity(List<float> a, List<float> b)
    {
        if (a == null || b == null || a.Count == 0 || b.Count == 0) return 0.0;

        var n = Math.Min(a.Count, b.Count);
        double dot = 0, na = 0, nb = 0;

        for (int i = 0; i < n; i++)
        {
            double va = a[i];
            double vb = b[i];
            dot += va * vb;
            na += va * va;
            nb += vb * vb;
        }

        if (na == 0 || nb == 0) return 0.0;
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

        // Handle common Turkish character variations
        var turkishMappings = new Dictionary<string, string>
        {
            {"ı", "i"}, {"İ", "I"}, {"ğ", "g"}, {"Ğ", "G"},
            {"ü", "u"}, {"Ü", "U"}, {"ş", "s"}, {"Ş", "S"},
            {"ö", "o"}, {"Ö", "O"}, {"ç", "c"}, {"Ç", "C"}
        };

        foreach (var mapping in turkishMappings)
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
        if (normalizedContent.Contains(normalizedSearchName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Try partial matches for each word
        var searchWords = normalizedSearchName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contentWords = normalizedContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check if all search words are present in content
        return searchWords.All(searchWord =>
            contentWords.Any(contentWord =>
                contentWord.Contains(searchWord, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Check if query is a general conversation question (not document search)
    /// </summary>
    private static bool IsGeneralConversationQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        // Simple detection: if query has document-like structure, it's document search
        // Otherwise, it's general conversation

        var hasDocumentStructure = query.Any(char.IsDigit) ||
                                query.Contains(":") ||
                                query.Contains("/") ||
                                query.Contains("-") ||
                                query.Length > 50; // Very long queries are usually document searches

        // If it has document structure, it's document search
        // If not, it's general conversation
        return !hasDocumentStructure;
    }

    /// <summary>
    /// Handle general conversation queries
    /// </summary>
    private async Task<string> HandleGeneralConversationAsync(string query)
    {
        try
        {
            var anthropicConfig = _configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig == null || string.IsNullOrEmpty(anthropicConfig.ApiKey))
            {
                return "Sorry, I cannot chat right now. Please try again later.";
            }

            var aiProvider = _aiProviderFactory.CreateProvider(AIProvider.Anthropic);

            var prompt = $@"You are a helpful AI assistant. Answer the user's question naturally and friendly.

User: {query}

Answer:";

            return await aiProvider.GenerateTextAsync(prompt, anthropicConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] General conversation failed: {ex.Message}");
            return "Sorry, I cannot chat right now. Please try again later.";
        }
    }
}
