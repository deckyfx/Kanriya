using System.ComponentModel.DataAnnotations;
using HotChocolate;

namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input type for sign in mutation
/// Supports both principal user (email/password) and brand user (API credentials) authentication
/// </summary>
[GraphQLDescription("Input type for sign in mutation - supports both principal and brand authentication")]
public class SignInInput
{
    /// <summary>
    /// For principal auth: User's email address
    /// For brand auth: API Secret (16 characters)
    /// </summary>
    [Required]
    [GraphQLDescription("Email address for principal auth OR API Secret for brand auth")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// For principal auth: User's password
    /// For brand auth: API Password (32 characters)
    /// </summary>
    [Required]
    [GraphQLDescription("Password for principal auth OR API Password for brand auth")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Brand ID - if provided, uses brand authentication flow
    /// If null/empty, uses principal authentication flow
    /// </summary>
    [GraphQLDescription("Optional: Brand ID for brand authentication. If provided, email/password are treated as API credentials")]
    public string? BrandId { get; set; }
}