# Learn C# - GraphQL Server with HotChocolate

A modern GraphQL API built with .NET 9, HotChocolate, and PostgreSQL. Features a clean architecture with service layer pattern, Entity Framework Core with snake_case naming convention, and real-time subscriptions.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) and Docker Compose
- Git

## Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd learn-csharp
```

### 2. Set Up Environment Variables

Copy the example environment file and configure if needed:

```bash
cp .env.example .env
```

Default configuration (usually no changes needed):
- PostgreSQL runs on port `10005`
- GraphQL server runs on port `10000`
- pgAdmin runs on port `10080`

### 3. Start Database Services

```bash
docker-compose up -d
```

This starts:
- PostgreSQL database (port 10005)
- pgAdmin for database management (port 10080)

### 4. Install Dependencies & Tools

```bash
# Navigate to the project
cd src/GQLServer

# Restore NuGet packages
dotnet restore

# Install Entity Framework CLI tools (one-time global install)
dotnet tool install --global dotnet-ef
```

### 5. Set Up Database

```bash
# Go back to project root
cd ../..

# Apply database migrations
./bin/db-migrate update

# Or if the script doesn't work, use:
cd src/GQLServer
dotnet ef database update
```

### 6. Run the Server

```bash
# From project root
./bin/run-server

# Or directly with dotnet:
cd src/GQLServer
dotnet run
```

The GraphQL server will be available at:
- GraphQL Endpoint: http://localhost:10000/graphql
- GraphQL UI (Banana Cake Pop): http://localhost:10000/graphql

## Project Structure

```
learn-csharp/
├── src/
│   └── GQLServer/              # Main GraphQL server project
│       ├── Data/                # Database context & entities
│       ├── Queries/             # GraphQL queries
│       ├── Mutations/           # GraphQL mutations
│       ├── Subscriptions/       # GraphQL subscriptions
│       ├── Services/            # Business logic layer
│       └── Types/               # GraphQL types
├── bin/                         # Helper scripts
│   ├── run-server              # Server runner with DB checks
│   └── db-migrate              # Database migration helper
├── docker-compose.yml           # Docker services configuration
└── .env                        # Environment variables
```

## Available Scripts

### Server Management

```bash
# Run server with automatic database check
./bin/run-server

# Run server in watch mode (auto-restart on changes)
./bin/run-server --watch

# Skip database check
./bin/run-server --skip-db
```

### Database Management

```bash
# Create a new migration
./bin/db-migrate add MigrationName

# Apply pending migrations
./bin/db-migrate update

# List all migrations
./bin/db-migrate list

# Remove last migration
./bin/db-migrate remove

# Reset database (drop and recreate)
./bin/db-migrate reset
```

## Development Workflow

### Making Code Changes

1. Make your changes in the `src/GQLServer` directory
2. The server will auto-restart if running with `--watch` flag
3. Test your changes at http://localhost:10000/graphql

### Adding New Features

1. Create entity in `Data/` folder
2. Add entity configuration in `Data/EntityConfigurations/`
3. Create service interface and implementation in `Services/`
4. Add GraphQL operations in `Queries/`, `Mutations/`, or `Subscriptions/`
5. Create a migration: `./bin/db-migrate add FeatureName`
6. Apply migration: `./bin/db-migrate update`

### Database Conventions

This project uses PostgreSQL snake_case naming convention:
- C# Properties: `Id`, `CreatedAt`, `UserName` (PascalCase)
- Database Columns: `id`, `created_at`, `user_name` (snake_case)
- Automatic mapping via EF Core's `UseSnakeCaseNamingConvention()`

## Accessing Services

### GraphQL Playground
- URL: http://localhost:10000/graphql
- Interactive GraphQL IDE with schema documentation

### pgAdmin (Database Management)
- URL: http://localhost:10080
- Email: `admin@admin.com`
- Password: `admin`
- Add server with:
  - Host: `db` (when pgAdmin is in Docker)
  - Port: `5432`
  - Username: `user`
  - Password: `password`

## Common Issues

### "dotnet ef command not found"

```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Add to PATH (if needed)
export PATH="$PATH:$HOME/.dotnet/tools"
```

### "Cannot connect to database"

```bash
# Check if Docker containers are running
docker-compose ps

# Restart containers if needed
docker-compose restart

# Check logs
docker-compose logs db
```

### "Port already in use"

```bash
# Change ports in .env file
POSTGRES_PORT=15432  # Different port
```

## Testing

### Sample GraphQL Queries

```graphql
# Check server version
query {
  version {
    version
    codename
    buildDate
    fullVersion
  }
}

# Health check
query {
  health {
    status
    timestamp
    version
  }
}

# Add a greeting
mutation {
  addGreetLog(input: { content: "Hello World!" }) {
    id
    timestamp
    content
  }
}

# Get all greetings
query {
  greetLogs {
    id
    timestamp
    content
  }
}

# Subscribe to ALL greet log changes (unified subscription)
subscription {
  onGreetLogChanged {
    event        # CREATED, UPDATED, or DELETED
    time         # When the event occurred
    document {   # Current state (null for deletes)
      id
      timestamp
      content
    }
    _previous {  # Previous state (for updates/deletes)
      id
      timestamp
      content
    }
  }
}
```

## Technology Stack

- **.NET 9**: Modern C# framework
- **HotChocolate**: GraphQL server implementation
- **Entity Framework Core**: ORM with code-first migrations
- **PostgreSQL**: Primary database
- **Docker**: Container orchestration
- **pgAdmin**: Database management UI

## License

This project is for learning purposes.