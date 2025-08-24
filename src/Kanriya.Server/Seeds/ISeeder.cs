namespace Kanriya.Server.Seeds;

/// <summary>
/// Base interface for all database seeders
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Order of execution (lower numbers run first)
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// Name of the seeder for logging purposes
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Execute the seeding operation
    /// </summary>
    Task SeedAsync(IServiceProvider serviceProvider);
}