
namespace SmartRAG.Helpers;


internal static class SearchSourceHelper
{
    internal static bool HasContentBearingSource(SearchSource s)
    {
        var t = s?.SourceType ?? string.Empty;
        return t == "Document" || t == "Image" || t == "Audio";
    }
}

