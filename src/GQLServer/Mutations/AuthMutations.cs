using HotChocolate;
using HotChocolate.Authorization;
using GQLServer.Services;
using GQLServer.Types.Inputs;
using GQLServer.Types.Outputs;

namespace GQLServer.Mutations;

// AUTHENTICATION MUTATIONS - Handle user registration and login
// ==============================================================
// These mutations handle:
// - User registration
// - User login with JWT token generation
// - Password reset (future implementation)

[ExtendObjectType("Mutation")]
public class AuthMutations
{
    // LOGIN MUTATION - Authenticate user and get JWT token
    // Usage in GraphQL Playground:
    // mutation {
    //   login(input: { username: "admin", password: "admin123" }) {
    //     token
    //     user {
    //       id
    //       username
    //       email
    //       role
    //     }
    //     expiresAt
    //   }
    // }
    public async Task<AuthPayload> Login(
        LoginInput input,
        [Service] AuthService authService)
    {
        var result = await authService.LoginAsync(input.Username, input.Password);
        
        if (result == null)
        {
            throw new GraphQLException("Invalid credentials");
        }
        
        return result;
    }
    
    // REGISTER MUTATION - Create a new user account
    // Usage in GraphQL Playground:
    // mutation {
    //   register(input: { 
    //     username: "newuser", 
    //     email: "newuser@example.com", 
    //     password: "password123" 
    //   }) {
    //     id
    //     username
    //     email
    //     role
    //   }
    // }
    public async Task<User> Register(
        RegisterInput input,
        [Service] AuthService authService)
    {
        var user = await authService.RegisterAsync(
            input.Username, 
            input.Email, 
            input.Password,
            input.Role);
        
        if (user == null)
        {
            throw new GraphQLException("Registration failed");
        }
        
        return user;
    }
    
    // ADMIN REGISTER MUTATION - Admin can create users with specific roles
    // Protected with [Authorize] attribute - requires Admin role
    // Usage (must be authenticated as Admin):
    // mutation {
    //   adminRegister(input: { 
    //     username: "newmod", 
    //     email: "mod@example.com", 
    //     password: "mod123",
    //     role: "Moderator"
    //   }) {
    //     id
    //     username
    //     role
    //   }
    // }
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<User> AdminRegister(
        RegisterInput input,
        [Service] AuthService authService)
    {
        // Admin can set any role during registration
        var user = await authService.RegisterAsync(
            input.Username, 
            input.Email, 
            input.Password,
            input.Role ?? "User");
        
        if (user == null)
        {
            throw new GraphQLException("Registration failed");
        }
        
        return user;
    }
}