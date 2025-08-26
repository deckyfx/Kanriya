using Kanriya.Shared.Utils;

namespace Kanriya.Tests.Helpers;

/// <summary>
/// Represents a test user for reuse across tests
/// </summary>
public class TestUser
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Id { get; set; }
    public string? Token { get; set; }
    public string? VerificationToken { get; set; }
    public string? FullName { get; set; }
    
    /// <summary>
    /// Generate a random test user
    /// </summary>
    public static TestUser Generate(string prefix = "test")
    {
        return new TestUser
        {
            Email = $"{prefix}_{StringUtils.GenerateRandomAlphaNumeric(16)}@test.com",
            Password = "TestPassword123!",
            FullName = $"Test User {DateTime.Now.Ticks}"
        };
    }
}