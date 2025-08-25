# Kanriya Server - Multi-Tenant GraphQL API with HotChocolate

A production-ready multi-tenant GraphQL API built with .NET 9, HotChocolate, and PostgreSQL. Features schema-based tenant isolation, dual authentication (principal users and brand API credentials), real-time subscriptions, and a clean service-oriented architecture.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) and Docker Compose
- Git

## Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Kanriya
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
cd src/Kanriya.Server

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
cd src/Kanriya.Server
dotnet ef database update
```

### 6. Run the Server

```bash
# From project root
./bin/run-server

# Or directly with dotnet:
cd src/Kanriya.Server
dotnet run
```

The GraphQL server will be available at:
- GraphQL Endpoint: http://localhost:10000/graphql
- GraphQL UI (Banana Cake Pop): http://localhost:10000/graphql

## Project Structure

```
Kanriya/
├── src/
│   └── Kanriya.Server/              # Main GraphQL server project
│       ├── Data/                # Database context & entities
│       │   ├── Brand.cs        # Multi-tenant brand entity
│       │   ├── BrandSchema/    # Brand-specific tenant schemas
│       │   └── EntityConfigurations/
│       ├── Modules/             # GraphQL modules (queries, mutations, subscriptions)
│       │   ├── BrandModule.cs  # Brand management operations
│       │   ├── UserModule.cs   # User authentication & management
│       │   └── SystemModule.cs # System operations
│       ├── Services/            # Business logic layer
│       │   ├── Data/           # Data-related services
│       │   │   ├── UserService.cs
│       │   │   ├── BrandService.cs
│       │   │   └── BrandConnectionService.cs
│       │   └── System/         # System services
│       │       ├── LogService.cs
│       │       └── MailerService.cs
│       ├── Types/               # GraphQL types
│       │   ├── Inputs/         # Input types
│       │   └── Payloads/       # Response payloads
│       └── Program/            # Application configuration
│           ├── EnvironmentConfig.cs  # Centralized env vars
│           └── GraphQLConfig.cs      # GraphQL setup
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

1. Make your changes in the `src/Kanriya.Server` directory
2. The server will auto-restart if running with `--watch` flag
3. Test your changes at http://localhost:10000/graphql

### Adding New Features

1. Create entity in `Data/` folder
2. Add entity configuration in `Data/EntityConfigurations/`
3. Create service interface and implementation in appropriate `Services/` subfolder:
   - `Services/Data/` for data-related services
   - `Services/System/` for system services
4. Add GraphQL operations in `Modules/` folder using module pattern
5. Register services in `Program/GraphQLConfig.cs`
6. Create a migration: `./bin/db-migrate add FeatureName`
7. Apply migration: `./bin/db-migrate update`

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

## Key Features

### Multi-Tenant Architecture
- **Schema Isolation**: Each brand gets its own PostgreSQL schema (`brand_[guid]`)
- **Dual Authentication**: 
  - Principal users: Email/password authentication
  - Brand users: API key authentication for programmatic access
- **Role-Based Access**: BrandOwner and BrandOperator roles
- **Dynamic Connection Management**: Automatic schema switching based on context

### Service Architecture
- **Data Services**: User management, brand management, database operations
- **System Services**: Logging, mailing, background jobs, monitoring
- **Centralized Configuration**: All environment variables through `EnvironmentConfig`
- **Clean Separation**: Business logic in services, GraphQL in modules

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

# User Registration
mutation {
  signUp(input: {
    email: "user@example.com"
    password: "SecurePassword123!"
    name: "John Doe"
  }) {
    success
    user {
      id
      email
      name
    }
  }
}

# User Login
mutation {
  signIn(input: {
    email: "user@example.com"
    password: "SecurePassword123!"
  }) {
    success
    token
    user {
      id
      email
      roles
    }
  }
}

# Create a Brand (requires authentication)
mutation {
  createBrand(input: {
    name: "Geprek Bensu"
    contactEmail: "admin@geprekbensu.com"
  }) {
    success
    brand {
      id
      name
      schemaName
    }
  }
}

# Get My Brands
query {
  getMyBrands {
    id
    name
    isActive
    createdAt
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

- **.NET 9**: Modern C# framework with minimal APIs
- **HotChocolate**: Enterprise GraphQL server with subscriptions
- **Entity Framework Core**: ORM with code-first migrations and snake_case convention
- **PostgreSQL**: Primary database with schema-based multi-tenancy
- **Hangfire**: Background job processing
- **Serilog**: Structured logging with Spectre.Console
- **Docker**: Container orchestration for development
- **pgAdmin**: Database management UI

## License

This project is for learning purposes.