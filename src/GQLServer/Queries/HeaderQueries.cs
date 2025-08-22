using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;

namespace GQLServer.Queries;

/// <summary>
/// Queries that demonstrate reading custom HTTP headers
/// </summary>
[ExtendObjectType("Query")]
public class HeaderQueries
{
    /// <summary>
    /// Get all headers from the current request
    /// Useful for debugging and understanding what headers are available
    /// </summary>
    public Dictionary<string, string> GetAllHeaders([Service] IHttpContextAccessor httpContextAccessor)
    {
        var headers = new Dictionary<string, string>();
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext?.Request?.Headers != null)
        {
            foreach (var header in httpContext.Request.Headers)
            {
                // Join multiple values with comma if header has multiple values
                headers[header.Key] = string.Join(", ", header.Value.ToArray());
            }
        }
        
        return headers;
    }
    
    /// <summary>
    /// Get a specific header value by name
    /// </summary>
    public string? GetHeader(string headerName, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext?.Request?.Headers != null && 
            httpContext.Request.Headers.TryGetValue(headerName, out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }
        
        return null;
    }
    
    /// <summary>
    /// Example of a query that requires a specific API key in X-API-KEY header
    /// This uses custom validation logic instead of JWT
    /// </summary>
    public string GetSecretDataWithApiKey([Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var apiKey = httpContext?.Request?.Headers["X-API-KEY"].FirstOrDefault();
        
        // Validate API key (in production, check against database or config)
        var validApiKeys = new[] { "secret-api-key-123", "another-valid-key-456" };
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new GraphQLException("X-API-KEY header is required for this query");
        }
        
        if (!validApiKeys.Contains(apiKey))
        {
            throw new GraphQLException("Invalid API key provided");
        }
        
        return $"Secret data accessed with API key: {apiKey.Substring(0, 10)}...";
    }
    
    /// <summary>
    /// Example that checks multiple authentication methods
    /// Accepts either JWT token OR API key
    /// </summary>
    public string GetDataWithMultiAuth(
        [Service] IHttpContextAccessor httpContextAccessor,
        ClaimsPrincipal? claimsPrincipal)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        // Check if user is authenticated via JWT
        if (claimsPrincipal?.Identity?.IsAuthenticated == true)
        {
            var username = claimsPrincipal.Identity.Name;
            return $"Accessed via JWT authentication. User: {username}";
        }
        
        // Check for API key authentication
        var apiKey = httpContext?.Request?.Headers["X-API-KEY"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
        {
            // Validate API key
            var validApiKeys = new[] { "secret-api-key-123", "another-valid-key-456" };
            if (validApiKeys.Contains(apiKey))
            {
                return $"Accessed via API key authentication";
            }
        }
        
        throw new GraphQLException("Authentication required. Provide either JWT token or X-API-KEY header");
    }
    
    /// <summary>
    /// Example that reads custom client information headers
    /// </summary>
    public ClientInfo GetClientInfo([Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var headers = httpContext?.Request?.Headers;
        
        return new ClientInfo
        {
            UserAgent = headers?["User-Agent"].FirstOrDefault() ?? "Unknown",
            ClientId = headers?["X-Client-ID"].FirstOrDefault(),
            ClientVersion = headers?["X-Client-Version"].FirstOrDefault(),
            RequestId = headers?["X-Request-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString(),
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown"
        };
    }
}

/// <summary>
/// Client information extracted from headers
/// </summary>
public class ClientInfo
{
    public string UserAgent { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? ClientVersion { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}