using Kanriya.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kanriya.Server.Services;

/// <summary>
/// Service implementation for GreetLog operations
/// Provides data access and business logic for greeting logs
/// Registered as a singleton service for performance
/// </summary>
public class GreetLogService : IGreetLogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GreetLogService> _logger;
    
    /// <summary>
    /// Initializes a new instance of the GreetLogService
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped DbContext</param>
    /// <param name="logger">Logger for service operations</param>
    public GreetLogService(IServiceProvider serviceProvider, ILogger<GreetLogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates a scoped DbContext for database operations
    /// Since this service is a singleton, we need to create scoped DbContext instances
    /// </summary>
    private IServiceScope CreateScope() => _serviceProvider.CreateScope();
    
    // ==================== CREATE OPERATIONS ====================
    
    /// <summary>
    /// Create a new greet log entry
    /// </summary>
    public async Task<GreetLog> CreateAsync(string content, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be empty", nameof(content));
        }
        
        if (content.Length > 500)
        {
            throw new ArgumentException("Content cannot exceed 500 characters", nameof(content));
        }
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var greetLog = new GreetLog
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Content = content.Trim()
        };
        
        dbContext.GreetLogs.Add(greetLog);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Created new greet log with ID: {Id}", greetLog.Id);
        
        return greetLog;
    }
    
    /// <summary>
    /// Create multiple greet log entries in bulk
    /// </summary>
    public async Task<IEnumerable<GreetLog>> CreateBulkAsync(IEnumerable<string> contents, CancellationToken cancellationToken = default)
    {
        var contentList = contents.ToList();
        
        // Validate all contents
        foreach (var content in contentList)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Content cannot be empty");
            }
            
            if (content.Length > 500)
            {
                throw new ArgumentException($"Content '{content.Substring(0, 50)}...' exceeds 500 characters");
            }
        }
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var greetLogs = contentList.Select(content => new GreetLog
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Content = content.Trim()
        }).ToList();
        
        dbContext.GreetLogs.AddRange(greetLogs);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Created {Count} greet logs in bulk", greetLogs.Count);
        
        return greetLogs;
    }
    
    // ==================== READ OPERATIONS ====================
    
    /// <summary>
    /// Get a greet log by its ID
    /// </summary>
    public async Task<GreetLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await dbContext.GreetLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }
    
    /// <summary>
    /// Get all greet logs with optional pagination
    /// </summary>
    public async Task<IEnumerable<GreetLog>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        IQueryable<GreetLog> query = dbContext.GreetLogs
            .AsNoTracking()
            .OrderByDescending(g => g.Timestamp);
        
        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }
        
        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }
        
        return await query.ToListAsync(cancellationToken);
    }
    
    /// <summary>
    /// Get recent greet logs
    /// </summary>
    public async Task<IEnumerable<GreetLog>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        // Validate and limit count
        if (count < 1) count = 1;
        if (count > 100) count = 100;
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await dbContext.GreetLogs
            .AsNoTracking()
            .OrderByDescending(g => g.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
    
    /// <summary>
    /// Get greet logs within a date range
    /// </summary>
    public async Task<IEnumerable<GreetLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // Ensure dates are in UTC
        startDate = startDate.ToUniversalTime();
        endDate = endDate.ToUniversalTime();
        
        // Validate date range
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await dbContext.GreetLogs
            .AsNoTracking()
            .Where(g => g.Timestamp >= startDate && g.Timestamp <= endDate)
            .OrderByDescending(g => g.Timestamp)
            .ToListAsync(cancellationToken);
    }
    
    /// <summary>
    /// Search greet logs by content
    /// </summary>
    public async Task<IEnumerable<GreetLog>> SearchByContentAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Enumerable.Empty<GreetLog>();
        }
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Case-insensitive search using PostgreSQL ILIKE
        return await dbContext.GreetLogs
            .AsNoTracking()
            .Where(g => EF.Functions.ILike(g.Content, $"%{searchTerm}%"))
            .OrderByDescending(g => g.Timestamp)
            .ToListAsync(cancellationToken);
    }
    
    /// <summary>
    /// Get queryable for advanced filtering (used by GraphQL)
    /// Note: This returns a queryable that must be used within a scope
    /// </summary>
    public IQueryable<GreetLog> GetQueryable()
    {
        // This method is tricky for singleton services
        // The caller must ensure proper scope management
        // Consider making this service scoped instead of singleton if this is used frequently
        var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return dbContext.GreetLogs.AsNoTracking();
    }
    
    // ==================== UPDATE OPERATIONS ====================
    
    /// <summary>
    /// Update a greet log's content
    /// </summary>
    public async Task<GreetLog?> UpdateAsync(string id, string newContent, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(newContent))
        {
            throw new ArgumentException("Content cannot be empty", nameof(newContent));
        }
        
        if (newContent.Length > 500)
        {
            throw new ArgumentException("Content cannot exceed 500 characters", nameof(newContent));
        }
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var greetLog = await dbContext.GreetLogs
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        
        if (greetLog == null)
        {
            _logger.LogWarning("Attempted to update non-existent greet log with ID: {Id}", id);
            return null;
        }
        
        greetLog.Content = newContent.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated greet log with ID: {Id}", id);
        
        return greetLog;
    }
    
    // ==================== DELETE OPERATIONS ====================
    
    /// <summary>
    /// Delete a greet log by its ID
    /// </summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var greetLog = await dbContext.GreetLogs
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        
        if (greetLog == null)
        {
            _logger.LogWarning("Attempted to delete non-existent greet log with ID: {Id}", id);
            return false;
        }
        
        dbContext.GreetLogs.Remove(greetLog);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deleted greet log with ID: {Id}", id);
        
        return true;
    }
    
    /// <summary>
    /// Delete multiple greet logs by their IDs
    /// </summary>
    public async Task<int> DeleteBulkAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        
        if (!idList.Any())
        {
            return 0;
        }
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var greetLogs = await dbContext.GreetLogs
            .Where(g => idList.Contains(g.Id))
            .ToListAsync(cancellationToken);
        
        if (!greetLogs.Any())
        {
            return 0;
        }
        
        dbContext.GreetLogs.RemoveRange(greetLogs);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deleted {Count} greet logs in bulk", greetLogs.Count);
        
        return greetLogs.Count;
    }
    
    /// <summary>
    /// Delete greet logs older than a specified date
    /// </summary>
    public async Task<int> DeleteOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        // Ensure date is in UTC
        olderThan = olderThan.ToUniversalTime();
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var greetLogs = await dbContext.GreetLogs
            .Where(g => g.Timestamp < olderThan)
            .ToListAsync(cancellationToken);
        
        if (!greetLogs.Any())
        {
            return 0;
        }
        
        dbContext.GreetLogs.RemoveRange(greetLogs);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deleted {Count} greet logs older than {Date}", greetLogs.Count, olderThan);
        
        return greetLogs.Count;
    }
    
    // ==================== UTILITY OPERATIONS ====================
    
    /// <summary>
    /// Get the total count of greet logs
    /// </summary>
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await dbContext.GreetLogs.CountAsync(cancellationToken);
    }
    
    /// <summary>
    /// Check if a greet log exists by ID
    /// </summary>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await dbContext.GreetLogs
            .AnyAsync(g => g.Id == id, cancellationToken);
    }
}