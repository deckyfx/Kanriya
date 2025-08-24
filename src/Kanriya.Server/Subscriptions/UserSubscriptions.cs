using Kanriya.Server.Data;
using Kanriya.Server.Types.Outputs;

namespace Kanriya.Server.Subscriptions;

/// <summary>
/// GraphQL subscriptions for user-related events
/// Following standardized event structure
/// </summary>
[ExtendObjectType(typeof(RootSubscription))]
public class UserSubscriptions
{
    /// <summary>
    /// Subscribe to all user changes (created, updated, deleted, activated)
    /// </summary>
    [Subscribe]
    [Topic("UserChanged")]
    public SubscriptionEvent<User> OnUserChanged([EventMessage] SubscriptionEvent<User> userEvent)
    {
        return userEvent;
    }
    
    /// <summary>
    /// Subscribe to pending user changes (created, verified, expired)
    /// </summary>
    [Subscribe]
    [Topic("PendingUserChanged")]
    public SubscriptionEvent<PendingUser> OnPendingUserChanged([EventMessage] SubscriptionEvent<PendingUser> pendingUserEvent)
    {
        return pendingUserEvent;
    }
}