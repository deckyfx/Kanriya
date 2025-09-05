using Kanriya.Server.Data;

namespace Kanriya.Server.Blazor.Services
{
    public interface IAuthService
    {
        Task<(bool success, string? token, User? user)> AuthenticateAsync(string email, string password);
        Task<bool> ValidateTokenAsync(string token);
        Task SignOutAsync();
    }
}