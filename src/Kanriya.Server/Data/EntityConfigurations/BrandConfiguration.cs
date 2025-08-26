using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kanriya.Server.Data.EntityConfigurations;

/// <summary>
/// Configuration for the Brand entity
/// </summary>
public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        // Table name
        builder.ToTable("brands");
        
        // Primary key
        builder.HasKey(b => b.Id);
        
        // Indexes
        builder.HasIndex(b => b.Name)
            .IsUnique()
            .HasDatabaseName("ix_brands_name");
            
        builder.HasIndex(b => b.SchemaName)
            .IsUnique()
            .HasDatabaseName("ix_brands_schema_name");
            
        builder.HasIndex(b => b.DatabaseUser)
            .IsUnique()
            .HasDatabaseName("ix_brands_database_user");
            
        builder.HasIndex(b => b.OwnerId)
            .HasDatabaseName("ix_brands_owner_id");
            
        builder.HasIndex(b => b.IsActive)
            .HasDatabaseName("ix_brands_is_active");
            
        builder.HasIndex(b => b.CreatedAt)
            .HasDatabaseName("ix_brands_created_at");
        
        // Property configurations
        builder.Property(b => b.Id)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(b => b.Name)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(b => b.OwnerId)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(b => b.SchemaName)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(b => b.DatabaseUser)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(b => b.EncryptedPassword)
            .IsRequired();
            
        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(b => b.CreatedAt)
            .IsRequired();
            
        builder.Property(b => b.UpdatedAt)
            .IsRequired();
            
        // Relationships
        builder.HasOne(b => b.Owner)
            .WithMany(u => u.OwnedBrands)
            .HasForeignKey(b => b.OwnerId)
            .OnDelete(DeleteBehavior.Cascade); // When user is deleted, delete all their brands
    }
}