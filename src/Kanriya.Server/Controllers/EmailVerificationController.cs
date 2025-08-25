using Microsoft.AspNetCore.Mvc;
using Kanriya.Server.ViewModels;
using Kanriya.Server.Services;
using Kanriya.Server.Services.Data;

namespace Kanriya.Server.Controllers;

/// <summary>
/// Email verification controller
/// </summary>
public class EmailVerificationController : Controller
{
    private readonly IUserService _userService;
    
    public EmailVerificationController(IUserService userService)
    {
        _userService = userService;
    }
    
    /// <summary>
    /// Handle email verification
    /// </summary>
    [HttpGet("/verify-email")]
    public async Task<IActionResult> Index([FromQuery] string? token)
    {
        var model = new EmailVerificationViewModel();
        
        if (string.IsNullOrEmpty(token))
        {
            model.Success = false;
            model.Message = "Invalid verification link. Token is missing.";
        }
        else
        {
            try
            {
                var result = await _userService.VerifyEmailAsync(token);
                model.Success = result.Success;
                model.Message = result.Message;
                model.UserEmail = result.User?.Email;
            }
            catch (Exception)
            {
                model.Success = false;
                model.Message = "An error occurred during verification. Please try again or contact support.";
            }
        }
        
        return View(model);
    }
}