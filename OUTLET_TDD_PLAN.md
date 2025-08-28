# Outlet TDD Implementation Plan

## Overview
This document outlines the complete implementation plan for the Outlet functionality in the Kanriya multi-tenant brand management system. Outlets are physical or logical business locations that belong to a Brand, with their own data isolation and employee management.

## Core Architecture

### Conceptual Model
- **Brand**: The parent entity (e.g., "Geprek Bensu")
- **Outlet**: Individual business locations under a brand (e.g., "Geprek Bensu - Mall A")
- **User**: Brand-level API credentials for system access
- **Employee**: People who work for the brand
- **Outlet-Employee**: Assignment of employees to specific outlets with positions

### Data Isolation Strategy
- All outlets share the brand's PostgreSQL schema (`brand_xxx`)
- Outlet-specific data uses `outlet_id` foreign keys for isolation
- Brand-level data (products, employees) shared across outlets
- Outlet-level data (inventory, transactions) isolated per outlet

## Database Schema Design

### 1. Outlets Table
```sql
CREATE TABLE outlets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL,
    name VARCHAR(255) NOT NULL,
    address TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(code)  -- Unique within brand schema
);
```

### 2. User-Outlet Permissions
```sql
CREATE TABLE user_outlets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    outlet_id UUID NOT NULL REFERENCES outlets(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, outlet_id)
);
```

### 3. Employees (Brand-level)
```sql
CREATE TABLE employees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    phone VARCHAR(50),
    address TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(code)  -- Unique within brand schema
);
```

### 4. Outlet-Employee Assignments
```sql
CREATE TABLE outlet_employees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    outlet_id UUID NOT NULL REFERENCES outlets(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    position VARCHAR(100) NOT NULL,  -- e.g., "manager", "cashier", "supervisor"
    pin VARCHAR(255) NOT NULL,  -- BCrypt hashed 6-digit PIN
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(outlet_id, employee_id),
    UNIQUE(outlet_id, pin)  -- PIN unique within outlet
);
```

## Authentication & Authorization Flow

### Current System Enhancement

#### 1. Brand Authentication (Existing)
```mermaid
Client App -> Server: API Key + Password + Brand ID
Server -> Client App: JWT with brand context
```

#### 2. Outlet Access (New)
```mermaid
JWT includes:
- brand_id
- brand_schema
- user_id
- outlet_ids: ["id1", "id2", ...] // From user_outlets table
- roles: ["BrandOwner", "BrandOperator"]
```

#### 3. Employee Check-in Flow (Future Implementation)
```mermaid
Client -> Server: Select Outlet from JWT.outlet_ids
Client -> Server: Enter 6-digit PIN
Server: Validate PIN against outlet_employees
Server -> Client: Employee info + position
Client: Operating as Employee X at Outlet Y
```

## GraphQL API Specification

### Outlet Management Module

#### Mutations
```graphql
type Mutation {
    # Outlet CRUD operations
    createOutlet(input: CreateOutletInput!): CreateOutletOutput!
    updateOutlet(id: ID!, input: UpdateOutletInput!): UpdateOutletOutput!
    deleteOutlet(id: ID!): DeleteOutletOutput!
    
    # User-Outlet permission management
    grantUserOutletAccess(input: GrantOutletAccessInput!): GrantOutletAccessOutput!
    revokeUserOutletAccess(userId: ID!, outletId: ID!): RevokeOutletAccessOutput!
    updateUserOutlets(userId: ID!, outletIds: [ID!]!): UpdateUserOutletsOutput!
}

input CreateOutletInput {
    code: String!
    name: String!
    address: String
}

input UpdateOutletInput {
    code: String
    name: String
    address: String
    isActive: Boolean
}

input GrantOutletAccessInput {
    userId: ID!
    outletId: ID!
}
```

#### Queries
```graphql
type Query {
    # Outlet queries
    outlets: [Outlet!]!
    outlet(id: ID!): Outlet
    myOutlets: [Outlet!]!  # Based on current user's user_outlets
    
    # Permission queries
    userOutletAccess(userId: ID!): [Outlet!]!
    outletUsers(outletId: ID!): [BrandUser!]!
}

type Outlet {
    id: ID!
    code: String!
    name: String!
    address: String
    isActive: Boolean!
    createdAt: DateTime!
    updatedAt: DateTime!
    employees: [OutletEmployee!]!
    users: [BrandUser!]!
}
```

#### Subscriptions
```graphql
type Subscription {
    onOutletChanged: OutletEvent!
}

type OutletEvent {
    event: EventType!  # CREATED, UPDATED, DELETED
    document: Outlet
    time: DateTime!
    _previous: Outlet
}
```

### Employee Management Module

#### Mutations
```graphql
type Mutation {
    # Employee CRUD
    createEmployee(input: CreateEmployeeInput!): CreateEmployeeOutput!
    updateEmployee(id: ID!, input: UpdateEmployeeInput!): UpdateEmployeeOutput!
    deleteEmployee(id: ID!): DeleteEmployeeOutput!
    
    # Outlet-Employee assignment
    assignEmployeeToOutlet(input: AssignEmployeeInput!): AssignEmployeeOutput!
    updateEmployeeAssignment(id: ID!, input: UpdateAssignmentInput!): UpdateAssignmentOutput!
    removeEmployeeFromOutlet(outletId: ID!, employeeId: ID!): RemoveEmployeeOutput!
    
    # PIN management
    generateEmployeePin(outletId: ID!, employeeId: ID!): GeneratePinOutput!
    updateEmployeePin(outletId: ID!, employeeId: ID!, pin: String!): UpdatePinOutput!
}

input CreateEmployeeInput {
    code: String!
    name: String!
    email: String
    phone: String
    address: String
}

input AssignEmployeeInput {
    outletId: ID!
    employeeId: ID!
    position: String!
    pin: String  # Optional, auto-generate if not provided
}
```

#### Queries
```graphql
type Query {
    # Employee queries
    employees: [Employee!]!
    employee(id: ID!): Employee
    
    # Assignment queries
    outletEmployees(outletId: ID!): [OutletEmployee!]!
    employeeOutlets(employeeId: ID!): [OutletEmployee!]!
    
    # PIN validation (for client apps)
    validatePin(outletId: ID!, pin: String!): ValidatePinOutput!
}

type Employee {
    id: ID!
    code: String!
    name: String!
    email: String
    phone: String
    address: String
    isActive: Boolean!
    createdAt: DateTime!
    updatedAt: DateTime!
    outletAssignments: [OutletEmployee!]!
}

type OutletEmployee {
    id: ID!
    outlet: Outlet!
    employee: Employee!
    position: String!
    isActive: Boolean!
    createdAt: DateTime!
    updatedAt: DateTime!
    # PIN not exposed in GraphQL for security
}
```

#### Subscriptions
```graphql
type Subscription {
    onEmployeeChanged: EmployeeEvent!
    onOutletEmployeeChanged: OutletEmployeeEvent!
}
```

## Implementation Roadmap

### Phase 1: Core Outlet Management (Week 1)
- [ ] Create Outlet entity class
- [ ] Add database migration for outlets table
- [ ] Implement OutletService with CRUD operations
- [ ] Create OutletModule with queries/mutations
- [ ] Add outlet subscription
- [ ] Write unit tests for OutletService
- [ ] Write integration tests for GraphQL endpoints

### Phase 2: User-Outlet Permissions (Week 1)
- [ ] Create UserOutlet entity class
- [ ] Add database migration for user_outlets table
- [ ] Extend OutletService with permission management
- [ ] Add permission management mutations
- [ ] Update JWT token generation to include outlet_ids
- [ ] Update CurrentUser model to include outlet context
- [ ] Write tests for permission management

### Phase 3: Employee Management (Week 2)
- [ ] Create Employee entity class
- [ ] Add database migration for employees table
- [ ] Implement EmployeeService with CRUD operations
- [ ] Create EmployeeModule with queries/mutations
- [ ] Add employee subscription
- [ ] Write unit tests for EmployeeService
- [ ] Write integration tests for employee endpoints

### Phase 4: Outlet-Employee Assignment (Week 2)
- [ ] Create OutletEmployee entity class
- [ ] Add database migration for outlet_employees table
- [ ] Implement assignment management in EmployeeService
- [ ] Implement PinService for PIN generation/validation
- [ ] Add assignment mutations
- [ ] Add PIN management endpoints
- [ ] Write tests for assignments and PIN validation

## File Structure

```
/src/Kanriya.Server/
├── /Data/
│   └── /BrandSchema/
│       ├── Outlet.cs
│       ├── UserOutlet.cs
│       ├── Employee.cs
│       └── OutletEmployee.cs
├── /Services/
│   └── /Data/
│       ├── OutletService.cs
│       ├── EmployeeService.cs
│       └── PinService.cs
├── /Modules/
│   ├── OutletModule.cs
│   └── EmployeeModule.cs
├── /Types/
│   ├── /Inputs/
│   │   ├── CreateOutletInput.cs
│   │   ├── UpdateOutletInput.cs
│   │   ├── GrantOutletAccessInput.cs
│   │   ├── CreateEmployeeInput.cs
│   │   ├── UpdateEmployeeInput.cs
│   │   └── AssignEmployeeInput.cs
│   └── /Outputs/
│       ├── CreateOutletOutput.cs
│       ├── GrantOutletAccessOutput.cs
│       ├── CreateEmployeeOutput.cs
│       ├── AssignEmployeeOutput.cs
│       └── ValidatePinOutput.cs
└── /Subscriptions/
    ├── OutletSubscriptions.cs
    └── EmployeeSubscriptions.cs
```

## Key Implementation Details

### PIN Generation & Management
```csharp
public class PinService
{
    // Generate unique 6-digit PIN for outlet
    public async Task<string> GenerateUniquePin(Guid outletId)
    {
        string pin;
        do {
            pin = Random.Shared.Next(100000, 999999).ToString();
        } while (await PinExistsInOutlet(outletId, pin));
        return pin;
    }
    
    // Store PIN using BCrypt (like passwords)
    public string HashPin(string pin)
    {
        return BCrypt.Net.BCrypt.HashPassword(pin);
    }
    
    // Validate PIN
    public bool ValidatePin(string pin, string hashedPin)
    {
        return BCrypt.Net.BCrypt.Verify(pin, hashedPin);
    }
}
```

### JWT Token Enhancement
```csharp
// In TokenService or AuthenticationService
var claims = new List<Claim>
{
    new Claim("nameid", brandUser.Id.ToString()),
    new Claim("brand_id", brandId),
    new Claim("brand_schema", brandSchema),
    new Claim("token_type", "BRAND"),
};

// Add outlet access claims
var userOutlets = await outletService.GetUserOutlets(brandUser.Id);
foreach (var outlet in userOutlets)
{
    claims.Add(new Claim("outlet_id", outlet.Id.ToString()));
}
```

### Authorization Rules

#### BrandOwner Role
- Full access to all outlets (bypass user_outlets check)
- Can manage all employees and assignments
- Can grant/revoke outlet access to other users

#### BrandOperator Role  
- Access limited to outlets in user_outlets table
- Can view employees but not manage them
- Cannot grant outlet access to other users

#### Future: Position-based Permissions
```csharp
// Example position-based authorization
if (outletEmployee.Position == "manager")
{
    // Manager permissions at this outlet
}
else if (outletEmployee.Position == "cashier")
{
    // Cashier permissions at this outlet
}
```

## Testing Strategy

### Unit Tests

#### OutletService Tests
```csharp
[Fact]
public async Task CreateOutlet_ShouldGenerateUniqueCode()
[Fact]
public async Task DeleteOutlet_ShouldRemoveUserAssociations()
[Fact]
public async Task GrantUserAccess_ShouldUpdatePermissions()
```

#### EmployeeService Tests
```csharp
[Fact]
public async Task AssignEmployee_ShouldGenerateUniquePin()
[Fact]
public async Task Employee_CanHaveMultipleOutletAssignments()
[Fact]
public async Task ValidatePin_ShouldReturnCorrectEmployee()
```

### Integration Tests

#### Authorization Tests
```csharp
[Fact]
public async Task User_CanOnlyAccessAssignedOutlets()
[Fact]
public async Task BrandOwner_CanAccessAllOutlets()
[Fact]
public async Task Pin_MustBeUniqueWithinOutlet()
```

#### GraphQL Tests
```csharp
[Fact]
public async Task CreateOutlet_Mutation_ShouldWork()
[Fact]
public async Task MyOutlets_Query_ShouldReturnUserOutlets()
[Fact]
public async Task OutletSubscription_ShouldEmitEvents()
```

## Security Considerations

1. **PIN Security**
   - Always hash PINs using BCrypt
   - Never expose raw PINs in API responses
   - Implement rate limiting on PIN validation
   - Consider PIN expiration for added security

2. **Outlet Access Control**
   - Always validate outlet_id against user's permissions
   - BrandOwner role bypasses outlet restrictions
   - Implement audit logging for sensitive operations

3. **Data Isolation**
   - Ensure queries always filter by outlet_id where applicable
   - Use database-level constraints to enforce isolation
   - Regular security audits of data access patterns

## Future Enhancements

1. **Soft Delete Implementation**
   - Add `deleted_at` columns post-development
   - Implement restoration functionality
   - Archive historical data

2. **Advanced Features**
   - Outlet operating hours
   - Employee shift management
   - Check-in/check-out logging with timestamps
   - Position-based permission system
   - Outlet performance dashboards
   - Inter-outlet inventory transfers
   - Regional outlet grouping

3. **Client App Integration**
   - Outlet selector UI component
   - PIN entry interface
   - Employee dashboard per outlet
   - Real-time outlet switching

## Migration Considerations

When implementing this system:
1. Start with core tables (outlets, user_outlets)
2. Ensure existing brand users get default outlet access
3. Plan data migration strategy for existing systems
4. Consider backward compatibility for API clients

## Success Metrics

- All tests passing (unit, integration, E2E)
- JWT correctly includes outlet claims
- PIN validation working with proper security
- Subscriptions emitting correct events
- Clean separation of brand vs outlet data
- Performance: <100ms for outlet queries

## Notes for Development

- No soft deletes in development phase (hard delete for now)
- Keep employee as brand-level entity (shared across outlets)
- PIN is per outlet-employee assignment (not global)
- One employee can have different PINs at different outlets
- Focus on clean code over backward compatibility
- Follow existing project patterns and conventions

## More Planned tables (For Future)

- products (brand level)
- inventories (outlet level)
- action_logs (brand level + outlet level)
- taxes (outlet level)
- sell_bands (outlet level)
- type_sales (outlet level)
- discount (brand level)
- payment_medias (outlet level)
- vouchers (brand level)
- bills (outlet level)
- bill_entries (outlet level)
- payments (outlet level)
- table (outlet level)
- room (outlet level)