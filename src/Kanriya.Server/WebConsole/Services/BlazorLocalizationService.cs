using Kanriya.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Web;

namespace Kanriya.Server.WebConsole.Services;

/// <summary>
/// Blazor-specific wrapper for LocalizationService with cookie-based language persistence
/// Supports optional ?lang=xx query parameter for sharing/switching
/// </summary>
public class BlazorLocalizationService
{
    private readonly LocalizationService _localizationService;
    private readonly NavigationManager _navigationManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string LanguageCookieName = "kanriya_lang";
    private const string LanguageQueryParam = "lang";
    private bool _initialized = false;
    
    public BlazorLocalizationService(NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor)
    {
        _navigationManager = navigationManager;
        _httpContextAccessor = httpContextAccessor;
        _localizationService = LocalizationService.Instance;
        
        // Don't initialize in constructor to avoid cookie issues
        // Call Initialize() method when ready
    }
    
    /// <summary>
    /// Initialize the language - call this after the component is rendered
    /// </summary>
    public void Initialize()
    {
        if (!_initialized)
        {
            _initialized = true;
            InitializeLanguage();
        }
    }
    
    /// <summary>
    /// Get the localization service instance
    /// </summary>
    public LocalizationService Localizer => _localizationService;
    
    /// <summary>
    /// Shorthand for translation
    /// </summary>
    public string T(string key, params object[] args) => _localizationService.t(key, args);
    
    /// <summary>
    /// Get current language code
    /// </summary>
    public string CurrentLanguage => _localizationService.CurrentLanguage;
    
    /// <summary>
    /// Get supported languages
    /// </summary>
    public IEnumerable<string> SupportedLanguages => _localizationService.SupportedLanguages;
    
    /// <summary>
    /// Initialize language from query parameter, cookie, or default
    /// Priority: 1. Query parameter (?lang=xx), 2. Cookie, 3. Browser preference, 4. Default (en)
    /// </summary>
    private void InitializeLanguage()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _localizationService.SetLanguage("en");
            return;
        }
        
        // 1. Check query parameter first (for sharing/switching)
        var uri = new Uri(_navigationManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var queryLang = query[LanguageQueryParam];
        
        if (!string.IsNullOrEmpty(queryLang) && _localizationService.SupportedLanguages.Contains(queryLang))
        {
            _localizationService.SetLanguage(queryLang);
            
            // Try to set cookie if response hasn't started yet
            // This persists the language choice from query parameter
            if (httpContext.Response != null && !httpContext.Response.HasStarted)
            {
                try
                {
                    httpContext.Response.Cookies.Append(LanguageCookieName, queryLang, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        Path = "/console"
                    });
                }
                catch
                {
                    // Ignore cookie setting errors - the language is already set
                }
            }
            return;
        }
        
        // 2. Check cookie
        if (httpContext.Request.Cookies.TryGetValue(LanguageCookieName, out var cookieLang) && 
            !string.IsNullOrEmpty(cookieLang) && 
            _localizationService.SupportedLanguages.Contains(cookieLang))
        {
            _localizationService.SetLanguage(cookieLang);
            return;
        }
        
        // 3. Check browser Accept-Language header
        var acceptLanguage = httpContext.Request.Headers["Accept-Language"];
        if (!StringValues.IsNullOrEmpty(acceptLanguage))
        {
            var languages = acceptLanguage.ToString().Split(',')
                .Select(l => l.Split(';')[0].Trim())
                .ToList();
                
            foreach (var lang in languages)
            {
                // Check exact match
                if (_localizationService.SupportedLanguages.Contains(lang))
                {
                    _localizationService.SetLanguage(lang);
                    return;
                }
                
                // Check language without region (e.g., "zh" from "zh-CN")
                var baseLang = lang.Split('-')[0];
                if (_localizationService.SupportedLanguages.Contains(baseLang))
                {
                    _localizationService.SetLanguage(baseLang);
                    return;
                }
            }
        }
        
        // 4. Default to English
        _localizationService.SetLanguage("en");
    }
    
    
    
    /// <summary>
    /// Get URL for sharing with specific language
    /// </summary>
    public string GetShareableUrl(string? languageCode = null)
    {
        languageCode ??= CurrentLanguage;
        var uri = new Uri(_navigationManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        query[LanguageQueryParam] = languageCode;
        
        var uriBuilder = new UriBuilder(uri)
        {
            Query = query.ToString()
        };
        
        return uriBuilder.ToString();
    }
}