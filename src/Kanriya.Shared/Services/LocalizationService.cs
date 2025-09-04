using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;

namespace Kanriya.Shared.Services;

/// <summary>
/// Localization service for managing translations across all client platforms
/// Provides Node.js-like t() function for easy translation access
/// </summary>
public class LocalizationService : IStringLocalizer
{
    private readonly Dictionary<string, Dictionary<string, object>> _translations = new();
    private CultureInfo _currentCulture;
    private readonly List<string> _supportedLanguages = new() { "en", "id", "ja", "zh", "zh-CN" };
    private const string DefaultLanguage = "en";
    
    /// <summary>
    /// Singleton instance of LocalizationService
    /// </summary>
    private static LocalizationService? _instance;
    
    /// <summary>
    /// Get singleton instance of LocalizationService
    /// </summary>
    public static LocalizationService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new LocalizationService();
                _instance.Initialize();
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Private constructor for singleton pattern
    /// </summary>
    private LocalizationService()
    {
        _currentCulture = CultureInfo.CurrentUICulture;
    }
    
    /// <summary>
    /// Initialize the localization service and load translations
    /// </summary>
    private void Initialize()
    {
        LoadTranslations();
        
        // Set initial language based on system culture or default
        var systemLanguage = CultureInfo.CurrentUICulture.Name;
        if (systemLanguage == "") systemLanguage = "en";
        
        // Check for exact match first
        if (_supportedLanguages.Contains(systemLanguage))
        {
            SetLanguage(systemLanguage);
        }
        // Then check for parent language match (e.g., "zh" for "zh-CN")
        else if (systemLanguage.Contains("-"))
        {
            var parentLanguage = systemLanguage.Split('-')[0];
            if (_supportedLanguages.Contains(parentLanguage))
            {
                SetLanguage(parentLanguage);
            }
            else
            {
                SetLanguage(DefaultLanguage);
            }
        }
        else
        {
            // Check if we have a more specific version of the language
            var specificLanguage = _supportedLanguages.FirstOrDefault(l => l.StartsWith(systemLanguage + "-"));
            if (specificLanguage != null)
            {
                SetLanguage(specificLanguage);
            }
            else
            {
                SetLanguage(DefaultLanguage);
            }
        }
    }
    
    /// <summary>
    /// Load all translation files from embedded resources
    /// Merges multiple JSON files from folder structure
    /// </summary>
    private void LoadTranslations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        
        foreach (var language in _supportedLanguages)
        {
            var mergedTranslations = new Dictionary<string, object>();
            
            // Find all JSON files for this language
            var languageResources = resourceNames
                .Where(r => r.Contains($"Localization.{language}.") && r.EndsWith(".json"))
                .ToList();
            
            // Also check for the old single file format for backward compatibility
            var singleFileResource = resourceNames.FirstOrDefault(r => r.EndsWith($"Localization.{language}.json"));
            if (singleFileResource != null && !languageResources.Contains(singleFileResource))
            {
                languageResources.Add(singleFileResource);
            }
            
            foreach (var resourceName in languageResources)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    var translations = JsonSerializer.Deserialize(json, LocalizationJsonContext.Default.DictionaryStringObject);
                    
                    if (translations != null)
                    {
                        // Merge this file's translations into the merged dictionary
                        MergeTranslations(mergedTranslations, translations);
                    }
                }
            }
            
            if (mergedTranslations.Count > 0)
            {
                // Flatten the merged translations into dot notation keys
                var flattenedTranslations = FlattenJson(mergedTranslations);
                _translations[language] = flattenedTranslations;
            }
        }
    }
    
    /// <summary>
    /// Merge source translations into target dictionary
    /// </summary>
    private void MergeTranslations(Dictionary<string, object> target, Dictionary<string, object> source)
    {
        foreach (var kvp in source)
        {
            if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                // If the key exists and is also an object, merge recursively
                if (target.TryGetValue(kvp.Key, out var existingValue) && 
                    existingValue is JsonElement existingElement && 
                    existingElement.ValueKind == JsonValueKind.Object)
                {
                    // Convert both to dictionaries and merge
                    var existingDict = JsonSerializer.Deserialize(existingElement.GetRawText(), LocalizationJsonContext.Default.DictionaryStringObject) ?? new Dictionary<string, object>();
                    var newDict = JsonSerializer.Deserialize(element.GetRawText(), LocalizationJsonContext.Default.DictionaryStringObject) ?? new Dictionary<string, object>();
                    MergeTranslations(existingDict, newDict);
                    
                    // Convert back to JsonElement and update target
                    var mergedJson = JsonSerializer.Serialize(existingDict);
                    var mergedElement = JsonSerializer.Deserialize<object>(mergedJson);
                    if (mergedElement != null)
                    {
                        target[kvp.Key] = mergedElement;
                    }
                }
                else
                {
                    // Key doesn't exist or isn't an object, just set it
                    target[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // Simple value, just set or overwrite
                target[kvp.Key] = kvp.Value;
            }
        }
    }
    
    /// <summary>
    /// Flatten nested JSON structure into dot notation keys
    /// </summary>
    private Dictionary<string, object> FlattenJson(Dictionary<string, object> json, string prefix = "")
    {
        var result = new Dictionary<string, object>();
        
        foreach (var kvp in json)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
            
            if (kvp.Value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var nested = JsonSerializer.Deserialize(element.GetRawText(), LocalizationJsonContext.Default.DictionaryStringObject);
                    if (nested != null)
                    {
                        var flattened = FlattenJson(nested, key);
                        foreach (var flatKvp in flattened)
                        {
                            result[flatKvp.Key] = flatKvp.Value;
                        }
                    }
                }
                else if (element.ValueKind == JsonValueKind.String)
                {
                    result[key] = element.GetString() ?? string.Empty;
                }
                else
                {
                    result[key] = element.ToString();
                }
            }
            else
            {
                result[key] = kvp.Value?.ToString() ?? string.Empty;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Node.js-like t() function for getting translations
    /// </summary>
    public string t(string key, params object[] args)
    {
        return this[key, args].Value;
    }
    
    /// <summary>
    /// Get translation by key with optional formatting arguments
    /// </summary>
    public LocalizedString this[string key]
    {
        get
        {
            var languageCode = _currentCulture.Name == "" ? "en" : _currentCulture.Name;
            
            // Try current language first
            if (_translations.TryGetValue(languageCode, out var currentTranslations) &&
                currentTranslations.TryGetValue(key, out var translation))
            {
                return new LocalizedString(key, translation.ToString() ?? key, false);
            }
            
            // If it's a specific culture like zh-CN, try the parent culture zh
            if (languageCode.Contains("-"))
            {
                var parentLanguageCode = languageCode.Split('-')[0];
                if (_translations.TryGetValue(parentLanguageCode, out var parentTranslations) &&
                    parentTranslations.TryGetValue(key, out var parentTranslation))
                {
                    return new LocalizedString(key, parentTranslation.ToString() ?? key, false);
                }
            }
            
            // Fallback to default language
            if (_translations.TryGetValue(DefaultLanguage, out var defaultTranslations) &&
                defaultTranslations.TryGetValue(key, out var defaultTranslation))
            {
                return new LocalizedString(key, defaultTranslation.ToString() ?? key, false);
            }
            
            // Return key if translation not found
            return new LocalizedString(key, key, true);
        }
    }
    
    /// <summary>
    /// Get translation by key with formatting arguments
    /// </summary>
    public LocalizedString this[string key, params object[] arguments]
    {
        get
        {
            var localizedString = this[key];
            if (arguments != null && arguments.Length > 0 && !localizedString.ResourceNotFound)
            {
                var formattedValue = string.Format(localizedString.Value, arguments);
                return new LocalizedString(key, formattedValue, false);
            }
            return localizedString;
        }
    }
    
    /// <summary>
    /// Set the current language
    /// </summary>
    public void SetLanguage(string cultureName)
    {
        if (_supportedLanguages.Contains(cultureName))
        {
            _currentCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = _currentCulture;
            LanguageChanged?.Invoke(this, cultureName);
        }
    }
    
    /// <summary>
    /// Get the current language code
    /// </summary>
    public string CurrentLanguage => _currentCulture.Name == "" ? "en" : _currentCulture.Name;
    
    /// <summary>
    /// Get all supported language codes
    /// </summary>
    public IEnumerable<string> SupportedLanguages => _supportedLanguages;
    
    /// <summary>
    /// Event raised when language is changed
    /// </summary>
    public event EventHandler<string>? LanguageChanged;
    
    /// <summary>
    /// Get all strings (for IStringLocalizer compatibility)
    /// </summary>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var languageCode = _currentCulture.TwoLetterISOLanguageName;
        
        if (_translations.TryGetValue(languageCode, out var translations))
        {
            foreach (var kvp in translations)
            {
                yield return new LocalizedString(kvp.Key, kvp.Value.ToString() ?? kvp.Key, false);
            }
        }
    }
}