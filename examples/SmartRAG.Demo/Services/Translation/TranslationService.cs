namespace SmartRAG.Demo.Services.Translation;

/// <summary>
/// Service for translating queries and messages
/// </summary>
public class TranslationService : ITranslationService
{
    #region Fields

    private readonly Dictionary<string, Dictionary<string, string>> _templates;
    private readonly Dictionary<string, string> _languageCodes;
    private readonly Dictionary<string, string> _languageNames;

    #endregion

    #region Constructor

    public TranslationService()
    {
        _languageCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["English"] = "en",
            ["Turkish"] = "tr",
            ["German"] = "de",
            ["Russian"] = "ru"
        };

        _languageNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "English",
            ["tr"] = "Turkish",
            ["de"] = "German",
            ["ru"] = "Russian"
        };

        _templates = new Dictionary<string, Dictionary<string, string>>
        {
            ["show_records"] = new()
            {
                ["English"] = "Show all records from {0} with their related {1} information",
                ["Turkish"] = "{0} tablosundaki tüm kayıtları ilgili {1} bilgileriyle birlikte göster",
                ["German"] = "Zeige alle Datensätze aus {0} mit ihren zugehörigen {1} Informationen",
                ["Russian"] = "Показать все записи из {0} с соответствующей информацией {1}"
            },
            ["calculate_value"] = new()
            {
                ["English"] = "Calculate the combined value using {0} from {1} and {2} from {3}",
                ["Turkish"] = "{1} tablosundaki {0} ve {3} tablosundaki {2} değerlerini kullanarak toplam değeri hesapla",
                ["German"] = "Berechne den Gesamtwert mit {0} aus {1} und {2} aus {3}",
                ["Russian"] = "Рассчитайте общее значение используя {0} из {1} и {2} из {3}"
            },
            ["analyze_correlation"] = new()
            {
                ["English"] = "Analyze all available data to find correlations and patterns across all databases",
                ["Turkish"] = "Tüm veritabanlarındaki mevcut verileri analiz ederek korelasyonları ve kalıpları bul",
                ["German"] = "Analysiere alle verfügbaren Daten um Korrelationen und Muster über alle Datenbanken zu finden",
                ["Russian"] = "Проанализируйте все доступные данные чтобы найти корреляции и паттерны во всех базах данных"
            },
            ["timeline_correlation"] = new()
            {
                ["English"] = "What is the timeline correlation between {0} and {1} records?",
                ["Turkish"] = "{0} ve {1} kayıtları arasındaki zaman çizelgesi korelasyonu nedir?",
                ["German"] = "Was ist die zeitliche Korrelation zwischen {0} und {1} Datensätzen?",
                ["Russian"] = "Какова временная корреляция между записями {0} и {1}?"
            },
            ["analyze_relationship"] = new()
            {
                ["English"] = "Analyze relationship between {0} and {1}",
                ["Turkish"] = "{0} ve {1} arasındaki ilişkiyi analiz et",
                ["German"] = "Analysiere die Beziehung zwischen {0} und {1}",
                ["Russian"] = "Проанализируйте связь между {0} и {1}"
            }
        };
    }

    #endregion

    #region Public Methods

    public string TranslateQuery(string template, string language, params string[] parameters)
    {
        if (_templates.TryGetValue(template, out var translations) &&
            translations.TryGetValue(language, out var translatedTemplate))
        {
            if (parameters.Length > 0)
            {
                return string.Format(translatedTemplate, parameters);
            }
            return translatedTemplate;
        }

        return template;
    }

    public string GetLanguageName(string languageCode)
    {
        return _languageNames.TryGetValue(languageCode, out var name) ? name : languageCode;
    }

    public string GetLanguageCode(string languageName)
    {
        return _languageCodes.TryGetValue(languageName, out var code) ? code : "en";
    }

    #endregion
}

