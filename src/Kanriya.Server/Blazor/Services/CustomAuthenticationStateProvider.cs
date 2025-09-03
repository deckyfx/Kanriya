using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Kanriya.Server.Blazor.Services;

/// <summary>
/// Custom authentication state provider for Blazor Server
/// Currently returns an anonymous user - will be updated when authentication is implemented
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // For now, return an anonymous user
        // This will be updated when we implement actual authentication
        var anonymous = new ClaimsIdentity();
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(anonymous)));
    }
}