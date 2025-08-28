using Kanriya.Server.Data.BrandSchema;
using Kanriya.Server.Types.Outputs;
using HotChocolate;

namespace Kanriya.Server.Subscriptions;

/// <summary>
/// GraphQL subscriptions for outlet-related events
/// Following standardized event structure
/// </summary>
[ExtendObjectType(typeof(RootSubscription))]
public class OutletSubscriptions
{
    /// <summary>
    /// Subscribe to all outlet changes (created, updated, deleted)
    /// </summary>
    [Subscribe]
    [Topic("OutletChanged")]
    public SubscriptionEvent<Outlet> OnOutletChanged([EventMessage] SubscriptionEvent<Outlet> outletEvent)
    {
        return outletEvent;
    }
}