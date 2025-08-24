# Entity Configuration Guide

## Overview
This folder contains Entity Framework Core configuration files for each database entity. Each entity has its own configuration class that implements `IEntityTypeConfiguration<T>`, keeping the `AppDbContext` clean and maintainable.

## Structure
```
Data/
├── EntityConfigurations/
│   ├── GreetLogConfiguration.cs    # Configuration for GreetLog entity
│   ├── UserConfiguration.cs        # (Future) Configuration for User entity
│   └── README.md                    # This file
├── AppDbContext.cs                  # Main DbContext (auto-loads all configurations)
├── GreetLog.cs                      # Entity model
└── Migrations/                      # EF Core migrations
```

## Adding a New Entity

### 1. Create the Entity Model
Create your entity class in the `Data` folder:
```csharp
// Data/YourEntity.cs
public class YourEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
    // ... other properties
}
```

### 2. Create the Configuration Class
Create a configuration file in `Data/EntityConfigurations`:
```csharp
// Data/EntityConfigurations/YourEntityConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kanriya.Server.Data.EntityConfigurations;

public class YourEntityConfiguration : IEntityTypeConfiguration<YourEntity>
{
    public void Configure(EntityTypeBuilder<YourEntity> builder)
    {
        // Table name
        builder.ToTable("your_entities");
        
        // Primary key
        builder.HasKey(e => e.Id);
        
        // Properties
        builder.Property(e => e.Id)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_your_entities_name");
        
        // Relationships (if any)
        // builder.HasMany(e => e.RelatedItems)...
    }
}
```

### 3. Add DbSet to AppDbContext
Add the DbSet property to `AppDbContext.cs`:
```csharp
public DbSet<YourEntity> YourEntities { get; set; } = null!;
```

### 4. Create Migration
```bash
dotnet ef migrations add Add_YourEntity --output-dir Data/Migrations
dotnet ef database update
```

## Configuration Best Practices

### Property Configuration
- Always specify max length for string properties
- Use explicit column names and types for PostgreSQL
- Add comments for documentation
- Set appropriate default values

### Index Guidelines
- Add indexes for frequently queried columns
- Use composite indexes for multi-column queries
- Consider descending indexes for newest-first queries
- Use partial indexes for filtered queries

### PostgreSQL Specific Features
```csharp
// Full-text search index
builder.HasIndex(e => e.Content)
    .HasMethod("gin")
    .IsTsVectorExpressionIndex("english");

// JSON column
builder.Property(e => e.JsonData)
    .HasColumnType("jsonb");

// Array column
builder.Property(e => e.Tags)
    .HasColumnType("text[]");

// UUID generation
builder.Property(e => e.Id)
    .HasDefaultValueSql("gen_random_uuid()");
```

## Common Patterns

### Soft Delete
```csharp
builder.Property(e => e.IsDeleted)
    .HasDefaultValue(false);
    
builder.HasIndex(e => e.IsDeleted)
    .HasFilter("\"IsDeleted\" = false");
```

### Audit Fields
```csharp
builder.Property(e => e.CreatedAt)
    .HasDefaultValueSql("CURRENT_TIMESTAMP");
    
builder.Property(e => e.UpdatedAt)
    .HasDefaultValueSql("CURRENT_TIMESTAMP");
```

### Relationships
```csharp
// One-to-Many
builder.HasOne(e => e.Parent)
    .WithMany(p => p.Children)
    .HasForeignKey(e => e.ParentId)
    .OnDelete(DeleteBehavior.Cascade);

// Many-to-Many
builder.HasMany(e => e.Tags)
    .WithMany(t => t.Entities)
    .UsingEntity(j => j.ToTable("entity_tags"));
```

## Advantages of This Approach

1. **Maintainability**: Each entity configuration is in its own file
2. **Scalability**: Easy to add new entities without modifying AppDbContext
3. **Organization**: Clear separation of concerns
4. **Discoverability**: Easy to find and modify entity configurations
5. **Team Collaboration**: Reduces merge conflicts when multiple developers work on different entities
6. **Testing**: Configurations can be unit tested independently

## Auto-Discovery
The `AppDbContext` automatically discovers all configurations using:
```csharp
modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
```

This means new configuration classes are automatically applied without modifying `AppDbContext`.