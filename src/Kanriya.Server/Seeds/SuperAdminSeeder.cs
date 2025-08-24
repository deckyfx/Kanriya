using Kanriya.Server.Data;
using Kanriya.Server.Program;
using Kanriya.Server.Services;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Kanriya.Server.Seeds;

/// <summary>
/// Seeds the default SuperAdmin user from environment configuration
/// </summary>
public class SuperAdminSeeder : ISeeder
{
    public int Order => 1; // Run first to ensure admin exists
    public string Name => "SuperAdmin User";
    
    public async Task SeedAsync(IServiceProvider serviceProvider)
    {
        // Check if admin credentials are configured
        if (!EnvironmentConfig.Admin.HasAdminConfig)
        {
            LogService.LogInfo($"[{Name}] No admin credentials configured in environment. Skipping.");
            return;
        }
        
        var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = await contextFactory.CreateDbContextAsync();
        
        var adminEmail = EnvironmentConfig.Admin.Username!;
        var adminPassword = EnvironmentConfig.Admin.Password!;
        
        // Check if user already exists
        var existingUser = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == adminEmail);
            
        if (existingUser != null)
        {
            // Check if user has SuperAdmin role
            var hasSuperAdmin = existingUser.UserRoles.Any(ur => ur.Role == "SuperAdmin");
            
            if (!hasSuperAdmin)
            {
                // Add SuperAdmin role to existing user
                var superAdminRole = new UserRole
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = existingUser.Id,
                    Role = "SuperAdmin",
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = "SYSTEM_SEEDER"
                };
                
                context.UserRoles.Add(superAdminRole);
                await context.SaveChangesAsync();
                
                LogService.LogSuccess($"[{Name}] Granted SuperAdmin role to existing user: {adminEmail}");
            }
            else
            {
                LogService.LogInfo($"[{Name}] SuperAdmin user already exists: {adminEmail}");
            }
            
            return;
        }
        
        // Create new SuperAdmin user
        var newUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            FullName = "System Administrator",
            ProfilePictureUrl = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Add SuperAdmin role
        var userRole = new UserRole
        {
            Id = Guid.NewGuid().ToString(),
            UserId = newUser.Id,
            Role = "SuperAdmin",
            AssignedAt = DateTime.UtcNow,
            AssignedBy = "SYSTEM_SEEDER"
        };
        
        context.Users.Add(newUser);
        context.UserRoles.Add(userRole);
        
        await context.SaveChangesAsync();
        
        LogService.LogSuccess($"[{Name}] Created SuperAdmin user: {adminEmail}");
    }
}