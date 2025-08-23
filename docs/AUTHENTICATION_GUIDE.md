# GraphQL Authentication & Authorization Guide

## Overview

This project implements JWT-based authentication with role-based and policy-based authorization using HotChocolate.

## Default Test Users

| Username   | Password   | Role      | Permissions                    |
|------------|------------|-----------|--------------------------------|
| admin      | admin123   | Admin     | Full access to all operations |
| moderator  | mod123     | Moderator | Moderate content, view users  |
| user       | user123    | User      | Basic user operations only    |

## Authentication Flow

### 1. Login to Get JWT Token

```graphql
mutation Login {
  login(input: { 
    username: "admin", 
    password: "admin123" 
  }) {
    token
    user {
      id
      username
      email
      role
    }
    expiresAt
  }
}
```

### 2. Use the Token

In GraphQL Playground, set the HTTP headers:

```json
{
  "Authorization": "Bearer YOUR_JWT_TOKEN_HERE"
}
```

### 3. Register New User

Public registration (creates User role):
```graphql
mutation Register {
  register(input: {
    username: "newuser"
    email: "new@example.com"
    password: "password123"
  }) {
    id
    username
    email
    role
  }
}
```

Admin registration (can set any role):
```graphql
mutation AdminRegister {
  adminRegister(input: {
    username: "newmod"
    email: "mod@example.com"
    password: "mod123"
    role: "Moderator"
  }) {
    id
    username
    role
  }
}
```

## Authorization Levels

### Public Queries (No Auth Required)

```graphql
query PublicQueries {
  # Basic queries
  hello
  serverStatus
  currentTime
  
  # Public user info (partial)
  publicUserInfo(username: "admin") {
    username    # Public
    createdAt   # Public
    # email     # Requires auth
    # role      # Requires mod/admin
  }
}
```

### Authenticated Queries (Any Logged User)

```graphql
query AuthenticatedQueries {
  # Get current user info
  me {
    id
    username
    email
    role
  }
}
```

### Moderator Queries (Moderator or Admin)

```graphql
query ModeratorQueries {
  # View specific user
  userById(id: 1) {
    id
    username
    email
    role
  }
  
  # System statistics
  systemStats {
    totalUsers
    totalAdmins
    totalModerators
  }
}
```

### Admin-Only Queries

```graphql
query AdminQueries {
  # Get all users
  allUsers {
    id
    username
    email
    role
    createdAt
  }
}
```

## Protected Mutations

### User-Level Mutations (Any Authenticated User)

```graphql
mutation UserMutations {
  # Update profile
  updateMyProfile(newEmail: "newemail@example.com") {
    success
    message
  }
  
  # Change password
  changeMyPassword(
    oldPassword: "current"
    newPassword: "newpass"
  ) {
    success
    message
  }
}
```

### Moderator Mutations (Moderator or Admin)

```graphql
mutation ModeratorMutations {
  # Moderate content
  moderateContent(
    contentId: 123
    action: "approve"
  ) {
    success
    message
  }
  
  # Ban user
  banUser(
    userId: 456
    reason: "Spam"
    duration: 7
  ) {
    success
    message
  }
}
```

### Admin-Only Mutations

```graphql
mutation AdminMutations {
  # Delete user
  deleteUser(userId: 123) {
    success
    message
  }
  
  # Grant role
  grantRole(
    userId: 2
    newRole: "Moderator"
  ) {
    success
    message
  }
}
```

## Authorization Attributes

### In C# Code

```csharp
// No authorization - public access
public string GetPublicData() => "Public";

// Requires authentication (any role)
[Authorize]
public string GetPrivateData() => "Private";

// Requires specific role(s)
[Authorize(Roles = new[] { "Admin" })]
public string GetAdminData() => "Admin only";

// Multiple roles (OR condition)
[Authorize(Roles = new[] { "Admin", "Moderator" })]
public string GetModeratorData() => "Moderator or Admin";

// Policy-based authorization
[Authorize(Policy = "ModeratorOrAbove")]
public string GetPolicyData() => "Policy-based";
```

## Policies (Defined in Program.cs)

| Policy Name       | Requirements                                     |
|-------------------|--------------------------------------------------|
| Authenticated     | Any authenticated user                          |
| AdminOnly         | Admin role required                             |
| ModeratorOrAbove  | Admin OR Moderator role                         |
| EmailVerified     | Claim: email_verified = true                    |
| PremiumUser       | Authenticated + Any role + subscription=premium |

## Field-Level Authorization

Individual fields can have different authorization:

```csharp
public class PublicUserInfo
{
    public string Username { get; set; }  // Public
    
    [Authorize]
    public string Email { get; set; }     // Requires auth
    
    [Authorize(Roles = new[] { "Admin", "Moderator" })]
    public string Role { get; set; }      // Requires mod/admin
}
```

## Testing Authorization

### Step 1: Login as Different Users

Test each role to see different access levels:
- User: Basic access
- Moderator: Extended access
- Admin: Full access

### Step 2: Test Without Token

Try protected queries without authentication to see errors:

```graphql
query {
  me {  # Will fail - requires auth
    id
  }
}
```

### Step 3: Test Wrong Role

Login as "user" and try admin operations:

```graphql
query {
  allUsers {  # Will fail - requires Admin role
    id
  }
}
```

## JWT Configuration

Configure in `appsettings.json` or environment variables:

```json
{
  "JWT": {
    "Secret": "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!",
    "Issuer": "YourGraphQLServer",
    "Audience": "YourGraphQLClient",
    "ExpirationMinutes": 60
  }
}
```

## Security Best Practices

1. **Never expose password hashes** in GraphQL responses
2. **Use strong JWT secrets** (minimum 32 characters)
3. **Set appropriate token expiration** (1 hour default)
4. **Validate all inputs** to prevent injection attacks
5. **Use HTTPS in production** to protect JWT tokens
6. **Implement refresh tokens** for production apps
7. **Add rate limiting** to prevent brute force attacks
8. **Log authentication events** for security monitoring

## Common Issues

### "Unauthorized" Error
- Check if JWT token is included in headers
- Verify token hasn't expired
- Ensure correct "Bearer " prefix

### "Forbidden" Error  
- User is authenticated but lacks required role
- Check user's role matches requirement

### Token Not Working
- Verify JWT secret matches between generation and validation
- Check token expiration time
- Ensure Authorization header format is correct

## Next Steps

1. **Add Database**: Replace in-memory storage with Entity Framework
2. **Add Refresh Tokens**: Implement token refresh mechanism
3. **Add Email Verification**: Implement email verification flow
4. **Add Password Reset**: Implement forgot password feature
5. **Add OAuth**: Support Google, GitHub, etc. login
6. **Add Rate Limiting**: Prevent brute force attacks
7. **Add Audit Logging**: Track all authentication events