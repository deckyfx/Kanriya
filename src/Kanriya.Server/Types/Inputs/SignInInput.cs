using System.ComponentModel.DataAnnotations;
using HotChocolate;

namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input type for user sign in mutation
/// </summary>
[GraphQLDescription("Input type for user sign in mutation")]
public class SignInInput
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required]
    [EmailAddress]
    [GraphQLDescription("User's email address")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User's password
    /// </summary>
    [Required]
    [GraphQLDescription("User's password")]
    public string Password { get; set; } = string.Empty;
}