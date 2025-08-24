using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kanriya.Server.Data.EntityConfigurations;

public class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("email_outbox");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ToEmail)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(e => e.FromEmail)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(e => e.SenderType)
            .HasConversion<string>();
            
        builder.Property(e => e.MailDriver)
            .HasConversion<string>();
            
        builder.Property(e => e.Status)
            .HasConversion<string>();
            
        builder.Property(e => e.Priority)
            .HasDefaultValue(5);
            
        builder.Property(e => e.MaxAttempts)
            .HasDefaultValue(3);
            
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        // Indexes for performance
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Priority);
        builder.HasIndex(e => e.ScheduledFor);
        builder.HasIndex(e => new { e.Status, e.Priority, e.ScheduledFor })
            .HasDatabaseName("ix_email_outbox_processing");
            
        // Relationship
        builder.HasOne(e => e.Template)
            .WithMany(t => t.EmailOutboxes)
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}