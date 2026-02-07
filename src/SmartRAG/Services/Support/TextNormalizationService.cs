
namespace SmartRAG.Services.Support;


/// <summary>
/// Service for text normalization operations
/// </summary>
public class TextNormalizationService : ITextNormalizationService
{
    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        string decoded;
        try
        {
            decoded = Regex.Unescape(text);
        }
        catch (ArgumentException)
        {

            decoded = DecodeUnicodeEscapesSafely(text);
        }


        var normalized = decoded.Normalize(NormalizationForm.FormC);

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
                switch (text[i + 1])
                {
                    case 'u' when i + 5 < text.Length:
                    {
                        var hexString = text.Substring(i + 2, 4);
                        if (int.TryParse(hexString, NumberStyles.HexNumber, null, out int codePoint))
                        {
                            result.Append((char)codePoint);
                            i += 6;
                            continue;
                        }

                        break;
                    }
                    case 'n':
                        result.Append('\n');
                        i += 2;
                        continue;
                    case 't':
                        result.Append('\t');
                        i += 2;
                        continue;
                    case 'r':
                        result.Append('\r');
                        i += 2;
                        continue;
                    case '\\':
                        result.Append('\\');
                        i += 2;
                        continue;
                }
            }

            result.Append(text[i]);
            i++;
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

        if (normalizedContent.ToLowerInvariant().Contains(normalizedSearchName.ToLowerInvariant()))
            return true;

        var searchWords = normalizedSearchName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var contentWords = normalizedContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        return searchWords.All(searchWord =>
            contentWords.Any(contentWord =>
                contentWord.ToLowerInvariant().Contains(searchWord.ToLowerInvariant())));
    }

}


