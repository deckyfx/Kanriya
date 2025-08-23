using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GQLServer.Data.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for the UserRole entity
/// Defines table mapping for user role assignments
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    /// <summary>
    /// Configures the UserRole entity mapping to the database
    /// </summary>
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Table name will be 'user_roles' due to snake_case convention
        builder.ToTable("user_roles");
        
        // Primary Key
        builder.HasKey(e => e.Id);
        
        // Property Configurations
        builder.Property(e => e.Id)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.UserId)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Role)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.AssignedAt)
            .IsRequired();
        
        builder.Property(e => e.AssignedBy)
            .HasMaxLength(50);
        
        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_user_roles_user_id");
        
        builder.HasIndex(e => e.Role)
            .HasDatabaseName("ix_user_roles_role");
        
        // Composite index for user_id and role (to prevent duplicate role assignments)
        builder.HasIndex(e => new { e.UserId, e.Role })
            .IsUnique()
            .HasDatabaseName("ix_user_roles_user_id_role_unique");
        
        builder.HasIndex(e => e.AssignedAt)
            .IsDescending()
            .HasDatabaseName("ix_user_roles_assigned_at");
    }
}