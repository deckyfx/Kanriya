# Authentication & Authorization Flow for Outlets

## Authentication Contexts Overview

```mermaid
graph TB
    subgraph "Authentication Contexts"
        P[Principal Context<br/>System-level Operations]
        B[Brand Context<br/>Brand-specific Operations]
    end
    
    subgraph "System Database"
        UP[users table<br/>Principal Users]
        BR[brands table<br/>Brand Registry]
    end
    
    subgraph "Brand Schema: brand_xxx"
        BU[users table<br/>Brand Users/API Keys]
        BUR[user_roles table<br/>BrandOwner/BrandOperator]
        O[outlets table<br/>Physical Locations]
        UO[user_outlets table<br/>User-Outlet Access]
        E[employees table<br/>Brand Employees]
        OE[outlet_employees table<br/>Employee-Outlet Assignment]
    end
    
    P --> UP
    P --> BR
    B --> BU
    B --> BUR
    B --> O
    B --> UO
    BU --> UO
    O --> UO
    E --> OE
    O --> OE
```

## Authentication Flow

### 1. Principal Authentication (System Admin)
```mermaid
sequenceDiagram
    participant Client
    participant Server
    participant SystemDB as System DB
    participant JWT
    
    Client->>Server: Email + Password
    Server->>SystemDB: Validate in users table
    SystemDB-->>Server: User found
    Server->>JWT: Generate Token
    Note over JWT: Claims:<br/>- nameid: user_guid<br/>- email: user@example.com<br/>- token_type: null/missing<br/>- roles: []
    JWT-->>Client: JWT Token (Principal Context)
    
    Note over Client: Can: Create brands, manage system<br/>Cannot: Access any brand data
```

### 2. Brand Authentication (Brand Operations)
```mermaid
sequenceDiagram
    participant Client
    participant Server
    participant SystemDB as System DB
    participant BrandDB as Brand Schema
    participant JWT
    
    Client->>Server: API Key + Password + Brand ID
    Server->>SystemDB: Validate Brand exists
    Server->>BrandDB: Connect to brand_xxx schema
    Server->>BrandDB: Validate in brand_xxx.users table
    BrandDB-->>Server: Brand User found
    Server->>BrandDB: Get user_outlets for user
    BrandDB-->>Server: Outlet IDs: [id1, id2, ...]
    Server->>JWT: Generate Token
    Note over JWT: Claims:<br/>- nameid: brand_user_guid<br/>- brand_id: brand_guid<br/>- brand_schema: brand_xxx<br/>- token_type: BRAND<br/>- outlet_ids: [id1, id2]<br/>- roles: [BrandOwner/BrandOperator]
    JWT-->>Client: JWT Token (Brand Context)
    
    Note over Client: Can: Access brand data<br/>Access allowed outlets only
```

## Authorization Matrix

### Role-Based Outlet Access

```mermaid
graph LR
    subgraph "Roles & Permissions"
        BO[BrandOwner Role]
        BOP[BrandOperator Role]
    end
    
    subgraph "Outlet Access Control"
        ALL[All Outlets<br/>No user_outlets check]
        ASSIGNED[Assigned Outlets Only<br/>Check user_outlets table]
    end
    
    subgraph "Operations"
        CREATE[Create/Delete Outlets]
        MANAGE[Manage Outlet Data]
        GRANT[Grant/Revoke Access]
        VIEW[View Outlet Data]
    end
    
    BO --> ALL
    BO --> CREATE
    BO --> MANAGE
    BO --> GRANT
    BO --> VIEW
    
    BOP --> ASSIGNED
    BOP --> MANAGE
    BOP --> VIEW
    BOP -.-> GRANT
    
    style BO fill:#90EE90
    style BOP fill:#ADD8E6
    style CREATE fill:#FFB6C1
    style GRANT fill:#FFB6C1
```

## Outlet Access Validation Flow

```mermaid
flowchart TD
    Start[Request with JWT]
    CheckContext{Token Type?}
    
    CheckContext -->|BRAND| BrandPath[Brand Context]
    CheckContext -->|null/missing| PrincipalPath[❌ Deny: Outlet operations<br/>require Brand Context]
    
    BrandPath --> CheckRole{User Role?}
    
    CheckRole -->|BrandOwner| AllAccess[✅ Access ALL outlets<br/>in this brand]
    CheckRole -->|BrandOperator| CheckUserOutlets{Check user_outlets}
    
    CheckUserOutlets -->|outlet_id IN user_outlets| AllowAccess[✅ Access granted<br/>to specific outlet]
    CheckUserOutlets -->|outlet_id NOT IN user_outlets| DenyAccess[❌ Access denied<br/>Not assigned to outlet]
    
    AllAccess --> Operations[Perform Operations:<br/>- CRUD Outlets<br/>- Manage Employees<br/>- Grant Access<br/>- View Data]
    
    AllowAccess --> LimitedOps[Perform Operations:<br/>- View Outlet Data<br/>- Manage assigned outlet<br/>- Cannot grant access]
```

## Employee Authentication (Future - Phase 3/4)

```mermaid
sequenceDiagram
    participant Employee
    participant Client as Client App
    participant Server
    participant BrandDB as Brand Schema
    
    Note over Employee: Already has Brand JWT<br/>with outlet_ids claim
    
    Employee->>Client: Select Outlet from available list
    Client->>Client: Set current_outlet = selected_id
    Employee->>Client: Enter 6-digit PIN
    Client->>Server: Validate PIN for outlet_id
    Server->>BrandDB: Query outlet_employees<br/>WHERE outlet_id = ? AND pin_hash = ?
    BrandDB-->>Server: Employee record + position
    Server-->>Client: Employee validated
    
    Note over Client: Operating as:<br/>Employee X<br/>Position: Manager<br/>At Outlet Y
```

## Key Security Points

1. **Principal Context**: 
   - Cannot access ANY brand-specific data
   - Only for system administration
   - No outlet access whatsoever

2. **Brand Context Required**:
   - ALL outlet operations require Brand authentication
   - Must have valid brand_id and brand_schema in JWT
   - token_type must be "BRAND"

3. **Outlet Access Control**:
   - BrandOwner: Bypasses user_outlets check, full access
   - BrandOperator: Must be in user_outlets table for access
   - No cross-brand outlet access possible

4. **Data Isolation**:
   - Each brand's outlets completely isolated in brand_xxx schema
   - No shared outlet data between brands
   - outlet_id only meaningful within brand context

## Common Mistakes to Avoid

❌ **Wrong**: Allowing Principal users to access outlets
✅ **Right**: Outlets only accessible in Brand context

❌ **Wrong**: Checking outlet permissions for BrandOwner
✅ **Right**: BrandOwner has implicit access to all brand outlets

❌ **Wrong**: Using "Business" or "Tenant" terminology
✅ **Right**: Always use "Brand" terminology

❌ **Wrong**: Sharing outlet IDs across brands
✅ **Right**: Outlet IDs are brand-scoped (UUID but unique per schema)