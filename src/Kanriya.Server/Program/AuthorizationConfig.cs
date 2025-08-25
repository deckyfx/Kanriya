using Kanriya.Server.Constants;
using Kanriya.Server.Services;
using Kanriya.Server.Services.System;

namespace Kanriya.Server.Program;

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
            
            // Brand Owner policy - requires BrandOwner role
            // For operations that brand owners can perform
            options.AddPolicy("BrandOwnerOnly", policy =>
                policy.RequireRole(UserRoles.BrandOwner));
            
            // Brand Operator policy - requires BrandOperator role
            // For operations that brand operators can perform
            options.AddPolicy("BrandOperatorOnly", policy =>
                policy.RequireRole(UserRoles.BrandOperator));
            
            // Brand Access policy - requires either BrandOwner or BrandOperator role
            // For operations that any brand member can perform
            options.AddPolicy("BrandAccess", policy =>
                policy.RequireRole(UserRoles.BrandOwner, UserRoles.BrandOperator));
            
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

        LogService.LogSuccess("Authorization policies configured");
    }
}