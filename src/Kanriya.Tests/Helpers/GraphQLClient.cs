using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace Kanriya.Tests.Helpers;

/// <summary>
/// GraphQL client helper for making requests
/// </summary>
public class GraphQLClient
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    
    public GraphQLClient(string serverUrl)
    {
        _httpClient = new HttpClient();
        _serverUrl = serverUrl;
    }
    
    /// <summary>
    /// Set authentication token
    /// </summary>
    public void SetAuthToken(string? token)
    {
        _currentToken = token;
        if (string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
    
    /// <summary>
    /// Get current authentication token
    /// </summary>
    public string? GetAuthToken()
    {
        return _currentToken;
    }
    
    private string? _currentToken;
    
    /// <summary>
    /// Execute a GraphQL query/mutation
    /// </summary>
    public async Task<JsonDocument?> ExecuteAsync(string query, object? variables = null)
    {
        try
        {
            var request = new
            {
                query = query,
                variables = variables
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_serverUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var doc = JsonDocument.Parse(responseContent);
            
            // Check for errors
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                foreach (var error in errors.EnumerateArray())
                {
                    if (error.TryGetProperty("message", out var msg))
                    {
                        AnsiConsole.MarkupLine($"  [red dim]GraphQL Error: {msg.GetString()}[/]");
                    }
                }
            }
            
            return doc;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Request failed: {ex.Message}[/]");
            return null;
        }
    }
    
    /// <summary>
    /// Make an HTTP GET request (for non-GraphQL endpoints)
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _httpClient.GetAsync(url);
    }
    
    /// <summary>
    /// Extract a success boolean from response
    /// </summary>
    public static bool GetSuccess(JsonDocument? doc, params string[] path)
    {
        if (doc == null) return false;
        
        var element = doc.RootElement;
        foreach (var prop in path)
        {
            if (!element.TryGetProperty(prop, out element))
                return false;
        }
        
        if (element.TryGetProperty("success", out var success))
            return success.ValueKind == JsonValueKind.True;
            
        return false;
    }
    
    /// <summary>
    /// Extract a string value from response
    /// </summary>
    public static string? GetString(JsonDocument? doc, params string[] path)
    {
        if (doc == null) return null;
        
        var element = doc.RootElement;
        foreach (var prop in path)
        {
            if (!element.TryGetProperty(prop, out element))
                return null;
        }
        
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }
    
    /// <summary>
    /// Extract message from response
    /// </summary>
    public static string GetMessage(JsonDocument? doc, params string[] path)
    {
        if (doc == null) return "Unknown error";
        
        var element = doc.RootElement;
        foreach (var prop in path)
        {
            if (!element.TryGetProperty(prop, out element))
                return "Unknown error";
        }
        
        if (element.TryGetProperty("message", out var message))
            return message.GetString() ?? "Unknown error";
            
        return "Unknown error";
    }
}