using Kanriya.Server.Types;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;

namespace Kanriya.Server.Program;

/// <summary>
/// HTTP request interceptor to inject CurrentUser into GraphQL global state
/// </summary>
public class CurrentUserGlobalState : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // Get CurrentUser from HttpContext items (set by authentication middleware)
        if (context.Items.TryGetValue("CurrentUser", out var currentUserObj) && 
            currentUserObj is CurrentUser currentUser)
        {
            // Add to global state for GraphQL resolvers
            // HotChocolate expects camelCase for parameter names
            requestBuilder.SetGlobalState("currentUser", currentUser);
        }
        else
        {
            // Add empty CurrentUser if not authenticated
            requestBuilder.SetGlobalState("currentUser", new CurrentUser());
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}