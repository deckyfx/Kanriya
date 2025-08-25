# Brand Creation/Deletion TDD Plan

## Overview

This document outlines the Test-Driven Development (TDD) plan for implementing Brand Creation and Deletion functionality in the Kanriya multi-tenant system. Each brand gets its own PostgreSQL schema with isolated data and dedicated database user.

## Test Structure

### Test Suite Organization
```
src/Kanriya.Tests/Suites/BrandTestSuite.cs
├── Stage 1: Brand Creation Tests
├── Stage 2: Brand Access & Authorization Tests  
├── Stage 3: Brand Name Management Tests
└── Stage 4: Brand Deletion Tests
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
- **Expected**: System should create brand successfully
- **Database Validation**:
  - Brand record exists in `brands` table with correct `owner_id`
  - Brand has unique `schema_name` (format: `brand_[guid]`)
  - PostgreSQL schema exists with schema name
  - PostgreSQL user created with format `brand_user_[guid]`
  - User has appropriate permissions on schema only
  - Schema contains seeded tables and roles
  - Brand owner credentials seeded in schema

### Scenario 1.5: Duplicate Brand Name (Negative Test)
- **Test**: Create brand with name already used by same user
- **Expected**: System should allow (brands can have same name, different schemas)
- **Validation**: Both brands exist with different schema names

### Scenario 1.6: Database Schema Validation (Infrastructure Test)
- **Test**: Verify schema structure after brand creation
- **Expected**: Schema contains required tables, roles, and seed data
- **Validation**:
  - Schema exists: `brand_[guid]`
  - Tables exist: `brand_users`, `brand_user_roles`, etc.
  - Roles exist: `brand_owner`, `brand_operator`  
  - Seed data exists: Brand owner user record
  - PostgreSQL user exists with schema-only permissions

### Scenario 1.7: PostgreSQL User Permissions (Security Test)
- **Test**: Verify database user has correct permissions
- **Expected**: User can only access their schema
- **Validation**:
  - Can connect to database
  - Can access tables in assigned schema
  - Cannot access other schemas
  - Cannot access system tables beyond granted permissions

## Stage 2: Brand Access & Authorization Tests

### Scenario 2.1: Access Non-Owned Brand (Negative Test)
- **Test**: User tries to access brand owned by another user
- **Expected**: System should reject with 403 Forbidden
- **Validation**: Error message indicates insufficient permissions

### Scenario 2.2: Access Non-Existent Brand (Negative Test)
- **Test**: User tries to access brand that doesn't exist
- **Expected**: System should reject with 404 Not Found
- **Validation**: Error message indicates brand not found

### Scenario 2.3: Valid Brand Access (Positive Test)
- **Test**: User accesses their own brand
- **Expected**: System should return brand details
- **Validation**: Correct brand data returned

### Scenario 2.4: Brand List Access (Positive Test)
- **Test**: User requests list of their brands
- **Expected**: System should return only user's brands
- **Validation**: 
  - Only brands owned by user returned
  - No brands from other users included

## Stage 3: Brand Name Management Tests

### Scenario 3.1: Update Brand Name - Unauthorized (Negative Test)
- **Test**: Update brand name without authentication
- **Expected**: System should reject with 401 Unauthorized

### Scenario 3.2: Update Non-Owned Brand Name (Negative Test)  
- **Test**: User tries to update brand name of brand not owned by them
- **Expected**: System should reject with 403 Forbidden

### Scenario 3.3: Update Brand Name - Invalid Format (Negative Test)
- **Test**: Update brand name with invalid format
- **Expected**: System should reject with validation error

### Scenario 3.4: Valid Brand Name Update (Positive Test)
- **Test**: User updates their brand name with valid name
- **Expected**: System should update brand name successfully
- **Database Validation**:
  - Brand name updated in database
  - Schema name remains unchanged
  - Brand functionality still works
  - Update timestamp modified

## Stage 4: Brand Deletion Tests

### Scenario 4.1: Delete Non-Owned Brand (Negative Test)
- **Test**: User tries to delete brand not owned by them
- **Expected**: System should reject with 403 Forbidden

### Scenario 4.2: Delete Non-Existent Brand (Negative Test)
- **Test**: User tries to delete brand that doesn't exist  
- **Expected**: System should reject with 404 Not Found

### Scenario 4.3: Delete Without Authentication (Negative Test)
- **Test**: Delete brand without authentication token
- **Expected**: System should reject with 401 Unauthorized

### Scenario 4.4: Valid Brand Deletion (Positive Test)
- **Test**: User deletes their own brand
- **Expected**: System should delete brand successfully
- **Database Validation**:
  - Brand record removed from `brands` table
  - PostgreSQL schema dropped completely
  - PostgreSQL user removed
  - No orphaned data remains

### Scenario 4.5: Database Cleanup Verification (Infrastructure Test)
- **Test**: Verify complete cleanup after brand deletion
- **Expected**: No traces of brand remain in system
- **Validation**:
  - Schema does not exist in PostgreSQL
  - Database user does not exist
  - No references in system tables
  - Cannot connect with old credentials

### Scenario 4.6: Deletion Impact on Other Brands (Isolation Test)
- **Test**: Delete one brand while user has multiple brands
- **Expected**: Other brands remain unaffected
- **Validation**:
  - Other brands still accessible
  - Other schemas still exist
  - Other database users still work

## Implementation Requirements

### GraphQL Mutations Required
```graphql
# Brand Creation
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
  }
}

# Brand Name Update  
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

# Brand Deletion
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

#### IBrandService
```csharp
Task<(bool Success, string Message, Brand? Brand)> CreateBrandAsync(
    string userId, 
    string brandName, 
    CancellationToken cancellationToken = default);

Task<(bool Success, string Message, Brand? Brand)> UpdateBrandNameAsync(
    string userId, 
    string brandId, 
    string newName, 
    CancellationToken cancellationToken = default);

Task<(bool Success, string Message)> DeleteBrandAsync(
    string userId, 
    string brandId, 
    CancellationToken cancellationToken = default);

Task<Brand?> GetBrandAsync(
    string userId, 
    string brandId, 
    CancellationToken cancellationToken = default);

Task<IEnumerable<Brand>> GetUserBrandsAsync(
    string userId, 
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

#### BrandTestHelper
```csharp
Task<(bool success, string? brandId)> CreateBrandAsync(string name);
Task<(bool success, Brand? brand)> GetBrandAsync(string brandId);  
Task<IEnumerable<Brand>> GetMyBrandsAsync();
Task<bool> UpdateBrandNameAsync(string brandId, string newName);
Task<bool> DeleteBrandAsync(string brandId);
```

#### DatabaseHelper (Extensions)
```csharp
Task<bool> BrandExistsAsync(string brandId);
Task<bool> SchemaExistsAsync(string schemaName);
Task<bool> DatabaseUserExistsAsync(string username);
Task<Brand?> GetBrandFromDatabaseAsync(string brandId);
```

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

This TDD plan ensures comprehensive testing of brand management functionality with proper multi-tenant isolation, security, data integrity validation, and cascade deletion when users are removed from the system.