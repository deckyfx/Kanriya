using GQLServer.Program;
using GQLServer.Services;
using GQLServer.Constants;
using Serilog;
using Serilog.Events;

// GRAPHQL SERVER WITH GREETLOG MANAGEMENT
// =================================================
// Version: 1.0.1 (GreetLog) - 2025-08-22
// This server provides a simple greeting log system with:
// - PostgreSQL database integration using Entity Framework Core
// - CRUD operations for greeting logs
// - Real-time subscriptions for live updates
// - JWT authentication support (optional)
//
// Main Features:
// - GreetLog entity with ID, Timestamp, and Content
// - Query operations: list all, get by ID, get recent logs
// - Mutation operations: add, update, delete greet logs
// - Subscription operations: watch for additions, updates, deletions
//
// Configuration is modularized into separate classes:
// - EnvironmentConfig: Handles .env file loading, server URLs, and database connection
// - DatabaseConfig: Configures Entity Framework with PostgreSQL
// - JwtConfig: Configures JWT Bearer authentication (optional)
// - AuthorizationConfig: Sets up authorization policies (optional)
// - GraphQLConfig: Configures GraphQL server and middleware

try
{
    // Step 1: Create the web application builder
    var builder = WebApplication.CreateBuilder(args);
    
    // Step 2: Load environment configuration FIRST (before logging)
    // This loads .env file and configures server URLs from APP_IP and APP_PORT
    EnvironmentConfig.LoadEnvironmentWithoutLogging(builder);
    
    // Step 3: Initialize logging service after environment is loaded
    var logConfig = new LogConfiguration
    {
        MinimumLevel = LogEventLevel.Debug,
        EnableConsole = true,
        EnableFile = true,
        ConsoleMinimumLevel = LogEventLevel.Information,
        FileMinimumLevel = LogEventLevel.Debug
    };
    LogService.Initialize(logConfig);
    
    // Configure Serilog as the logging provider
    builder.Host.UseSerilog(LogService.Logger);
    
    // Display startup banner
    LogService.DisplayStartupBanner("GraphQL Server", AppVersion.GetFullVersion());
    LogService.LogSection("Environment Configuration");
    LogService.LogSuccess($"Loaded environment from: {EnvironmentConfig.LastLoadedEnvPath ?? "defaults"}");
    
    var appIp = Environment.GetEnvironmentVariable("APP_IP") ?? "localhost";
    var appPort = Environment.GetEnvironmentVariable("APP_PORT") ?? "5000";
    LogService.LogInfo($"Server will listen on: http://{appIp}:{appPort}");

    // Step 3: Configure Database (PostgreSQL with Entity Framework)
    LogService.LogSection("Database Configuration");
    DatabaseConfig.ConfigureDatabase(builder.Services);

    // Step 4: Configure JWT Authentication
    LogService.LogSection("Authentication Configuration");
    JwtConfig.ConfigureJwtAuthentication(builder.Services);

    // Step 5: Configure Authorization with Policies
    LogService.LogSection("Authorization Configuration");
    AuthorizationConfig.ConfigureAuthorization(builder.Services);

    // Step 6: Configure GraphQL Server
    LogService.LogSection("GraphQL Configuration");
    GraphQLConfig.ConfigureGraphQL(builder.Services);

    // Step 7: Build the application
    LogService.LogSection("Building Application");
    var app = await LogService.RunWithProgressAsync("Building application", async () => 
    {
        await Task.Delay(100); // Small delay for visual effect
        return builder.Build();
    });

    // Step 8: Initialize Database
    LogService.LogSection("Database Initialization");
    await DatabaseConfig.InitializeDatabaseAsync(app);

    // Step 9: Configure GraphQL middleware pipeline
    LogService.LogSection("Middleware Configuration");
    GraphQLConfig.ConfigureGraphQLMiddleware(app);

    // Step 10: Run the application
    LogService.LogSection("Starting Server");
    LogService.LogSuccess("Server is ready!");
    LogService.LogInfo("Press Ctrl+C to shutdown gracefully");
    app.RunWithGraphQLCommands(args);
}
catch (Exception ex)
{
    LogService.LogError("Fatal error during startup", ex);
    throw;
}
finally
{
    LogService.Shutdown();
}

// EXAMPLE GRAPHQL OPERATIONS:
// ===========================
// 
// 1. List all greet logs:
//    query {
//      greetLogs {
//        id
//        timestamp
//        content
//      }
//    }
//
// 2. Add a new greet log:
//    mutation {
//      addGreetLog(input: { content: "Hello, World!" }) {
//        id
//        timestamp
//        content
//      }
//    }
//
// 3. Subscribe to new greet logs:
//    subscription {
//      onGreetLogAdded {
//        id
//        timestamp
//        content
//      }
//    }
//
// 4. Get recent greet logs:
//    query {
//      recentGreetLogs(count: 5) {
//        id
//        timestamp
//        content
//      }
//    }