# Project Structure Guidelines - Multi-Tenant Brand Management System

## Multi-Tenant Architecture with Dual Contexts

This project implements a sophisticated multi-tenant architecture using PostgreSQL schema isolation with dual authentication contexts:

### Core Architectural Concepts

#### 1. Brand (Multi-Tenant Entity)
- Represents a business entity (e.g., "Geprek Bensu" - an F&B brand with multiple outlets)
- Each brand is completely isolated from others
- Has its own PostgreSQL schema, users, and data

#### 2. Schema Isolation Strategy
- **Pattern**: `brand_[guid]` (e.g., `brand_4b523a33_dade_4f9c_9be6_66ff2221ab0c`)
- **Tables per schema**:
  - `users`: Brand-specific users with API credentials
  - `user_roles`: Role assignments for brand users
  - `infoes`: Key-value configuration store (includes display name)
- **Security**: Each schema has its own database user with restricted permissions

#### 3. Dual Authentication Contexts
**Principal Context** (System Administration):
- Email/password authentication
- System-wide operations (create brands, manage system users)
- Cannot access brand-specific data directly
- JWT token with `token_type: null`

**Brand Context** (Business Operations - 90% of all operations):
- API key/password authentication
- Brand-specific operations
- Scoped to single brand, cannot access other brands
- JWT token with `token_type: BRAND`, includes `brand_id` and `brand_schema` claims

### Terminology
- **Brand** = Multi-tenant entity (never "Business" or "Tenant")
- **Principal** = System-level user account
- **BrandUser** = Brand-specific user with API credentials
- **BrandOwner** = Role for brand administrators
- **BrandOperator** = Role for brand operators
- **brand_** = Schema prefix for brand-specific schemas

## Blazor Development Standards

### Mock Mode Support
**MANDATORY**: All Blazor pages which has form input or success / failed state MUST include mock mode support:

1. **Query Parameter Detection** in `OnInitialized()`:
```csharp
protected override void OnInitialized()
{
    var uri = new Uri(Navigation.Uri);
    var query = HttpUtility.ParseQueryString(uri.Query);
    if (query["mock"] != null)
    {
        isMockMode = true;
        // Set up mock data/state
    }
}
```

2. **Hidden Field Persistence** for forms:
```razor
@if (isMockMode)
{
    <input type="hidden" name="mock" value="true" />
}
```

3. **Mock Behavior** in handlers:
```csharp
if (isMockMode)
{
    await Task.Delay(1000);  // Simulate processing
    // Set success state with mock data
    return;
}
```

### GraphQL Query Restrictions
**NEVER** make direct GraphQL queries from Blazor pages. Instead:
- Use existing services (UserService, AuthService, BrandService, etc.)
- Create new service methods if needed
- Services handle GraphQL communication
- Blazor pages only call service methods

Example:
```csharp
// ❌ WRONG - Direct GraphQL in Blazor page
var graphqlQuery = new { query = "mutation ..." };
await Http.PostAsJsonAsync("/graphql", graphqlQuery);

// ✅ CORRECT - Use service layer
await authService.VerifyEmailAsync(token);
await userService.RequestPasswordResetAsync(email);
```

## Environment Configuration

**IMPORTANT**: All environment variable access MUST go through the centralized `EnvironmentConfig` class.

### Rules
1. **Never use** `Environment.GetEnvironmentVariable()` directly in code
2. **Always use** `EnvironmentConfig` static properties:
   - `EnvironmentConfig.App.Port` for SERVER_LISTEN_PORT
   - `EnvironmentConfig.Database.Host` for POSTGRES_HOST
   - `EnvironmentConfig.Mail.Provider` for MAIL_PROVIDER
   - `EnvironmentConfig.Jwt.Secret` for AUTH_JWT_SECRET
   - etc.

### Adding New Environment Variables
When adding new environment variables:
1. Add the property to the appropriate nested class in `EnvironmentConfig.cs`
2. Use the centralized property throughout the codebase
3. Never access environment variables directly

### Example
```csharp
// ❌ WRONG - Direct access
var port = Environment.GetEnvironmentVariable("SERVER_LISTEN_PORT") ?? "5000";

// ✅ CORRECT - Through EnvironmentConfig
var port = EnvironmentConfig.App.Port;
```

## Database Naming Convention

This project follows PostgreSQL naming conventions for database objects:

### Naming Rules
1. **Database Objects**: Use snake_case for all database objects (tables, columns, indexes)
   - Tables: `brands`, `user_profiles`, `brand_users`
   - Columns: `id`, `created_at`, `owner_id`, `is_active`
   - Indexes: `ix_brands_owner_id`, `pk_user_profiles`

2. **C# Properties**: Use PascalCase for C# properties (standard .NET convention)
   - Properties: `Id`, `CreatedAt`, `OwnerId`, `IsActive`
   
3. **Automatic Mapping**: EF Core is configured with `UseSnakeCaseNamingConvention()`
   - Automatically converts PascalCase properties to snake_case columns
   - No need for explicit column name mappings in entity configurations
   - Example: C# property `OwnerId` → Database column `owner_id`

### Benefits
- **PostgreSQL Best Practices**: Follows database conventions
- **No Quotes Required**: Snake_case identifiers don't need quotes in SQL
- **Clean C# Code**: Properties follow .NET naming standards
- **Automatic Conversion**: No manual mapping configuration needed
- **Consistency**: All database objects follow the same pattern

## Folder Structure Convention

This project follows a clear, organized structure for GraphQL components:

```
/src/Kanriya.Server
├── /Queries         # All GraphQL query declarations
├── /Mutations       # All GraphQL mutation declarations  
├── /Subscriptions   # All GraphQL subscription declarations
├── /Types           # Input types, output types, and custom scalar types
├── /Data            # Database context, entities, configurations
│   └── /BrandSchema # Brand-specific entities (BrandUser, BrandUserRole)
├── /Services        # Business logic and service layer
│   ├── /Data        # Data-related services (UserService, BrandService, etc.)
│   └── /System      # System services (LogService, MailerService, etc.)
└── Program.cs       # Application entry point
```

## Service Organization

### `/Services/Data`
Data-related services that interact with the database:
- `UserService` - User management and authentication
- `BrandService` - Brand creation and management
- `BrandConnectionService` - Brand database connection management
- `PostgreSQLManagementService` - PostgreSQL schema and user management
- `ApiCredentialService` - API credential generation and validation
- `DatabaseSeeder` - Database initialization and seeding

### `/Services/System`
System services that handle infrastructure concerns:
- `LogService` - Centralized logging
- `MailerService` - Email sending functionality
- `MailProcessor` - Email queue processing
- `SystemSmtpMailDriver` - SMTP implementation
- `HangfireLogProvider` - Hangfire logging integration

## Strict Folder Rules

### `/Queries`
- **Purpose**: Contains ONLY GraphQL query declarations
- **Pattern**: All classes extending RootQuery
- **Naming**: `*Queries.cs` (e.g., `UserQueries.cs`, `BrandQueries.cs`)

### `/Mutations`
- **Purpose**: Contains ONLY GraphQL mutation declarations
- **Pattern**: All classes extending RootMutation
- **Naming**: `*Mutations.cs` (e.g., `UserMutations.cs`, `BrandMutations.cs`)

### `/Subscriptions`
- **Purpose**: Contains ONLY GraphQL subscription declarations
- **Pattern**: All classes marked with subscription attributes
- **Naming**: `*Subscriptions.cs` (e.g., `UserSubscriptions.cs`)

### `/Types`
- **Purpose**: Contains input types, output types, and custom scalar types
- **Subfolders**:
  - `/Types/Inputs` - Input object types for mutations
  - `/Types/Outputs` - Custom output object types
- **Naming**:
  - Input types: `*Input.cs` (e.g., `CreateBrandInput.cs`)
  - Output types: `*Output.cs` (e.g., `CreateBrandOutput.cs`)

### `/Data`
- **Purpose**: Data access layer, EF Core context, entities
- **Contents**: 
  - `AppDbContext` - Main application database context
  - `Brand.cs` - Brand entity
  - `/BrandSchema/` - Brand-specific entities

## Important Rules

1. **NO mixing**: Never put queries in Mutations folder or vice versa
2. **Clear separation**: Each folder has a single responsibility
3. **Consistent naming**: Follow the naming conventions strictly
4. **Use dependency injection**: Inject services into queries/mutations
5. **Keep it thin**: Queries/Mutations should be thin - business logic goes in Services
6. **Service organization**: Data services in `/Services/Data`, system services in `/Services/System`

## Brand Module Organization

```
/Modules
  - BrandModule.cs         # Contains BrandQueries and BrandMutations

/Types/Inputs
  - CreateBrandInput.cs    # Name only (simplified)
  - UpdateBrandInfoInput.cs # Key-value for infoes table

/Types/Outputs  
  - CreateBrandOutput.cs   # Includes apiSecret and apiPassword
  - DeleteBrandOutput.cs   # Success/message response
  - UpdateBrandInfoOutput.cs # Update confirmation
  - BrandInfo.cs            # Key-value from infoes table
```

## Authentication Flow Architecture

### Token Generation and Validation

```csharp
// Principal Token Claims
{
  "nameid": "user-guid",
  "email": "user@example.com",
  "roles": ["BrandOwner"],
  "token_type": null  // Missing or null indicates principal
}

// Brand Token Claims  
{
  "nameid": "brand-user-guid",
  "brand_id": "brand-guid",
  "brand_schema": "brand_xxx",
  "token_type": "BRAND",
  "roles": ["BrandOwner"]
}
```

### CurrentUser Population

```csharp
// AuthenticationMiddleware.cs
if (tokenTypeClaim?.Value == "BRAND")
{
    currentUser.BrandUser = brandUser;
    currentUser.BrandId = brandIdClaim.Value;
    currentUser.BrandSchema = brandSchemaClaim.Value;
}
else
{
    currentUser.User = await userService.GetByIdAsync(userIdClaim.Value);
}
```

### Authorization in GraphQL

```csharp
// Brand Context Required
if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId))
{
    throw new GraphQLException("Brand-context authentication required");
}

// Principal Context Required  
if (currentUser?.User == null)
{
    throw new GraphQLException("Principal authentication required");
}
```

## Testing and Validation

When running lint or build commands, ensure:
- All GraphQL operations are in their correct folders
- No business logic in Query/Mutation classes
- All input/output types are in `/Types`
- Proper use of HotChocolate attributes
- Services are properly organized in Data or System subfolders

## Commands to Remember

```bash
# Build and check for errors
dotnet build

# Run the GraphQL server
dotnet run

# Export schema
dotnet run -- schema export --output schema.graphql

# Create migrations
dotnet ef migrations add MigrationName --context AppDbContext

# Apply migrations
dotnet ef database update --context AppDbContext
```

## Development Philosophy

### Live on the Edge
- **NO backward compatibility during development** - We refactor aggressively
- **Clean code over legacy support** - Remove old code when implementing new patterns
- **Single source of truth** - One way to do things, not multiple
- **Continuous improvement** - Each version can break previous versions
- **Resource efficiency** - Optimize for current needs, not past decisions

### Examples
- When consolidating subscriptions: Remove individual ones, keep only unified
- When changing patterns: Update everything to new pattern, no old code
- When improving architecture: Full refactor, no compatibility layers
- When renaming concepts: Update all references, no aliases

## Subscription Standards

### Unified Event Structure
All GraphQL subscriptions MUST follow this standardized structure:

```graphql
type EntityEvent {
  event: EventType!      # CREATED, UPDATED, DELETED
  document: Entity       # Current state (null for deletes)
  time: DateTime!        # When the event occurred
  _previous: Entity      # Previous state (for updates/deletes)
}
```

### Naming Conventions
- **Subscription name**: `on{Entity}Changed` (e.g., `onBrandChanged`)
- **Topic name**: `{Entity}Changes` (e.g., `BrandChanges`)
- **Event types**: `Created`, `Updated`, `Deleted` (not Added/Modified/Removed)
- **Field names**: `event`, `document`, `time`, `_previous`

### Implementation Rules
1. **Single subscription per entity** - No separate add/update/delete subscriptions
2. **Full object in _previous** - Not just changed fields
3. **Consistent field names** - Never use custom names like eventType, timestamp, etc.
4. **Use generic base class** - Inherit from `SubscriptionEvent<T>` when possible

## Critical Implementation Patterns

### Brand Schema Creation
When creating a new brand:
1. Create PostgreSQL schema `brand_[guid]`
2. Create database user `user_[guid]` with schema permissions
3. Create tables: `users`, `user_roles`, `infoes`
4. Insert initial brand owner in `users` table
5. Insert BrandOwner role in `user_roles`
6. Insert "Brand Name" in `infoes` table
7. Log all created tables and data for verification

### Brand Context Operations
- **MUST** check `currentUser.BrandUser != null`
- **MUST** verify `currentUser.BrandId` matches requested resource
- **MUST** use brand-specific database connection
- **NEVER** allow cross-brand data access

### Immutability Rules
- Brand names in registry are **IMMUTABLE**
- Display names can only change in `brand_xxx.infoes` table
- API credentials are shown **ONLY ONCE** at creation
- Schema names never change after creation

## Notes for AI Assistant

When working on this project:
1. **Module Pattern**: Use `/Modules` folder for combined Query/Mutation classes
2. **Authorization First**: Always check context before operations
3. **Brand Context is Primary**: 90% of operations are brand-scoped
4. **Immutable Registry**: Never allow brand name updates in registry
5. **Schema Isolation**: Each brand is completely isolated
6. **Token Claims**: Use claims to determine authentication context
7. **Service Layer**: All business logic in Services, not GraphQL resolvers
8. **NEVER maintain backward compatibility during development**
9. **ALWAYS remove old code when implementing new patterns**
10. **Optimize for clean, efficient code over compatibility**
11. **Use Brand terminology consistently**: Not Business, not Tenant
12. **Test Both Contexts**: Always test principal AND brand authentication paths