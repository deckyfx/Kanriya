using GQLServer.Data;

namespace GQLServer.Services;

/// <summary>
/// Service interface for GreetLog operations
/// Defines the contract for all GreetLog data access and business logic
/// </summary>
public interface IGreetLogService
{
    // ==================== CREATE OPERATIONS ====================
    
    /// <summary>
    /// Create a new greet log entry
    /// </summary>
    /// <param name="content">The greeting message content</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The created greet log entry</returns>
    Task<GreetLog> CreateAsync(string content, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create multiple greet log entries in bulk
    /// </summary>
    /// <param name="contents">Collection of greeting message contents</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of created greet log entries</returns>
    Task<IEnumerable<GreetLog>> CreateBulkAsync(IEnumerable<string> contents, CancellationToken cancellationToken = default);
    
    // ==================== READ OPERATIONS ====================
    
    /// <summary>
    /// Get a greet log by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the greet log</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The greet log if found, null otherwise</returns>
    Task<GreetLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all greet logs with optional pagination
    /// </summary>
    /// <param name="skip">Number of records to skip (for pagination)</param>
    /// <param name="take">Number of records to take (for pagination)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of greet logs</returns>
    Task<IEnumerable<GreetLog>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get recent greet logs
    /// </summary>
    /// <param name="count">Number of recent logs to retrieve</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of recent greet logs</returns>
    Task<IEnumerable<GreetLog>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get greet logs within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of greet logs within the date range</returns>
    Task<IEnumerable<GreetLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search greet logs by content
    /// </summary>
    /// <param name="searchTerm">The search term to look for in content</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of matching greet logs</returns>
    Task<IEnumerable<GreetLog>> SearchByContentAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get queryable for advanced filtering (used by GraphQL)
    /// </summary>
    /// <returns>IQueryable for LINQ operations</returns>
    IQueryable<GreetLog> GetQueryable();
    
    // ==================== UPDATE OPERATIONS ====================
    
    /// <summary>
    /// Update a greet log's content
    /// </summary>
    /// <param name="id">The ID of the greet log to update</param>
    /// <param name="newContent">The new content</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The updated greet log if found and updated, null otherwise</returns>
    Task<GreetLog?> UpdateAsync(string id, string newContent, CancellationToken cancellationToken = default);
    
    // ==================== DELETE OPERATIONS ====================
    
    /// <summary>
    /// Delete a greet log by its ID
    /// </summary>
    /// <param name="id">The ID of the greet log to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete multiple greet logs by their IDs
    /// </summary>
    /// <param name="ids">Collection of IDs to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of records deleted</returns>
    Task<int> DeleteBulkAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete greet logs older than a specified date
    /// </summary>
    /// <param name="olderThan">Delete records older than this date</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of records deleted</returns>
    Task<int> DeleteOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    
    // ==================== UTILITY OPERATIONS ====================
    
    /// <summary>
    /// Get the total count of greet logs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Total number of greet logs</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a greet log exists by ID
    /// </summary>
    /// <param name="id">The ID to check</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}