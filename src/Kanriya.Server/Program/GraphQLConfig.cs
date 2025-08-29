using HotChocolate.Data;
using Kanriya.Server.Services;
using Kanriya.Server.Services.System;
using Kanriya.Server.Services.Data;
using Kanriya.Shared;
using Serilog.Context;
using System.Reflection;

namespace Kanriya.Server.Program;

/// <summary>
/// Configures GraphQL server with all queries, mutations, subscriptions, and middleware
/// Version: 1.0.0 - Kanriya Server
/// </summary>
public static class GraphQLConfig
{
    /// <summary>
    /// Configure GraphQL server with all types and extensions
    /// </summary>
    public static void ConfigureGraphQL(IServiceCollection services)
    {
        // Register application services as singletons for performance
        // Each service manages its own scoped DbContext instances
        services.AddSingleton<IUserService, UserService>();
        
        // Future services will be registered here
        // services.AddSingleton<ICategoryService, CategoryService>();
        
        // Register HTTP context accessor for headers in resolvers
        services.AddHttpContextAccessor();
        
        // Register GraphQL interceptors
        services.AddScoped<GraphQLLoggingInterceptor>();
        services.AddScoped<CurrentUserGlobalState>();
        
        // Configure GraphQL server
        services
            .AddGraphQLServer()
            .AddHttpRequestInterceptor<GraphQLLoggingInterceptor>()
            .AddHttpRequestInterceptor<CurrentUserGlobalState>()
            
            
            // Configure GraphQL with domain-based modules
            // Each module contains all operations (queries, mutations, subscriptions) for its domain
            .AddQueryType<Kanriya.Server.Queries.RootQuery>()
            .AddMutationType<Kanriya.Server.Mutations.RootMutation>()
            .AddSubscriptionType<Kanriya.Server.Subscriptions.RootSubscription>()
            
            // User/Auth Module - All User and Authentication operations
            .AddTypeExtension<Kanriya.Server.Modules.UserQueries>()
            .AddTypeExtension<Kanriya.Server.Modules.UserAuthMutations>()
            .AddTypeExtension<Kanriya.Server.Modules.UserManagementMutations>()
            .AddTypeExtension<Kanriya.Server.Modules.UserAccountModule>()  // Account deletion
            .AddTypeExtension<Kanriya.Server.Modules.UserSubscriptions>()
            
            // System Module - All System operations
            .AddTypeExtension<Kanriya.Server.Modules.SystemQueries>()
            
            // Brand Module - All Brand and Multi-Tenant operations
            .AddTypeExtension<Kanriya.Server.Modules.BrandQueries>()
            .AddTypeExtension<Kanriya.Server.Modules.BrandMutations>()
            
            // Outlet Module - Outlet management within brands
            .AddTypeExtension<Kanriya.Server.Modules.OutletQueries>()
            .AddTypeExtension<Kanriya.Server.Modules.OutletMutations>()
            .AddTypeExtension<Kanriya.Server.Subscriptions.OutletSubscriptions>()
            
            // Add filtering and sorting capabilities
            .AddFiltering()
            .AddSorting()
            
            // Enable authorization middleware (optional)
            // This allows using [Authorize] attributes on queries and mutations
            .AddAuthorization()
            
            // Enable in-memory subscriptions
            // This allows real-time updates via WebSocket connections
            .AddInMemorySubscriptions()
            
            // Set schema version and error handling
            .ModifyOptions(options => 
            {
                options.StrictValidation = false; // Allow nullable reference types
                options.UseXmlDocumentation = true; // Enable XML documentation
                options.EnableOneOf = true; // Enable OneOf input objects
            })
            
            // Add error filter for better error logging
            .AddErrorFilter(error =>
            {
                using (LogContext.PushProperty("Tag", "GraphQL"))
                {
                    // Log the error with full details
                    if (error.Exception != null)
                    {
                        LogService.Logger.Error(error.Exception, 
                            "[GraphQL] Error | Code: {Code} | Path: {Path} | Message: {Message}",
                            error.Code ?? "UNKNOWN",
                            error.Path?.Print() ?? "Unknown",
                            error.Message);
                    }
                    else
                    {
                        LogService.Logger.Warning(
                            "[GraphQL] Error | Code: {Code} | Path: {Path} | Message: {Message}",
                            error.Code ?? "UNKNOWN", 
                            error.Path?.Print() ?? "Unknown",
                            error.Message);
                    }
                }
                
                return error;
            });

        var assembly = Assembly.GetExecutingAssembly();
        LogService.LogSuccess($"GraphQL server configured ({BuildInfo.GetShortVersion(assembly)})");
    }
    
    /// <summary>
    /// Configure middleware pipeline for GraphQL
    /// </summary>
    public static void ConfigureGraphQLMiddleware(WebApplication app)
    {
        // Custom authentication middleware to populate CurrentUser
        app.UseCustomAuthentication();
        
        // Authentication must come before Authorization
        app.UseAuthentication();
        
        // Authorization must come before MapGraphQL
        app.UseAuthorization();
        
        // WebSockets required for GraphQL subscriptions
        app.UseWebSockets();
        
        // Map GraphQL endpoint at /graphql
        app.MapGraphQL();
        
        LogService.LogSuccess("GraphQL middleware configured at /graphql endpoint");
    }
}