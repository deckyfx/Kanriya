using Microsoft.AspNetCore.Mvc;

namespace Kanriya.Server.Controllers;

/// <summary>
/// Health check endpoint for monitoring
/// </summary>
[ApiController]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Simple health check endpoint
    /// </summary>
    [HttpGet("/health")]
    public IActionResult GetHealth()
    {
        return Ok(new 
        { 
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Kanriya.Server"
        });
    }
}