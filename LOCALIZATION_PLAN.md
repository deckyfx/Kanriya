# Kanriya Client Localization Implementation Plan

## Overview
Implement Microsoft.Extensions.Localization (Option 2) for JSON-based localization system in the shared Kanriya client library.

## Architecture Goals
- **Single Source of Truth**: Shared client library manages all localization
- **JSON Files**: Similar to Node.js i18n structure (`en.json`, `id.json`, etc.)
- **Platform Agnostic**: Works across Desktop, Android, iOS, Browser
- **Embedded Resources**: Translation files embedded in assembly
- **Node.js-like Usage**: `_localizer["key.path"]` similar to `t("key.path")`

## Implementation Plan

### 1. Project Setup
- Add `Microsoft.Extensions.Localization` NuGet package to shared client
- Add `Microsoft.Extensions.Localization.Abstractions` if needed
- Configure MSBuild to embed JSON files as resources

### 2. Folder Structure
```
src/Kanriya.Client.Avalonia/Kanriya.Client.Avalonia/
├── Localization/
│   ├── en.json          # Default English
│   ├── id.json          # Indonesian
│   ├── jp.json          # Japan
├── Services/
│   └── LocalizationService.cs
└── ... (existing files)
```

### 3. JSON Structure
Following Node.js-style nested structure:
```json
{
  "app": {
    "name": "Kanriya",
    "welcome": "Welcome to Kanriya Client"
  },
  "platform": {
    "info": "Platform: {0}",
    "desktop": "Desktop",
    "android": "Android",
    "ios": "iOS",
    "browser": "Browser"
  },
  "server": {
    "connection": "Server Connection",
    "url": "Server URL: {0}",
    "status": {
      "connected": "Connected",
      "connecting": "Connecting...",
      "disconnected": "Disconnected"
    }
  },
  "errors": {
    "network": "Network connection failed",
    "config": "Configuration error"
  }
}
```

### 4. LocalizationService Implementation
```csharp
public class LocalizationService
{
    private readonly IStringLocalizer _localizer;
    
    // Node.js-like t() function
    public string t(string key, params object[] args)
    {
        return _localizer[key, args];
    }
    
    // Property access
    public string this[string key] => _localizer[key];
    
    // Language switching
    public void SetLanguage(string cultureName);
    public string CurrentLanguage { get; }
    public IEnumerable<string> SupportedLanguages { get; }
}
```

### 5. Integration Points

#### ClientEnvironmentConfig Extension
```csharp
public static class ClientEnvironmentConfig
{
    // ... existing code ...
    
    /// <summary>
    /// Localization configuration
    /// </summary>
    public static class Localization
    {
        public static LocalizationService Service => GetLocalizationService();
        public static string CurrentLanguage => Service.CurrentLanguage;
        public static void SetLanguage(string culture) => Service.SetLanguage(culture);
    }
}
```

#### Platform Integration
- **Desktop**: Standard .NET localization
- **Android**: Integrate with Android locale settings
- **iOS**: Integrate with iOS locale settings  
- **Browser**: Integrate with browser language preferences

### 6. Usage Examples

#### In ViewModels
```csharp
public class MainViewModel : ViewModelBase
{
    private readonly LocalizationService _localization;
    
    public string WelcomeMessage => _localization["app.welcome"];
    public string PlatformInfo => _localization["platform.info", Platform.Current);
}
```

#### In Views (XAML)
```xml
<TextBlock Text="{Binding WelcomeMessage}" />
<TextBlock Text="{Binding ServerStatus}" />
```

### 7. Language Detection Strategy
1. **User Preference**: Saved in app settings
2. **Platform Default**: Device/OS language setting
3. **Fallback**: English (en) as default

### 8. Supported Languages (Initial)
- **en**: English (default)
- **id**: Indonesian (primary market)
- **Additional**: French, Spanish (future expansion)

### 9. Build Integration
- Configure MSBuild to embed JSON files as embedded resources
- Ensure localization files are included in all platform builds
- Add build-time validation for missing translation keys

### 10. Testing Strategy
- Unit tests for LocalizationService
- Integration tests for platform-specific locale detection
- Validation tests for translation key completeness across languages

## Benefits
- **Consistency**: Same localization system across all platforms
- **Maintainability**: Single location for all translations
- **Developer Experience**: Node.js-like `t()` function usage
- **Performance**: Embedded resources, no external file dependencies
- **Scalability**: Easy to add new languages and keys

## Future Enhancements
- Pluralization support (ICU message format)
- Dynamic language loading from server
- Translation management integration
- Right-to-left (RTL) language support
- Context-aware translations