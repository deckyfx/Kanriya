using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GQLServer.Data.EntityConfigurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Action)
            .HasConversion<string>();
            
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        // Index for querying logs by email
        builder.HasIndex(e => e.EmailOutboxId);
        builder.HasIndex(e => e.CreatedAt);
        
        // Relationship
        builder.HasOne(e => e.EmailOutbox)
            .WithMany()
            .HasForeignKey(e => e.EmailOutboxId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}