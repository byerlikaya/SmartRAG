
namespace SmartRAG.Helpers;


internal static class SearchSourceHelper
{
    internal static bool HasContentBearingSource(SearchSource s)
    {
        var t = s.SourceType;
        return t is "Document" or "Image" or "Audio";
    }
}

