using Microsoft.AspNetCore.Mvc;
using Kanriya.Server.ViewModels;
using Kanriya.Server.Program;

namespace Kanriya.Server.Controllers;

/// <summary>
/// Home controller for the main landing page
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Display the home page
    /// </summary>
    [HttpGet("/")]
    public IActionResult Index()
    {
        var model = new HomeViewModel
        {
            Title = "GQLServer",
            Version = AppVersion.GetFullVersion(),
            Description = "A modern GraphQL API server with authentication, real-time subscriptions, and comprehensive data management capabilities.",
            GraphQLEndpoint = "/graphql",
            SwaggerEndpoint = "/swagger",
            Features = new[]
            {
                new Feature { Icon = "🔐", Name = "JWT Auth" },
                new Feature { Icon = "📊", Name = "PostgreSQL" },
                new Feature { Icon = "⚡", Name = "Subscriptions" },
                new Feature { Icon = "🔍", Name = "Filtering" },
                new Feature { Icon = "📝", Name = "CRUD Ops" },
                new Feature { Icon = "👥", Name = "Role-Based" }
            },
            Endpoints = new[]
            {
                new EndpointInfo { Label = "GraphQL API", Path = "/graphql" },
                new EndpointInfo { Label = "Swagger UI", Path = "/swagger" },
                new EndpointInfo { Label = "Health Check", Path = "/health" },
                new EndpointInfo { Label = "API Info", Path = "/api" },
                new EndpointInfo { Label = "Email Verification", Path = "/verify-email" }
            }
        };
        
        return View(model);
    }
    
    /// <summary>
    /// Display the about page
    /// </summary>
    [HttpGet("/about")]
    public IActionResult About()
    {
        var model = new AboutViewModel();
        return View(model);
    }
}