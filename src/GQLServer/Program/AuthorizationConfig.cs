using GQLServer.Constants;

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
            
            // SuperAdmin policy - requires SuperAdmin role
            // For operations that only super administrators should perform
            options.AddPolicy("SuperAdminOnly", policy =>
                policy.RequireRole(UserRoles.SuperAdmin));
            
            // Business Owner policy - requires BusinessOwner role
            // For operations that business owners can perform
            options.AddPolicy("BusinessOwnerOnly", policy =>
                policy.RequireRole(UserRoles.BusinessOwner));
            
            // Business Operator policy - requires BusinessOperator role
            // For operations that business operators can perform
            options.AddPolicy("BusinessOperatorOnly", policy =>
                policy.RequireRole(UserRoles.BusinessOperator));
            
            // Business Access policy - requires either BusinessOwner or BusinessOperator role
            // For operations that any business member can perform
            options.AddPolicy("BusinessAccess", policy =>
                policy.RequireRole(UserRoles.BusinessOwner, UserRoles.BusinessOperator));
            
            // Custom policy example - requires specific claim
            // For users who have verified their email address
            options.AddPolicy("EmailVerified", policy =>
                policy.RequireClaim("email_verified", "true"));
            
            // Complex policy example - SuperAdmin or explicit permission
            // For operations that require SuperAdmin or specific permissions
            options.AddPolicy("AdminOperations", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.IsInRole(UserRoles.SuperAdmin) ||
                    context.User.HasClaim("permission", "admin_operations"));
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
        Console.WriteLine("  - SuperAdminOnly: Requires SuperAdmin role");
        Console.WriteLine("  - BusinessOwnerOnly: Requires BusinessOwner role");
        Console.WriteLine("  - BusinessOperatorOnly: Requires BusinessOperator role");
        Console.WriteLine("  - BusinessAccess: Requires BusinessOwner or BusinessOperator role");
        Console.WriteLine("  - EmailVerified: Requires verified email claim");
        Console.WriteLine("  - AdminOperations: Requires SuperAdmin or admin_operations permission");
        Console.WriteLine("  - ServiceAccount: Requires service account claim");
    }
}