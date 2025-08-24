using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Kanriya.Server.Data;

/// <summary>
/// Application database context for Entity Framework Core
/// Manages database connections and entity configurations
/// Uses separate configuration files for each entity to maintain clean architecture
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the AppDbContext
    /// </summary>
    /// <param name="options">The options to configure the context</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options)
    {
    }

    /// <summary>
    /// DbSet for GreetLog entities - represents the greet_logs table in the database
    /// </summary>
    public DbSet<GreetLog> GreetLogs { get; set; } = null!;
    
    /// <summary>
    /// DbSet for User entities - represents the users table in the database
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;
    
    /// <summary>
    /// DbSet for PendingUser entities - represents the pending_users table in the database
    /// </summary>
    public DbSet<PendingUser> PendingUsers { get; set; } = null!;
    
    /// <summary>
    /// DbSet for UserRole entities - represents the user_roles table in the database
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    
    /// <summary>
    /// DbSet for EmailOutbox entities - represents the email_outbox table in the database
    /// </summary>
    public DbSet<EmailOutbox> EmailOutboxes { get; set; } = null!;
    
    /// <summary>
    /// DbSet for EmailTemplate entities - represents the email_templates table in the database
    /// </summary>
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
    
    /// <summary>
    /// DbSet for UserMailSettings entities - represents the user_mail_settings table in the database
    /// </summary>
    public DbSet<UserMailSettings> UserMailSettings { get; set; } = null!;
    
    /// <summary>
    /// DbSet for EmailLog entities - represents the email_logs table in the database
    /// </summary>
    public DbSet<EmailLog> EmailLogs { get; set; } = null!;

    /// <summary>
    /// Configures the model and relationships for the database
    /// Applies all entity configurations from the EntityConfigurations folder
    /// </summary>
    /// <param name="modelBuilder">The model builder used to construct the model</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all entity configurations from the current assembly
        // This automatically discovers and applies all classes that implement IEntityTypeConfiguration<T>
        // in the Data.EntityConfigurations namespace
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Alternative: Apply specific configurations manually (if you prefer explicit control)
        // modelBuilder.ApplyConfiguration(new EntityConfigurations.GreetLogConfiguration());
        // modelBuilder.ApplyConfiguration(new EntityConfigurations.UserConfiguration());
        // modelBuilder.ApplyConfiguration(new EntityConfigurations.CategoryConfiguration());
        
        // Global query filters (optional)
        // Example: Soft delete filter that could be applied to all entities
        // modelBuilder.Entity<GreetLog>().HasQueryFilter(e => !e.IsDeleted);
        
        // Global conventions (optional)
        // Example: Set all string properties to have a max length of 250 by default
        // foreach (var property in modelBuilder.Model.GetEntityTypes()
        //     .SelectMany(t => t.GetProperties())
        //     .Where(p => p.ClrType == typeof(string)))
        // {
        //     property.SetMaxLength(250);
        // }
    }
    
    /// <summary>
    /// Override SaveChanges to add custom logic like audit fields
    /// </summary>
    public override int SaveChanges()
    {
        // Add any pre-save logic here (e.g., setting audit fields)
        // UpdateAuditFields();
        return base.SaveChanges();
    }
    
    /// <summary>
    /// Override SaveChangesAsync to add custom logic like audit fields
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add any pre-save logic here (e.g., setting audit fields)
        // UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Example method for updating audit fields (CreatedAt, UpdatedAt, etc.)
    /// Uncomment and modify when you add auditable entities
    /// </summary>
    /*
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));
        
        foreach (var entry in entries)
        {
            var entity = (IAuditable)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
    */
}