using Kanriya.Server.Services;
using Kanriya.Server.Constants;

namespace Kanriya.Server.Seeds;

/// <summary>
/// Seeds default roles that the system expects to exist
/// </summary>
public class DefaultRolesSeeder : ISeeder
{
    public int Order => 0; // Run before user seeders
    public string Name => "Default Roles";
    
    public async Task SeedAsync(IServiceProvider serviceProvider)
    {
        // This is a placeholder for future role management
        // Currently roles are created on-demand when assigned to users
        // In the future, we might want to have a separate Roles table
        
        LogService.LogInfo($"[{Name}] Role seeding placeholder - roles are created on-demand");
        
        // Example of what this might look like with a Roles table:
        /*
        var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = await contextFactory.CreateDbContextAsync();
        
        foreach (var roleName in DefaultRoles)
        {
            var exists = await context.Roles.AnyAsync(r => r.Name == roleName);
            if (!exists)
            {
                context.Roles.Add(new Role 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    Description = $"Default {roleName} role",
                    CreatedAt = DateTime.UtcNow
                });
                LogService.LogSuccess($"[{Name}] Created role: {roleName}");
            }
        }
        
        await context.SaveChangesAsync();
        */
        
        await Task.CompletedTask;
    }
}