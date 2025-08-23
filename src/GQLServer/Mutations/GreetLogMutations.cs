using GQLServer.Data;
using GQLServer.Services;
using GQLServer.Types.Inputs;
using GQLServer.Types.Outputs;
using HotChocolate.Subscriptions;

namespace GQLServer.Mutations;

/// <summary>
/// GraphQL mutations for GreetLog operations
/// Uses GreetLogService for all data access and business logic
/// </summary>
[ExtendObjectType(typeof(RootMutation))]
public class GreetLogMutations
{
    /// <summary>
    /// Add a new greet log entry to the database
    /// </summary>
    /// <param name="input">The input data for creating a new greet log</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="eventSender">Event sender for subscriptions</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The created greet log entry</returns>
    public async Task<GreetLog> AddGreetLog(
        AddGreetLogInput input,
        [Service] IGreetLogService service,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use service to create the greet log
            var greetLog = await service.CreateAsync(input.Content, cancellationToken);
            
            // Send event for unified subscription
            var eventData = new GreetLogEvent
            {
                Event = EventType.Created,
                Document = greetLog,
                Time = DateTime.UtcNow,
                Previous = null
            };
            await eventSender.SendAsync("GreetLogChanges", eventData, cancellationToken);
            
            return greetLog;
        }
        catch (ArgumentException ex)
        {
            // Convert ArgumentException to GraphQLException for proper error reporting
            throw new GraphQLException(ex.Message);
        }
    }
    
    /// <summary>
    /// Update an existing greet log's content
    /// </summary>
    /// <param name="id">The ID of the greet log to update</param>
    /// <param name="newContent">The new content for the greet log</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="eventSender">Event sender for subscriptions</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The updated greet log entry</returns>
    public async Task<GreetLog> UpdateGreetLog(
        string id,
        string newContent,
        [Service] IGreetLogService service,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the original greet log for comparison
            var originalGreetLog = await service.GetByIdAsync(id, cancellationToken);
            
            // Use service to update the greet log
            var greetLog = await service.UpdateAsync(id, newContent, cancellationToken);
            
            if (greetLog == null)
            {
                throw new GraphQLException($"GreetLog with ID '{id}' not found");
            }
            
            // Send event for unified subscription
            var eventData = new GreetLogEvent
            {
                Event = EventType.Updated,
                Document = greetLog,
                Time = DateTime.UtcNow,
                Previous = originalGreetLog
            };
            await eventSender.SendAsync("GreetLogChanges", eventData, cancellationToken);
            
            return greetLog;
        }
        catch (ArgumentException ex)
        {
            // Convert ArgumentException to GraphQLException for proper error reporting
            throw new GraphQLException(ex.Message);
        }
    }
    
    /// <summary>
    /// Delete a greet log entry from the database
    /// </summary>
    /// <param name="id">The ID of the greet log to delete</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="eventSender">Event sender for subscriptions</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if the deletion was successful, false otherwise</returns>
    public async Task<bool> DeleteGreetLog(
        string id,
        [Service] IGreetLogService service,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken = default)
    {
        // Get the greet log before deletion for the event
        var greetLogToDelete = await service.GetByIdAsync(id, cancellationToken);
        
        // Use service to delete the greet log
        var deleted = await service.DeleteAsync(id, cancellationToken);
        
        if (!deleted)
        {
            throw new GraphQLException($"GreetLog with ID '{id}' not found");
        }
        
        // Send event for unified subscription
        var eventData = new GreetLogEvent
        {
            Event = EventType.Deleted,
            Document = null, // No current state for deleted items
            Time = DateTime.UtcNow,
            Previous = greetLogToDelete // Full previous state
        };
        await eventSender.SendAsync("GreetLogChanges", eventData, cancellationToken);
        
        return true;
    }
    
    /// <summary>
    /// Create multiple greet log entries in bulk
    /// </summary>
    /// <param name="contents">Collection of greeting message contents</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of created greet log entries</returns>
    public async Task<IEnumerable<GreetLog>> AddGreetLogsBulk(
        IEnumerable<string> contents,
        [Service] IGreetLogService service,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await service.CreateBulkAsync(contents, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }
    
    /// <summary>
    /// Delete greet logs older than a specified date
    /// </summary>
    /// <param name="olderThan">Delete records older than this date</param>
    /// <param name="service">The greet log service injected by dependency injection</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of records deleted</returns>
    public async Task<int> DeleteOldGreetLogs(
        DateTime olderThan,
        [Service] IGreetLogService service,
        CancellationToken cancellationToken = default)
    {
        return await service.DeleteOlderThanAsync(olderThan, cancellationToken);
    }
}