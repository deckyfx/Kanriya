using GQLServer.Data;
using GQLServer.Services;
using GQLServer.Types.Inputs;
using GQLServer.Types.Outputs;
using GQLServer.Queries;
using GQLServer.Mutations;
using GQLServer.Subscriptions;
using HotChocolate.Subscriptions;
using HotChocolate.Execution;

namespace GQLServer.Modules;

/// <summary>
/// GraphQL module for GreetLog domain
/// Contains all queries, mutations, and subscriptions related to GreetLogs
/// </summary>
[ExtendObjectType(typeof(RootQuery))]
public class GreetLogQueries
{
    /// <summary>
    /// Get all greet logs from the database
    /// </summary>
    [GraphQLName("greetLogs")]
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
    [GraphQLName("greetLogById")]
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
    [GraphQLName("recentGreetLogs")]
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
    [GraphQLName("searchGreetLogs")]
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
    [GraphQLName("greetLogsByDateRange")]
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
    [GraphQLName("greetLogCount")]
    public async Task<int> GetGreetLogCount(
        [Service] IGreetLogService service,
        CancellationToken cancellationToken = default)
    {
        return await service.GetCountAsync(cancellationToken);
    }
}

[ExtendObjectType(typeof(RootSubscription))]
public class GreetLogMutations
{
    /// <summary>
    /// Add a new greet log to the database
    /// </summary>
    [GraphQLName("addGreetLog")]
    public async Task<GreetLog> AddGreetLog(
        AddGreetLogInput input,
        [Service] IGreetLogService service,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        var greetLog = await service.CreateAsync(input.Content, cancellationToken);
        
        // Publish event for subscriptions
        var evt = new GreetLogEvent
        {
            Event = EventType.Created,
            Document = greetLog,
            Time = DateTime.UtcNow,
            Previous = null
        };
        await sender.SendAsync("GreetLogChanges", evt, cancellationToken);
        
        return greetLog;
    }
    
    /// <summary>
    /// Update an existing greet log
    /// </summary>
    [GraphQLName("updateGreetLog")]
    public async Task<GreetLog> UpdateGreetLog(
        string id,
        string newContent,
        [Service] IGreetLogService service,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        var oldGreetLog = await service.GetByIdAsync(id, cancellationToken);
        if (oldGreetLog == null)
        {
            throw new GraphQLException($"GreetLog with ID '{id}' not found.");
        }
        
        var updatedGreetLog = await service.UpdateAsync(id, newContent, cancellationToken);
        if (updatedGreetLog == null)
        {
            throw new GraphQLException($"Failed to update GreetLog with ID '{id}'.");
        }
        
        // Publish event for subscriptions
        var evt = new GreetLogEvent
        {
            Event = EventType.Updated,
            Document = updatedGreetLog,
            Time = DateTime.UtcNow,
            Previous = oldGreetLog
        };
        await sender.SendAsync("GreetLogChanges", evt, cancellationToken);
        
        return updatedGreetLog;
    }
    
    /// <summary>
    /// Delete a greet log
    /// </summary>
    [GraphQLName("deleteGreetLog")]
    public async Task<bool> DeleteGreetLog(
        string id,
        [Service] IGreetLogService service,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        var greetLog = await service.GetByIdAsync(id, cancellationToken);
        if (greetLog == null)
        {
            throw new GraphQLException($"GreetLog with ID '{id}' not found.");
        }
        
        var result = await service.DeleteAsync(id, cancellationToken);
        
        if (result)
        {
            // Publish event for subscriptions
            var evt = new GreetLogEvent
            {
                Event = EventType.Deleted,
                Document = null,
                Time = DateTime.UtcNow,
                Previous = greetLog
            };
            await sender.SendAsync("GreetLogChanges", evt, cancellationToken);
        }
        
        return result;
    }
    
    /// <summary>
    /// Add multiple greet logs in bulk
    /// </summary>
    [GraphQLName("addGreetLogsBulk")]
    public async Task<IEnumerable<GreetLog>> AddGreetLogsBulk(
        List<string> contents,
        [Service] IGreetLogService service,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        var greetLogs = await service.CreateBulkAsync(contents, cancellationToken);
        
        // Publish events for each created log
        foreach (var greetLog in greetLogs)
        {
            var evt = new GreetLogEvent
            {
                Event = EventType.Created,
                Document = greetLog,
                Time = DateTime.UtcNow,
                Previous = null
            };
            await sender.SendAsync("GreetLogChanges", evt, cancellationToken);
        }
        
        return greetLogs;
    }
    
    /// <summary>
    /// Delete old greet logs
    /// </summary>
    [GraphQLName("deleteOldGreetLogs")]
    public async Task<int> DeleteOldGreetLogs(
        DateTime olderThan,
        [Service] IGreetLogService service,
        CancellationToken cancellationToken = default)
    {
        return await service.DeleteOlderThanAsync(olderThan, cancellationToken);
    }
}

[ExtendObjectType(typeof(RootSubscription))]
public class GreetLogSubscriptions
{
    /// <summary>
    /// Subscribe to all greet log changes (created, updated, deleted)
    /// </summary>
    [Subscribe]
    [Topic("GreetLogChanges")]
    [GraphQLName("onGreetLogChanged")]
    public GreetLogEvent OnGreetLogChanged([EventMessage] GreetLogEvent greetLogEvent) => greetLogEvent;
}