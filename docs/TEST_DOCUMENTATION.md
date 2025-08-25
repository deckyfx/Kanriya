# Kanriya Test Suite Documentation

## Overview

This document describes the test infrastructure, test suites, and testing approach for the Kanriya project. We follow a **Test-Driven Development (TDD)** approach for all new features and bug fixes.

## Test Infrastructure

### Architecture

The test suite is designed to test against a **running server instance** rather than starting its own server. This approach:
- Simulates real-world usage more accurately
- Avoids complex test server configuration
- Uses the main database for testing (with proper cleanup)
- Ensures tests validate actual server behavior

### Key Components

#### 1. BaseApiTest (`src/Kanriya.Server.Tests/Infrastructure/BaseApiTest.cs`)
Base class for all API tests providing:
- HTTP client configuration for GraphQL endpoint
- Authentication token management
- GraphQL query/mutation execution helpers
- Database helpers for test data setup
- Unique test data generation (GUIDs for emails/names)

#### 2. Test Runner Scripts (`bin/`)
- **`test-runner`**: Interactive menu-driven test runner with multiple options
- **`test-simple`**: Quick script to run SimpleApiTest
- **`test-watch`**: Continuous test execution on file changes

## Test Suites

### 1. SimpleApiTest ✅ (Working)
**Purpose**: Validates the complete user lifecycle from creation to deletion

**Test Scenarios**:
- `FullUserLifecycle_CreateLoginDelete_ShouldWork`
  - Creates a new user account (signup)
  - Activates the account (email verification simulation)
  - Signs in and receives JWT token
  - Deletes the account with authentication
  - **Cleanup**: Account is deleted as part of the test

### 2. UserAuthorizationTests 🚧 (To be migrated)
**Purpose**: Comprehensive user authentication and authorization testing

**Planned Scenarios**:
- User Signup
  - Valid data creates pending user
  - Duplicate email returns error
  - Invalid email format rejected
  - Weak password rejected
  - Missing required fields rejected
  
- Email Verification
  - Resend verification for pending users
  - Cannot resend for verified users
  - Invalid tokens rejected
  
- User Signin
  - Valid credentials return JWT token
  - Wrong password returns error
  - Non-existent user returns error
  - Unverified user cannot sign in
  
- User Profile
  - Authenticated users can access profile
  - Unauthenticated requests rejected
  
- Account Deletion
  - Authenticated users can delete account
  - Wrong password prevents deletion
  - Unauthenticated requests rejected
  - Deletion cascades to owned brands

### 3. BrandManagementTests 🚧 (To be migrated)
**Purpose**: Multi-tenant brand management functionality

**Planned Scenarios**:
- Brand Creation
  - Authenticated users can create brands
  - Brand gets unique schema and database user
  - API credentials generated for brand
  - Unauthenticated requests rejected
  - Invalid data returns errors
  
- Brand Access
  - Owners can view their brands
  - List returns only accessible brands
  - Brand details include schema info
  
- Brand Updates
  - Owners can update brand settings
  - Non-owners cannot modify brands
  - Updates preserve schema integrity
  
- Brand Deletion
  - Owners can delete brands
  - Deletion removes schema and database user
  - Non-owners cannot delete brands
  - **Cleanup**: Each test deletes created brands

### 4. BasicCleanupTest 🚧 (To be migrated)
**Purpose**: Validates cleanup mechanisms work correctly

**Planned Scenarios**:
- User cleanup removes all user data
- Brand cleanup removes schema and users
- Cascading deletes work correctly
- No orphaned data remains

## Test Data Management

### Principles
1. **Isolation**: Each test uses unique data (GUID-based)
2. **Cleanup**: Every test must clean up after itself
3. **No Shared State**: Tests don't depend on other tests
4. **Idempotent**: Tests can run multiple times safely

### Test Data Patterns

```csharp
// Unique email generation
var testEmail = $"test_{Guid.NewGuid():N}@test.com";

// Unique name generation  
var testName = $"Test_{Guid.NewGuid():N}";

// Cleanup pattern
try 
{
    // Test operations
}
finally 
{
    // Always cleanup (delete user/brand)
}
```

## TDD Workflow

### 1. Red Phase (Write Failing Test)
```csharp
[Fact]
public async Task NewFeature_ShouldWork()
{
    // Arrange
    var input = CreateTestInput();
    
    // Act
    var result = await ExecuteFeature(input);
    
    // Assert
    result.Should().BeSuccessful();
}
```

### 2. Green Phase (Make Test Pass)
- Implement minimal code to pass test
- Focus on correctness, not optimization

### 3. Refactor Phase (Improve Code)
- Clean up implementation
- Remove duplication
- Improve design
- Tests must still pass

## Running Tests

### Using Test Runner Scripts

```bash
# Interactive menu
./bin/test-runner

# Run specific suite
./bin/test-runner simple   # SimpleApiTest
./bin/test-runner auth     # UserAuthorizationTests  
./bin/test-runner brand    # BrandManagementTests
./bin/test-runner all      # All tests

# Quick test
./bin/test-simple

# Watch mode (continuous testing)
./bin/test-watch

# Stress testing
./bin/test-runner stress 100 SimpleApiTest
```

### Using dotnet CLI

```bash
# Run all tests
dotnet test src/Kanriya.Server.Tests

# Run specific test class
dotnet test --filter "FullyQualifiedName~SimpleApiTest"

# Run with detailed output
dotnet test --logger "console;verbosity=normal"

# Watch mode
dotnet watch test --project src/Kanriya.Server.Tests
```

## Test Prerequisites

1. **Database**: PostgreSQL running on port 10005
2. **Server**: Kanriya server running on port 10000
3. **Migrations**: Database migrations applied

Start the server:
```bash
dotnet run --project src/Kanriya.Server
```

## Test Coverage Goals

- **Unit Tests**: 80% code coverage minimum
- **Integration Tests**: All GraphQL operations
- **E2E Tests**: Critical user workflows
- **Performance Tests**: Response time < 200ms

## Best Practices

### DO ✅
- Write tests before implementation (TDD)
- Use descriptive test names
- Keep tests independent
- Clean up test data
- Test edge cases
- Use unique test data
- Assert specific outcomes
- Test error scenarios

### DON'T ❌
- Share state between tests
- Use production data
- Skip cleanup
- Test implementation details
- Write overly complex tests
- Ignore failing tests
- Use fixed/hardcoded test data

## GraphQL Testing Patterns

### Query Testing
```csharp
var query = @"
    query GetUser($id: String!) {
        user(id: $id) {
            id
            email
            fullName
        }
    }";

var result = await ExecuteQueryAsync<UserResponse>(query, new { id = userId });
result.Data.User.Should().NotBeNull();
```

### Mutation Testing
```csharp
var mutation = @"
    mutation CreateUser($input: CreateUserInput!) {
        createUser(input: $input) {
            success
            message
            user { id }
        }
    }";

var result = await ExecuteQueryAsync<CreateUserResponse>(mutation, new { input });
result.Data.CreateUser.Success.Should().BeTrue();
```

### Authentication Testing
```csharp
// Set auth token
SetAuthToken(jwtToken);

// Make authenticated request
var result = await ExecuteQueryAsync<SecureResponse>(query);

// Clear auth
ClearAuthToken();
```

## Troubleshooting

### Common Issues

1. **"Server not running"**
   - Start server: `dotnet run --project src/Kanriya.Server`
   - Check port 10000 is free

2. **"Database connection failed"**
   - Check PostgreSQL is running on port 10005
   - Verify credentials: `kanriya/kanriya`

3. **"Test data conflicts"**
   - Tests should use unique GUIDs
   - Check cleanup is working
   - Clear database if needed

4. **"Unexpected Execution Error"**
   - Check server logs for details
   - Verify GraphQL schema is current
   - Check service injection configuration

## Future Enhancements

1. **Test Categories**
   - Smoke tests (critical paths)
   - Regression tests
   - Performance tests
   - Security tests

2. **CI/CD Integration**
   - GitHub Actions workflow
   - Automated test runs on PR
   - Coverage reports

3. **Test Data Builders**
   - Fluent API for test data
   - Realistic data generation
   - Scenario templates

4. **Mocking Strategy**
   - External service mocks
   - Email service mocking
   - Time-based testing

## Contributing

When adding new features:
1. Write failing tests first (TDD)
2. Implement feature to pass tests
3. Add integration tests for GraphQL
4. Update this documentation
5. Ensure all tests pass
6. Submit PR with tests

## Test Metrics

Current Status:
- ✅ SimpleApiTest: 1/1 passing
- 🚧 UserAuthorizationTests: 0/15 (to be migrated)
- 🚧 BrandManagementTests: 0/13 (to be migrated)
- 🚧 BasicCleanupTest: 0/1 (to be migrated)

**Total**: 1/30 tests passing (3.3% - migration in progress)