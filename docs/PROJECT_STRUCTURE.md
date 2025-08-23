# Clean Project Structure ✅

## Current Folder Structure (Cleaned)

```
/learn-csharp
├── CLAUDE.md             # Project conventions for AI assistant
├── PROJECT_STRUCTURE.md  # This file
├── learn-csharp.sln      # Solution file
├── docker-compose.yml    # Docker configuration
├── .vscode/              # VS Code configuration
│   ├── settings.json
│   ├── tasks.json
│   └── launch.json
│
└── /src/GQLServer
    ├── Program.cs                 # Application entry point
    ├── GQLServer.csproj          # Project file
    ├── appsettings.json          # Configuration
    ├── appsettings.Development.json
    │
    ├── /Queries                  # All GraphQL queries
    │   ├── BasicQueries.cs
    │   ├── HelloQueries.cs
    │   ├── GreetingQueries.cs
    │   ├── TimeQueries.cs
    │   └── WeatherQueries.cs
    │
    ├── /Mutations                # All GraphQL mutations
    │   └── ExampleMutations.cs
    │
    ├── /Subscriptions            # All GraphQL subscriptions
    │   └── ExampleSubscriptions.cs
    │
    └── /Types                    # Input and output types
        ├── /Inputs
        │   └── CreateMessageInput.cs
        └── /Outputs
            └── SimpleResponse.cs
```

## Clean and Organized Benefits

✅ **No unused folders** - Only essential directories remain
✅ **Clear separation** - Queries, Mutations, Subscriptions, Types
✅ **Flat structure** - Easy to navigate, no deep nesting
✅ **Convention-based** - Consistent naming patterns

## Folder Purpose

| Folder | Purpose | File Pattern |
|--------|---------|-------------|
| `/Queries` | All GraphQL query operations | `*Queries.cs` |
| `/Mutations` | All GraphQL mutation operations | `*Mutations.cs` |
| `/Subscriptions` | All GraphQL subscription operations | `*Subscriptions.cs` |
| `/Types/Inputs` | Input types for mutations | `*Input.cs` |
| `/Types/Outputs` | Output types for queries/mutations | `*Type.cs` or model names |

## Future Additions (When Needed)

- `/Data` - Database context and repositories (when adding EF Core)
- `/Services` - Business logic layer (when logic gets complex)
- `/Extensions` - Custom HotChocolate extensions
- `/Middleware` - Custom GraphQL middleware

## Test All Operations

```bash
# Start server
dotnet run

# Open browser
http://localhost:5000/graphql
```

### Test Query
```graphql
query {
  hello
  greet(name: "Test")
  currentTime
}
```

### Test Mutation
```graphql
mutation {
  createMessage(content: "Test")
}
```

### Test Subscription
```graphql
subscription {
  onTimeUpdate
}
```

The project structure is now **clean, simple, and ready for learning**!