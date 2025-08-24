using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kanriya.Server.Data.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for the PendingUser entity
/// Defines table mapping for users awaiting email verification
/// </summary>
public class PendingUserConfiguration : IEntityTypeConfiguration<PendingUser>
{
    /// <summary>
    /// Configures the PendingUser entity mapping to the database
    /// </summary>
    public void Configure(EntityTypeBuilder<PendingUser> builder)
    {
        // Table name will be 'pending_users' due to snake_case convention
        builder.ToTable("pending_users");
        
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
        
        builder.Property(e => e.CreatedAt)
            .IsRequired();
        
        builder.Property(e => e.VerificationToken)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(e => e.TokenExpiresAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("ix_pending_users_email_unique");
        
        builder.HasIndex(e => e.VerificationToken)
            .IsUnique()
            .HasDatabaseName("ix_pending_users_token_unique");
        
        builder.HasIndex(e => e.TokenExpiresAt)
            .HasDatabaseName("ix_pending_users_token_expires");
        
        builder.HasIndex(e => e.CreatedAt)
            .IsDescending()
            .HasDatabaseName("ix_pending_users_created_at");
    }
}