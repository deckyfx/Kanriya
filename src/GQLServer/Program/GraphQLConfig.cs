using GQLServer.Services;

namespace GQLServer.Program;

/// <summary>
/// Configures GraphQL server with all queries, mutations, subscriptions, and middleware
/// </summary>
public static class GraphQLConfig
{
    /// <summary>
    /// Configure GraphQL server with all types and extensions
    /// </summary>
    public static void ConfigureGraphQL(IServiceCollection services)
    {
        // Register application services
        services.AddSingleton<AuthService>(); // Authentication service for user management
        services.AddHttpContextAccessor(); // Required for accessing HTTP headers in resolvers
        
        // Configure GraphQL server
        services
            .AddGraphQLServer()
            
            // Configure base GraphQL types
            // These are the root types that will be extended
            .AddQueryType(q => q.Name("Query"))
            .AddMutationType(m => m.Name("Mutation"))
            .AddSubscriptionType(s => s.Name("Subscription"))
            
            // Add query extensions from /Queries folder
            // Each class extends the root Query type with additional fields
            .AddTypeExtension<GQLServer.Queries.BasicQueries>()
            .AddTypeExtension<GQLServer.Queries.HelloQueries>()
            .AddTypeExtension<GQLServer.Queries.GreetingQueries>()
            .AddTypeExtension<GQLServer.Queries.TimeQueries>()
            .AddTypeExtension<GQLServer.Queries.UserQueries>()
            .AddTypeExtension<GQLServer.Queries.HeaderQueries>()
            
            // Add mutation extensions from /Mutations folder
            // Each class extends the root Mutation type with additional fields
            .AddTypeExtension<GQLServer.Mutations.ExampleMutations>()
            .AddTypeExtension<GQLServer.Mutations.AuthMutations>()
            .AddTypeExtension<GQLServer.Mutations.ProtectedMutations>()
            
            // Add subscription extensions from /Subscriptions folder
            // Each class extends the root Subscription type with additional fields
            .AddTypeExtension<GQLServer.Subscriptions.ExampleSubscriptions>()
            
            // Enable authorization middleware
            // This allows using [Authorize] attributes on queries and mutations
            .AddAuthorization()
            
            // Enable in-memory subscriptions
            // This allows real-time updates via WebSocket connections
            .AddInMemorySubscriptions();

        Console.WriteLine("✓ GraphQL server configured:");
        Console.WriteLine("  - Queries: Basic, Hello, Greeting, Time, User, Header");
        Console.WriteLine("  - Mutations: Example, Auth, Protected");
        Console.WriteLine("  - Subscriptions: Example");
        Console.WriteLine("  - Authorization: Enabled");
        Console.WriteLine("  - In-Memory Subscriptions: Enabled");
        Console.WriteLine("  - HTTP Context Access: Enabled");
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