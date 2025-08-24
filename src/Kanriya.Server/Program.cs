using Kanriya.Server.Program;
using Kanriya.Server.Services;
using Kanriya.Server.Constants;
using Serilog;
using Serilog.Events;

// KANRIYA GRAPHQL SERVER
// =================================================
// Version: 1.0.2 - 2025-08-24
// This server provides a comprehensive backend system with:
// - PostgreSQL database integration using Entity Framework Core
// - User authentication and authorization with JWT
// - Email queue system with SMTP support
// - Real-time subscriptions via WebSockets
//
// Main Features:
// - User management with role-based access control
// - Email templating and queue system with Hangfire
// - GraphQL API with queries, mutations, and subscriptions
// - Secure authentication and authorization
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
    // This loads .env file and configures server URLs from SERVER_BIND_IP and SERVER_LISTEN_PORT
    EnvironmentConfig.LoadEnvironmentWithoutLogging(builder);
    
    // Step 3: Initialize logging service after environment is loaded
    var logConfig = new LogConfiguration
    {
        MinimumLevel = LogEventLevel.Debug,
        // Keep console output enabled - we'll handle deduplication in LogService
        EnableConsole = true,
        EnableFile = true,
        ConsoleMinimumLevel = LogEventLevel.Information,
        FileMinimumLevel = LogEventLevel.Debug,
        // Enable Seq for centralized logging (doesn't output to console)
        EnableSeq = true,
        SeqServerUrl = EnvironmentConfig.Seq.ServerUrl,
        SeqApiKey = EnvironmentConfig.Seq.ApiKey,
        SeqMinimumLevel = LogEventLevel.Debug
    };
    LogService.Initialize(logConfig);
    
    // Configure Serilog as the logging provider
    builder.Host.UseSerilog(LogService.Logger);
    
    // Display startup banner
    LogService.DisplayStartupBanner("Kanriya", AppVersion.GetFullVersion());
    LogService.LogSection("Environment Configuration");
    LogService.LogSuccess($"Loaded environment from: {EnvironmentConfig.LastLoadedEnvPath ?? "defaults"}");
    
    LogService.LogInfo($"Server will listen on: {EnvironmentConfig.App.Urls}");

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

    // Step 7: Configure Hangfire for Background Jobs
    LogService.LogSection("Hangfire Configuration");
    HangfireConfig.ConfigureHangfireServices(builder.Services, builder.Configuration);
    LogService.LogSuccess("Hangfire configured with PostgreSQL storage");
    LogService.LogInfo("Dashboard will be available at: /hangfire");

    // Step 8: Configure Mail Services
    LogService.LogSection("Mail Services Configuration");
    MailServiceConfig.ConfigureMailServices(builder.Services);
    LogService.LogSuccess("Mail services configured with SMTP driver");
    LogService.LogInfo("Email queue system ready for processing");

    // Step 9: Feature flags removed (too complex for current needs)

    // Step 10: Configure HTTP Endpoints and Swagger
    LogService.LogSection("HTTP Endpoints Configuration");
    HttpEndpointsConfig.ConfigureHttpServices(builder.Services);

    // Step 11: Build the application
    LogService.LogSection("Building Application");
    var app = await LogService.RunWithProgressAsync("Building application", async () => 
    {
        await Task.Delay(100); // Small delay for visual effect
        return builder.Build();
    });

    // Step 12: Initialize Database
    LogService.LogSection("Database Initialization");
    await DatabaseConfig.InitializeDatabaseAsync(app);

    // Step 13: Initialize Mail Services (load SMTP config at startup)
    LogService.LogSection("Mail Services Initialization");
    await MailServiceConfig.InitializeMailServicesAsync(app.Services);

    // Step 14: Configure middleware pipeline
    LogService.LogSection("Middleware Configuration");
    
    // Configure HTTP endpoints first
    HttpEndpointsConfig.ConfigureHttpMiddleware(app);
    
    // Configure Hangfire middleware
    HangfireConfig.ConfigureHangfireMiddleware(app);
    
    
    // Then configure GraphQL middleware
    GraphQLConfig.ConfigureGraphQLMiddleware(app);

    // Step 15: Run the application
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
// 1. Sign in:
//    mutation {
//      signIn(input: { email: "user@example.com", password: "password" }) {
//        success
//        message
//        token
//        user { id, email, fullName }
//      }
//    }
//
// 2. Get current user:
//    query {
//      me {
//        id
//        email
//        fullName
//        userRoles { role, assignedAt }
//      }
//    }
//
// 3. Subscribe to user changes:
//    subscription {
//      onUserChanged {
//        event
//        document { id, email, fullName }
//        time
//      }
//    }