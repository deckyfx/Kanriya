using Microsoft.AspNetCore.Mvc;
using Kanriya.Server.Services;
using Kanriya.Server.Services.Data;

namespace Kanriya.Server.Controllers;

/// <summary>
/// HTTP endpoints for authentication operations
/// Used for email activation links and other auth operations that require HTTP endpoints
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    /// <summary>
    /// Activate user account via email verification token
    /// This endpoint is called when user clicks the activation link in their email
    /// </summary>
    [HttpGet("activate")]
    public async Task<IActionResult> ActivateAccount([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { success = false, message = "Invalid activation token" });
        }
        
        try
        {
            var result = await _userService.VerifyEmailAsync(token);
            
            if (result.Success)
            {
                _logger.LogInformation("Account activated successfully for user {Email}", result.User?.Email);
                
                // Redirect to a success page or return success HTML
                var successHtml = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Account Activated</title>
                        <style>
                            body {
                                font-family: Arial, sans-serif;
                                display: flex;
                                justify-content: center;
                                align-items: center;
                                height: 100vh;
                                margin: 0;
                                background-color: #f0f0f0;
                            }
                            .container {
                                text-align: center;
                                padding: 40px;
                                background: white;
                                border-radius: 10px;
                                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                                max-width: 500px;
                            }
                            h1 { color: #4CAF50; }
                            p { color: #666; margin: 20px 0; }
                            .info-box {
                                background: #e8f5e9;
                                padding: 15px;
                                border-radius: 5px;
                                margin: 20px 0;
                                border-left: 4px solid #4CAF50;
                            }
                            .button {
                                display: inline-block;
                                padding: 12px 30px;
                                background-color: #4CAF50;
                                color: white;
                                text-decoration: none;
                                border-radius: 5px;
                                margin-top: 20px;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h1>✅ Account Activated Successfully!</h1>
                            <p>Your account has been activated successfully.</p>
                            <div class='info-box'>
                                <strong>Next Step:</strong> Sign in with your email and password to get your access token.
                            </div>
                            <p>Use the GraphQL playground to sign in and receive your JWT token for API access.</p>";
                
                successHtml += @"
                            <a href='/graphql' class='button'>Go to GraphQL Playground</a>
                        </div>
                    </body>
                    </html>";
                
                return Content(successHtml, "text/html");
            }
            else
            {
                _logger.LogWarning("Account activation failed: {Message}", result.Message);
                
                var errorHtml = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Activation Failed</title>
                        <style>
                            body {{
                                font-family: Arial, sans-serif;
                                display: flex;
                                justify-content: center;
                                align-items: center;
                                height: 100vh;
                                margin: 0;
                                background-color: #f0f0f0;
                            }}
                            .container {{
                                text-align: center;
                                padding: 40px;
                                background: white;
                                border-radius: 10px;
                                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                                max-width: 500px;
                            }}
                            h1 {{ color: #f44336; }}
                            p {{ color: #666; margin: 20px 0; }}
                            .button {{
                                display: inline-block;
                                padding: 12px 30px;
                                background-color: #2196F3;
                                color: white;
                                text-decoration: none;
                                border-radius: 5px;
                                margin-top: 20px;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h1>❌ Activation Failed</h1>
                            <p>{result.Message}</p>
                            <p>The activation link may have expired or is invalid.</p>
                            <a href='/graphql' class='button'>Request New Activation Link</a>
                        </div>
                    </body>
                    </html>";
                
                return Content(errorHtml, "text/html");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account activation");
            return StatusCode(500, new { success = false, message = "An error occurred during activation" });
        }
    }
    
    /// <summary>
    /// Reset password endpoint (for future implementation)
    /// This endpoint would be called from password reset email links
    /// </summary>
    [HttpGet("reset-password")]
    public IActionResult ResetPassword([FromQuery] string token)
    {
        // TODO: Implement password reset page/flow
        var html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Password Reset</title>
                <style>
                    body {
                        font-family: Arial, sans-serif;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        margin: 0;
                        background-color: #f0f0f0;
                    }
                    .container {
                        text-align: center;
                        padding: 40px;
                        background: white;
                        border-radius: 10px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                        max-width: 500px;
                    }
                    h1 { color: #2196F3; }
                    p { color: #666; margin: 20px 0; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <h1>Password Reset</h1>
                    <p>Password reset functionality will be implemented soon.</p>
                    <p>Token: " + token + @"</p>
                </div>
            </body>
            </html>";
        
        return Content(html, "text/html");
    }
}