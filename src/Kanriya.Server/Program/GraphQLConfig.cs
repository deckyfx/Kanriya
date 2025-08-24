using HotChocolate.Data;
using Kanriya.Server.Services;

namespace Kanriya.Server.Program;

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
        services.AddSingleton<IUserService, UserService>();
        
        // Future services will be registered here
        // services.AddSingleton<ICategoryService, CategoryService>();
        
        // Register HTTP context accessor for headers in resolvers
        services.AddHttpContextAccessor();
        
        // Configure GraphQL server
        services
            .AddGraphQLServer()
            .AddHttpRequestInterceptor<CurrentUserGlobalState>()
            
            // Configure GraphQL with domain-based modules
            // Each module contains all operations (queries, mutations, subscriptions) for its domain
            .AddQueryType<Kanriya.Server.Queries.RootQuery>()
            .AddMutationType<Kanriya.Server.Mutations.RootMutation>()
            .AddSubscriptionType<Kanriya.Server.Subscriptions.RootSubscription>()
            
            // GreetLog Module - All GreetLog operations
            .AddTypeExtension<Kanriya.Server.Modules.GreetLogQueries>()
            .AddTypeExtension<Kanriya.Server.Modules.GreetLogMutations>()
            .AddTypeExtension<Kanriya.Server.Modules.GreetLogSubscriptions>()
            
            // User/Auth Module - All User and Authentication operations
            .AddTypeExtension<Kanriya.Server.Modules.UserQueries>()
            .AddTypeExtension<Kanriya.Server.Modules.UserAuthMutations>()
            .AddTypeExtension<Kanriya.Server.Modules.UserManagementMutations>()
            .AddTypeExtension<Kanriya.Server.Modules.UserSubscriptions>()
            
            // System Module - All System operations
            .AddTypeExtension<Kanriya.Server.Modules.SystemQueries>()
            
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

        Console.WriteLine($"✓ GraphQL server configured ({AppVersion.GetShortVersion()}):");
        Console.WriteLine("  - Architecture: Domain-Based Modular Design");
        Console.WriteLine("  - Services:");
        Console.WriteLine("    • GreetLogService: CRUD operations for greet logs");
        Console.WriteLine("    • UserService: Authentication and user management");
        Console.WriteLine("");
        Console.WriteLine("  - Domain Modules:");
        Console.WriteLine("    📦 GreetLog Module:");
        Console.WriteLine("       Queries: greetLogs, greetLogById, recentGreetLogs, searchGreetLogs,");
        Console.WriteLine("                greetLogsByDateRange, greetLogCount");
        Console.WriteLine("       Mutations: addGreetLog, updateGreetLog, deleteGreetLog,");
        Console.WriteLine("                  addGreetLogsBulk, deleteOldGreetLogs");
        Console.WriteLine("       Subscriptions: onGreetLogChanged");
        Console.WriteLine("");
        Console.WriteLine("    👤 User/Auth Module:");
        Console.WriteLine("       Queries: me, userById, userByEmail, users, pendingUsers, isEmailAvailable");
        Console.WriteLine("       Auth Mutations: signUp, verifyEmail, signIn, resendVerification,");
        Console.WriteLine("                       changePassword, updateProfile");
        Console.WriteLine("       Admin Mutations: grantRole, revokeRole, deleteUser, forceVerifyUser");
        Console.WriteLine("       Subscriptions: onUserChanged, onPendingUserChanged");
        Console.WriteLine("");
        Console.WriteLine("    ⚙️ System Module:");
        Console.WriteLine("       Queries: version, health");
        Console.WriteLine("");
        Console.WriteLine("  - Features: Filtering, Sorting, Pagination, JWT Authentication");
        Console.WriteLine("  - Authorization: Role-based policies (SuperAdmin, BusinessOwner, etc.)");
        Console.WriteLine("  - In-Memory Subscriptions: Enabled");
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
        
        Console.WriteLine("✓ GraphQL middleware configured at /graphql endpoint");
    }
}