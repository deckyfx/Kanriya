using GQLServer.Program;

// GRAPHQL SERVER WITH AUTHENTICATION & AUTHORIZATION
// ===================================================
// This server includes:
// - JWT authentication
// - Role-based authorization
// - Policy-based authorization
// - Protected queries and mutations
//
// Configuration is modularized into separate classes:
// - EnvironmentConfig: Handles .env file loading and server URLs
// - JwtConfig: Configures JWT Bearer authentication
// - AuthorizationConfig: Sets up authorization policies
// - GraphQLConfig: Configures GraphQL server and middleware

// Step 1: Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// Step 2: Load environment configuration
// This loads .env file and configures server URLs from APP_IP and APP_PORT
EnvironmentConfig.LoadEnvironment(builder);

// Step 3: Configure JWT Authentication
JwtConfig.ConfigureJwtAuthentication(builder.Services);

// Step 4: Configure Authorization with Policies
AuthorizationConfig.ConfigureAuthorization(builder.Services);

// Step 5: Configure GraphQL Server
GraphQLConfig.ConfigureGraphQL(builder.Services);

// Step 6: Build the application
var app = builder.Build();

// Step 7: Configure GraphQL middleware pipeline
GraphQLConfig.ConfigureGraphQLMiddleware(app);

// Step 8: Run the application
app.RunWithGraphQLCommands(args);

// DEFAULT USERS FOR TESTING:
// ==========================
// Admin:     username: "admin",     password: "admin123"
// Moderator: username: "moderator", password: "mod123"
// User:      username: "user",      password: "user123"
//
// TEST AUTHENTICATION:
// 1. Login to get JWT token:
//    mutation {
//      login(input: { username: "admin", password: "admin123" }) {
//        token
//        user { id username role }
//      }
//    }
//
// 2. Use token in HTTP headers:
//    {
//      "Authorization": "Bearer YOUR_JWT_TOKEN_HERE"
//    }
//
// 3. Access protected queries/mutations with proper authorization