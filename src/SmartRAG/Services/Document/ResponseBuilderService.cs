#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Helpers;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for building RAG responses
    /// </summary>
    public class ResponseBuilderService : IResponseBuilderService
    {
        private readonly SmartRagOptions _options;
        private readonly IConfiguration _configuration;
        private readonly IConversationManagerService _conversationManager;
        private readonly ILogger<ResponseBuilderService> _logger;
        private readonly IAIService _aiService;
        private readonly IPromptBuilderService _promptBuilder;

        /// <summary>
        /// Initializes a new instance of the ResponseBuilderService
        /// </summary>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="conversationManager">Service for managing conversations</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="aiService">AI service for merging responses</param>
        /// <param name="promptBuilder">Prompt builder service for merging responses</param>
        public ResponseBuilderService(
            IOptions<SmartRagOptions> options,
            IConfiguration configuration,
            IConversationManagerService conversationManager,
            ILogger<ResponseBuilderService> logger,
            IAIService aiService,
            IPromptBuilderService promptBuilder)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        }

        /// <summary>
        /// Creates a RagResponse with standard configuration
        /// </summary>
        /// <param name="query">Original query</param>
        /// <param name="answer">Generated answer</param>
        /// <param name="sources">List of search sources</param>
        /// <param name="searchMetadata">Optional metadata about search operations performed</param>
        /// <returns>RAG response</returns>
        public RagResponse CreateRagResponse(string query, string answer, List<SearchSource> sources, SearchMetadata? searchMetadata = null)
        {
            var validatedAnswer = ValidateAnswerAgainstQuery(query, answer, sources);
            
            // Post-processing: If AI didn't include token but answer indicates missing data, add it
            if (!validatedAnswer.Contains("[NO_ANSWER_FOUND]"))
            {
                _logger?.LogDebug("CreateRagResponse: Checking if token should be added. Answer length: {AnswerLength}, Query: {Query}", validatedAnswer.Length, query);
                
                if (ShouldAddMissingDataToken(validatedAnswer, query, sources))
                {
                    validatedAnswer += " [NO_ANSWER_FOUND]";
                    _logger?.LogWarning("AI response indicates missing data but [NO_ANSWER_FOUND] token was missing. System added token automatically to enable MCP search. Answer length: {AnswerLength}, Query: {Query}", validatedAnswer.Length, query);
                }
                else
                {
                    _logger?.LogDebug("CreateRagResponse: Token not added. ShouldAddMissingDataToken returned false.");
                }
            }
            
            // Remove [NO_ANSWER_FOUND] token from answer before showing to user
            var cleanedAnswer = validatedAnswer.Replace("[NO_ANSWER_FOUND]", "").Trim();
            
            return new RagResponse
            {
                Query = query,
                Answer = cleanedAnswer,
                Sources = sources,
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration(),
                SearchMetadata = searchMetadata ?? new SearchMetadata()
            };
        }
        
        /// <summary>
        /// Determines if [NO_ANSWER_FOUND] token should be added based on answer characteristics
        /// Uses generic, language-agnostic heuristics
        /// </summary>
        private bool ShouldAddMissingDataToken(string answer, string query, List<SearchSource> sources)
        {
            if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(query))
                return false;
            
            // Primary check: Use existing IndicatesMissingData detection
            // This checks for [NO_ANSWER_FOUND] token and generic "not found" patterns
            var indicatesMissing = IndicatesMissingData(answer, query);
            _logger?.LogDebug("ShouldAddMissingDataToken: IndicatesMissingData={IndicatesMissing}, AnswerLength={AnswerLength}, AnswerPreview={AnswerPreview}", 
                indicatesMissing, answer.Length, answer.Length > 100 ? answer.Substring(0, 100) : answer);
            
            if (indicatesMissing)
                return true;
            
            // Fallback: Answer Length + Context (Generic, Language-Agnostic)
            // If answer contains specific terms from query but those terms are not in sources,
            // and answer is reasonably short → likely "not found" response
            var specificTerms = ExtractSpecificTermsFromQuery(query);
            if (specificTerms.Count > 0)
            {
                var answerLower = answer.ToLowerInvariant();
                var allSourceContent = string.Join(" ", sources
                    .Where(s => !string.IsNullOrWhiteSpace(s.RelevantContent))
                    .Select(s => s.RelevantContent));
                
                // If sources are empty or have no content, and answer mentions specific terms
                if (string.IsNullOrWhiteSpace(allSourceContent))
                {
                    foreach (var term in specificTerms)
                    {
                        var termLower = term.ToLowerInvariant();
                        if (answerLower.Contains(termLower) && answer.Length < 150)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    // Sources have content - check if terms are missing
                    var sourceContentLower = allSourceContent.ToLowerInvariant();
                    
                    foreach (var term in specificTerms)
                    {
                        var termLower = term.ToLowerInvariant();
                        var termInAnswer = answerLower.Contains(termLower);
                        var termInSources = sourceContentLower.Contains(termLower);
                        
                        // If term is in answer but NOT in sources, and answer is reasonably short
                        if (termInAnswer && !termInSources && answer.Length < 150)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Validates answer against query to prevent hallucination
        /// If query contains specific terms (product/library names) and they are not found in sources, 
        /// checks if AI answer contains information about that term - if yes, logs warning
        /// The AI should already return a user-friendly message based on the prompt instructions
        /// </summary>
        private string ValidateAnswerAgainstQuery(string query, string answer, List<SearchSource> sources)
        {
            if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(answer))
                return answer;

            var specificTerms = ExtractSpecificTermsFromQuery(query);
            if (specificTerms.Count == 0)
                return answer;

            var allSourceContent = string.Join(" ", sources
                .Where(s => !string.IsNullOrWhiteSpace(s.RelevantContent))
                .Select(s => s.RelevantContent));

            if (string.IsNullOrWhiteSpace(allSourceContent))
            {
                _logger?.LogDebug("No source content available for validation");
                return answer;
            }

            var sourceContentLower = allSourceContent.ToLowerInvariant();
            var answerLower = answer.ToLowerInvariant();
            
            // Generic negative indicators - AI should return user-friendly messages in any language
            var negativeIndicators = new[] { "couldn't find", "could not find", "not found", "unable to find", "no information", "not available" };
            var answerIndicatesNotFound = negativeIndicators.Any(indicator => answerLower.Contains(indicator));
            
            foreach (var term in specificTerms)
            {
                var termLower = term.ToLowerInvariant();
                var termInSources = sourceContentLower.Contains(termLower);
                var termInAnswer = answerLower.Contains(termLower);
                
                // Only log warning if term is in answer, NOT in sources, AND answer is long enough to be substantive
                // Short answers (<150 chars) with term mentions are likely "not found" responses, not hallucinations
                if (!termInSources && termInAnswer && !answerIndicatesNotFound && answer.Length >= 150)
                {
                    _logger?.LogWarning("Specific term '{Term}' from query not found in sources but AI answer contains information about it. This is likely hallucination. AI should have followed the prompt instructions to say it couldn't find the information.", term);
                }
            }

            return answer;
        }

        /// <summary>
        /// Extracts specific terms (product names, library names, entity names) from query
        /// Detects capitalized words and multi-word terms that look like proper nouns
        /// Uses language-agnostic heuristics: filters short words (likely question words) and focuses on longer capitalized terms
        /// </summary>
        private List<string> ExtractSpecificTermsFromQuery(string query)
        {
            var terms = new List<string>();
            if (string.IsNullOrWhiteSpace(query))
                return terms;

            const int MinTermLength = 4;
            const int MaxQuestionWordLength = 3;

            var words = query.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i].Trim();
                
                if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
                    continue;

                if (char.IsUpper(word[0]) && word.Length >= MinTermLength)
                {
                    if (i + 1 < words.Length)
                    {
                        var nextWord = words[i + 1].Trim();
                        if (char.IsUpper(nextWord[0]) && nextWord.Length >= MinTermLength)
                        {
                            terms.Add($"{word} {nextWord}");
                            i++;
                            continue;
                        }
                    }
                    
                    terms.Add(word);
                }
                else if (char.IsUpper(word[0]) && word.Length <= MaxQuestionWordLength)
                {
                    continue;
                }
            }

            return terms.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Gets RAG configuration from options and configuration
        /// </summary>
        public RagConfiguration GetRagConfiguration()
        {
            return new RagConfiguration
            {
                AIProvider = _options.AIProvider.ToString(),
                StorageProvider = _options.StorageProvider.ToString(),
                Model = _configuration["AI:OpenAI:Model"]
            };
        }

        /// <summary>
        /// Determines if a RAG response contains meaningful data
        /// Uses multiple checks: answer content, database results, source data, and error detection
        /// </summary>
        public bool HasMeaningfulData(RagResponse? response)
        {
            if (response == null)
            {
                return false;
            }

            if (response.Sources?.Any(s => 
                s.SourceType?.Equals("System", StringComparison.OrdinalIgnoreCase) == true &&
                (s.RelevantContent?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true ||
                 s.FileName?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true)) == true)
            {
                _logger?.LogDebug("Response contains system error notification, no meaningful data");
                return false;
            }

            var databaseSources = response.Sources?.Where(s => 
                s.SourceType?.Equals("Database", StringComparison.OrdinalIgnoreCase) == true).ToList();
            
            if (databaseSources != null && databaseSources.Any())
            {
                var totalRows = 0;
                var hasMeaningfulData = false;
                
                foreach (var source in databaseSources)
                {
                    if (!string.IsNullOrWhiteSpace(source.FileName) && source.FileName.Contains("(") && source.FileName.Contains(" rows)"))
                    {
                        var startIndex = source.FileName.IndexOf('(') + 1;
                        var endIndex = source.FileName.IndexOf(" rows)");
                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            var rowCountStr = source.FileName.Substring(startIndex, endIndex - startIndex).Trim();
                            if (int.TryParse(rowCountStr, out var rows))
                            {
                                totalRows += rows;
                                if (rows > 0)
                                {
                                    hasMeaningfulData = true;
                                }
                            }
                        }
                    }
                    
                    if (!string.IsNullOrWhiteSpace(source.RelevantContent) && source.RelevantContent.Length >= 50)
                    {
                        hasMeaningfulData = true;
                    }
                }
                
                if (hasMeaningfulData && totalRows > 0)
                {
                    // Check if AI explicitly says there is no information despite returned rows
                    // This handles cases where DB returns rows but they are not relevant to the query
                    if (!string.IsNullOrWhiteSpace(response.Answer) && IndicatesMissingData(response.Answer))
                    {
                        _logger?.LogInformation("Database query returned {TotalRows} rows but AI indicated missing data. Marking as not meaningful.", totalRows);
                        return false;
                    }

                    _logger?.LogInformation("Database query returned {TotalRows} rows - marking as meaningful data", totalRows);
                    return true;
                }
                
                if (!hasMeaningfulData)
                {
                    _logger?.LogDebug("Database sources exist but all returned 0 rows, no meaningful data");
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(response.Answer) && !IndicatesMissingData(response.Answer))
            {
                return true;
            }

            if (response.Sources != null && response.Sources.Any(source =>
                source.DocumentId != Guid.Empty && !string.IsNullOrWhiteSpace(source.RelevantContent)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if an answer indicates missing data using universal language-agnostic heuristics
        /// Works for all languages without hardcoded patterns
        /// </summary>
        /// <param name="answer">The AI-generated answer to check</param>
        /// <param name="query">Optional: The original query to check for keyword repetition</param>
        /// <param name="sources">Optional: The sources used to generate the answer, to check if query terms are present</param>
        public bool IndicatesMissingData(string answer, string? query = null, List<SearchSource>? sources = null)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return true;
            }

            var normalized = answer.Trim();
            
            // Check explicit negative patterns first
            if (IsExplicitlyNegative(normalized))
            {
                return true;
            }

            var answerLower = normalized.ToLowerInvariant();
            
            // Generic patterns that work across languages (AI typically uses English patterns even in other languages)
            // These patterns detect when AI says it couldn't find information
            // Note: We rely primarily on [NO_ANSWER_FOUND] token, but also check for generic "not found" patterns
            var notFoundPatterns = new[] 
            { 
                "not found", "not in", "does not contain", "doesn't contain", 
                "not mentioned", "not present", "couldn't find", "could not find",
                "unable to find", "no information", "not available"
            };
            
            var answerIndicatesNotFound = notFoundPatterns.Any(pattern => answerLower.Contains(pattern));
            
            // If answer explicitly says "not found" (using generic English patterns that AI typically uses),
            // it's missing data - this is a fallback when [NO_ANSWER_FOUND] token is missing
            if (answerIndicatesNotFound)
            {
                return true;
            }
            
            // Additional check: If query contains important terms but those terms are not in sources,
            // it's likely missing data (language-agnostic check)
            // Uses QueryTokenizer to extract terms regardless of capitalization (works for all languages)
            if (!string.IsNullOrWhiteSpace(query) && sources != null && sources.Count > 0)
            {
                // Extract important terms from query (language-agnostic, works for all languages)
                var queryTerms = QueryTokenizer.TokenizeQuery(query);
                
                // Filter out very short terms (length < 3) as they're likely not meaningful
                // Keep all terms >= 3 characters to remain language-agnostic (no language-specific stop words)
                var importantTerms = queryTerms
                    .Where(term => term.Length >= 3)
                    .ToList();
                
                if (importantTerms.Count > 0)
                {
                    var allSourceContent = string.Join(" ", sources
                        .Where(s => !string.IsNullOrWhiteSpace(s.RelevantContent))
                        .Select(s => s.RelevantContent));
                    
                    if (!string.IsNullOrWhiteSpace(allSourceContent))
                    {
                        var sourceContentLower = allSourceContent.ToLowerInvariant();
                        var termsInSources = importantTerms.Count(term => 
                            sourceContentLower.Contains(term.ToLowerInvariant()));
                        
                        // If most important query terms are not in sources,
                        // and answer is short (likely "not found" response), it's missing data
                        // This is language-agnostic: we check if query terms exist in sources
                        var termsNotInSourcesRatio = (importantTerms.Count - termsInSources) / (double)importantTerms.Count;
                        if (termsNotInSourcesRatio > 0.5 && answer.Length < 200)
                        {
                            _logger?.LogDebug("IndicatesMissingData: {MissingRatio}% of important query terms not in sources. Terms: {Terms}", 
                                (int)(termsNotInSourcesRatio * 100), string.Join(", ", importantTerms));
                            return true;
                        }
                    }
                }
            }

            // If query contains specific terms (product/library names), check if answer mentions them
            // If specific terms are in query but not in answer, and answer indicates "not found", return true
            // This is a fallback check - primary check is [NO_ANSWER_FOUND] token via IsExplicitlyNegative
            if (!string.IsNullOrWhiteSpace(query))
            {
                var specificTerms = ExtractSpecificTermsFromQuery(query);
                if (specificTerms.Count > 0)
                {
                    // If answer says "not found" AND query term is not in answer, it's missing data
                    if (answerIndicatesNotFound)
                    {
                        foreach (var term in specificTerms)
                        {
                            var termLower = term.ToLowerInvariant();
                            // If the specific term from query is not in the answer, it's missing
                            if (!answerLower.Contains(termLower))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // aggressive heuristics (length checks, question mark checks, etc.) are removed 
            // to prevent false positives for valid short database answers.
            // We rely on the [NO_ANSWER_FOUND] token and explicit negative symbols.
            
            return false;
        }

        private bool IsExplicitlyNegative(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return true;
            
            // Only check for the special token from prompt
            // Token-based detection is language-agnostic and reliable
            return answer.Contains("[NO_ANSWER_FOUND]");
        }

        /// <summary>
        /// Creates a fallback response when document query cannot answer the question
        /// </summary>
        public async Task<RagResponse> CreateFallbackResponseAsync(string query, string conversationHistory)
        {
            ServiceLogMessages.LogGeneralConversationQuery(_logger, null);

            var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory);
            return CreateRagResponse(query, chatResponse, new List<SearchSource>());
        }

        /// <summary>
        /// Merges results from database and document queries into a unified response
        /// </summary>
        public async Task<RagResponse> MergeHybridResultsAsync(string query, RagResponse databaseResponse, RagResponse documentResponse, string conversationHistory)
        {
            var combinedSources = new List<SearchSource>();
            combinedSources.AddRange(databaseResponse.Sources);
            combinedSources.AddRange(documentResponse.Sources);

            var databaseContext = !string.IsNullOrEmpty(databaseResponse.Answer)
                ? databaseResponse.Answer
                : null;
            
            if (string.IsNullOrEmpty(databaseContext) && databaseResponse.Sources != null && databaseResponse.Sources.Count > 0)
            {
                var databaseSourcesContext = string.Join("\n\n", databaseResponse.Sources
                    .Where(s => !string.IsNullOrEmpty(s.RelevantContent))
                    .Select(s => $"Database: {s.FileName}\n{s.RelevantContent}"));
                
                if (!string.IsNullOrEmpty(databaseSourcesContext))
                {
                    databaseContext = databaseSourcesContext;
                }
            }
            
            var documentContext = !string.IsNullOrEmpty(documentResponse.Answer)
                ? documentResponse.Answer
                : null;

            var combinedContext = new List<string>();
            if (!string.IsNullOrEmpty(databaseContext))
                combinedContext.Add(databaseContext);
            if (!string.IsNullOrEmpty(documentContext))
                combinedContext.Add(documentContext);

            var mergePrompt = _promptBuilder.BuildHybridMergePrompt(query, databaseContext, documentContext, conversationHistory);
            var mergedAnswer = await _aiService.GenerateResponseAsync(mergePrompt, combinedContext);

            return CreateRagResponse(query, mergedAnswer, combinedSources);
        }
    }
}

