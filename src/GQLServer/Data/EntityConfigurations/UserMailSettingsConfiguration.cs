using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GQLServer.Data.EntityConfigurations;

public class UserMailSettingsConfiguration : IEntityTypeConfiguration<UserMailSettings>
{
    public void Configure(EntityTypeBuilder<UserMailSettings> builder)
    {
        builder.ToTable("user_mail_settings");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.HasIndex(e => e.UserId)
            .IsUnique();
            
        builder.Property(e => e.MailDriver)
            .HasConversion<string>();
            
        builder.Property(e => e.SmtpEncryption)
            .HasConversion<string>();
            
        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);
            
        builder.Property(e => e.SentToday)
            .HasDefaultValue(0);
            
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}