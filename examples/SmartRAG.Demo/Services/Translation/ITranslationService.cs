namespace SmartRAG.Demo.Services.Translation;

/// <summary>
/// Service for translating text between languages
/// </summary>
public interface ITranslationService
{
    string TranslateQuery(string template, string language, params string[] parameters);
    string GetLanguageName(string languageCode);
    string GetLanguageCode(string languageName);
}

