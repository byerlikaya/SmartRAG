using System.Text.RegularExpressions;

namespace SmartRAG.Services.Helpers;


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
        cleaned = CorrectOcrErrors(cleaned);
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
    /// Normalizes whitespace by collapsing multiple spaces/tabs on each line while preserving line breaks
    /// </summary>
    private static string RemoveExcessiveWhitespace(string content)
    {
        // CRITICAL: Preserve line breaks to maintain list structure (e.g., numbered lists)
        // Split by newlines, clean each line individually, then rejoin
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            // Only collapse horizontal whitespace (spaces and tabs) on same line
            lines[i] = Regex.Replace(lines[i], @"[ \t]+", " ").Trim();
        }
        return string.Join("\n", lines);
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

    /// <summary>
    /// Corrects common OCR errors in extracted text
    /// Based on industry best practices: fixes percentage misreads, number/character confusions
    /// </summary>
    private static string CorrectOcrErrors(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var corrected = content;

        corrected = CorrectPercentageMisreads(corrected);
        corrected = CorrectNumberCharacterConfusions(corrected);

        return corrected;
    }

    /// <summary>
    /// Corrects percentage misreads using language-agnostic structural pattern detection
    /// OCR commonly misreads "%XX" as "6XX" when percentage symbol is not clearly separated
    /// Uses generic pattern: 3-digit number starting with 6 followed by word boundary (structural pattern, no language-specific words)
    /// </summary>
    private static string CorrectPercentageMisreads(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var corrected = text;

        var pattern = @"\b6(\d{2})\s*['']i\s+[a-zA-Z]{3,}";
        corrected = Regex.Replace(corrected, pattern, match =>
        {
            var number = match.Groups[1].Value;
            var after = match.Value.Substring(3);
            return $"%{number}" + after;
        }, RegexOptions.IgnoreCase);

        pattern = @"\b6(\d{2})\s+[a-zA-Z]{3,}";
        corrected = Regex.Replace(corrected, pattern, match =>
        {
            var number = match.Groups[1].Value;
            var after = match.Value.Substring(3);
            return $"%{number}" + after;
        }, RegexOptions.IgnoreCase);

        return corrected;
    }

    /// <summary>
    /// Corrects common OCR character/number confusions
    /// </summary>
    private static string CorrectNumberCharacterConfusions(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var corrected = text;

        corrected = Regex.Replace(corrected, @"\b0([A-Za-z])\b", "O$1");
        corrected = Regex.Replace(corrected, @"\b([A-Za-z])0\b", "$1O");

        return corrected;
    }
}

