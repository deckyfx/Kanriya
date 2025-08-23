using GQLServer.Data;
using GQLServer.Types.Outputs;
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
    /// Unified subscription for all GreetLog changes
    /// Single subscription that handles all event types (add, update, delete)
    /// More resource-efficient than multiple separate subscriptions
    /// </summary>
    /// <param name="greetLogEvent">The event containing type and data</param>
    /// <returns>The greet log event with all details</returns>
    [Subscribe]
    [Topic("GreetLogChanges")]
    public GreetLogEvent OnGreetLogChanged([EventMessage] GreetLogEvent greetLogEvent) => greetLogEvent;
}