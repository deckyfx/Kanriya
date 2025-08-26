# Brand Implementation Status Report

## Comparison: TDD Plan vs Current Implementation

### ✅ Already Implemented

#### 1. **Brand Creation (Stage 1.4)**
- **Mutation**: `createBrand` exists in BrandModule.cs
- **Service**: `CreateBrandAsync` in BrandService.cs
- **Features**:
  - Creates PostgreSQL schema (brand_[guid])
  - Creates PostgreSQL user (user_[guid])
  - Sets up brand-specific tables (users, user_roles)
  - Seeds brand owner with API credentials
  - Returns brand object with apiSecret and apiPassword
  - Transaction support with rollback on failure

#### 2. **Brand Deletion (Stage 4.4)**
- **Mutation**: `deleteBrand` exists in BrandModule.cs
- **Service**: `DeleteBrandAsync` in BrandService.cs
- **Features**:
  - Drops PostgreSQL schema (CASCADE)
  - Drops PostgreSQL user
  - Removes brand record from database
  - Clears cache
  - Transaction support with rollback

#### 3. **Get User's Brands (Query)**
- **Query**: `myBrands` exists in BrandModule.cs
- **Service**: `GetUserBrandsAsync` in BrandService.cs
- **Note**: Currently returns ALL brands (TODO: implement proper user-brand filtering)

#### 4. **PostgreSQL Management**
- **Service**: Complete PostgreSQLManagementService
- **Features**:
  - CreateDatabaseUserAsync
  - CreateSchemaAsync
  - GrantSchemaAccessAsync
  - DropSchemaAsync
  - DropUserAsync
  - UserExistsAsync
  - SchemaExistsAsync

#### 5. **GraphQL Types**
- **BrandType**: Basic type exists in Shared project
- **BrandInputs**: CreateBrandInput, BrandAuthInput
- **BrandPayloads**: CreateBrandPayload, DeleteBrandPayload, BrandAuthPayload

---

### ❌ Not Yet Implemented

#### 1. **Update Brand Name (Stage 3.4)**
- **Missing Mutation**: `updateBrandName`
- **Missing Service Method**: `UpdateBrandNameAsync`
- **Required**:
  - Update brand name in database
  - Keep schema name unchanged
  - Validate ownership before update

#### 2. **Get Specific Brand (Query)**
- **Missing Query**: `brand(id: String!)`
- **Service Method Exists**: `GetBrandAsync` (but not exposed via GraphQL)

#### 3. **Brand Validation & Authorization**
- **Missing**: Ownership validation in GraphQL layer
- **Current Issue**: GetUserBrandsAsync returns ALL brands (no filtering)
- **Required**:
  - Check brand ownership before update/delete
  - Filter brands by actual ownership

#### 4. **User Deletion Cascade (Stage 5)**
- **Missing**: User deletion doesn't cascade to brands
- **Required in UserService**:
  - Get all user's brands
  - Delete each brand (schema + user)
  - Then delete user record

---

### 🔧 Needs Enhancement

#### 1. **GetUserBrandsAsync Method**
```csharp
// Current (returns all brands - incorrect)
public async Task<List<Brand>> GetUserBrandsAsync(string userId)
{
    // TODO: Implement proper user-brand relationship
    return await GetAllBrandsAsync();
}

// Should be:
public async Task<List<Brand>> GetUserBrandsAsync(string userId)
{
    using var context = await _contextFactory.CreateDbContextAsync();
    return await context.Brands
        .Where(b => b.OwnerId == userId && b.IsActive)
        .OrderBy(b => b.CreatedAt)
        .ToListAsync();
}
```

#### 2. **Brand Authorization**
- Currently no authorization checks in mutations
- Need to verify user owns brand before update/delete

---

## Implementation Checklist

### Immediate Tasks (Required for TDD)

- [ ] Fix `GetUserBrandsAsync` to filter by OwnerId
- [ ] Add `updateBrandName` mutation
- [ ] Add `brand(id)` query
- [ ] Add ownership validation to mutations
- [ ] Implement user deletion cascade to brands

### Service Layer Methods Needed

```csharp
// IBrandService additions needed:
Task<(bool Success, string Message, Brand? Brand)> UpdateBrandNameAsync(
    string userId, 
    string brandId, 
    string newName, 
    CancellationToken cancellationToken = default);

Task<bool> UserOwnsBrandAsync(string userId, string brandId);
```

### GraphQL Mutations Needed

```graphql
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
```

### User Service Updates Needed

```csharp
// In DeleteUserAsync - add brand cleanup:
var userBrands = await _brandService.GetUserBrandsAsync(userId, cancellationToken);
foreach (var brand in userBrands)
{
    await _brandService.DeleteBrandAsync(brand.Id);
}
```

---

## Test Helper Methods Status

### Already Available
- GraphQL client setup
- User authentication helpers
- Database validation helpers

### Need to Create
- `BrandTestHelper.cs` with:
  - CreateBrandAsync
  - GetBrandAsync
  - GetMyBrandsAsync
  - UpdateBrandNameAsync
  - DeleteBrandAsync

### Database Helper Extensions Needed
- BrandExistsAsync
- SchemaExistsAsync
- DatabaseUserExistsAsync
- GetBrandFromDatabaseAsync
- UserHasBrandsAsync
- SchemasExistForUserAsync

---

## Summary

### What's Ready
✅ Core brand creation/deletion functionality
✅ PostgreSQL schema management
✅ Basic GraphQL types and payloads
✅ Transaction support and rollback

### What's Missing
❌ Update brand name functionality
❌ Proper user-brand ownership filtering
❌ Authorization checks in mutations
❌ User deletion cascade to brands
❌ Test helper methods

### Next Steps
1. Fix `GetUserBrandsAsync` to filter by owner
2. Implement `updateBrandName` mutation and service
3. Add authorization checks to all brand mutations
4. Update user deletion to cascade brand cleanup
5. Create BrandTestHelper.cs
6. Begin writing BrandTestSuite.cs following TDD plan