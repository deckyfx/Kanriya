using GQLServer.Program;

// GRAPHQL SERVER WITH GREETLOG MANAGEMENT (v1.0.0)
// =================================================
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

// Step 1: Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// Step 2: Load environment configuration
// This loads .env file and configures server URLs from APP_IP and APP_PORT
EnvironmentConfig.LoadEnvironment(builder);

// Step 3: Configure Database (PostgreSQL with Entity Framework)
DatabaseConfig.ConfigureDatabase(builder.Services);

// Step 4: Configure JWT Authentication
JwtConfig.ConfigureJwtAuthentication(builder.Services);

// Step 5: Configure Authorization with Policies
AuthorizationConfig.ConfigureAuthorization(builder.Services);

// Step 6: Configure GraphQL Server
GraphQLConfig.ConfigureGraphQL(builder.Services);

// Step 7: Build the application
var app = builder.Build();

// Step 8: Initialize Database
await DatabaseConfig.InitializeDatabaseAsync(app);

// Step 9: Configure GraphQL middleware pipeline
GraphQLConfig.ConfigureGraphQLMiddleware(app);

// Step 10: Run the application
app.RunWithGraphQLCommands(args);

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