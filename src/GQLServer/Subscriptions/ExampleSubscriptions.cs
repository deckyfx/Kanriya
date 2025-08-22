using HotChocolate;
using HotChocolate.Types;

namespace GQLServer.Subscriptions;

// SUBSCRIPTIONS - For real-time updates
// ======================================
// Subscriptions allow clients to receive real-time updates when data changes

[ExtendObjectType("Subscription")]
public class ExampleSubscriptions
{
    // Simple subscription that sends a message every second
    // In GraphQL Playground:
    // subscription {
    //   onTimeUpdate
    // }
    public async IAsyncEnumerable<string> OnTimeUpdate()
    {
        while (true)
        {
            yield return $"Current time: {DateTime.Now:HH:mm:ss}";
            await Task.Delay(1000); // Wait 1 second
        }
    }
    
    // Subscription that listens for events
    // This would typically be triggered by a mutation
    [Subscribe]
    [Topic("message-created")]
    public string OnMessageCreated([EventMessage] string message)
        => message;
}