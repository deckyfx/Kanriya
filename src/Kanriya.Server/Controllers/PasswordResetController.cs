using Microsoft.AspNetCore.Mvc;
using Kanriya.Server.ViewModels;
using Kanriya.Server.Services.Data;

namespace Kanriya.Server.Controllers;

/// <summary>
/// Password reset controller for handling password reset links from emails
/// </summary>
public class PasswordResetController : Controller
{
    private readonly IUserService _userService;
    
    public PasswordResetController(IUserService userService)
    {
        _userService = userService;
    }
    
    /// <summary>
    /// Display password reset form
    /// </summary>
    [HttpGet("/reset-password")]
    public IActionResult Index([FromQuery] string? token)
    {
        var model = new PasswordResetViewModel
        {
            Token = token ?? string.Empty
        };
        
        if (string.IsNullOrEmpty(token))
        {
            model.Success = false;
            model.Message = "Invalid password reset link. Token is missing.";
        }
        
        return View(model);
    }
    
    /// <summary>
    /// Handle password reset form submission
    /// </summary>
    [HttpPost("/reset-password")]
    public async Task<IActionResult> ResetPassword([FromForm] PasswordResetViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Success = false;
            model.Message = "Please fill in all required fields correctly.";
            return View("Index", model);
        }
        
        if (model.NewPassword != model.ConfirmPassword)
        {
            model.Success = false;
            model.Message = "Passwords do not match.";
            return View("Index", model);
        }
        
        try
        {
            var result = await _userService.ResetPasswordAsync(model.Token, model.NewPassword);
            model.Success = result.Success;
            model.Message = result.Message;
            
            if (result.Success)
            {
                // Clear sensitive data
                model.NewPassword = string.Empty;
                model.ConfirmPassword = string.Empty;
                model.Token = string.Empty;
            }
        }
        catch (Exception)
        {
            model.Success = false;
            model.Message = "An error occurred during password reset. Please try again or contact support.";
        }
        
        return View("Index", model);
    }
}