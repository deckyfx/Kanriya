using Kanriya.Server.Data;
using Kanriya.Server.Services;
using Kanriya.Server.Services.System;
using Kanriya.Shared;
using Microsoft.EntityFrameworkCore;

namespace Kanriya.Server.Seeds;

/// <summary>
/// Seeds system email templates
/// </summary>
public class EmailTemplateSeeder : ISeeder
{
    public int Order => 2; // Run after roles and users
    public string Name => "Email Templates";
    
    public async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = await contextFactory.CreateDbContextAsync();
        
        var templates = GetSystemTemplates();
        
        foreach (var template in templates)
        {
            var existing = await context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Name == template.Name);
                
            if (existing == null)
            {
                context.EmailTemplates.Add(template);
                LogService.LogSuccess($"[{Name}] Created template: {template.Name}");
            }
            else if (existing.CreatedBy == "SYSTEM")
            {
                // Update system templates to ensure they're current
                existing.SubjectTemplate = template.SubjectTemplate;
                existing.HtmlBodyTemplate = template.HtmlBodyTemplate;
                existing.TextBodyTemplate = template.TextBodyTemplate;
                existing.UpdatedAt = DateTime.UtcNow;
                LogService.LogInfo($"[{Name}] Updated system template: {template.Name}");
            }
            else
            {
                LogService.LogInfo($"[{Name}] Template already exists (user-modified): {template.Name}");
            }
        }
        
        await context.SaveChangesAsync();
    }
    
    private static List<EmailTemplate> GetSystemTemplates()
    {
        var publicUrl = Shared.EnvironmentConfig.Server.PublicUrl;
        var appName = "Kanriya";
        
        return new List<EmailTemplate>
        {
            new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Name = "user_activation",
                SubjectTemplate = $"Activate Your Account - {appName}",
                HtmlBodyTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #007bff; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f8f9fa; }
        .button { display: inline-block; padding: 12px 24px; background-color: #28a745; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; color: #6c757d; font-size: 14px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to {{appName}}!</h1>
        </div>
        <div class='content'>
            <h2>Hi {{userName}},</h2>
            <p>Thank you for signing up! Please activate your account by clicking the button below:</p>
            <div style='text-align: center;'>
                <a href='{{activationUrl}}' class='button'>Activate Account</a>
            </div>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{{activationUrl}}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create an account, you can safely ignore this email.</p>
        </div>
        <div class='footer'>
            <p>© {{year}} {{appName}}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>",
                TextBodyTemplate = @"Welcome to {{appName}}!

Hi {{userName}},

Thank you for signing up! Please activate your account by visiting this link:

{{activationUrl}}

This link will expire in 24 hours.

If you didn't create an account, you can safely ignore this email.

© {{year}} {{appName}}. All rights reserved.",
                DefaultFromEmail = Shared.EnvironmentConfig.Mail.FromAddress,
                DefaultFromName = Shared.EnvironmentConfig.Mail.FromName,
                IsActive = true,
                CreatedBy = "SYSTEM",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            
            new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Name = "password_reset",
                SubjectTemplate = $"Reset Your Password - {appName}",
                HtmlBodyTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #dc3545; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f8f9fa; }
        .button { display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; color: #6c757d; font-size: 14px; }
        .warning { background-color: #fff3cd; border: 1px solid #ffc107; padding: 10px; margin: 15px 0; border-radius: 4px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset Request</h1>
        </div>
        <div class='content'>
            <h2>Hi {{userName}},</h2>
            <p>We received a request to reset your password for your {{appName}} account.</p>
            <div style='text-align: center;'>
                <a href='{{resetUrl}}' class='button'>Reset Password</a>
            </div>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{{resetUrl}}</p>
            <div class='warning'>
                <p><strong>⚠️ Important:</strong> This link will expire in {{expiryHours}} hours.</p>
            </div>
            <p>If you didn't request a password reset, please ignore this email. Your password won't be changed.</p>
            <p>For security reasons, this link can only be used once.</p>
        </div>
        <div class='footer'>
            <p>© {{year}} {{appName}}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>",
                TextBodyTemplate = @"Password Reset Request

Hi {{userName}},

We received a request to reset your password for your {{appName}} account.

Reset your password by visiting this link:

{{resetUrl}}

⚠️ Important: This link will expire in {{expiryHours}} hours.

If you didn't request a password reset, please ignore this email. Your password won't be changed.

For security reasons, this link can only be used once.

© {{year}} {{appName}}. All rights reserved.",
                DefaultFromEmail = Shared.EnvironmentConfig.Mail.FromAddress,
                DefaultFromName = Shared.EnvironmentConfig.Mail.FromName,
                IsActive = true,
                CreatedBy = "SYSTEM",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            
            new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Name = "welcome",
                SubjectTemplate = $"Welcome to {appName}!",
                HtmlBodyTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #28a745; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f8f9fa; }
        .button { display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; color: #6c757d; font-size: 14px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to {{appName}}!</h1>
        </div>
        <div class='content'>
            <h2>Hi {{userName}},</h2>
            <p>Your account has been successfully activated! We're excited to have you on board.</p>
            <p>Here are some things you can do to get started:</p>
            <ul>
                <li>Complete your profile</li>
                <li>Explore our features</li>
                <li>Connect with other users</li>
            </ul>
            <div style='text-align: center;'>
                <a href='{{loginUrl}}' class='button'>Go to Dashboard</a>
            </div>
            <p>If you have any questions, feel free to contact our support team.</p>
        </div>
        <div class='footer'>
            <p>© {{year}} {{appName}}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>",
                TextBodyTemplate = @"Welcome to {{appName}}!

Hi {{userName}},

Your account has been successfully activated! We're excited to have you on board.

Here are some things you can do to get started:
- Complete your profile
- Explore our features
- Connect with other users

Go to Dashboard: {{loginUrl}}

If you have any questions, feel free to contact our support team.

© {{year}} {{appName}}. All rights reserved.",
                DefaultFromEmail = Shared.EnvironmentConfig.Mail.FromAddress,
                DefaultFromName = Shared.EnvironmentConfig.Mail.FromName,
                IsActive = true,
                CreatedBy = "SYSTEM",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }
}