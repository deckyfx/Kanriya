using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kanriya.Server.Data.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for the User entity
/// Defines table mapping, column specifications, indexes, and relationships
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Configures the User entity mapping to the database
    /// </summary>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name will be 'users' due to snake_case convention
        builder.ToTable("users");
        
        // Primary Key
        builder.HasKey(e => e.Id);
        
        // Property Configurations
        builder.Property(e => e.Id)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Email)
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(e => e.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(e => e.FullName)
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(e => e.CreatedAt)
            .IsRequired();
        
        builder.Property(e => e.UpdatedAt)
            .IsRequired();
        
        builder.Property(e => e.ProfilePictureUrl)
            .HasMaxLength(500);
        
        builder.Property(e => e.LastLoginAt);
        
        // Indexes
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email_unique");
        
        builder.HasIndex(e => e.CreatedAt)
            .IsDescending()
            .HasDatabaseName("ix_users_created_at");
        
        // Relationships
        builder.HasMany(e => e.UserRoles)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete user roles when user is deleted
    }
}