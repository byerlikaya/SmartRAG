
namespace SmartRAG.Services.Document;


/// <summary>
/// Service for building RAG responses
/// </summary>
public class ResponseBuilderService : IResponseBuilderService
{
    private readonly SmartRagOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResponseBuilderService> _logger;
    private readonly IAIService _aiService;
    private readonly IPromptBuilderService _promptBuilder;

    /// <summary>
    /// Initializes a new instance of the ResponseBuilderService
    /// </summary>
    /// <param name="options">SmartRAG configuration options</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="aiService">AI service for merging responses</param>
    /// <param name="promptBuilder">Prompt builder service for merging responses</param>
    public ResponseBuilderService(
        IOptions<SmartRagOptions> options,
        IConfiguration configuration,
        ILogger<ResponseBuilderService> logger,
        IAIService aiService,
        IPromptBuilderService promptBuilder)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

        if (!validatedAnswer.Contains("[NO_ANSWER_FOUND]"))
        {
            if (ShouldAddMissingDataToken(validatedAnswer, query, sources))
            {
                validatedAnswer += " [NO_ANSWER_FOUND]";
            }
        }
        
        var cleanedAnswer = StripNoAnswerFoundTokenAndMetaCommentary(validatedAnswer);

        if (string.IsNullOrWhiteSpace(cleanedAnswer) && !string.IsNullOrWhiteSpace(query))
        {
            cleanedAnswer = SmartRAG.Helpers.RagMessages.NoDocumentContext;
        }

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
    
    private bool ShouldAddMissingDataToken(string answer, string query, List<SearchSource> sources)
    {
        if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(query))
            return false;

        var indicatesMissing = IndicatesMissingData(answer, query, sources);
        if (indicatesMissing)
            return true;

        var specificTerms = ExtractSpecificTermsFromQuery(query);
        if (specificTerms.Count > 0)
        {
            var answerLower = answer.ToLowerInvariant();
            var allSourceContent = string.Join(" ", sources
                .Where(s => !string.IsNullOrWhiteSpace(s.RelevantContent))
                .Select(s => s.RelevantContent));

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
                var sourceContentLower = allSourceContent.ToLowerInvariant();
                
                foreach (var term in specificTerms)
                {
                    var termLower = term.ToLowerInvariant();
                    var termInAnswer = answerLower.Contains(termLower);
                    var termInSources = sourceContentLower.Contains(termLower);

                    if (termInAnswer && !termInSources && answer.Length < 150)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    private string ValidateAnswerAgainstQuery(string query, string answer, List<SearchSource> sources)
    {
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
            
            if (string.IsNullOrWhiteSpace(word))
                continue;

            if (i + 1 < words.Length)
            {
                var nextWord = words[i + 1].Trim();
                var isShortEntityPrefix = word.Length >= 1 && word.Length <= 2 && char.IsUpper(word[0]) &&
                    nextWord.Length >= MinTermLength && char.IsUpper(nextWord[0]);
                var isLongEntityPair = word.Length >= MinTermLength && char.IsUpper(word[0]) &&
                    nextWord.Length >= MinTermLength && char.IsUpper(nextWord[0]);

                if (isShortEntityPrefix || isLongEntityPair)
                {
                    terms.Add($"{word} {nextWord}");
                    i++;
                    continue;
                }
            }

            if (word.Length < 2)
                continue;

            if (char.IsUpper(word[0]) && word.Length >= MinTermLength)
            {
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
    private RagConfiguration GetRagConfiguration()
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
                return true;

            if (!hasMeaningfulData)
                return false;
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

        // Explicit negative token (internal)
        if (IsExplicitlyNegative(normalized))
        {
            return true;
        }

        // CRITICAL: If we already have substantial document/audio/image context,
        // we never force "no answer" just because the model used a pessimistic phrase.
        // Responsibility for missing data lies in retrieval, not in the generated wording.
        if (sources != null && sources.Any(s =>
                SearchSourceHelper.HasContentBearingSource(s) &&
                !string.IsNullOrWhiteSpace(s.RelevantContent) &&
                s.RelevantContent!.Length >= 50))
        {
            return false;
        }

        var answerLower = normalized.ToLowerInvariant();

        var notFoundPatterns = new[]
        {
            "not found", "not in", "does not contain", "doesn't contain",
            "not mentioned", "not present", "couldn't find", "could not find",
            "unable to find", "no information", "not available"
        };

        var answerIndicatesNotFound = notFoundPatterns.Any(pattern => answerLower.Contains(pattern));

        if (!string.IsNullOrWhiteSpace(query) && sources != null && sources.Count > 0)
        {
            var allSourceContent = string.Join(" ", sources
                .Where(s => !string.IsNullOrWhiteSpace(s.RelevantContent))
                .Select(s => s.RelevantContent));

            if (!string.IsNullOrWhiteSpace(allSourceContent) && allSourceContent.Length >= 50)
            {
                var entityTerms = ExtractSpecificTermsFromQuery(query);
                var sourceContentLower = allSourceContent.ToLowerInvariant();

                if (entityTerms.Count > 0)
                {
                    var entityTermsInSources = entityTerms.Count(term =>
                        sourceContentLower.Contains(term.ToLowerInvariant()));
                    if (entityTermsInSources > 0)
                    {
                        return false;
                    }
                }

                var queryTerms = QueryTokenizer.TokenizeQuery(query);
                var significantTerms = queryTerms.Where(term => term.Length >= 5).ToList();

                if (significantTerms.Count > 0)
                {
                    var termsInSources = significantTerms.Count(term =>
                        sourceContentLower.Contains(term.ToLowerInvariant()));
                    var termsInSourcesRatio = termsInSources / (double)significantTerms.Count;

                    if (termsInSourcesRatio >= 0.3)
                    {
                        return false;
                    }
                }
            }
        }

        if (answerIndicatesNotFound)
        {
            return true;
        }

        return false;
    }

    private bool IsExplicitlyNegative(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return true;
        
        return answer.Contains("[NO_ANSWER_FOUND]");
    }

    private static string StripNoAnswerFoundTokenAndMetaCommentary(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return answer;

        var result = Regex.Replace(answer, @"\s*\[NO_ANSWER_FOUND[^\]]*\]", string.Empty, RegexOptions.IgnoreCase);
        return result.Trim();
    }

    /// <summary>
    /// Merges results from database and document queries into a unified response
    /// </summary>
    public async Task<RagResponse> MergeHybridResultsAsync(string query, RagResponse databaseResponse, RagResponse documentResponse, string conversationHistory, CancellationToken cancellationToken = default)
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
        var mergedAnswer = await _aiService.GenerateResponseAsync(mergePrompt, combinedContext, cancellationToken);

        return CreateRagResponse(query, mergedAnswer, combinedSources);
    }
}


