using Kanriya.Server.Data;
using HotChocolate;

namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Standardized event type for all subscription events
/// </summary>
public enum EventType
{
    /// <summary>
    /// A new record was created
    /// </summary>
    Created,
    
    /// <summary>
    /// An existing record was updated
    /// </summary>
    Updated,
    
    /// <summary>
    /// A record was deleted
    /// </summary>
    Deleted
}

/// <summary>
/// Standardized subscription event structure for GreetLog
/// This pattern will be used for all future entity subscriptions
/// </summary>
public class GreetLogEvent
{
    /// <summary>
    /// The type of event that occurred (created, updated, deleted)
    /// </summary>
    [GraphQLName("event")]
    public EventType Event { get; set; }
    
    /// <summary>
    /// The current state of the document (null for delete events)
    /// </summary>
    [GraphQLName("document")]
    public GreetLog? Document { get; set; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    [GraphQLName("time")]
    public DateTime Time { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Previous state of the document (for updates and deletes)
    /// Contains the full previous object, not just changed fields
    /// </summary>
    [GraphQLName("_previous")]
    public GreetLog? Previous { get; set; }
}