# Brand Creation/Deletion TDD Plan

## Overview

This document outlines the Test-Driven Development (TDD) plan for implementing Brand Creation and Deletion functionality in the Kanriya multi-tenant system. Each brand gets its own PostgreSQL schema with isolated data and dedicated database user.

## Dual Authentication System

### Principal Authentication (System-Wide)
- **Purpose**: Authenticate system users (brand owners)
- **Method**: Email + Password → JWT Bearer Token
- **Scope**: System-wide operations, brand management
- **Limitations**: Cannot perform brand-specific operations without brand context

### Brand Authentication (Brand-Specific)
- **Purpose**: Authenticate within a specific brand context
- **Method**: API-Key + API-Password + BrandId → Brand-Context Bearer Token
- **Scope**: Brand-specific operations only
- **Requirements**: Must have brand-context token to operate on brand data
- **Note**: API credentials are permanent and cannot be changed (must be stored securely)

## Test Structure

### Test Suite Organization
```
src/Kanriya.Tests/Suites/BrandTestSuite.cs
├── Stage 1: Brand Creation Tests
├── Stage 2: Brand Authentication Tests
├── Stage 3: Brand Context Operations Tests
├── Stage 4: Brand Access & Authorization Tests  
├── Stage 5: Brand Information Management Tests
└── Stage 6: Brand Deletion Tests
```

## Stage 1: Brand Creation Tests

### Scenario 1.1: Invalid Brand Name (Negative Test)
- **Test**: Create brand with empty/null name
- **Expected**: System should reject with validation error
- **Validation**: Error message contains "Brand name is required"

### Scenario 1.2: Invalid Brand Name Format (Negative Test)
- **Test**: Create brand with invalid characters (special chars, numbers only, too short/long)
- **Expected**: System should reject with format validation error
- **Validation**: Error message contains format requirements

### Scenario 1.3: Unauthorized User (Negative Test)
- **Test**: Create brand without authentication token
- **Expected**: System should reject with 401 Unauthorized
- **Validation**: Error message indicates authentication required

### Scenario 1.4: Valid Brand Creation (Positive Test)
- **Test**: Create brand with valid name and authenticated user
- **Expected**: System should create brand successfully with API credentials
- **Response Validation**:
  - Brand object returned with id, name, schemaName
  - API-Key returned (16 characters)
  - API-Password returned (secure random string)
  - Success message confirms creation
- **Database Validation**:
  - Brand record exists in `brands` table with correct `owner_id`
  - Brand has unique `schema_name` (format: `brand_[guid]`)
  - PostgreSQL schema exists with schema name
  - PostgreSQL user created with format `user_[guid]`
  - User has appropriate permissions on schema only
  - Schema contains all required tables:
    - `brand_[guid].users` (with api_secret and api_password_hash columns)
    - `brand_[guid].user_roles`
    - `brand_[guid].brand_infoes` (key-value pairs)
    - `brand_[guid].brand_configs` (key-value pairs)
  - Brand owner user seeded in `brand_[guid].users` with:
    - Generated API-Key stored in api_secret
    - Hashed API-Password in api_password_hash
    - BrandOwner role in user_roles table

### Scenario 1.5: Duplicate Brand Name (Negative Test)
- **Test**: Create brand with name already used by same user
- **Expected**: System should allow (brands can have same name, different schemas)
- **Validation**: Both brands exist with different schema names

### Scenario 1.6: Database Schema Validation (Infrastructure Test)
- **Test**: Verify schema structure after brand creation
- **Expected**: Schema contains required tables and seed data
- **Validation**:
  - Schema exists: `brand_[guid]`
  - Tables exist with correct structure:
    - `brand_[guid].users` with columns: id, api_secret, api_password_hash, brand_schema, display_name, is_active, created_at, updated_at, last_login_at
    - `brand_[guid].user_roles` with columns: id, user_id, role, is_active, created_at, updated_at
    - `brand_[guid].brand_infoes` with columns: id, key, value, created_at, updated_at
    - `brand_[guid].brand_configs` with columns: id, key, value, created_at, updated_at
  - Indexes exist for performance:
    - Index on users.api_secret for fast authentication
    - Index on users.is_active for filtering
    - Unique index on user_roles(user_id, role)
  - Seed data exists:
    - Brand owner user record with API credentials
    - BrandOwner role assignment
  - PostgreSQL user exists with schema-only permissions

### Scenario 1.7: PostgreSQL User Permissions (Security Test)
- **Test**: Verify database user has correct permissions
- **Expected**: User can only access their schema
- **Validation**:
  - Can connect to database
  - Can access tables in assigned schema
  - Cannot access other schemas
  - Cannot access system tables beyond granted permissions

## Stage 2: Brand Authentication Tests

### Scenario 2.1: Sign In with Invalid API-Key (Negative Test)
- **Test**: Attempt brand authentication with non-existent API-Key
- **Expected**: System should reject with authentication error
- **Validation**: Error message indicates invalid credentials

### Scenario 2.2: Sign In with Wrong API-Password (Negative Test)
- **Test**: Use valid API-Key but wrong API-Password
- **Expected**: System should reject with authentication error
- **Validation**: Error message indicates invalid credentials

### Scenario 2.3: Sign In without Brand ID (Negative Test)
- **Test**: Provide API-Key and API-Password but no Brand ID
- **Expected**: System should reject with validation error
- **Validation**: Error message indicates brand ID required

### Scenario 2.4: Sign In with Wrong Brand ID (Negative Test)
- **Test**: Use valid API credentials but wrong Brand ID
- **Expected**: System should reject with authorization error
- **Validation**: Error message indicates credentials don't match brand

### Scenario 2.5: Valid Brand Authentication (Positive Test)
- **Test**: Sign in with correct API-Key, API-Password, and Brand ID
- **Expected**: System returns brand-context bearer token
- **Validation**:
  - JWT token returned with brand context claims
  - Token contains brand ID in claims
  - Token contains user role (BrandOwner)
  - Token can be used for brand-specific operations

### Scenario 2.6: Brand Token Expiration (Security Test)
- **Test**: Use expired brand-context token
- **Expected**: System should reject with 401 Unauthorized
- **Validation**: Must re-authenticate to get new token

## Stage 3: Brand Context Operations Tests

### Scenario 3.1: Update Brand Info without Brand Context (Negative Test)
- **Test**: Try to update brand info using principal token (not brand-context)
- **Expected**: System should reject with 403 Forbidden
- **Validation**: Error message indicates brand context required

### Scenario 3.2: Update Brand Info with Wrong Brand Context (Negative Test)
- **Test**: Use brand-context token from Brand A to update Brand B
- **Expected**: System should reject with 403 Forbidden
- **Validation**: Error message indicates wrong brand context

### Scenario 3.3: Valid Brand Info Update (Positive Test)
- **Test**: Update brand info with correct brand-context token
- **Input**: Key-value pairs for brand_infoes table
- **Expected**: System updates brand information successfully
- **Database Validation**:
  - Records created/updated in brand_[guid].brand_infoes
  - Timestamps updated correctly
  - Changes isolated to correct brand schema

### Scenario 3.4: Update Brand Config (Positive Test)
- **Test**: Update brand configuration with brand-context token
- **Input**: Key-value pairs for brand_configs table
- **Expected**: System updates configuration successfully
- **Database Validation**:
  - Records created/updated in brand_[guid].brand_configs
  - Configuration changes applied
  - Audit trail maintained

### Scenario 3.5: Principal Token Cannot Access Brand Data (Security Test)
- **Test**: Verify principal token (email/password auth) cannot access brand-specific data
- **Expected**: All brand operations require brand-context token
- **Validation**: Proper separation of authentication contexts

## Stage 4: Brand Access & Authorization Tests

### Scenario 4.1: Access Non-Owned Brand (Negative Test)
- **Test**: User tries to access brand owned by another user
- **Expected**: System should reject with 403 Forbidden
- **Validation**: Error message indicates insufficient permissions

### Scenario 4.2: Access Non-Existent Brand (Negative Test)
- **Test**: User tries to access brand that doesn't exist
- **Expected**: System should reject with 404 Not Found
- **Validation**: Error message indicates brand not found

### Scenario 4.3: Valid Brand Access (Positive Test)
- **Test**: User accesses their own brand using principal token
- **Expected**: System should return brand details
- **Validation**: 
  - Brand information returned (id, name, schemaName)
  - API credentials NOT returned (security)
  - Owner verification successful

### Scenario 4.4: Brand List Access (Positive Test)
- **Test**: User requests list of their brands
- **Expected**: System should return only user's brands
- **Validation**: 
  - Only brands owned by user returned
  - No brands from other users included
  - Each brand shows basic info only

## Stage 5: Brand Information Management Tests

### Scenario 5.1: Update Brand Name without Principal Token (Negative Test)
- **Test**: Update brand name without authentication
- **Expected**: System should reject with 401 Unauthorized
- **Note**: Brand name can only be changed by principal owner, not via brand-context token

### Scenario 5.2: Update Non-Owned Brand Name (Negative Test)  
- **Test**: User tries to update brand name of brand not owned by them
- **Expected**: System should reject with 403 Forbidden

### Scenario 5.3: Update Brand Name with Brand-Context Token (Negative Test)
- **Test**: Try to update brand name using brand-context token instead of principal token
- **Expected**: System should reject - only principal owner can rename brand
- **Validation**: Error message indicates principal authentication required

### Scenario 5.4: Update Brand Name - Invalid Format (Negative Test)
- **Test**: Update brand name with invalid format
- **Expected**: System should reject with validation error

### Scenario 5.5: Valid Brand Name Update (Positive Test)
- **Test**: Principal owner updates their brand name with valid name
- **Expected**: System should update brand name successfully
- **Database Validation**:
  - Brand name updated in database
  - Schema name remains unchanged (important!)
  - API credentials remain unchanged
  - Brand functionality still works
  - Update timestamp modified

### Scenario 5.6: Update Brand Info with Brand-Context Token (Positive Test)
- **Test**: Update brand information using brand-context token
- **Mutation**: `updateBrandInfo`
- **Input**: Key-value pairs (e.g., address, phone, email)
- **Expected**: Information stored in brand_infoes table
- **Validation**:
  - Data stored in correct brand schema
  - Can retrieve updated information
  - Isolation from other brands verified

## Stage 6: Brand Deletion Tests

### Scenario 6.1: Delete Brand with Brand-Context Token (Negative Test)
- **Test**: Try to delete brand using brand-context token
- **Expected**: System should reject - only principal owner can delete brand
- **Validation**: Error message indicates principal authentication required

### Scenario 6.2: Delete Non-Owned Brand (Negative Test)
- **Test**: User tries to delete brand not owned by them
- **Expected**: System should reject with 403 Forbidden

### Scenario 6.3: Delete Non-Existent Brand (Negative Test)
- **Test**: User tries to delete brand that doesn't exist  
- **Expected**: System should reject with 404 Not Found

### Scenario 6.4: Delete Without Authentication (Negative Test)
- **Test**: Delete brand without authentication token
- **Expected**: System should reject with 401 Unauthorized

### Scenario 6.5: Valid Brand Deletion (Positive Test)
- **Test**: Principal owner deletes their own brand
- **Expected**: System should delete brand successfully
- **Database Validation**:
  - Brand record removed from `brands` table
  - PostgreSQL schema dropped completely (including all tables)
  - PostgreSQL user removed
  - All brand_infoes data deleted
  - All brand_configs data deleted
  - All brand users deleted
  - No orphaned data remains

### Scenario 6.6: Database Cleanup Verification (Infrastructure Test)
- **Test**: Verify complete cleanup after brand deletion
- **Expected**: No traces of brand remain in system
- **Validation**:
  - Schema `brand_[guid]` does not exist in PostgreSQL
  - Database user `user_[guid]` does not exist
  - No references in system tables
  - Cannot authenticate with old API credentials
  - Brand-context tokens become invalid

### Scenario 6.7: Deletion Impact on Other Brands (Isolation Test)
- **Test**: Delete one brand while user has multiple brands
- **Expected**: Other brands remain unaffected
- **Validation**:
  - Other brands still accessible
  - Other schemas still exist
  - Other database users still work
  - Other API credentials still valid

## Implementation Requirements

### GraphQL Mutations Required
```graphql
# Brand Creation (Principal Token Required)
mutation CreateBrand($input: CreateBrandInput!) {
  createBrand(input: $input) {
    success
    message
    brand {
      id
      name
      schemaName
      createdAt
    }
    apiKey        # Brand owner API-Key (store securely!)
    apiPassword   # Brand owner API-Password (store securely!)
  }
}

# Brand Authentication (Get Brand-Context Token)
mutation SignInBrand($input: BrandSignInInput!) {
  signInBrand(input: $input) {
    success
    message
    token         # Brand-context bearer token
    user {
      id
      displayName
      role
    }
  }
}

# Update Brand Info (Brand-Context Token Required)
mutation UpdateBrandInfo($input: UpdateBrandInfoInput!) {
  updateBrandInfo(input: $input) {
    success
    message
    brandInfo {
      key
      value
      updatedAt
    }
  }
}

# Brand Name Update (Principal Token Required)
mutation UpdateBrandName($brandId: String!, $newName: String!) {
  updateBrandName(brandId: $brandId, newName: $newName) {
    success
    message
    brand {
      id
      name
      updatedAt
    }
  }
}

# Brand Deletion (Principal Token Required)
mutation DeleteBrand($brandId: String!) {
  deleteBrand(brandId: $brandId) {
    success
    message
  }
}
```

### GraphQL Queries Required
```graphql
# Get User's Brands
query GetMyBrands {
  myBrands {
    id
    name
    schemaName
    createdAt
    updatedAt
  }
}

# Get Specific Brand
query GetBrand($brandId: String!) {
  brand(id: $brandId) {
    id
    name
    schemaName
    createdAt
    updatedAt
  }
}
```

### Service Layer Methods Required

#### IBrandService (Principal Operations)
```csharp
// Create brand and return API credentials
Task<(bool Success, string Message, Brand? Brand, string? ApiKey, string? ApiPassword)> 
    CreateBrandAsync(
        string userId, 
        string brandName, 
        CancellationToken cancellationToken = default);

// Update brand name (principal only)
Task<(bool Success, string Message, Brand? Brand)> UpdateBrandNameAsync(
    string userId, 
    string brandId, 
    string newName, 
    CancellationToken cancellationToken = default);

// Delete brand (principal only)
Task<(bool Success, string Message)> DeleteBrandAsync(
    string userId, 
    string brandId, 
    CancellationToken cancellationToken = default);

// Get brand details (principal)
Task<Brand?> GetBrandAsync(
    string userId, 
    string brandId, 
    CancellationToken cancellationToken = default);

// Get user's brands (principal)
Task<IEnumerable<Brand>> GetUserBrandsAsync(
    string userId, 
    CancellationToken cancellationToken = default);

// Verify brand ownership
Task<bool> UserOwnsBrandAsync(
    string userId, 
    string brandId,
    CancellationToken cancellationToken = default);
```

#### IBrandAuthService (Brand-Context Authentication)
```csharp
// Authenticate with API credentials to get brand-context token
Task<(bool Success, string Message, string? Token, BrandUser? User)> 
    AuthenticateBrandUserAsync(
        string brandId,
        string apiKey, 
        string apiPassword,
        CancellationToken cancellationToken = default);

// Validate brand-context token
Task<(bool IsValid, string? BrandId, string? UserId, string? Role)> 
    ValidateBrandTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
```

#### IBrandDataService (Brand-Context Operations)
```csharp
// Update brand info (requires brand-context token)
Task<(bool Success, string Message)> UpdateBrandInfoAsync(
    string brandId,
    string key,
    string value,
    string userId, // From brand-context token
    CancellationToken cancellationToken = default);

// Get brand info
Task<Dictionary<string, string>> GetBrandInfoAsync(
    string brandId,
    CancellationToken cancellationToken = default);

// Update brand config
Task<(bool Success, string Message)> UpdateBrandConfigAsync(
    string brandId,
    string key,
    string value,
    string userId, // From brand-context token
    CancellationToken cancellationToken = default);
```

#### IPostgreSQLManagementService  
```csharp
Task<(bool Success, string SchemaName, string DatabaseUser, string DatabasePassword)> 
    CreateBrandSchemaAsync(string brandId, string brandName);

Task<bool> DropBrandSchemaAsync(string schemaName, string databaseUser);

Task<bool> ValidateSchemaAccessAsync(string schemaName, string databaseUser);
```

### Test Helper Methods Required

#### BrandTestHelper (Principal Operations)
```csharp
// Create brand and get API credentials
Task<(bool success, string? brandId, string? apiKey, string? apiPassword)> 
    CreateBrandAsync(string name);

// Get brand details
Task<(bool success, Brand? brand)> GetBrandAsync(string brandId);  

// Get user's brands
Task<IEnumerable<Brand>> GetMyBrandsAsync();

// Update brand name (principal token)
Task<bool> UpdateBrandNameAsync(string brandId, string newName);

// Delete brand (principal token)
Task<bool> DeleteBrandAsync(string brandId);

// Brand authentication
Task<(bool success, string? token)> SignInBrandAsync(
    string brandId, string apiKey, string apiPassword);

// Update brand info (brand-context token)
Task<bool> UpdateBrandInfoAsync(string key, string value);

// Get brand info
Task<Dictionary<string, string>> GetBrandInfoAsync(string brandId);
```

#### DatabaseHelper (Extensions)
```csharp
Task<bool> BrandExistsAsync(string brandId);
Task<bool> SchemaExistsAsync(string schemaName);
Task<bool> DatabaseUserExistsAsync(string username);
Task<Brand?> GetBrandFromDatabaseAsync(string brandId);
```

## Authentication Context Testing

### Principal Token vs Brand-Context Token

#### Principal Token Operations (Email/Password Auth)
- **Create Brand**: ✅ Allowed - Returns API credentials
- **Delete Brand**: ✅ Allowed - Only owner can delete
- **Update Brand Name**: ✅ Allowed - Only owner can rename
- **List User's Brands**: ✅ Allowed - See owned brands
- **Get Brand Details**: ✅ Allowed - Basic info only
- **Update Brand Info**: ❌ Not Allowed - Requires brand context
- **Update Brand Config**: ❌ Not Allowed - Requires brand context

#### Brand-Context Token Operations (API-Key/Password Auth)
- **Create Brand**: ❌ Not Allowed - Principal only
- **Delete Brand**: ❌ Not Allowed - Principal only  
- **Update Brand Name**: ❌ Not Allowed - Principal only
- **List User's Brands**: ❌ Not Allowed - Principal only
- **Get Brand Details**: ✅ Allowed - Full brand data access
- **Update Brand Info**: ✅ Allowed - Primary purpose
- **Update Brand Config**: ✅ Allowed - Primary purpose
- **Access Brand Tables**: ✅ Allowed - Full schema access

### Token Validation Tests
1. **Wrong Context**: Using principal token for brand operations should fail
2. **Cross-Brand Access**: Brand A token cannot access Brand B data
3. **Expired Tokens**: Both token types should expire and require renewal
4. **Invalid Tokens**: Malformed tokens should be rejected
5. **Permission Boundaries**: Each token type has strict permission limits

## Test Data Management

### Test User Setup
- Create verified users for testing (reuse from AuthTestSuite)
- Each test stage uses authenticated users
- Clean up test users after all tests

### Test Brand Lifecycle
- Create test brands with predictable names
- Track created brands for cleanup
- Verify complete removal after deletion tests
- Handle test failures gracefully (orphaned data cleanup)

### Database State Validation
- Before each test: Verify clean state
- During tests: Validate intermediate states  
- After each test: Verify expected final state
- Suite cleanup: Remove all test data

## Performance Considerations

### Schema Creation Performance
- Monitor schema creation time (should be < 30 seconds)
- Test concurrent brand creation
- Validate resource usage during creation

### Deletion Performance
- Monitor schema deletion time (should be < 10 seconds)
- Test large schema deletion
- Validate complete cleanup

## Security Testing

### Authorization Testing
- Test all unauthorized access scenarios
- Validate JWT token requirements
- Test expired/invalid tokens

### SQL Injection Prevention  
- Test brand names with SQL injection attempts
- Validate input sanitization
- Test schema name generation security

### Data Isolation Testing
- Verify users cannot access other schemas
- Test database user permission boundaries
- Validate cross-tenant data isolation

## Error Handling Testing

### Database Connection Failures
- Test behavior when database is unavailable
- Test partial failures (schema created, user creation fails)
- Validate rollback mechanisms

### Concurrent Operations
- Test simultaneous brand creation by same user
- Test concurrent deletion attempts
- Validate state consistency

## Success Criteria

### All Tests Pass
- ✅ All negative test scenarios properly rejected
- ✅ All positive test scenarios succeed  
- ✅ Database integrity maintained
- ✅ Complete cleanup after deletion
- ✅ Security boundaries enforced

### Performance Targets
- ✅ Brand creation: < 30 seconds
- ✅ Brand deletion: < 10 seconds  
- ✅ Brand queries: < 1 second

### Security Validation
- ✅ No unauthorized access possible
- ✅ Complete data isolation between brands
- ✅ No data leakage between tenants

## Stage 5: User Deletion Impact Tests (Cross-Suite Integration)

### Scenario 5.1: User Deletion Cascades to Brands
- **Test**: Delete a user who owns multiple brands
- **Expected**: All user's brands should be deleted automatically
- **Database Validation**:
  - All brand records removed from `brands` table
  - All brand schemas dropped
  - All brand-specific PostgreSQL users removed
  - No orphaned data remains

### Scenario 5.2: Verify Complete Cleanup After User Deletion
- **Test**: Verify no brand data remains after user deletion
- **Expected**: Complete cleanup of all brand-related resources
- **Validation**:
  - No brand records exist for deleted user
  - No schemas remain in PostgreSQL
  - No database users remain
  - No brand-related permissions remain

### Scenario 5.3: User Deletion Does Not Affect Other Users' Brands
- **Test**: Delete user while other users have brands
- **Expected**: Other users' brands remain intact
- **Validation**:
  - Other users' brands still accessible
  - Other schemas still exist
  - System stability maintained

## AuthTestSuite Updates Required

After implementing Brand TDD, the AuthTestSuite needs to be updated:

### Stage 5 (Account Deletion) Modifications

#### Enhanced Scenario 5.2: Delete Account with Brands
```csharp
// Before deletion: Create test brands
var brand1 = await brandHelper.CreateBrandAsync("TestBrand1");
var brand2 = await brandHelper.CreateBrandAsync("TestBrand2");

// Verify brands exist
TestReporter.ReportInfo($"Created {2} test brands for deletion test");

// Delete user account
var deleteSuccess = await userHelper.DeleteAccountAsync(testUser.Password);

// Verify cascade deletion
if (deleteSuccess)
{
    // Check user removed
    var userRemoved = !await dbHelper.UserIsActiveAsync(testUser.Email);
    TestReporter.ReportPositiveTest("User removed from database", userRemoved);
    
    // Check all brands removed
    var brandsRemoved = !await dbHelper.UserHasBrandsAsync(testUser.Id);
    TestReporter.ReportPositiveTest("All user brands removed", brandsRemoved);
    
    // Check schemas dropped
    var schemasDropped = !await dbHelper.SchemasExistForUserAsync(testUser.Id);
    TestReporter.ReportPositiveTest("All brand schemas dropped", schemasDropped);
    
    // Check PostgreSQL users removed
    var dbUsersRemoved = !await dbHelper.BrandDbUsersRemovedAsync(testUser.Id);
    TestReporter.ReportPositiveTest("All brand database users removed", dbUsersRemoved);
}
```

## Service Layer Updates Required

### IUserService Updates
```csharp
/// <summary>
/// Delete user account and all associated brands
/// </summary>
Task<bool> DeleteUserAsync(
    string userId, 
    CancellationToken cancellationToken = default);
```

### UserService Implementation Updates
```csharp
public async Task<bool> DeleteUserAsync(
    string userId, 
    CancellationToken cancellationToken = default)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    
    try
    {
        // Get all user's brands
        var userBrands = await _brandService.GetUserBrandsAsync(userId, cancellationToken);
        
        // Delete each brand (cascades to schema and db user)
        foreach (var brand in userBrands)
        {
            await _brandService.DeleteBrandAsync(userId, brand.Id, cancellationToken);
        }
        
        // Delete user record
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(cancellationToken);
        _logger.LogError(ex, "Failed to delete user {UserId} with cascade", userId);
        throw;
    }
}
```

## Database Helper Extensions Required
```csharp
// Check if user has any brands
Task<bool> UserHasBrandsAsync(string userId);

// Check if any schemas exist for user's brands
Task<bool> SchemasExistForUserAsync(string userId);

// Check if brand database users are removed
Task<bool> BrandDbUsersRemovedAsync(string userId);

// Get count of user's brands
Task<int> GetUserBrandCountAsync(string userId);
```

## Transaction and Rollback Considerations

### Atomic Operations Required
1. User deletion must be atomic with brand deletion
2. If any brand deletion fails, entire operation rolls back
3. Use database transactions for consistency

### Rollback Scenarios
- Schema deletion fails → Rollback user deletion
- Database user removal fails → Rollback all changes
- Any database error → Complete rollback

### Error Recovery
```csharp
// Cleanup orphaned resources on failure
Task CleanupOrphanedBrandResourcesAsync(string userId);

// Retry mechanism for transient failures
Task<bool> RetryDeleteUserWithBrandsAsync(string userId, int maxRetries = 3);
```

## Performance Considerations for Cascade Deletion

### Deletion Time Estimates
- User with 0 brands: < 1 second
- User with 1-5 brands: < 30 seconds
- User with 5-10 brands: < 60 seconds
- Consider async/background processing for large deletions

### Optimization Strategies
1. Parallel brand deletion where possible
2. Batch schema operations
3. Use background jobs for large cleanups
4. Implement progress tracking for UI feedback

## Next Steps

1. **Implement Test Suite Structure**
   - Create `BrandTestSuite.cs` with stage framework
   - Create helper methods in `BrandTestHelper.cs`
   - Extend `DatabaseHelper.cs` with brand validation methods

2. **Implement Service Layer (TDD)**
   - Start with failing tests
   - Implement `IBrandService` and `BrandService`
   - Implement `IPostgreSQLManagementService` extensions
   - Update `UserService` with cascade deletion

3. **Implement GraphQL Layer (TDD)**  
   - Create brand mutations and queries
   - Implement proper authorization
   - Add input validation
   - Update user deletion to handle brands

4. **Database Schema & Seeding**
   - Implement brand schema creation logic
   - Create brand-specific seeding templates
   - Implement cleanup mechanisms
   - Add cascade deletion support

5. **Integration Testing**
   - Run complete test suite
   - Validate multi-tenant isolation
   - Performance testing under load
   - Test cascade deletion scenarios

6. **Update AuthTestSuite**
   - Modify Stage 5 to test brand cascade deletion
   - Add brand creation before user deletion tests
   - Validate complete cleanup

---

## Key Implementation Points

### Dual Authentication System
1. **Principal Authentication**: Email/Password for system-wide operations
2. **Brand Authentication**: API-Key/Password/BrandId for brand-specific operations
3. **Strict Separation**: Principal tokens cannot perform brand data operations
4. **API Credentials**: Generated once during brand creation, cannot be changed

### Database Schema Structure
Each brand gets its own PostgreSQL schema containing:
- `brand_[guid].users` - Brand users with API authentication
- `brand_[guid].user_roles` - Role assignments (BrandOwner, BrandOperator)
- `brand_[guid].brand_infoes` - Key-value store for brand information
- `brand_[guid].brand_configs` - Key-value store for brand configuration

### Security Boundaries
- Principal owners manage brands but need brand-context for data operations
- Brand-context tokens are isolated to their specific brand
- Cross-brand access is strictly prohibited
- API credentials must be stored securely by users

### Testing Focus Areas
1. **Authentication Context**: Verify correct token type for each operation
2. **Data Isolation**: Ensure complete separation between brands
3. **Permission Enforcement**: Test all negative scenarios thoroughly
4. **Cascade Operations**: User deletion must clean up all brand resources

---

This TDD plan ensures comprehensive testing of brand management functionality with proper multi-tenant isolation, dual authentication system, strict security boundaries, and cascade deletion when users are removed from the system.