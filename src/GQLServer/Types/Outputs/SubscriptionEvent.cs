using HotChocolate;

namespace GQLServer.Types.Outputs;

/// <summary>
/// Generic base class for all subscription events
/// This standardized structure ensures consistency across all entity subscriptions
/// </summary>
/// <typeparam name="T">The entity type being tracked</typeparam>
public class SubscriptionEvent<T> where T : class
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
    public T? Document { get; set; }
    
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
    public T? Previous { get; set; }
}

/// <summary>
/// Standard subscription event naming convention
/// </summary>
public static class SubscriptionTopics
{
    /// <summary>
    /// Generate standard topic name for an entity
    /// </summary>
    public static string ForEntity<T>() => $"{typeof(T).Name}Changes";
}