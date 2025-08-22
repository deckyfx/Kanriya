# Service Layer Pattern Guide

## Overview
This folder contains service classes that implement business logic and data access for the application. Each entity has its own service class that encapsulates all operations related to that entity, providing a clean separation between GraphQL resolvers and database access.

## Architecture

```
Services/
├── IGreetLogService.cs      # Interface defining contract
├── GreetLogService.cs        # Implementation with business logic
├── IUserService.cs          # (Future) User service interface
├── UserService.cs           # (Future) User service implementation
└── README.md                # This file
```

## Service Pattern Benefits

1. **Separation of Concerns**: GraphQL resolvers focus on API concerns, services handle business logic
2. **Testability**: Services can be easily mocked for unit testing
3. **Reusability**: Business logic can be reused across different entry points (GraphQL, REST, CLI)
4. **Singleton Performance**: Services are registered as singletons for better performance
5. **Scoped DbContext**: Each operation creates its own scoped DbContext to avoid concurrency issues

## Creating a New Service

### 1. Create the Interface

```csharp
// Services/IYourEntityService.cs
using YourNamespace.Data;

namespace GQLServer.Services;

/// <summary>
/// Service interface for YourEntity operations
/// </summary>
public interface IYourEntityService
{
    // CREATE
    Task<YourEntity> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<YourEntity>> CreateBulkAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    
    // READ
    Task<YourEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<YourEntity>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<YourEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    IQueryable<YourEntity> GetQueryable(); // For GraphQL filtering/sorting
    
    // UPDATE
    Task<YourEntity?> UpdateAsync(string id, string newName, CancellationToken cancellationToken = default);
    
    // DELETE
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<int> DeleteBulkAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    
    // UTILITY
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
```

### 2. Create the Implementation

```csharp
// Services/YourEntityService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YourNamespace.Data;

namespace GQLServer.Services;

/// <summary>
/// Service implementation for YourEntity operations
/// </summary>
public class YourEntityService : IYourEntityService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<YourEntityService> _logger;
    
    public YourEntityService(IServiceProvider serviceProvider, ILogger<YourEntityService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates a scoped DbContext for database operations
    /// </summary>
    private IServiceScope CreateScope() => _serviceProvider.CreateScope();
    
    public async Task<YourEntity> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var entity = new YourEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        
        dbContext.YourEntities.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Created YourEntity with ID: {Id}", entity.Id);
        
        return entity;
    }
    
    // ... implement other methods
}
```

### 3. Register the Service

In `Program/GraphQLConfig.cs`:

```csharp
public static void ConfigureGraphQL(IServiceCollection services)
{
    // Register services as singletons
    services.AddSingleton<IGreetLogService, GreetLogService>();
    services.AddSingleton<IYourEntityService, YourEntityService>(); // Add your service
    
    // ... rest of configuration
}
```

### 4. Use in GraphQL Resolvers

#### In Queries:

```csharp
[QueryType]
public class YourEntityQueries
{
    public async Task<IEnumerable<YourEntity>> GetYourEntities(
        [Service] IYourEntityService service,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await service.GetAllAsync(skip, take, cancellationToken);
    }
    
    public async Task<YourEntity?> GetYourEntityById(
        string id,
        [Service] IYourEntityService service,
        CancellationToken cancellationToken = default)
    {
        return await service.GetByIdAsync(id, cancellationToken);
    }
}
```

#### In Mutations:

```csharp
[MutationType]
public class YourEntityMutations
{
    public async Task<YourEntity> CreateYourEntity(
        string name,
        [Service] IYourEntityService service,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await service.CreateAsync(name, cancellationToken);
            
            // Send subscription event
            await eventSender.SendAsync("OnYourEntityCreated", entity, cancellationToken);
            
            return entity;
        }
        catch (ArgumentException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }
}
```

## Service Method Patterns

### Validation Pattern
```csharp
public async Task<T> CreateAsync(string input, CancellationToken cancellationToken = default)
{
    // 1. Validate input
    if (string.IsNullOrWhiteSpace(input))
        throw new ArgumentException("Input cannot be empty", nameof(input));
    
    if (input.Length > MaxLength)
        throw new ArgumentException($"Input cannot exceed {MaxLength} characters", nameof(input));
    
    // 2. Create scope and get DbContext
    using var scope = CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // 3. Perform operation
    // 4. Log operation
    // 5. Return result
}
```

### Search Pattern (PostgreSQL)
```csharp
public async Task<IEnumerable<T>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(searchTerm))
        return Enumerable.Empty<T>();
    
    using var scope = CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Use PostgreSQL ILIKE for case-insensitive search
    return await dbContext.Set<T>()
        .AsNoTracking()
        .Where(e => EF.Functions.ILike(e.Name, $"%{searchTerm}%"))
        .ToListAsync(cancellationToken);
}
```

### Bulk Operations Pattern
```csharp
public async Task<IEnumerable<T>> CreateBulkAsync(IEnumerable<CreateInput> inputs, CancellationToken cancellationToken = default)
{
    var inputList = inputs.ToList();
    
    // Validate all inputs first
    foreach (var input in inputList)
    {
        ValidateInput(input);
    }
    
    using var scope = CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var entities = inputList.Select(input => CreateEntity(input)).ToList();
    
    dbContext.Set<T>().AddRange(entities);
    await dbContext.SaveChangesAsync(cancellationToken);
    
    _logger.LogInformation("Created {Count} entities in bulk", entities.Count);
    
    return entities;
}
```

## Best Practices

### 1. Scoped DbContext Management
Since services are singletons, always create a scoped DbContext:
```csharp
using var scope = CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

### 2. Consistent Error Handling
- Use `ArgumentException` for validation errors
- Convert to `GraphQLException` in resolvers
- Log all operations for debugging

### 3. Async/Await Pattern
- Always use async methods for I/O operations
- Include `CancellationToken` support
- Use `ConfigureAwait(false)` in library code (not needed here)

### 4. Query Optimization
- Use `AsNoTracking()` for read-only queries
- Use `Include()` for eager loading relationships
- Implement pagination for large datasets

### 5. Logging
```csharp
_logger.LogInformation("Operation completed for entity {Id}", entity.Id);
_logger.LogWarning("Attempted to access non-existent entity {Id}", id);
_logger.LogError(ex, "Error occurred during operation");
```

## Testing Services

### Unit Testing Example
```csharp
[TestClass]
public class YourEntityServiceTests
{
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<ILogger<YourEntityService>> _loggerMock;
    private YourEntityService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<YourEntityService>>();
        _service = new YourEntityService(_serviceProviderMock.Object, _loggerMock.Object);
    }
    
    [TestMethod]
    public async Task CreateAsync_WithValidInput_ReturnsEntity()
    {
        // Arrange
        var dbContextMock = new Mock<AppDbContext>();
        // ... setup mocks
        
        // Act
        var result = await _service.CreateAsync("Test", CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Name);
    }
}
```

## Common Service Methods Reference

| Method | Purpose | Returns |
|--------|---------|---------|
| `CreateAsync` | Create single entity | Created entity |
| `CreateBulkAsync` | Create multiple entities | Collection of created entities |
| `GetByIdAsync` | Get entity by ID | Entity or null |
| `GetAllAsync` | Get all entities with pagination | Collection of entities |
| `GetRecentAsync` | Get recent entities | Collection of entities |
| `GetByDateRangeAsync` | Get entities in date range | Collection of entities |
| `SearchAsync` | Search entities | Collection of matching entities |
| `GetQueryable` | Get IQueryable for LINQ | IQueryable<T> |
| `UpdateAsync` | Update entity | Updated entity or null |
| `DeleteAsync` | Delete single entity | Boolean success |
| `DeleteBulkAsync` | Delete multiple entities | Count of deleted |
| `DeleteOlderThanAsync` | Delete old entities | Count of deleted |
| `GetCountAsync` | Get total count | Integer count |
| `ExistsAsync` | Check if entity exists | Boolean |

## Performance Considerations

1. **Singleton Services**: Services are registered as singletons for better performance
2. **Scoped DbContext**: Each operation creates its own scope to avoid concurrency issues
3. **Connection Pooling**: Entity Framework manages connection pooling automatically
4. **Async Operations**: All I/O operations are async for better scalability
5. **Query Optimization**: Use indexes and appropriate query patterns

## Future Enhancements

- [ ] Add caching layer (Redis/Memory Cache)
- [ ] Implement Unit of Work pattern
- [ ] Add transaction support for complex operations
- [ ] Implement audit logging
- [ ] Add health checks for services
- [ ] Implement retry policies for transient failures
- [ ] Add metrics collection (Prometheus/Application Insights)