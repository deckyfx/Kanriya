using Kanriya.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using System.Reflection;

namespace Kanriya.Server.HttpRoutes;

/// <summary>
/// API information and health check routes
/// </summary>
public static class ApiInfoRoute
{
    /// <summary>
    /// Map API info and health check routes
    /// </summary>
    public static void MapApiInfoRoutes(this WebApplication app)
    {
        // Health check endpoint (simple HTTP)
        app.MapGet("/health", () => {
            var assembly = Assembly.GetExecutingAssembly();
            return Results.Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = BuildInfo.GetFullVersion(assembly),
                environment = app.Environment.EnvironmentName
            });
        })
        .WithName("HealthCheck")
        .WithOpenApi()
        .WithTags("Health");
        
        // API info endpoint
        app.MapGet("/api", () => {
            var assembly = Assembly.GetExecutingAssembly();
            return Results.Json(new
            {
                name = "GQLServer",
                version = BuildInfo.GetFullVersion(assembly),
            description = "GraphQL API Server with Authentication and Real-time Subscriptions",
            documentation = new
            {
                graphql = "/graphql",
                swagger = "/swagger",
                swaggerJson = "/swagger/v1/swagger.json"
            },
            endpoints = new
            {
                graphql = new
                {
                    url = "/graphql",
                    description = "GraphQL API endpoint with Banana Cake Pop playground"
                },
                swagger = new
                {
                    url = "/swagger",
                    description = "Swagger UI for REST API documentation"
                },
                health = new
                {
                    url = "/health",
                    description = "Health check endpoint"
                },
                home = new
                {
                    url = "/",
                    description = "Application home page"
                },
                emailVerification = new
                {
                    url = "/verify-email?token={token}",
                    description = "Email verification endpoint"
                }
            },
            features = new[]
            {
                "GraphQL API with HotChocolate",
                "JWT Authentication",
                "Real-time Subscriptions",
                "PostgreSQL Database",
                "Role-based Authorization",
                "Email Verification",
                "Swagger Documentation"
            }
            });
        })
        .WithName("ApiInfo")
        .WithOpenApi()
        .WithTags("Info");
    }
}