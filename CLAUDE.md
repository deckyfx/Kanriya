# Project Structure Guidelines - Learn C# GraphQL with HotChocolate

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
   - Tables: `greet_logs`, `user_profiles`, `order_items`
   - Columns: `id`, `created_at`, `user_name`, `is_active`
   - Indexes: `ix_greet_logs_timestamp`, `pk_user_profiles`

2. **C# Properties**: Use PascalCase for C# properties (standard .NET convention)
   - Properties: `Id`, `CreatedAt`, `UserName`, `IsActive`
   
3. **Automatic Mapping**: EF Core is configured with `UseSnakeCaseNamingConvention()`
   - Automatically converts PascalCase properties to snake_case columns
   - No need for explicit column name mappings in entity configurations
   - Example: C# property `UserName` → Database column `user_name`

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
├── /Data            # Database context, repositories, data access layer
├── /Services        # Business logic and service layer
└── Program.cs       # Application entry point
```

## Strict Folder Rules

### `/Queries`
- **Purpose**: Contains ONLY GraphQL query declarations
- **Pattern**: All classes marked with `[QueryType]`
- **Naming**: `*Queries.cs` (e.g., `UserQueries.cs`, `ProductQueries.cs`)
- **Example**:
  ```csharp
  [QueryType]
  public class UserQueries
  {
      public User GetUser(int id) => // ...
  }
  ```

### `/Mutations`
- **Purpose**: Contains ONLY GraphQL mutation declarations
- **Pattern**: All classes marked with `[MutationType]`
- **Naming**: `*Mutations.cs` (e.g., `UserMutations.cs`, `ProductMutations.cs`)
- **Example**:
  ```csharp
  [MutationType]
  public class UserMutations
  {
      public User CreateUser(CreateUserInput input) => // ...
  }
  ```

### `/Subscriptions`
- **Purpose**: Contains ONLY GraphQL subscription declarations
- **Pattern**: All classes marked with `[SubscriptionType]`
- **Naming**: `*Subscriptions.cs` (e.g., `UserSubscriptions.cs`, `NotificationSubscriptions.cs`)
- **Example**:
  ```csharp
  [SubscriptionType]
  public class UserSubscriptions
  {
      [Subscribe]
      public User OnUserCreated([EventMessage] User user) => user;
  }
  ```

### `/Types`
- **Purpose**: Contains input types, output types, and custom scalar types
- **Subfolders**:
  - `/Types/Inputs` - Input object types for mutations
  - `/Types/Outputs` - Custom output object types
  - `/Types/Scalars` - Custom scalar types
- **Naming**:
  - Input types: `*Input.cs` (e.g., `CreateUserInput.cs`)
  - Output types: `*Type.cs` or model name (e.g., `UserType.cs`, `User.cs`)
  - Scalars: `*Scalar.cs` (e.g., `DateTimeScalar.cs`)

### `/Data`
- **Purpose**: Data access layer, EF Core context, repositories
- **Contents**: DbContext, entity configurations, migrations

### `/Services`
- **Purpose**: Business logic layer
- **Contents**: Service classes that queries/mutations depend on

## Important Rules

1. **NO mixing**: Never put queries in Mutations folder or vice versa
2. **Clear separation**: Each folder has a single responsibility
3. **Consistent naming**: Follow the naming conventions strictly
4. **Use dependency injection**: Inject services into queries/mutations
5. **Keep it thin**: Queries/Mutations should be thin - business logic goes in Services

## File Organization Example

```
/Queries
  - UserQueries.cs
  - ProductQueries.cs
  - OrderQueries.cs

/Mutations  
  - UserMutations.cs
  - ProductMutations.cs
  - OrderMutations.cs

/Subscriptions
  - OrderSubscriptions.cs
  - NotificationSubscriptions.cs

/Types
  /Inputs
    - CreateUserInput.cs
    - UpdateProductInput.cs
  /Outputs
    - UserType.cs
    - ProductType.cs
    - OrderType.cs
```

## Testing and Validation

When running lint or build commands, ensure:
- All GraphQL operations are in their correct folders
- No business logic in Query/Mutation classes
- All input/output types are in `/Types`
- Proper use of HotChocolate attributes

## Commands to Remember

```bash
# Build and check for errors
dotnet build

# Run the GraphQL server
dotnet run

# Export schema
dotnet run -- schema export --output schema.graphql
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
- **Subscription name**: `on{Entity}Changed` (e.g., `onGreetLogChanged`)
- **Topic name**: `{Entity}Changes` (e.g., `GreetLogChanges`)
- **Event types**: `Created`, `Updated`, `Deleted` (not Added/Modified/Removed)
- **Field names**: `event`, `document`, `time`, `_previous`

### Implementation Rules
1. **Single subscription per entity** - No separate add/update/delete subscriptions
2. **Full object in _previous** - Not just changed fields
3. **Consistent field names** - Never use custom names like eventType, timestamp, etc.
4. **Use generic base class** - Inherit from `SubscriptionEvent<T>` when possible

## Notes for AI Assistant

When working on this project:
1. ALWAYS place queries in `/Queries` folder
2. ALWAYS place mutations in `/Mutations` folder
3. ALWAYS place subscriptions in `/Subscriptions` folder
4. ALWAYS place types (input/output) in `/Types` folder
5. Use partial classes when needed to split large files
6. Follow the naming conventions strictly
7. Keep business logic in Services, not in GraphQL operation classes
8. **NEVER maintain backward compatibility during development**
9. **ALWAYS remove old code when implementing new patterns**
10. **Optimize for clean, efficient code over compatibility**