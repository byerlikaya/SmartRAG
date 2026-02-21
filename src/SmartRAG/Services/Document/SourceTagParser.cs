using SmartRAG.Models.Schema;

namespace SmartRAG.Services.Document;


/// <summary>
/// Parses source tags from query (-d, -db, -mcp, -a, -i) and returns cleaned query and adjusted SearchOptions.
/// </summary>
public static class SourceTagParser
{
    private const string DocumentTagPattern = @"\s*-d\s*$";
    private const string DatabaseTagPattern = @"(?:^\s*-db\s*|\s*-db\s*$)";
    private const string McpTagPattern = @"\s*-mcp\s*$";
    private const string AudioTagPattern = @"\s*-a\s*$";
    private const string ImageTagPattern = @"\s*-i\s*$";
    private const string PunctuationPrefix = @"[\p{P}]";
    private const RegexOptions TagRegexOptions = RegexOptions.IgnoreCase;

    /// <summary>
    /// Parses source tags from query and adjusts SearchOptions accordingly.
    /// Tags: -d (document), -db (database), -mcp (MCP), -a (audio), -i (image).
    /// Returns cleaned query without tags.
    /// </summary>
    public static (string cleanedQuery, SearchOptions adjustedOptions) Parse(string query, SearchOptions baseOptions)
    {
        var cleanedQuery = query.TrimEnd();

        var tagHandlers = new[]
        {
            (Pattern: DocumentTagPattern, Factory: (Func<SearchOptions, SearchOptions>)SearchOptions.CreateDocumentOnly),
            (Pattern: DatabaseTagPattern, Factory: SearchOptions.CreateDatabaseOnly),
            (Pattern: McpTagPattern, Factory: SearchOptions.CreateMcpOnly),
            (Pattern: AudioTagPattern, Factory: SearchOptions.CreateAudioOnly),
            (Pattern: ImageTagPattern, Factory: SearchOptions.CreateImageOnly)
        };

        foreach (var (pattern, factory) in tagHandlers)
        {
            var match = Regex.Match(cleanedQuery, CreateTagPatternWithOptionalPunctuation(pattern), TagRegexOptions);
            if (!match.Success)
                continue;

            var adjustedOptions = factory(baseOptions);
            if (pattern == McpTagPattern)
                adjustedOptions.EnableMcpSearch = true;
            cleanedQuery = match.Index == 0
                ? cleanedQuery.Substring(match.Length).TrimStart()
                : cleanedQuery.Substring(0, match.Index).TrimEnd();
            return (cleanedQuery, adjustedOptions);
        }

        return (cleanedQuery, baseOptions);
    }

    private static string CreateTagPatternWithOptionalPunctuation(string baseTagPattern)
    {
        return $@"(?:{PunctuationPrefix})?{baseTagPattern}";
    }
}
