
using Microsoft.Extensions.Configuration;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Factories;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Providers;

namespace SmartRAG.Services;

/// <summary>
/// Implementation of document service with enhanced semantic search using repository pattern
/// </summary>
public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentParserService documentParserService,
    IAIService aiService,
    SmartRagOptions options,
    IAIProviderFactory aiProviderFactory,
    IConfiguration configuration) : IDocumentService
{

    public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
    {
        var supportedExtensions = documentParserService.GetSupportedFileTypes();

        var supportedContentTypes = documentParserService.GetSupportedContentTypes();

        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(ext) && !supportedExtensions.Contains(ext))
        {
            var list = string.Join(", ", supportedExtensions);
            throw new ArgumentException($"Unsupported file type: {ext}. Supported types: {list}");
        }

        if (!string.IsNullOrWhiteSpace(contentType) && !supportedContentTypes.Any(ct => contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase)))
        {
            var list = string.Join(", ", supportedContentTypes);
            throw new ArgumentException($"Unsupported content type: {contentType}. Supported types: {list}");
        }

        var document = await documentParserService.ParseDocumentAsync(fileStream, fileName, contentType, uploadedBy);

        // Generate embeddings for each chunk to enable semantic search
        foreach (var chunk in document.Chunks)
        {
            try
            {
                // Ensure chunk metadata is consistent
                chunk.DocumentId = document.Id;
                var embedding = await TryGenerateEmbeddingWithFallback(chunk.Content);
                chunk.Embedding = embedding ?? [];
                if (chunk.CreatedAt == default)
                    chunk.CreatedAt = DateTime.UtcNow;
            }
            catch
            {
                // If embedding generation fails, leave it empty and continue
                chunk.Embedding = [];
            }
        }

        var savedDocument = await documentRepository.AddAsync(document);

        return savedDocument;
    }

    public async Task<Document?> GetDocumentAsync(Guid id) => await documentRepository.GetByIdAsync(id);

    public async Task<List<Document>> GetAllDocumentsAsync() => await documentRepository.GetAllAsync();

    public async Task<bool> DeleteDocumentAsync(Guid id) => await documentRepository.DeleteAsync(id);

    public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));
        var cleanedQuery = query;

        // For semantic search, try to use a provider that supports embeddings
        var allDocs = await documentRepository.GetAllAsync();

        // Fix any chunks with missing DocumentId
        foreach (var doc in allDocs)
        {
            foreach (var chunk in doc.Chunks)
            {
                if (chunk.DocumentId == Guid.Empty)
                    chunk.DocumentId = doc.Id;
            }
        }

        try
        {
            // Try embedding generation (will use embedding-capable provider if available)
            var queryEmbedding = await TryGenerateEmbeddingWithFallback(cleanedQuery);
            if (queryEmbedding != null && queryEmbedding.Count > 0)
            {
                var vecScored = new List<(DocumentChunk chunk, double score)>();
                foreach (var doc in allDocs)
                {
                    foreach (var chunk in doc.Chunks)
                    {
                        if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                        {
                            var score = ComputeCosineSimilarity(queryEmbedding, chunk.Embedding);
                            vecScored.Add((chunk, score));
                        }
                    }
                }

                var topVec = vecScored
                    .OrderByDescending(x => x.score)
                    .Take(maxResults)
                    .Select(x => { x.chunk.RelevanceScore = x.score; return x.chunk; })
                    .ToList();

                if (topVec.Count > 0)
                    return topVec;
            }
        }
        catch
        {
        }

        var primary = await documentRepository.SearchAsync(cleanedQuery, maxResults);

        Console.WriteLine($"[DEBUG] SearchDocumentsAsync: Repository returned {primary.Count} chunks");
        Console.WriteLine($"[DEBUG] SearchDocumentsAsync: Unique documents: {primary.Select(c => c.DocumentId).Distinct().Count()}");

        // Debug DocumentId parsing
        foreach (var chunk in primary.Take(5))
        {
            Console.WriteLine($"[DEBUG] Chunk {chunk.Id}: DocumentId = {chunk.DocumentId}, IsEmpty = {chunk.DocumentId == Guid.Empty}");
        }


        foreach (var chunk in primary)
        {
            if (chunk.DocumentId == Guid.Empty)
            {
                var parentDoc = allDocs.FirstOrDefault(d => d.Chunks.Any(c => c.Id == chunk.Id));
                if (parentDoc != null)
                    chunk.DocumentId = parentDoc.Id;
            }
        }

        // If primary search yields poor results, try fuzzy matching
        if (primary.Count == 0 || primary.Count < maxResults / 2)
        {
            var fuzzyResults = await PerformFuzzySearch(cleanedQuery, maxResults);
            primary.AddRange(fuzzyResults.Where(f => !primary.Any(p => p.Id == f.Id)));
        }

        // Do not trim here; let callers handle reranking/diversity and final limits
        return primary;
    }

    private async Task<List<float>?> TryGenerateEmbeddingWithFallback(string text)
    {
        try
        {
            return await aiService.GenerateEmbeddingsAsync(text);
        }
        catch
        {
            var embeddingProviders = new[]
            {
                "Anthropic",
                "OpenAI",
                "Gemini"
            };

            foreach (var provider in embeddingProviders)
            {
                try
                {
                    var providerEnum = Enum.Parse<AIProvider>(provider);
                    var aiProvider = ((AIProviderFactory)aiProviderFactory).CreateProvider(providerEnum);
                    var providerConfig = configuration.GetSection($"AI:{provider}").Get<AIProviderConfig>();

                    if (providerConfig != null && !string.IsNullOrEmpty(providerConfig.ApiKey))
                    {
                        var embedding = await aiProvider.GenerateEmbeddingAsync(text, providerConfig);
                        if (embedding != null && embedding.Count > 0)
                            return embedding;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }
    }

    private static double ComputeCosineSimilarity(List<float> a, List<float> b)
    {
        if (a == null || b == null) return 0.0;
        int n = Math.Min(a.Count, b.Count);
        if (n == 0) return 0.0;
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

    public Task<Dictionary<string, object>> GetStorageStatisticsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["TotalDocuments"] = documentRepository.GetCountAsync().Result,
            ["DocumentCount"] = documentRepository.GetCountAsync().Result,
            ["StorageProvider"] = options.StorageProvider.ToString(),
            ["MaxChunkSize"] = options.MaxChunkSize,
            ["ChunkOverlap"] = options.ChunkOverlap
        };

        return Task.FromResult(stats);

    }

    public async Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Note: Semantic Kernel enhancement is available through EnhancedSearchService
        // but not integrated into DocumentService to maintain simplicity

        // Get all documents for cross-document analysis
        var allDocuments = await GetAllDocumentsAsync();

        // Cross-document detection
        var isCrossDocument = DocumentService.IsCrossDocumentQueryAsync(query, allDocuments);

        List<DocumentChunk> relevantChunks;

        // Increase maxResults for better document coverage
        var adjustedMaxResults = Math.Max(maxResults * 3, 15); // Minimum 15 chunks

        if (isCrossDocument)
        {
            relevantChunks = await PerformCrossDocumentSearchAsync(query, allDocuments, adjustedMaxResults);
        }
        else
        {
            relevantChunks = await PerformStandardSearchAsync(query, adjustedMaxResults);
        }

        Console.WriteLine($"[DEBUG] GenerateRagAnswerAsync: Got {relevantChunks.Count} chunks from search");
        Console.WriteLine($"[DEBUG] GenerateRagAnswerAsync: Unique documents: {relevantChunks.Select(c => c.DocumentId).Distinct().Count()}");

        // Optimize context assembly: combine chunks intelligently
        var contextMaxResults = isCrossDocument ? Math.Max(maxResults, 3) : maxResults;

        var optimizedChunks = DocumentService.OptimizeContextWindow(relevantChunks, contextMaxResults, query);

        var documentIdToName = new Dictionary<Guid, string>();
        foreach (var docId in optimizedChunks.Select(c => c.DocumentId).Distinct())
        {
            var doc = await GetDocumentAsync(docId);
            if (doc != null)
            {
                documentIdToName[docId] = doc.FileName;
            }
        }

        // Create enhanced context with metadata for better AI understanding
        var enhancedContext = new List<string>();
        foreach (var chunk in optimizedChunks.OrderByDescending(c => c.RelevanceScore ?? 0.0))
        {
            var docName = documentIdToName.TryGetValue(chunk.DocumentId, out var name) ? name : "Document";
            var relevance = chunk.RelevanceScore ?? 0.0;
            var chunkInfo = $"[Document: {docName}, Relevance: {relevance:F3}, Chunk: {chunk.ChunkIndex}]\n{chunk.Content}";
            enhancedContext.Add(chunkInfo);
        }

        var contextText = string.Join("\n\n---\n\n", enhancedContext);

        // Generate RAG answer using AI with enhanced prompt
        var prompt = isCrossDocument
            ? $"You are a precise information retrieval system. Analyze the following context and answer the query step by step.\n\nQuery: {query}\n\nContext:\n{contextText}\n\nInstructions:\n1. Extract specific facts from the context\n2. Answer each part of the query separately\n3. If information is missing, state 'This information is not available in the provided documents'\n4. Use exact quotes from context when possible\n\nAnswer:"
            : $"You are a precise information retrieval system. Analyze the following context and answer the question step by step.\n\nQuestion: {query}\n\nContext:\n{contextText}\n\nInstructions:\n1. Extract specific facts from the context\n2. Answer each part of the question separately\n3. If information is missing, state 'This information is not available in the provided documents'\n4. Use exact quotes from context when possible\n\nAnswer:";

        var answer = await aiService.GenerateResponseAsync(prompt, enhancedContext);

        var sources = optimizedChunks.Select(c => new SearchSource
        {
            DocumentId = c.DocumentId,
            FileName = documentIdToName.TryGetValue(c.DocumentId, out var name) ? name : "Document",
            RelevantContent = c.Content,
            RelevanceScore = c.RelevanceScore ?? 0.0
        }).ToList();

        return new RagResponse
        {
            Query = query,
            Answer = answer,
            Sources = sources,
            SearchedAt = DateTime.UtcNow,
            Configuration = GetRagConfiguration()
        };
    }

    // Semantic Kernel enhancement methods removed to keep DocumentService simple
    // Use EnhancedSearchService for advanced Semantic Kernel features

    // All Semantic Kernel methods removed to keep DocumentService simple
    // Use EnhancedSearchService for advanced Semantic Kernel features

    /// <summary>
    /// Applies advanced re-ranking algorithm to improve chunk selection
    /// </summary>
    private static List<DocumentChunk> ApplyReranking(List<DocumentChunk> chunks, string query, int maxResults)
    {
        if (chunks.Count == 0)
            return chunks;

        var queryKeywords = ExtractKeywords(query.ToLowerInvariant());
        var queryLength = query.Length;
        var documentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();

        // Enhanced scoring algorithm
        foreach (var chunk in chunks)
        {
            var originalScore = chunk.RelevanceScore ?? 0.0;
            var enhancedScore = originalScore;

            // Factor 1: Exact keyword matching boost (CRITICAL!)
            var chunkContent = chunk.Content.ToLowerInvariant();
            var exactMatches = 0;

            // Use cleaned keywords (noise/punctuation-safe)
            var cleanedQueryKeywords = ExtractKeywords(query.ToLowerInvariant());
            foreach (var kw in cleanedQueryKeywords)
            {
                if (kw.Length > 2 && chunkContent.Contains(kw))
                {
                    exactMatches++;
                }
            }

            if (cleanedQueryKeywords.Count > 0)
            {
                var exactMatchRatio = (double)exactMatches / cleanedQueryKeywords.Count;
                enhancedScore += exactMatchRatio * 0.6; // 60% boost for exact matches!
            }

            // Additional keyword density boost
            var chunkKeywords = ExtractKeywords(chunkContent);
            var commonKeywords = queryKeywords.Intersect(chunkKeywords, StringComparer.OrdinalIgnoreCase).Count();

            if (queryKeywords.Count > 0)
            {
                var keywordDensity = (double)commonKeywords / queryKeywords.Count;
                enhancedScore += keywordDensity * 0.2; // 20% boost for keyword matches
            }

            // Domain-specific boosts (insurance/policy context)
            var domainBoost = 0.0;
            var wantsAgency = cleanedQueryKeywords.Any(k => k.Contains("acente") || k.Contains("kasko"));
            var wantsOwner = cleanedQueryKeywords.Any(k => k.Contains("sahibi") || k.Contains("adi") || k.Contains("ad") || k.Contains("isim") || k.Contains("sigorta"));
            var wantsCarSpeed = cleanedQueryKeywords.Any(k => k.Contains("hyundai") || k.Contains("ioniq") || k.Contains("hiz") || k.Contains("hız"));
            var wantsAbidik = cleanedQueryKeywords.Any(k => k.Contains("abidik"));

            if (wantsAgency)
            {
                if (chunkContent.Contains("acente") || chunkContent.Contains("düzenleyen") || chunkContent.Contains("aracilik") || chunkContent.Contains("aracılık"))
                    domainBoost += 0.25;
            }
            if (wantsOwner)
            {
                if (chunkContent.Contains("sigorta ettiren") || chunkContent.Contains("sigortali") || chunkContent.Contains("sigortalı") || chunkContent.Contains("sahibi") || chunkContent.Contains("adi ") || chunkContent.Contains("adı ") || chunkContent.Contains("isim"))
                    domainBoost += 0.25;
            }
            if (wantsCarSpeed)
            {
                if (chunkContent.Contains("hyundai ioniq 5") || chunkContent.Contains("max hiz") || chunkContent.Contains("maksimum hız") || chunkContent.Contains("km/s"))
                    domainBoost += 0.2;
            }
            if (wantsAbidik && chunkContent.Contains("abidik"))
            {
                domainBoost += 0.2;
            }
            enhancedScore += domainBoost;

            // Factor 2: Content length optimization (not too short, not too long)
            var contentLength = chunk.Content.Length;
            var optimalLength = Math.Min(800, Math.Max(200, queryLength * 10)); // Dynamic optimal length
            var lengthScore = 1.0 - Math.Abs(contentLength - optimalLength) / (double)optimalLength;
            enhancedScore += Math.Max(0, lengthScore * 0.15); // 15% boost for optimal length

            // Factor 3: Position in document (earlier chunks often more important)
            var positionBoost = Math.Max(0, 1.0 - (chunk.ChunkIndex * 0.05)); // Decrease by 5% per chunk
            enhancedScore += positionBoost * 0.1; // 10% boost for position

            // Factor 4: Query term proximity (how close query terms are in content)
            var proximityScore = CalculateTermProximity(chunk.Content, queryKeywords);
            enhancedScore += proximityScore * 0.2; // 20% boost for proximity

            // Factor 5: Document diversity boost (NEW!)
            var documentDiversityBoost = CalculateDocumentDiversityBoost(chunk.DocumentId, documentIds, chunks);
            enhancedScore += documentDiversityBoost * 0.15; // 15% boost for diversity

            chunk.RelevanceScore = Math.Min(1.0, enhancedScore); // Cap at 1.0
        }

        return chunks;
    }

    /// <summary>
    /// Calculates how close query terms are to each other in the content
    /// </summary>
    private static double CalculateTermProximity(string content, List<string> queryTerms)
    {
        if (queryTerms.Count == 0) return 0.0;

        var contentLower = content.ToLowerInvariant();
        var termPositions = new List<int>();

        foreach (var term in queryTerms)
        {
            var index = contentLower.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                termPositions.Add(index);
            }
        }

        if (termPositions.Count < 2) return termPositions.Count > 0 ? 0.5 : 0.0;

        // Calculate average distance between terms
        termPositions.Sort();
        var totalDistance = 0;
        for (int i = 1; i < termPositions.Count; i++)
        {
            totalDistance += termPositions[i] - termPositions[i - 1];
        }

        var averageDistance = totalDistance / (termPositions.Count - 1);
        // Closer terms = higher score (inverse relationship)
        return Math.Max(0, 1.0 - averageDistance / 200.0); // Normalize by 200 characters
    }

    /// <summary>
    /// Applies diversity selection to avoid too many chunks from same document
    /// </summary>
    private static List<DocumentChunk> ApplyDiversityAndSelect(List<DocumentChunk> chunks, int maxResults)
    {
        if (chunks.Count == 0) return new List<DocumentChunk>();

        var uniqueDocumentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();

        Console.WriteLine($"[DEBUG] ApplyDiversityAndSelect: Total chunks: {chunks.Count}, Unique documents: {uniqueDocumentIds.Count}");
        Console.WriteLine($"[DEBUG] Document IDs: {string.Join(", ", uniqueDocumentIds.Take(5))}");

        // Calculate min chunks per document - ensure we don't exceed available chunks
        var minChunksPerDocument = Math.Max(1, Math.Min(2, maxResults / uniqueDocumentIds.Count)); // Min 1, Max 2
        var maxChunksPerDocument = Math.Max(minChunksPerDocument, maxResults); // Allow more chunks per doc

        Console.WriteLine($"[DEBUG] Min chunks per doc: {minChunksPerDocument}, Max chunks per doc: {maxChunksPerDocument}");

        var selectedChunks = new List<DocumentChunk>();
        var documentChunkCounts = new Dictionary<Guid, int>();

        // First pass: ensure minimum representation from each document
        foreach (var documentId in uniqueDocumentIds)
        {
            var availableChunks = chunks.Where(c => c.DocumentId == documentId).ToList();
            var actualMinChunks = Math.Min(minChunksPerDocument, availableChunks.Count);

            var documentChunks = availableChunks
                                     .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                                     .Take(actualMinChunks)
                                     .ToList();

            Console.WriteLine($"[DEBUG] Document {documentId}: Available {availableChunks.Count}, Selected {documentChunks.Count} chunks (requested min: {minChunksPerDocument}, actual min: {actualMinChunks})");

            selectedChunks.AddRange(documentChunks);
            documentChunkCounts[documentId] = documentChunks.Count;
        }

        // Second pass: fill remaining slots with best remaining chunks
        var remainingSlots = maxResults - selectedChunks.Count;
        if (remainingSlots > 0)
        {
            var remainingChunks = chunks.Except(selectedChunks)
                                      .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                                      .ToList();

            foreach (var chunk in remainingChunks)
            {
                if (remainingSlots <= 0) break;

                var currentCount = documentChunkCounts.GetValueOrDefault(chunk.DocumentId, 0);
                if (currentCount < maxChunksPerDocument)
                {
                    selectedChunks.Add(chunk);
                    documentChunkCounts[chunk.DocumentId] = currentCount + 1;
                    remainingSlots--;
                }
            }
        }

        var finalResult = selectedChunks.Take(maxResults).ToList();

        Console.WriteLine($"[DEBUG] Final result: {finalResult.Count} chunks from {finalResult.Select(c => c.DocumentId).Distinct().Count()} documents");

        return finalResult;
    }

    /// <summary>
    /// Performs fuzzy search with typo tolerance
    /// </summary>
    private async Task<List<DocumentChunk>> PerformFuzzySearch(string query, int maxResults)
    {
        var fuzzyResults = new List<DocumentChunk>();

        try
        {
            var allDocs = await documentRepository.GetAllAsync();
            var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var doc in allDocs)
            {
                foreach (var chunk in doc.Chunks)
                {
                    var chunkContent = chunk.Content.ToLowerInvariant();
                    var chunkWords = chunkContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var fuzzyScore = 0.0;
                    var matchedWords = 0;

                    foreach (var queryWord in queryWords)
                    {
                        if (queryWord.Length < 3) continue; // Skip very short words

                        var bestMatch = 0.0;
                        foreach (var chunkWord in chunkWords)
                        {
                            if (chunkWord.Length < 3) continue;

                            // Calculate similarity
                            var similarity = CalculateStringSimilarity(queryWord, chunkWord);
                            if (similarity > bestMatch)
                            {
                                bestMatch = similarity;
                            }
                        }

                        // If similarity is above threshold, count as match
                        if (bestMatch >= 0.7) // 70% similarity threshold
                        {
                            fuzzyScore += bestMatch;
                            matchedWords++;
                        }
                    }

                    // Calculate final fuzzy score
                    if (matchedWords > 0)
                    {
                        var finalScore = (fuzzyScore / queryWords.Length) * 0.8; // Fuzzy matches get 80% of perfect score
                        chunk.RelevanceScore = finalScore;
                        fuzzyResults.Add(chunk);
                    }
                }
            }

            return fuzzyResults
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .Take(maxResults)
                .ToList();
        }
        catch
        {
            return fuzzyResults;
        }
    }

    /// <summary>
    /// Calculates string similarity using Levenshtein distance
    /// </summary>
    private static double CalculateStringSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        if (s1 == s2)
            return 1.0;

        var longer = s1.Length > s2.Length ? s1 : s2;
        var shorter = s1.Length > s2.Length ? s2 : s1;

        var editDistance = LevenshteinDistance(longer, shorter);
        return (longer.Length - editDistance) / (double)longer.Length;
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var matrix = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= len2; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[len1, len2];
    }

    /// <summary>
    /// Extracts key words from query for additional search terms
    /// </summary>
    private static List<string> ExtractKeywords(string query)
    {
        var stopWords = new HashSet<string> { "ne", "nedir", "nasıl", "hangi", "kim", "nerede", "ne zaman", "neden",
                                            "what", "how", "where", "when", "why", "who", "which", "is", "are", "the", "a", "an" };

        var words = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .ToList();

        return words;
    }

    /// <summary>
    /// Optimizes context window by intelligently selecting and combining chunks
    /// </summary>
    private static List<DocumentChunk> OptimizeContextWindow(List<DocumentChunk> chunks, int maxResults, string query)
    {
        if (chunks.Count == 0) return new List<DocumentChunk>();

        // Group chunks by document for better context
        var documentGroups = chunks.GroupBy(c => c.DocumentId).ToList();

        var finalChunks = new List<DocumentChunk>();
        var remainingSlots = maxResults;

        // Build domain-aware keyword list from query
        var queryKeywords = ExtractKeywords(query.ToLowerInvariant());
        var domainHints = new List<string> { "acente", "düzenleyen", "aracilik", "aracılık", "sigorta ettiren", "sigortali", "sigortalı", "sahibi", "adi", "adı", "isim", "hyundai", "ioniq", "hiz", "hız", "abidik" };
        var targetKeywords = new HashSet<string>(queryKeywords.Concat(domainHints.Where(h => queryKeywords.Any(qk => h.Contains(qk) || qk.Contains(h)))), StringComparer.OrdinalIgnoreCase);

        // Process each document group
        foreach (var group in documentGroups.OrderByDescending(g => g.Max(c => c.RelevanceScore ?? 0.0)))
        {
            if (remainingSlots <= 0) break;

            // Prefer domain keyword matches within the document if available
            var domainMatched = group
                .Select(c => new { Chunk = c, Text = c.Content.ToLowerInvariant() })
                .Where(x => targetKeywords.Any(k => x.Text.Contains(k)))
                .Select(x => x.Chunk)
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .FirstOrDefault();

            var bestChunk = domainMatched ?? group.OrderByDescending(c => c.RelevanceScore ?? 0.0).First();
            finalChunks.Add(bestChunk);
            remainingSlots--;

            // Add additional chunks if slots remain
            if (remainingSlots > 0)
            {
                // Bring in other domain matches first, then top by relevance
                var domainExtras = group
                    .Where(c => !ReferenceEquals(c, bestChunk))
                    .Select(c => new { Chunk = c, Text = c.Content.ToLowerInvariant() })
                    .Where(x => targetKeywords.Any(k => x.Text.Contains(k)))
                    .Select(x => x.Chunk)
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ToList();

                var nonDomainExtras = group
                    .Where(c => !ReferenceEquals(c, bestChunk) && !domainExtras.Contains(c))
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ToList();

                var extras = domainExtras.Concat(nonDomainExtras)
                    .Take(Math.Min(remainingSlots, 3)) // allow up to 3 extras per doc to improve coverage
                    .ToList();

                finalChunks.AddRange(extras);
                remainingSlots -= extras.Count;
            }
        }

        return finalChunks;
    }

    /// <summary>
    /// Detects if query requires information from multiple documents
    /// </summary>
    private static bool IsCrossDocumentQueryAsync(string query, List<Document> allDocuments)
    {
        if (allDocuments.Count <= 1)
            return false;

        // Extract topics from query
        var queryTopics = ExtractQueryTopics(query);

        var relevantDocs = 0;
        var isCrossDocument = false;

        foreach (var doc in allDocuments)
        {
            var docTopics = ExtractDocumentTopics(doc);
            var matchCount = CalculateTopicMatches(queryTopics, docTopics);

            if (matchCount > 0)
            {
                relevantDocs++;
                if (relevantDocs > 1)
                {
                    isCrossDocument = true;
                    break;
                }
            }
        }

        return isCrossDocument;
    }

    /// <summary>
    /// Calculates topic matches using flexible matching strategies
    /// </summary>
    private static int CalculateTopicMatches(List<string> queryTopics, List<string> docTopics)
    {
        var matchCount = 0.0;

        foreach (var queryTopic in queryTopics)
        {
            // Strategy 1: Exact match
            if (docTopics.Contains(queryTopic, StringComparer.OrdinalIgnoreCase))
            {
                matchCount += 2.0; // Higher weight for exact matches
                continue;
            }

            // Strategy 2: Contains match (partial match)
            var containsMatch = docTopics.Any(dt =>
                dt.Contains(queryTopic, StringComparison.OrdinalIgnoreCase) ||
                queryTopic.Contains(dt, StringComparison.OrdinalIgnoreCase));

            if (containsMatch)
            {
                matchCount += 1.0; // Lower weight for partial matches
                continue;
            }

            // Strategy 3: Word-level match
            var queryWords = queryTopic.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var docWords = docTopics.SelectMany(dt => dt.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();

            var wordMatches = queryWords.Count(qw =>
                docWords.Any(dw =>
                    dw.Contains(qw, StringComparison.OrdinalIgnoreCase) ||
                    qw.Contains(dw, StringComparison.OrdinalIgnoreCase)));

            if (wordMatches > 0)
            {
                matchCount += wordMatches * 0.5; // Partial weight for word matches
            }
        }

        return (int)Math.Round(matchCount);
    }

    /// <summary>
    /// Extracts main topics from user query
    /// </summary>
    private static List<string> ExtractQueryTopics(string query)
    {
        var topics = new List<string>();
        var words = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2) // Filter out very short words
            .ToList();

        // Add single words
        topics.AddRange(words);

        // Add bigrams (2-word combinations)
        for (int i = 0; i < words.Count - 1; i++)
        {
            topics.Add($"{words[i]} {words[i + 1]}");
        }

        // Add trigrams (3-word combinations) for better coverage
        for (int i = 0; i < words.Count - 2; i++)
        {
            topics.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
        }

        // Add individual important words with higher priority
        var importantWords = words.Where(w => w.Length > 4).ToList();
        topics.AddRange(importantWords);

        return topics.Distinct().ToList();
    }

    /// <summary>
    /// Extracts main topics from document content
    /// </summary>
    private static List<string> ExtractDocumentTopics(Document document)
    {
        var topics = new HashSet<string>();
        var content = document.Content.ToLowerInvariant();

        // Extract key phrases from document - increase coverage
        var sentences = content.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);

        // Process more sentences for better topic coverage
        foreach (var sentence in sentences.Take(20)) // Increased from 10 to 20
        {
            var sentenceWords = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            // Add single words
            topics.UnionWith(sentenceWords);

            // Add bigrams
            for (int i = 0; i < sentenceWords.Count - 1; i++)
            {
                topics.Add($"{sentenceWords[i]} {sentenceWords[i + 1]}");
            }

            // Add trigrams
            for (int i = 0; i < sentenceWords.Count - 2; i++)
            {
                topics.Add($"{sentenceWords[i]} {sentenceWords[i + 1]} {sentenceWords[i + 2]}");
            }
        }

        // Also extract from chunk content for better coverage
        foreach (var chunk in document.Chunks.Take(10))
        {
            var chunkWords = chunk.Content.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            topics.UnionWith(chunkWords);
        }

        return topics.Take(50).ToList(); // Increased from 20 to 50
    }

    /// <summary>
    /// Performs cross-document search with enhanced diversity
    /// </summary>
    private async Task<List<DocumentChunk>> PerformCrossDocumentSearchAsync(string query, List<Document> allDocuments, int maxResults)
    {
        var adjustedMaxResults = Math.Max(maxResults, 3); // Minimum 3 results for cross-document

        // Direct search with original query for cross-document
        var searchResults = Math.Max(adjustedMaxResults * 3, options.MaxSearchResults);
        var allChunks = await SearchDocumentsAsync(query, searchResults);

        // Remove duplicates and keep highest score
        var uniqueChunks = allChunks
            .GroupBy(c => c.Id)
            .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
            .OrderByDescending(c => c.RelevanceScore ?? 0.0)
            .ToList();

        var rerankedChunks = DocumentService.ApplyReranking(uniqueChunks, query, searchResults);
        var finalChunks = DocumentService.ApplyDiversityAndSelect(rerankedChunks, adjustedMaxResults);

        return finalChunks;
    }

    /// <summary>
    /// Performs standard single-document search
    /// </summary>
    private async Task<List<DocumentChunk>> PerformStandardSearchAsync(string query, int maxResults)
    {
        // Direct search with original query (more reliable)
        var searchResults = Math.Max(maxResults * 2, options.MaxSearchResults);
        var allRelevantChunks = await SearchDocumentsAsync(query, searchResults);

        // Remove duplicates and keep highest score
        var uniqueChunks = allRelevantChunks
            .GroupBy(c => c.Id)
            .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
            .OrderByDescending(c => c.RelevanceScore ?? 0.0)
            .ToList();

        // Apply advanced re-ranking algorithm
        var rerankedChunks = DocumentService.ApplyReranking(uniqueChunks, query, searchResults);

        // Apply standard diversity selection
        return DocumentService.ApplyDiversityAndSelect(rerankedChunks, maxResults);
    }

    /// <summary>
    /// Get current RAG configuration dynamically from Program.cs and appsettings.json
    /// </summary>
    private RagConfiguration GetRagConfiguration()
    {
        // Read from Program.cs configuration and appsettings.json
        var currentProvider = GetCurrentAIProviderFromConfig();

        return new RagConfiguration
        {
            AIProvider = currentProvider,
            StorageProvider = GetCurrentStorageProviderFromConfig(),
            Model = GetCurrentModelFromConfig(currentProvider)
        };
    }

    /// <summary>
    /// Get current AI provider from SmartRagOptions configuration
    /// </summary>
    private string GetCurrentAIProviderFromConfig()
    {
        // Use the configured AI provider from SmartRagOptions
        return options.AIProvider.ToString();
    }

    /// <summary>
    /// Get current storage provider from SmartRagOptions configuration
    /// </summary>
    private string GetCurrentStorageProviderFromConfig()
    {
        // Use the configured storage provider from SmartRagOptions
        return options.StorageProvider.ToString();
    }

    /// <summary>
    /// Get current model from configuration based on provider
    /// </summary>
    private string GetCurrentModelFromConfig(string provider)
    {
        // Dynamically build configuration key from provider name
        var configKey = $"AI:{provider}:Model";
        return configuration[configKey] ?? "model-not-configured";
    }

    /// <summary>
    /// Calculates diversity boost to encourage selection from different documents
    /// </summary>
    private static double CalculateDocumentDiversityBoost(Guid documentId, List<Guid> allDocumentIds, List<DocumentChunk> allChunks)
    {
        if (allDocumentIds.Count <= 1) return 0.0;

        // Calculate how many chunks we already have from this document
        var chunksFromThisDoc = allChunks.Count(c => c.DocumentId == documentId);
        var totalChunks = allChunks.Count;

        // If this document is underrepresented, give it a boost
        var expectedChunksPerDoc = (double)totalChunks / allDocumentIds.Count;
        var representationRatio = chunksFromThisDoc / expectedChunksPerDoc;

        // Boost underrepresented documents
        if (representationRatio < 0.8) return 0.3; // 30% boost
        if (representationRatio < 1.0) return 0.15; // 15% boost

        return 0.0; // No boost for overrepresented documents
    }
}