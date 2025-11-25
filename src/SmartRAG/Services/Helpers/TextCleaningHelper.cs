using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Helpers
{
    /// <summary>
    /// Helper class for cleaning and validating text content
    /// </summary>
    public static class TextCleaningHelper
    {
        private const int MinContentLength = 5;
        private const double MinMeaningfulTextRatio = 0.1;

        /// <summary>
        /// Cleans text content by removing binary characters, excessive whitespace, and validating quality
        /// </summary>
        /// <param name="content">Content to clean</param>
        /// <returns>Cleaned content or empty string if invalid</returns>
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

        /// <summary>
        /// Removes binary control characters from content
        /// </summary>
        private static string RemoveBinaryCharacters(string content)
        {
            return Regex.Replace(content, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
        }

        /// <summary>
        /// Normalizes whitespace by replacing multiple spaces with single space
        /// </summary>
        private static string RemoveExcessiveWhitespace(string content)
        {
            return Regex.Replace(content, @"\s+", " ");
        }

        /// <summary>
        /// Reduces excessive line breaks to maximum of two consecutive newlines
        /// </summary>
        private static string RemoveExcessiveLineBreaks(string content)
        {
            return Regex.Replace(content, @"\n\s*\n", "\n\n");
        }

        /// <summary>
        /// Validates content has minimum length and meaningful text ratio
        /// </summary>
        private static bool IsContentValid(string content)
        {
            if (content.Length < MinContentLength)
            {
                return false;
            }

            if (content.Contains("Worksheet:") || content.Contains("Excel file"))
            {
                return true;
            }

            var meaningfulTextRatio = content.Count(c => char.IsLetterOrDigit(c)) / (double)content.Length;
            return meaningfulTextRatio >= MinMeaningfulTextRatio;
        }
    }
}
