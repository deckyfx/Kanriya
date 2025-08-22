using GQLServer.Data;
using GQLServer.Services;

namespace GQLServer.Queries;

/// <summary>
/// GraphQL queries for GreetLog operations
/// Uses GreetLogService for all data access
/// </summary>
public class GreetLogQueries
{
    /// <summary>
    /// Get all greet logs from the database
    /// Returns a list of all greeting entries with optional pagination
    /// Supports GraphQL filtering and sorting through attributes
    /// </summary>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="skip">Number of records to skip for pagination (optional)</param>
    /// <param name="take">Number of records to take for pagination (optional)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of GreetLog entries</returns>
    public async Task<IEnumerable<GreetLog>> GetGreetLogs(
        [Service] IGreetLogService service,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await service.GetAllAsync(skip, take, cancellationToken);
    }
    
    /// <summary>
    /// Get a specific greet log by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the greet log</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The greet log if found, null otherwise</returns>
    public async Task<GreetLog?> GetGreetLogById(
        string id,
        [Service] IGreetLogService service,
        CancellationToken cancellationToken = default)
    {
        return await service.GetByIdAsync(id, cancellationToken);
    }
    
    /// <summary>
    /// Get the most recent greet logs
    /// </summary>
    /// <param name="count">Number of recent logs to retrieve (default: 10, max: 100)</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A collection of the most recent greet logs</returns>
    public async Task<IEnumerable<GreetLog>> GetRecentGreetLogs(
        [Service] IGreetLogService service,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await service.GetRecentAsync(count, cancellationToken);
    }
    
    /// <summary>
    /// Search greet logs by content
    /// </summary>
    /// <param name="searchTerm">The search term to look for in content</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of matching greet logs</returns>
    public async Task<IEnumerable<GreetLog>> SearchGreetLogs(
        string searchTerm,
        [Service] IGreetLogService service,
        CancellationToken cancellationToken = default)
    {
        return await service.SearchByContentAsync(searchTerm, cancellationToken);
    }
    
    /// <summary>
    /// Get greet logs within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of greet logs within the date range</returns>
    public async Task<IEnumerable<GreetLog>> GetGreetLogsByDateRange(
        DateTime startDate,
        DateTime endDate,
        [Service] IGreetLogService service,
        CancellationToken cancellationToken = default)
    {
        return await service.GetByDateRangeAsync(startDate, endDate, cancellationToken);
    }
    
    /// <summary>
    /// Get the total count of greet logs
    /// </summary>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Total number of greet logs</returns>
    public async Task<int> GetGreetLogCount(
        [Service] IGreetLogService service,
        CancellationToken cancellationToken = default)
    {
        return await service.GetCountAsync(cancellationToken);
    }
}