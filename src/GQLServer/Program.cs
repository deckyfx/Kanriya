using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using GQLServer.Services;

// GRAPHQL SERVER WITH AUTHENTICATION & AUTHORIZATION
// ===================================================
// This server includes:
// - JWT authentication
// - Role-based authorization
// - Policy-based authorization
// - Protected queries and mutations

// Step 1: Load environment variables
DotNetEnv.Env.Load();

// Step 2: Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// Step 3: Configure JWT Authentication
var jwtSecret = builder.Configuration["JWT:Secret"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "YourGraphQLServer";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "YourGraphQLClient";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero // Remove default 5 min clock skew
        };
        
        // For GraphQL subscriptions over WebSocket
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                // If the request is for GraphQL and the token is in query string
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/graphql"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

// Step 4: Configure Authorization with Policies
builder.Services.AddAuthorization(options =>
{
    // Default policy - requires authenticated user
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
    
    // Admin policy - requires Admin role
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    // Moderator policy - requires Admin OR Moderator role
    options.AddPolicy("ModeratorOrAbove", policy =>
        policy.RequireRole("Admin", "Moderator"));
    
    // Custom policy example - requires specific claim
    options.AddPolicy("EmailVerified", policy =>
        policy.RequireClaim("email_verified", "true"));
    
    // Complex policy example - multiple requirements
    options.AddPolicy("PremiumUser", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("User", "Moderator", "Admin");
        policy.RequireClaim("subscription", "premium");
    });
});

// Step 5: Register Services
builder.Services.AddSingleton<AuthService>(); // Authentication service

// Step 6: Configure GraphQL with Authorization
builder.Services
    .AddGraphQLServer()
    
    // Configure base types
    .AddQueryType(q => q.Name("Query"))
    .AddMutationType(m => m.Name("Mutation"))
    .AddSubscriptionType(s => s.Name("Subscription"))
    
    // Add query extensions from /Queries folder
    .AddTypeExtension<GQLServer.Queries.BasicQueries>()
    .AddTypeExtension<GQLServer.Queries.HelloQueries>()
    .AddTypeExtension<GQLServer.Queries.GreetingQueries>()
    .AddTypeExtension<GQLServer.Queries.TimeQueries>()
    .AddTypeExtension<GQLServer.Queries.UserQueries>()
    
    // Add mutation extensions from /Mutations folder
    .AddTypeExtension<GQLServer.Mutations.ExampleMutations>()
    .AddTypeExtension<GQLServer.Mutations.AuthMutations>()
    .AddTypeExtension<GQLServer.Mutations.ProtectedMutations>()
    
    // Add subscription extensions from /Subscriptions folder
    .AddTypeExtension<GQLServer.Subscriptions.ExampleSubscriptions>()
    
    // Enable authorization
    .AddAuthorization()
    
    // Enable in-memory subscriptions
    .AddInMemorySubscriptions();

// Step 7: Build the application
var app = builder.Build();

// Step 8: Configure middleware pipeline
app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();  // Must come before MapGraphQL
app.UseWebSockets();     // Required for subscriptions

// Step 9: Map GraphQL endpoint
app.MapGraphQL();

// Step 10: Run the application
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