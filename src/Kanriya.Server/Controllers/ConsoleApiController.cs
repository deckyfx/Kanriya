using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kanriya.Server.Controllers;

/// <summary>
/// API Controller for Blazor Console operations
/// Supports all HTTP methods: GET, POST, PUT, PATCH, DELETE
/// </summary>
[Route("api/console")]
[ApiController]
public class ConsoleApiController : ControllerBase
{
    // ===== COOKIE OPERATIONS =====
    
    /// <summary>
    /// GET: Read all cookies
    /// </summary>
    [HttpGet("cookies")]
    public IActionResult GetCookies()
    {
        var cookies = Request.Cookies.Select(c => new { key = c.Key, value = c.Value }).ToList();
        return Ok(new { count = cookies.Count, cookies });
    }
    
    /// <summary>
    /// GET: Read specific cookie
    /// </summary>
    [HttpGet("cookies/{name}")]
    public IActionResult GetCookie(string name)
    {
        var value = Request.Cookies[name];
        if (value != null)
            return Ok(new { name, value });
        
        return NotFound(new { message = $"Cookie '{name}' not found" });
    }
    
    /// <summary>
    /// POST: Create new cookie
    /// </summary>
    [HttpPost("cookies")]
    public IActionResult CreateCookie([FromBody] CookieRequest request)
    {
        if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Value))
            return BadRequest(new { message = "Name and value are required" });
        
        Response.Cookies.Append(request.Name, request.Value, new CookieOptions
        {
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(request.DaysToExpire ?? 7),
            HttpOnly = request.HttpOnly,
            Secure = request.Secure,
            SameSite = request.SameSite ?? SameSiteMode.Lax
        });
        
        return Created($"/api/console/cookies/{request.Name}", 
            new { message = $"Cookie '{request.Name}' created", request.Name, request.Value });
    }
    
    /// <summary>
    /// PUT: Update existing cookie (replace)
    /// </summary>
    [HttpPut("cookies/{name}")]
    public IActionResult UpdateCookie(string name, [FromBody] CookieUpdateRequest request)
    {
        // Delete old cookie
        Response.Cookies.Delete(name);
        
        // Set new cookie
        Response.Cookies.Append(name, request.Value, new CookieOptions
        {
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(request.DaysToExpire ?? 7),
            HttpOnly = request.HttpOnly,
            Secure = request.Secure,
            SameSite = request.SameSite ?? SameSiteMode.Lax
        });
        
        return Ok(new { message = $"Cookie '{name}' updated", name, value = request.Value });
    }
    
    /// <summary>
    /// PATCH: Partially update cookie (extend expiry)
    /// </summary>
    [HttpPatch("cookies/{name}")]
    public IActionResult PatchCookie(string name, [FromBody] CookiePatchRequest request)
    {
        var currentValue = Request.Cookies[name];
        if (currentValue == null)
            return NotFound(new { message = $"Cookie '{name}' not found" });
        
        // Re-set cookie with new expiry
        Response.Cookies.Append(name, request.Value ?? currentValue, new CookieOptions
        {
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(request.DaysToExpire ?? 7),
            HttpOnly = request.HttpOnly ?? false,
            Secure = request.Secure ?? false,
            SameSite = request.SameSite ?? SameSiteMode.Lax
        });
        
        return Ok(new { message = $"Cookie '{name}' patched", name });
    }
    
    /// <summary>
    /// DELETE: Remove specific cookie
    /// </summary>
    [HttpDelete("cookies/{name}")]
    public IActionResult DeleteCookie(string name)
    {
        Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
        return Ok(new { message = $"Cookie '{name}' deleted" });
    }
    
    /// <summary>
    /// DELETE: Remove all cookies
    /// </summary>
    [HttpDelete("cookies")]
    public IActionResult DeleteAllCookies()
    {
        var count = 0;
        foreach (var cookie in Request.Cookies.Keys)
        {
            Response.Cookies.Delete(cookie, new CookieOptions { Path = "/" });
            count++;
        }
        return Ok(new { message = $"Deleted {count} cookies" });
    }
    
    // ===== SESSION OPERATIONS =====
    
    /// <summary>
    /// GET: Read all session data
    /// </summary>
    [HttpGet("session")]
    public async Task<IActionResult> GetSession()
    {
        await HttpContext.Session.LoadAsync();
        var keys = HttpContext.Session.Keys.ToList();
        
        if (!keys.Any())
            return Ok(new { count = 0, message = "No session data" });
        
        var items = keys.Select(k => new { key = k, value = HttpContext.Session.GetString(k) });
        return Ok(new { 
            sessionId = HttpContext.Session.Id,
            count = keys.Count, 
            items 
        });
    }
    
    /// <summary>
    /// GET: Read specific session value
    /// </summary>
    [HttpGet("session/{key}")]
    public async Task<IActionResult> GetSessionValue(string key)
    {
        await HttpContext.Session.LoadAsync();
        var value = HttpContext.Session.GetString(key);
        
        if (value != null)
            return Ok(new { key, value });
        
        return NotFound(new { message = $"Session key '{key}' not found" });
    }
    
    /// <summary>
    /// POST: Create new session value
    /// </summary>
    [HttpPost("session")]
    public async Task<IActionResult> CreateSessionValue([FromBody] SessionRequest request)
    {
        if (string.IsNullOrEmpty(request.Key) || string.IsNullOrEmpty(request.Value))
            return BadRequest(new { message = "Key and value are required" });
        
        await HttpContext.Session.LoadAsync();
        HttpContext.Session.SetString(request.Key, request.Value);
        await HttpContext.Session.CommitAsync();
        
        return Created($"/api/console/session/{request.Key}", 
            new { message = $"Session value '{request.Key}' created", request.Key, request.Value });
    }
    
    /// <summary>
    /// PUT: Update session value (replace)
    /// </summary>
    [HttpPut("session/{key}")]
    public async Task<IActionResult> UpdateSessionValue(string key, [FromBody] SessionUpdateRequest request)
    {
        await HttpContext.Session.LoadAsync();
        HttpContext.Session.SetString(key, request.Value);
        await HttpContext.Session.CommitAsync();
        
        return Ok(new { message = $"Session value '{key}' updated", key, value = request.Value });
    }
    
    /// <summary>
    /// PATCH: Append to session value
    /// </summary>
    [HttpPatch("session/{key}")]
    public async Task<IActionResult> PatchSessionValue(string key, [FromBody] SessionPatchRequest request)
    {
        await HttpContext.Session.LoadAsync();
        var currentValue = HttpContext.Session.GetString(key);
        
        if (currentValue == null)
            return NotFound(new { message = $"Session key '{key}' not found" });
        
        var newValue = request.Append ? currentValue + request.Value : request.Value ?? currentValue;
        HttpContext.Session.SetString(key, newValue);
        await HttpContext.Session.CommitAsync();
        
        return Ok(new { message = $"Session value '{key}' patched", key, value = newValue });
    }
    
    /// <summary>
    /// DELETE: Remove specific session value
    /// </summary>
    [HttpDelete("session/{key}")]
    public async Task<IActionResult> DeleteSessionValue(string key)
    {
        await HttpContext.Session.LoadAsync();
        HttpContext.Session.Remove(key);
        await HttpContext.Session.CommitAsync();
        
        return Ok(new { message = $"Session value '{key}' deleted" });
    }
    
    /// <summary>
    /// DELETE: Clear all session data
    /// </summary>
    [HttpDelete("session")]
    public async Task<IActionResult> DeleteAllSession()
    {
        await HttpContext.Session.LoadAsync();
        HttpContext.Session.Clear();
        await HttpContext.Session.CommitAsync();
        
        return Ok(new { message = "Session cleared" });
    }
}

// Request DTOs
public class CookieRequest
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public int? DaysToExpire { get; set; }
    public bool HttpOnly { get; set; }
    public bool Secure { get; set; }
    public SameSiteMode? SameSite { get; set; }
}

public class CookieUpdateRequest
{
    public string Value { get; set; } = "";
    public int? DaysToExpire { get; set; }
    public bool HttpOnly { get; set; }
    public bool Secure { get; set; }
    public SameSiteMode? SameSite { get; set; }
}

public class CookiePatchRequest
{
    public string? Value { get; set; }
    public int? DaysToExpire { get; set; }
    public bool? HttpOnly { get; set; }
    public bool? Secure { get; set; }
    public SameSiteMode? SameSite { get; set; }
}

public class SessionRequest
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class SessionUpdateRequest
{
    public string Value { get; set; } = "";
}

public class SessionPatchRequest
{
    public string? Value { get; set; }
    public bool Append { get; set; }
}