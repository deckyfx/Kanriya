using System.Reflection;
using Kanriya.Server.Services.System;
using Kanriya.Server.Seeds;

namespace Kanriya.Server.Services.Data;

/// <summary>
/// Service responsible for orchestrating all database seeders
/// </summary>
public class DatabaseSeeder(IServiceProvider serviceProvider)
{
    /// <summary>
    /// Discover and execute all seeders in the Seeds folder
    /// </summary>
    public async Task SeedAsync()
    {
        LogService.LogInfo("Starting database seeding...");
        
        // Discover all seeder classes that implement ISeeder
        var seederTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ISeeder).IsAssignableFrom(t))
            .ToList();
        
        if (!seederTypes.Any())
        {
            LogService.LogInfo("No seeders found to execute.");
            return;
        }
        
        // Create instances and order them
        var seeders = new List<ISeeder>();
        foreach (var seederType in seederTypes)
        {
            if (Activator.CreateInstance(seederType) is ISeeder seeder)
            {
                seeders.Add(seeder);
            }
        }
        
        // Sort seeders by their Order property
        seeders = seeders.OrderBy(s => s.Order).ToList();
        
        LogService.LogInfo($"Found {seeders.Count} seeder(s) to execute:");
        foreach (var seeder in seeders)
        {
            LogService.LogInfo($"  - {seeder.Name} (Order: {seeder.Order})");
        }
        
        // Execute each seeder in order
        foreach (var seeder in seeders)
        {
            try
            {
                LogService.LogInfo($"Executing seeder: {seeder.Name}...");
                await seeder.SeedAsync(serviceProvider);
            }
            catch (Exception ex)
            {
                LogService.LogError($"Error executing seeder '{seeder.Name}': {ex.Message}", ex);
                // Continue with other seeders even if one fails
            }
        }
        
        LogService.LogSuccess("Database seeding completed!");
    }
}