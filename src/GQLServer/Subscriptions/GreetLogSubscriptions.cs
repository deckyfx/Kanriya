using GQLServer.Data;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;

namespace GQLServer.Subscriptions;

/// <summary>
/// GraphQL subscriptions for GreetLog operations
/// Provides real-time updates when greet logs are added, updated, or deleted
/// </summary>
public class GreetLogSubscriptions
{
    /// <summary>
    /// Subscribe to be notified when a new greet log is added
    /// Clients will receive real-time updates whenever a new greeting is created
    /// </summary>
    /// <param name="greetLog">The newly added greet log (provided by the event)</param>
    /// <returns>The newly added greet log</returns>
    [Subscribe]
    [Topic]
    public GreetLog OnGreetLogAdded([EventMessage] GreetLog greetLog) => greetLog;
    
    /// <summary>
    /// Subscribe to be notified when a greet log is updated
    /// Clients will receive real-time updates whenever a greeting's content is modified
    /// </summary>
    /// <param name="greetLog">The updated greet log (provided by the event)</param>
    /// <returns>The updated greet log</returns>
    [Subscribe]
    [Topic]
    public GreetLog OnGreetLogUpdated([EventMessage] GreetLog greetLog) => greetLog;
    
    /// <summary>
    /// Subscribe to be notified when a greet log is deleted
    /// Clients will receive the ID of the deleted greet log
    /// </summary>
    /// <param name="greetLogId">The ID of the deleted greet log (provided by the event)</param>
    /// <returns>The ID of the deleted greet log</returns>
    [Subscribe]
    [Topic]
    public string OnGreetLogDeleted([EventMessage] string greetLogId) => greetLogId;
    
    // Note: Combined watch subscription removed in v1.0.0 for simplicity
    // Use individual subscriptions (onGreetLogAdded, onGreetLogUpdated, onGreetLogDeleted) instead
}