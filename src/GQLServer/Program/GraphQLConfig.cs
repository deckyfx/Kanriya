using HotChocolate.Data;
using GQLServer.Services;

namespace GQLServer.Program;

/// <summary>
/// Configures GraphQL server with all queries, mutations, subscriptions, and middleware
/// Version: 1.0.0 - GreetLog Management System
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
        services.AddSingleton<IGreetLogService, GreetLogService>();
        
        // Future services will be registered here
        // services.AddSingleton<IUserService, UserService>();
        // services.AddSingleton<ICategoryService, CategoryService>();
        
        // Register HTTP context accessor for headers in resolvers
        services.AddHttpContextAccessor();
        
        // Configure GraphQL server
        services
            .AddGraphQLServer()
            
            // Configure GraphQL types for GreetLog operations
            // Using direct type registration (not attribute-based)
            .AddQueryType<GQLServer.Queries.GreetLogQueries>()
            .AddMutationType<GQLServer.Mutations.GreetLogMutations>()
            .AddSubscriptionType<GQLServer.Subscriptions.GreetLogSubscriptions>()
            
            // Add filtering and sorting capabilities
            .AddFiltering()
            .AddSorting()
            
            // Enable authorization middleware (optional)
            // This allows using [Authorize] attributes on queries and mutations
            .AddAuthorization()
            
            // Enable in-memory subscriptions
            // This allows real-time updates via WebSocket connections
            .AddInMemorySubscriptions()
            
            // Set schema version
            .ModifyOptions(options => 
            {
                options.StrictValidation = false; // Allow nullable reference types
            });

        Console.WriteLine("✓ GraphQL server configured (v1.0.0):");
        Console.WriteLine("  - Architecture: Service Layer Pattern with Singleton Services");
        Console.WriteLine("  - Services: GreetLogService (Singleton with Scoped DbContext)");
        Console.WriteLine("  - Queries: GreetLog (list, getById, getRecent, search, dateRange, count)");
        Console.WriteLine("  - Mutations: GreetLog (add, update, delete, bulkAdd, deleteOld)");
        Console.WriteLine("  - Subscriptions: GreetLog (onAdded, onUpdated, onDeleted)");
        Console.WriteLine("  - Features: Filtering, Sorting, Pagination");
        Console.WriteLine("  - Authorization: Enabled (optional)");
        Console.WriteLine("  - In-Memory Subscriptions: Enabled");
    }
    
    /// <summary>
    /// Configure middleware pipeline for GraphQL
    /// </summary>
    public static void ConfigureGraphQLMiddleware(WebApplication app)
    {
        // Authentication must come before Authorization
        app.UseAuthentication();
        
        // Authorization must come before MapGraphQL
        app.UseAuthorization();
        
        // WebSockets required for GraphQL subscriptions
        app.UseWebSockets();
        
        // Map GraphQL endpoint at /graphql
        app.MapGraphQL();
        
        Console.WriteLine("✓ GraphQL middleware configured at /graphql endpoint");
    }
}