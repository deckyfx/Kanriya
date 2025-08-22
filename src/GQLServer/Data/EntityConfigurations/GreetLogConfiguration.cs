using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GQLServer.Data.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for the GreetLog entity
/// Defines table mapping, column specifications, indexes, and relationships
/// </summary>
public class GreetLogConfiguration : IEntityTypeConfiguration<GreetLog>
{
    /// <summary>
    /// Configures the GreetLog entity mapping to the database
    /// </summary>
    /// <param name="builder">The entity type builder for GreetLog</param>
    public void Configure(EntityTypeBuilder<GreetLog> builder)
    {
        // Table Configuration
        // Map entity to the greet_logs table in the database
        builder.ToTable("greet_logs");
        
        // Primary Key Configuration
        // Define Id as the primary key
        builder.HasKey(e => e.Id);
        
        // Property Configurations
        ConfigureIdProperty(builder);
        ConfigureTimestampProperty(builder);
        ConfigureContentProperty(builder);
        
        // Index Configurations
        ConfigureIndexes(builder);
        
        // Seed Data (optional)
        // ConfigureSeedData(builder);
    }
    
    /// <summary>
    /// Configure the Id property specifications
    /// </summary>
    private static void ConfigureIdProperty(EntityTypeBuilder<GreetLog> builder)
    {
        builder.Property(e => e.Id)
            .HasMaxLength(50)                    // Limit ID length to 50 characters
            .IsRequired()                         // NOT NULL constraint
            .HasColumnType("varchar(50)")         // Explicit PostgreSQL type
            .HasComment("Unique identifier for the greet log entry");
            // Column name will be 'id' due to snake_case convention
    }
    
    /// <summary>
    /// Configure the Timestamp property specifications
    /// </summary>
    private static void ConfigureTimestampProperty(EntityTypeBuilder<GreetLog> builder)
    {
        builder.Property(e => e.Timestamp)
            .IsRequired()                         // NOT NULL constraint
            .HasColumnType("timestamp with time zone") // PostgreSQL timestamp with timezone
            .HasDefaultValueSql("CURRENT_TIMESTAMP")   // Default to current time on insert
            .HasComment("UTC timestamp when the greeting was logged");
            // Column name will be 'timestamp' due to snake_case convention
    }
    
    /// <summary>
    /// Configure the Content property specifications
    /// </summary>
    private static void ConfigureContentProperty(EntityTypeBuilder<GreetLog> builder)
    {
        builder.Property(e => e.Content)
            .HasMaxLength(500)                    // Limit content to 500 characters
            .IsRequired()                         // NOT NULL constraint
            .HasColumnType("varchar(500)")        // Explicit PostgreSQL type
            .HasComment("The greeting message content");
            // Column name will be 'content' due to snake_case convention
    }
    
    /// <summary>
    /// Configure database indexes for optimized querying
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<GreetLog> builder)
    {
        // Index on Timestamp for efficient date-based queries
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_greet_logs_timestamp")
            .IsDescending(); // Create descending index for newest-first queries
        
        // Composite index for content search with timestamp (if needed in future)
        // builder.HasIndex(e => new { e.Content, e.Timestamp })
        //     .HasDatabaseName("IX_greet_logs_content_timestamp");
        
        // Full-text search index (if needed in future)
        // builder.HasIndex(e => e.Content)
        //     .HasDatabaseName("IX_greet_logs_content_fulltext")
        //     .HasAnnotation("Npgsql:IndexMethod", "gin")
        //     .HasAnnotation("Npgsql:TsVectorConfig", "english");
    }
    
    /// <summary>
    /// Configure seed data for initial database population (optional)
    /// </summary>
    private static void ConfigureSeedData(EntityTypeBuilder<GreetLog> builder)
    {
        // Example seed data - uncomment if needed
        /*
        builder.HasData(
            new GreetLog 
            { 
                Id = "seed-001", 
                Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Content = "Welcome to GreetLog System!" 
            },
            new GreetLog 
            { 
                Id = "seed-002", 
                Timestamp = new DateTime(2024, 1, 1, 0, 1, 0, DateTimeKind.Utc),
                Content = "This is a sample greeting." 
            }
        );
        */
    }
}