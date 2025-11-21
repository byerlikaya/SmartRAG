#nullable enable

using SmartRAG.Interfaces.Support;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Support
{
    /// <summary>
    /// Service for text normalization operations
    /// </summary>
    public class TextNormalizationService : ITextNormalizationService
    {
        /// <summary>
        /// Normalizes text for better search matching (handles Unicode encoding issues)
        /// Uses Unicode normalization to handle character variations for all languages generically
        /// </summary>
        public string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string decoded;
            try
            {
                // Decode Unicode escape sequences (e.g., \u0061, \n, \t)
                // This may fail if text contains invalid escape sequences (e.g., \h from OCR errors)
                decoded = Regex.Unescape(text);
            }
            catch (ArgumentException)
            {
                // Fallback: If Regex.Unescape fails (invalid escape sequences in .NET Standard 2.1),
                // manually decode only valid Unicode escape sequences and leave others as-is
                decoded = DecodeUnicodeEscapesSafely(text);
            }

            // Normalize Unicode characters using FormC (Canonical Composition)
            // This handles character variations for all languages generically without hardcoded mappings
            var normalized = decoded.Normalize(System.Text.NormalizationForm.FormC);

            return normalized;
        }

        /// <summary>
        /// Safely decodes Unicode escape sequences (\uXXXX) while leaving invalid escape sequences unchanged
        /// </summary>
        private static string DecodeUnicodeEscapesSafely(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var result = new StringBuilder(text.Length);
            var i = 0;
            while (i < text.Length)
            {
                if (text[i] == '\\' && i + 1 < text.Length)
                {
                    // Check for Unicode escape sequence \uXXXX
                    if (text[i + 1] == 'u' && i + 5 < text.Length)
                    {
                        var hexString = text.Substring(i + 2, 4);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
                        {
                            result.Append((char)codePoint);
                            i += 6;
                            continue;
                        }
                    }
                    // Check for common escape sequences
                    else if (text[i + 1] == 'n')
                    {
                        result.Append('\n');
                        i += 2;
                        continue;
                    }
                    else if (text[i + 1] == 't')
                    {
                        result.Append('\t');
                        i += 2;
                        continue;
                    }
                    else if (text[i + 1] == 'r')
                    {
                        result.Append('\r');
                        i += 2;
                        continue;
                    }
                    else if (text[i + 1] == '\\')
                    {
                        result.Append('\\');
                        i += 2;
                        continue;
                    }
                    // Invalid escape sequence - leave as-is (e.g., \h becomes \h)
                    result.Append(text[i]);
                    i++;
                }
                else
                {
                    result.Append(text[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Normalizes text for matching purposes (removes control characters and normalizes whitespace)
        /// </summary>
        public string NormalizeForMatching(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = Regex.Replace(value, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized.Trim();
        }

        /// <summary>
        /// Checks if content contains normalized name (handles encoding issues)
        /// </summary>
        public bool ContainsNormalizedName(string content, string searchName)
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
        /// Sanitizes user input for safe logging by removing control characters and limiting length.
        /// Prevents log injection attacks by removing newlines, carriage returns, and other control characters.
        /// </summary>
        public string SanitizeForLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            const int maxLogLength = 500;

            // Remove control characters (including newlines, carriage returns, tabs, etc.)
            var sanitized = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                // Allow printable characters and common whitespace (space only)
                if (!char.IsControl(c) || c == ' ')
                {
                    sanitized.Append(c);
                }
            }

            var result = sanitized.ToString();

            // Limit length to prevent log flooding
            if (result.Length > maxLogLength)
            {
                result = result.Substring(0, maxLogLength) + "... (truncated)";
            }

            return result;
        }
    }
}

