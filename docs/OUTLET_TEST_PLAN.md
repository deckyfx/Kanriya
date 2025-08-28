# Outlet Test Suite - Comprehensive Test Plan

## Overview
This document outlines all test scenarios for the Outlet Management functionality within the multi-tenant Brand system.

## Test Environment Setup
- **Principal User**: System-level user for brand creation
- **Brand Context**: Test brand with BrandOwner privileges
- **Database**: Clean state before each test run

## Test Stages and Scenarios

### Stage 1: Setup Infrastructure
**Purpose**: Create necessary test data and authentication contexts

#### 1.1 Create Principal User
- **Action**: Create and verify a principal user account
- **Expected**: User created, email verified, signed in successfully
- **Token**: Principal JWT token obtained

#### 1.2 Create Test Brand
- **Action**: Create a brand using principal context
- **Endpoint**: `mutation createBrand`
- **Input**: Brand name (unique with timestamp)
- **Expected**: Brand created with ID, API key, and API password
- **Output**: Brand credentials stored for subsequent tests

#### 1.3 Sign In with Brand Context
- **Action**: Authenticate using brand API credentials
- **Endpoint**: `mutation signIn`
- **Input**: API key as email, API password, brand ID
- **Expected**: Brand JWT token with BrandOwner role
- **Claims**: Should include `outlet_access: all` for BrandOwner

---

### Stage 2: Outlet CRUD Operations
**Purpose**: Test basic outlet management functionality

#### 2.1 Create Primary Outlet
- **Action**: Create first outlet for the brand
- **Endpoint**: `mutation createOutlet`
- **Input**:
  ```graphql
  {
    code: "OUT001",
    name: "Main Outlet - Downtown",
    address: "123 Main Street, Downtown"
  }
  ```
- **Expected**: Outlet created successfully with unique ID
- **Validation**: Outlet ID stored for future operations

#### 2.2 Create Secondary Outlet
- **Action**: Create second outlet with different code
- **Endpoint**: `mutation createOutlet`
- **Input**:
  ```graphql
  {
    code: "OUT002",
    name: "Branch Outlet - Mall",
    address: "456 Shopping Mall, Level 2"
  }
  ```
- **Expected**: Second outlet created successfully
- **Purpose**: Test multiple outlets per brand

#### 2.3 List All Outlets
- **Action**: Retrieve all outlets for current brand
- **Endpoint**: `query outlets`
- **Expected**: Returns array with both created outlets
- **Validation**: Count should be >= 2

#### 2.4 Update Outlet Information
- **Action**: Modify outlet name and address
- **Endpoint**: `mutation updateOutlet`
- **Input**:
  ```graphql
  {
    id: "primary-outlet-id",
    name: "Updated Main Outlet",
    address: "789 New Address"
  }
  ```
- **Expected**: Outlet updated successfully
- **What's Updated**: Name changed from "Main Outlet - Downtown" to "Updated Main Outlet", Address changed to "789 New Address"

#### 2.5 Get Single Outlet
- **Action**: Retrieve specific outlet by ID
- **Endpoint**: `query outlet(id: String!)`
- **Input**: Primary outlet ID
- **Expected**: Returns outlet with updated information
- **Validation**: Name should be "Updated Main Outlet"

---

### Stage 3: Permission and Access Control Tests
**Purpose**: Validate role-based access control

#### 3.1 BrandOwner Access to All Outlets
- **Action**: List outlets accessible to current user (BrandOwner)
- **Endpoint**: `query myOutlets`
- **Context**: Using BrandOwner token
- **Expected**: Returns all outlets (same as `outlets` query)
- **Validation**: BrandOwner has unrestricted access

#### 3.2 BrandOperator Limited Access (Future Implementation)
- **Planned Scenario**: Create BrandOperator user
- **Steps**:
  1. Create new brand user with BrandOperator role
  2. Grant access to specific outlet(s)
  3. Test `myOutlets` returns only granted outlets
  4. Test CRUD operations restricted to assigned outlets
- **Note**: Currently not implemented - requires additional user creation

#### 3.3 Cross-Brand Isolation
- **Planned Scenario**: Verify outlets are isolated between brands
- **Steps**:
  1. Create second brand
  2. Attempt to access first brand's outlets
  3. Should fail with authorization error

---

### Stage 4: Negative Test Cases
**Purpose**: Validate error handling and security

#### 4.1 Duplicate Outlet Code
- **Action**: Attempt to create outlet with existing code
- **Endpoint**: `mutation createOutlet`
- **Input**: Same code "OUT001" as primary outlet
- **Expected**: Operation fails with error message
- **Error**: "Outlet with code OUT001 already exists"

#### 4.2 Access Without Brand Context
- **Action**: Switch to principal token and try to list outlets
- **Endpoint**: `query outlets`
- **Context**: Principal JWT (no brand context)
- **Expected**: Authorization error
- **Error**: "Brand-context authentication required"

#### 4.3 Invalid Outlet ID
- **Action**: Attempt to get/update non-existent outlet
- **Endpoint**: `query outlet` or `mutation updateOutlet`
- **Input**: Invalid/random outlet ID
- **Expected**: Not found error

#### 4.4 Empty Required Fields
- **Action**: Create outlet without required fields
- **Tests**:
  - Missing code
  - Missing name
  - Empty strings
- **Expected**: Validation errors

---

### Stage 5: Advanced Scenarios (Future Implementation)

#### 5.1 Outlet Deactivation
- **Action**: Set outlet `isActive` to false
- **Expected**: Outlet marked inactive but not deleted
- **Validation**: Inactive outlets still appear in lists

#### 5.2 Outlet Deletion
- **Endpoint**: `mutation deleteOutlet`
- **Expected**: Soft delete or hard delete based on implementation
- **Validation**: Deleted outlet not in lists

#### 5.3 Outlet-Employee Assignment
- **Future**: Assign employees to specific outlets
- **Validation**: Employees can only access assigned outlets

#### 5.4 Outlet-Specific Inventory
- **Future**: Track inventory per outlet
- **Validation**: Inventory isolated between outlets

---

## GraphQL Schema Reference

### Mutations
```graphql
mutation createOutlet($input: CreateOutletInput!) {
  createOutlet(input: $input) {
    success
    message
    outlet {
      id
      code
      name
      address
      isActive
    }
  }
}

mutation updateOutlet($input: UpdateOutletInput!) {
  updateOutlet(input: $input) {
    success
    message
    outlet {
      id
      code
      name
      address
      isActive
    }
  }
}

mutation deleteOutlet($id: String!) {
  deleteOutlet(id: $id) {
    success
    message
  }
}
```

### Queries
```graphql
query outlets {
  outlets {
    id
    code
    name
    address
    isActive
  }
}

query outlet($id: String!) {
  outlet(id: $id) {
    id
    code
    name
    address
    isActive
  }
}

query myOutlets {
  myOutlets {
    id
    code
    name
    address
    isActive
  }
}
```

### Input Types
```graphql
input CreateOutletInput {
  code: String!
  name: String!
  address: String!
}

input UpdateOutletInput {
  id: String!
  name: String
  address: String
  isActive: Boolean
}
```

---

## Success Metrics
- **CRUD Operations**: All basic operations work correctly
- **Authorization**: Role-based access properly enforced
- **Data Isolation**: Outlets isolated per brand
- **Error Handling**: Appropriate errors for invalid operations
- **Performance**: Operations complete within reasonable time

## Current Implementation Status
✅ **Implemented**:
- Basic CRUD operations
- BrandOwner full access
- Duplicate code prevention
- Principal context denial

⏳ **Pending**:
- BrandOperator role testing
- Outlet deletion
- Cross-brand isolation tests
- Employee-outlet assignment

## Notes
- All operations require brand context (except brand creation)
- Outlet codes must be unique within a brand
- BrandOwners have access to all outlets
- BrandOperators have access only to assigned outlets