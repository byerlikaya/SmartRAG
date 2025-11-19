using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Helpers
{
    public static class TextCleaningHelper
    {
        private const int MinContentLength = 5;
        private const double MinMeaningfulTextRatio = 0.1;

        public static string CleanContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var cleaned = RemoveBinaryCharacters(content);
            cleaned = RemoveExcessiveWhitespace(cleaned);
            cleaned = RemoveExcessiveLineBreaks(cleaned);
            cleaned = cleaned.Trim();

            if (!IsContentValid(cleaned))
            {
                return string.Empty;
            }

            return cleaned;
        }

        private static string RemoveBinaryCharacters(string content)
        {
            return Regex.Replace(content, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
        }

        private static string RemoveExcessiveWhitespace(string content)
        {
            return Regex.Replace(content, @"\s+", " ");
        }

        private static string RemoveExcessiveLineBreaks(string content)
        {
            return Regex.Replace(content, @"\n\s*\n", "\n\n");
        }

        private static bool IsContentValid(string content)
        {
            if (content.Length < MinContentLength)
            {
                return false;
            }

            // For Excel files, be more lenient with content validation
            // This check might need to be context-aware or moved to specific parsers if needed
            if (content.Contains("Worksheet:") || content.Contains("Excel file"))
            {
                return true;
            }

            var meaningfulTextRatio = content.Count(c => char.IsLetterOrDigit(c)) / (double)content.Length;
            return meaningfulTextRatio >= MinMeaningfulTextRatio;
        }
    }
}
