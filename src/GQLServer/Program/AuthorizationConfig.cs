namespace GQLServer.Program;

/// <summary>
/// Configures authorization policies for the application
/// </summary>
public static class AuthorizationConfig
{
    /// <summary>
    /// Configure authorization with various policies for role-based and claim-based access control
    /// </summary>
    public static void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy - requires authenticated user
            // Used when no specific policy is specified
            options.AddPolicy("Authenticated", policy =>
                policy.RequireAuthenticatedUser());
            
            // Admin policy - requires Admin role
            // For operations that only administrators should perform
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));
            
            // Moderator policy - requires Admin OR Moderator role
            // For operations that moderators and admins can perform
            options.AddPolicy("ModeratorOrAbove", policy =>
                policy.RequireRole("Admin", "Moderator"));
            
            // Custom policy example - requires specific claim
            // For users who have verified their email address
            options.AddPolicy("EmailVerified", policy =>
                policy.RequireClaim("email_verified", "true"));
            
            // Complex policy example - multiple requirements
            // For premium users with valid subscription
            options.AddPolicy("PremiumUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("User", "Moderator", "Admin");
                policy.RequireClaim("subscription", "premium");
            });
            
            // Developer policy - for development/debugging operations
            options.AddPolicy("DeveloperOnly", policy =>
            {
                policy.RequireRole("Admin");
                policy.RequireClaim("developer", "true");
            });
            
            // Service account policy - for automated processes
            options.AddPolicy("ServiceAccount", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("account_type", "service");
            });
        });

        Console.WriteLine("✓ Authorization policies configured:");
        Console.WriteLine("  - Authenticated: Requires any authenticated user");
        Console.WriteLine("  - AdminOnly: Requires Admin role");
        Console.WriteLine("  - ModeratorOrAbove: Requires Admin or Moderator role");
        Console.WriteLine("  - EmailVerified: Requires verified email claim");
        Console.WriteLine("  - PremiumUser: Requires premium subscription claim");
        Console.WriteLine("  - DeveloperOnly: Requires Admin role with developer claim");
        Console.WriteLine("  - ServiceAccount: Requires service account claim");
    }
}