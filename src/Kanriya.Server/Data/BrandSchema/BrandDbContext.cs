using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Kanriya.Server.Data.BrandSchema;

/// <summary>
/// DbContext for brand-specific schemas
/// Each brand has its own isolated schema with this structure
/// </summary>
public class BrandDbContext : DbContext
{
    private readonly string _schemaName;
    
    public BrandDbContext(DbContextOptions<BrandDbContext> options, string schemaName) : base(options)
    {
        _schemaName = schemaName;
    }
    
    /// <summary>
    /// Brand users with API credentials
    /// </summary>
    public DbSet<BrandUser> Users { get; set; } = null!;
    
    /// <summary>
    /// Brand user roles
    /// </summary>
    public DbSet<BrandUserRole> UserRoles { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set the schema for all entities
        modelBuilder.HasDefaultSchema(_schemaName);
        
        // Note: Snake case naming convention is configured in the options builder
        // when creating the context, not here in OnModelCreating
        
        // Configure BrandUser
        modelBuilder.Entity<BrandUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            
            // API Secret must be unique within the brand
            entity.HasIndex(e => e.ApiSecret)
                .IsUnique()
                .HasDatabaseName("ix_users_api_secret");
            
            // Index for active users
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("ix_users_is_active");
            
            entity.Property(e => e.Id)
                .HasMaxLength(50);
            
            entity.Property(e => e.ApiSecret)
                .HasMaxLength(16)
                .IsRequired();
            
            entity.Property(e => e.ApiPasswordHash)
                .IsRequired();
            
            entity.Property(e => e.BrandSchema)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.DisplayName)
                .HasMaxLength(200);
            
            // Relationships
            entity.HasMany(e => e.Roles)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure BrandUserRole
        modelBuilder.Entity<BrandUserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => e.Id);
            
            // Unique constraint on UserId + Role
            entity.HasIndex(e => new { e.UserId, e.Role })
                .IsUnique()
                .HasDatabaseName("ix_user_roles_user_role");
            
            entity.Property(e => e.Id)
                .HasMaxLength(50);
            
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsRequired();
        });
    }
}