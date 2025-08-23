1. Executive Summary

  A multi-tenant GraphQL-based platform that enables Principal Users to create and manage multiple isolated businesses, each with its
  own schema, data isolation, and API authentication system. The platform provides complete data isolation between businesses while
  maintaining centralized management capabilities.

  2. Product Overview

  2.1 Vision

  Create a secure, scalable multi-tenant platform where business owners can manage multiple businesses with complete data isolation and
   delegated access control.

  2.2 Core Concepts

  - Principal User: Business owner with full platform access
  - Business: An isolated tenant with its own schema and data
  - Business User: Limited user with access to single business
  - Schema Isolation: Each business operates in its own database schema
  - API Key Authentication: Business-specific authentication mechanism

  3. User Personas

  3.1 Principal User

  - Role: Business owner/entrepreneur
  - Capabilities:
    - Create multiple businesses
    - Full access to all owned businesses
    - Manage API credentials for each business
    - View cross-business analytics
    - Manage business users across all businesses

  3.2 Business User

  - Role: Employee/Manager of specific business
  - Capabilities:
    - Access only assigned business
    - Operate within business schema
    - Cannot see other businesses
    - Limited by business-specific permissions

  4. Functional Requirements

  4.1 Authentication & Authorization

  4.1.1 Principal User Authentication (Two-Step Process)

  # Step 1: Principal login (gets management token without business context)
  type Mutation {
    # Principal user login with username/password
    principalLogin(input: PrincipalLoginInput!): PrincipalAuthPayload!

    # Step 2: Select business to manage (upgrades token with business context)
    selectBusiness(businessId: ID!): BusinessContextToken! @authorize(policy: "PrincipalOnly")

    # Principal user registration (if self-service enabled)
    principalRegister(input: PrincipalRegisterInput!): PrincipalAuthPayload!
  }

  type PrincipalLoginInput {
    email: String!
    password: String!
    twoFactorCode: String
  }

  type PrincipalAuthPayload {
    token: String!              # Management token (no business context)
    refreshToken: String!
    user: PrincipalUser!
    businesses: [BusinessSummary!]!  # List of owned businesses
    expiresAt: DateTime!
  }

  type BusinessSummary {
    id: ID!
    name: String!
    displayName: String!
    isActive: Boolean!
    lastAccessed: DateTime
  }

  type BusinessContextToken {
    token: String!              # New token with business context embedded
    business: Business!         # Selected business details
    schemaName: String!         # Database schema to use
    permissions: [String!]!     # Principal has all permissions
    expiresAt: DateTime!
  }

  4.1.1.1 Token Claims Structure

  # Management Token (Principal without business context)
  {
    "sub": "principal_user_id",
    "email": "john@example.com",
    "name": "John Doe",
    "role": "Principal",
    "type": "management",
    "iat": 1234567890,
    "exp": 1234571490,
    "jti": "unique_token_id"
  }

  # Business Context Token (Principal with selected business)
  {
    "sub": "principal_user_id",
    "email": "john@example.com",
    "name": "John Doe",
    "role": "Principal",
    "type": "business_context",
    "business_id": "business_uuid",
    "business_name": "acme-corp",
    "schema_name": "business_uuid",
    "permissions": ["all"],
    "iat": 1234567890,
    "exp": 1234571490,
    "jti": "unique_token_id"
  }

  4.1.2 Business User Authentication

  type Mutation {
    # Business user login with API key/secret
    businessLogin(input: BusinessLoginInput!): BusinessAuthPayload!
  }

  type BusinessLoginInput {
    apiKey: String!
    apiSecret: String!
  }

  type BusinessAuthPayload {
    token: String!
    business: Business!
    permissions: [String!]!
    expiresAt: DateTime!
  }

  4.2 Business Management

  4.2.1 Business Creation

  type Mutation {
    # Create new business (Principal only)
    createBusiness(input: CreateBusinessInput!): BusinessCreationResult! @authorize(policy: "PrincipalOnly")
  }

  type CreateBusinessInput {
    name: String!
    displayName: String!
    industry: String
    timezone: String!
    settings: BusinessSettingsInput
  }

  type BusinessCreationResult {
    business: Business!
    apiKey: String!      # Generated API key
    apiSecret: String!   # Generated API secret (shown once)
    schemaName: String!  # Database schema created
  }

  4.2.2 Business Operations

  type Query {
    # List all businesses owned by principal
    myBusinesses: [Business!]! @authorize(policy: "PrincipalOnly")

    # Get specific business details
    getBusiness(id: ID!): Business @authorize(policy: "PrincipalOrBusinessUser")

    # Get current business context (for Business Users)
    currentBusiness: Business @authorize(policy: "BusinessUser")
  }

  type Mutation {
    # Update business settings
    updateBusiness(id: ID!, input: UpdateBusinessInput!): Business! @authorize(policy: "PrincipalOnly")

    # Deactivate business (soft delete)
    deactivateBusiness(id: ID!): Business! @authorize(policy: "PrincipalOnly")

    # Generate new API credentials
    regenerateApiCredentials(businessId: ID!): ApiCredentials! @authorize(policy: "PrincipalOnly")
  }

  4.3 Schema Management

  4.3.1 System Schema (Shared) - PostgreSQL with UUID

  -- Enable UUID extension
  CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

  -- System tables (public schema)
  CREATE TABLE principal_users (
      id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
      email VARCHAR(255) UNIQUE NOT NULL,
      password_hash VARCHAR(255) NOT NULL,
      full_name VARCHAR(255),
      phone VARCHAR(50),
      avatar_url TEXT,
      created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
      updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
      last_login_at TIMESTAMPTZ,
      is_active BOOLEAN DEFAULT true,
      is_verified BOOLEAN DEFAULT false,
      two_factor_enabled BOOLEAN DEFAULT false,
      two_factor_secret VARCHAR(255),
      metadata JSONB DEFAULT '{}'::jsonb
  );

  CREATE TABLE businesses (
      id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
      principal_user_id UUID NOT NULL REFERENCES principal_users(id) ON DELETE CASCADE,
      name VARCHAR(100) UNIQUE NOT NULL,  -- URL-safe identifier (slug)
      display_name VARCHAR(255) NOT NULL,
      schema_name VARCHAR(100) UNIQUE NOT NULL,  -- Format: business_{uuid}
      api_key VARCHAR(255) UNIQUE NOT NULL,  -- Format: bk_{random}
      api_key_hash VARCHAR(255) NOT NULL,    -- BCrypt hash
      api_secret_hash VARCHAR(255) NOT NULL, -- BCrypt hash
      industry VARCHAR(100),
      timezone VARCHAR(50) DEFAULT 'UTC',
      logo_url TEXT,
      website VARCHAR(255),
      created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
      updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
      is_active BOOLEAN DEFAULT true,
      is_suspended BOOLEAN DEFAULT false,
      suspension_reason TEXT,
      settings JSONB DEFAULT '{}'::jsonb,
      limits JSONB DEFAULT '{"max_users": 10, "max_storage_gb": 10}'::jsonb
  );

  CREATE TABLE business_users (
      id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
      business_id UUID NOT NULL REFERENCES businesses(id) ON DELETE CASCADE,
      email VARCHAR(255) NOT NULL,
      password_hash VARCHAR(255),  -- Optional, can use SSO
      full_name VARCHAR(255),
      avatar_url TEXT,
      role VARCHAR(50) DEFAULT 'member',  -- admin, manager, member
      permissions JSONB DEFAULT '[]'::jsonb,
      created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
      updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
      last_login_at TIMESTAMPTZ,
      is_active BOOLEAN DEFAULT true,
      invited_by UUID REFERENCES business_users(id),
      invitation_token VARCHAR(255),
      invitation_expires_at TIMESTAMPTZ,
      UNIQUE(business_id, email)
  );

  CREATE TABLE audit_logs (
      id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
      principal_user_id UUID REFERENCES principal_users(id),
      business_user_id UUID REFERENCES business_users(id),
      business_id UUID REFERENCES businesses(id),
      action VARCHAR(100) NOT NULL,  -- CREATE, UPDATE, DELETE, LOGIN, LOGOUT
      entity_type VARCHAR(100),      -- User, Business, Product, etc.
      entity_id UUID,
      old_values JSONB,
      new_values JSONB,
      ip_address INET,
      user_agent TEXT,
      request_id VARCHAR(100),
      created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
  );

  CREATE TABLE refresh_tokens (
      id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
      token_hash VARCHAR(255) UNIQUE NOT NULL,
      principal_user_id UUID REFERENCES principal_users(id) ON DELETE CASCADE,
      business_user_id UUID REFERENCES business_users(id) ON DELETE CASCADE,
      expires_at TIMESTAMPTZ NOT NULL,
      created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
      revoked_at TIMESTAMPTZ,
      replaced_by UUID REFERENCES refresh_tokens(id),
      ip_address INET,
      user_agent TEXT
  );

  -- Indexes for performance
  CREATE INDEX idx_businesses_principal_user ON businesses(principal_user_id);
  CREATE INDEX idx_business_users_business ON business_users(business_id);
  CREATE INDEX idx_audit_logs_business ON audit_logs(business_id);
  CREATE INDEX idx_audit_logs_created ON audit_logs(created_at DESC);
  CREATE INDEX idx_refresh_tokens_expires ON refresh_tokens(expires_at) WHERE revoked_at IS NULL;

  4.3.2 Business Schema (Isolated per Business)

  -- Each business gets its own schema (e.g., business_uuid)
  CREATE SCHEMA business_${businessId};

  -- Example business-specific tables
  CREATE TABLE business_${businessId}.customers (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      name VARCHAR(255) NOT NULL,
      email VARCHAR(255),
      phone VARCHAR(50),
      created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
  );

  CREATE TABLE business_${businessId}.products (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      name VARCHAR(255) NOT NULL,
      description TEXT,
      price DECIMAL(10, 2),
      stock_quantity INTEGER DEFAULT 0,
      created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
  );

  CREATE TABLE business_${businessId}.orders (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      customer_id UUID REFERENCES business_${businessId}.customers(id),
      order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      total_amount DECIMAL(10, 2),
      status VARCHAR(50) DEFAULT 'pending'
  );

  4.4 Access Control & Security

  4.4.1 Permission Policies

  // Authorization policies
  public class AuthorizationPolicies
  {
      // Principal users only
      public const string PrincipalOnly = "PrincipalOnly";

      // Business users only (within their business)
      public const string BusinessUserOnly = "BusinessUserOnly";

      // Principal OR Business user (for shared operations)
      public const string PrincipalOrBusinessUser = "PrincipalOrBusinessUser";

      // Specific business role requirements
      public const string BusinessAdmin = "BusinessAdmin";
      public const string BusinessManager = "BusinessManager";
      public const string BusinessMember = "BusinessMember";
  }

  4.4.2 Context Isolation

  // Business context resolver
  public interface IBusinessContext
  {
      Guid? CurrentBusinessId { get; }
      string? CurrentSchemaName { get; }
      bool IsPrincipalUser { get; }
      bool IsBusinessUser { get; }
      Task<bool> HasAccessToBusinessAsync(Guid businessId);
  }

  // Database connection with schema switching
  public interface IMultiTenantDbConnection
  {
      Task<IDbConnection> GetConnectionAsync(string schemaName);
      Task<T> ExecuteInSchemaAsync<T>(string schemaName, Func<IDbConnection, Task<T>> action);
  }

  5. Non-Functional Requirements

  5.1 Security

  - Data Isolation: Complete schema-level isolation between businesses
  - Authentication: JWT tokens with 1-hour expiration, refresh tokens with 30-day expiration
  - API Keys: Hashed using BCrypt, never stored in plain text
  - Audit Logging: All critical operations logged with user, IP, and timestamp
  - Rate Limiting: 1000 requests/minute per business, 10000 requests/minute per principal

  5.2 Performance

  - Response Time: <200ms for 95% of queries
  - Concurrent Users: Support 1000+ concurrent business users
  - Schema Creation: <5 seconds for new business setup
  - Database Connections: Connection pooling per schema

  5.3 Scalability

  - Horizontal Scaling: Stateless application servers
  - Database Sharding: Ability to distribute schemas across multiple databases
  - Caching: Redis for session management and frequently accessed data
  - Background Jobs: Queue system for schema creation and maintenance

  5.4 Compliance

  - Data Residency: Support for region-specific data storage
  - GDPR: Data export and deletion capabilities
  - Audit Trail: Complete audit log for compliance reporting
  - Encryption: TLS 1.3 for transit, AES-256 for sensitive data at rest

  6. Technical Architecture

  6.1 Technology Stack

  Backend (Monolithic Architecture):
    Language: C# (.NET 8)
    Framework: ASP.NET Core
    GraphQL: HotChocolate v14+ (Latest stable)
    Database: PostgreSQL 15+
    ORM Options (Recommended):
      Primary: Entity Framework Core 8 with Npgsql
        - Pros: Full featured, great migrations, LINQ support, HotChocolate integration
        - Cons: Can be slower for bulk operations
        - Packages: Npgsql.EntityFrameworkCore.PostgreSQL
      Secondary: Dapper (for performance-critical queries)
        - Pros: Fast, lightweight, full SQL control
        - Cons: Manual mapping, no migrations
        - Packages: Dapper, Dapper.Contrib
      Alternative: Marten (Document DB on PostgreSQL)
        - Pros: Document storage with JSONB, event sourcing
        - Cons: Different paradigm, learning curve
        - Packages: Marten
    ID Strategy: UUID (Guid) for all tables
    Architecture: Monolithic with Service pattern (no microservices)

  Core Services (Singleton Pattern):
    - ILoggerService: Structured logging with Serilog
    - IMailerService: Email sending (SMTP/SendGrid/AWS SES)
    - IAuthService: Authentication & token management
    - ICacheService: In-memory and distributed caching
    - ISchemaService: Database schema management
    - IAuditService: Audit trail and activity logging
    - IStorageService: File storage (local/S3/Azure Blob)
    - IQueueService: Background job processing
    - IMetricsService: Performance and business metrics

  Authentication:
    Principal: JWT with refresh tokens
    Business: API Key/Secret pairs (BCrypt hashed)
    Token Storage: In-memory cache with Redis backup
    Session Management: Stateless JWT-based

  Client Access:
    API Type: GraphQL only (no REST)
    Access Method: Direct HTTP/WebSocket to GraphQL endpoint
    Client Support: Client-agnostic (any GraphQL client)
    Subscriptions: WebSocket for real-time updates

  Development Tools:
    Package Manager: NuGet
    Build: dotnet CLI
    Hot Reload: dotnet watch
    Testing: xUnit + Moq
    API Testing: GraphQL Playground / Banana Cake Pop
    Database Admin Options:
      - pgAdmin 4 (via Docker Compose) - Full PostgreSQL admin
      - Adminer (lightweight, multi-DB) - Single PHP file
      - Custom Admin Panel Options for .NET:
        * AdminLTE with Blazor - Custom admin dashboard
        * Oqtane - Modular Blazor CMS/Admin framework
        * ABP Framework - Full admin UI included
        * OrchardCore - CMS with admin panel
        * Custom GraphQL mutations - Build your own with HotChocolate

  6.2 Service Architecture (Monolithic)

  Core Service Interfaces:

  // Logging Service
  public interface ILoggerService
  {
      void LogInfo(string message, object? data = null);
      void LogWarning(string message, object? data = null);
      void LogError(string message, Exception? ex = null, object? data = null);
      void LogAudit(string action, Guid? userId, Guid? businessId, object? data = null);
  }

  // Mailer Service
  public interface IMailerService
  {
      Task SendEmailAsync(string to, string subject, string htmlBody);
      Task SendTemplateEmailAsync(string to, string template, object data);
      Task SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody);
      Task SendVerificationEmailAsync(string email, string token);
      Task SendPasswordResetEmailAsync(string email, string token);
  }

  // Authentication Service
  public interface IAuthService
  {
      Task<AuthResult> AuthenticatePrincipalAsync(string email, string password);
      Task<AuthResult> AuthenticateBusinessAsync(string apiKey, string apiSecret);
      Task<string> GenerateTokenAsync(TokenClaims claims);
      Task<bool> ValidateTokenAsync(string token);
      Task<string> RefreshTokenAsync(string refreshToken);
      Task RevokeTokenAsync(string token);
  }

  // Cache Service
  public interface ICacheService
  {
      Task<T?> GetAsync<T>(string key);
      Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
      Task RemoveAsync(string key);
      Task<bool> ExistsAsync(string key);
      Task FlushAsync(string pattern);
  }

  // Schema Management Service
  public interface ISchemaService
  {
      Task<string> CreateBusinessSchemaAsync(Guid businessId);
      Task MigrateSchemaAsync(string schemaName);
      Task DeleteSchemaAsync(string schemaName);
      Task<bool> SchemaExistsAsync(string schemaName);
      Task<List<string>> GetAllSchemasAsync();
  }

  // Background Job Service
  public interface IQueueService
  {
      Task EnqueueAsync<T>(T job) where T : IBackgroundJob;
      Task<Guid> ScheduleAsync<T>(T job, DateTime runAt) where T : IBackgroundJob;
      Task CancelJobAsync(Guid jobId);
      Task<JobStatus> GetJobStatusAsync(Guid jobId);
  }

  Service Registration (Program.cs):

  // Register all services as Singleton for performance
  builder.Services.AddSingleton<ILoggerService, SerilogLoggerService>();
  builder.Services.AddSingleton<IMailerService, SendGridMailerService>();
  builder.Services.AddSingleton<IAuthService, JwtAuthService>();
  builder.Services.AddSingleton<ICacheService, RedisCacheService>();
  builder.Services.AddSingleton<ISchemaService, PostgresSchemaService>();
  builder.Services.AddSingleton<IAuditService, AuditService>();
  builder.Services.AddSingleton<IStorageService, S3StorageService>();
  builder.Services.AddSingleton<IQueueService, InMemoryQueueService>();
  builder.Services.AddSingleton<IMetricsService, PrometheusMetricsService>();

  6.3 ORM Configuration (Entity Framework Core + PostgreSQL)

  Required NuGet Packages:
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.*" />
    <PackageReference Include="EFCore.NamingConventions" Version="8.0.*" />  <!-- Snake case -->

  DbContext Configuration:

  public class SystemDbContext : DbContext
  {
      public DbSet<PrincipalUser> PrincipalUsers { get; set; }
      public DbSet<Business> Businesses { get; set; }
      public DbSet<BusinessUser> BusinessUsers { get; set; }
      public DbSet<AuditLog> AuditLogs { get; set; }
      public DbSet<RefreshToken> RefreshTokens { get; set; }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
          optionsBuilder
              .UseNpgsql("Host=localhost;Database=multitenant;Username=postgres;Password=password")
              .UseSnakeCaseNamingConvention()  // Convert to snake_case
              .EnableSensitiveDataLogging(isDevelopment)
              .UseLoggerFactory(loggerFactory);
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          // Configure UUID generation
          modelBuilder.Entity<PrincipalUser>()
              .Property(e => e.Id)
              .HasDefaultValueSql("uuid_generate_v4()");

          // Configure JSONB columns
          modelBuilder.Entity<Business>()
              .Property(e => e.Settings)
              .HasColumnType("jsonb");

          // Configure indexes
          modelBuilder.Entity<Business>()
              .HasIndex(e => e.Name)
              .IsUnique();

          // Configure cascade deletes
          modelBuilder.Entity<Business>()
              .HasOne(e => e.PrincipalUser)
              .WithMany(e => e.Businesses)
              .OnDelete(DeleteBehavior.Cascade);
      }
  }

  Entity Models:

  public class PrincipalUser
  {
      public Guid Id { get; set; }
      public string Email { get; set; } = null!;
      public string PasswordHash { get; set; } = null!;
      public string? FullName { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
      public bool IsActive { get; set; } = true;
      
      // Navigation properties
      public ICollection<Business> Businesses { get; set; } = new List<Business>();
  }

  Migration Commands:

  # Add migration
  dotnet ef migrations add InitialCreate --context SystemDbContext

  # Update database
  dotnet ef database update --context SystemDbContext

  # Generate SQL script
  dotnet ef migrations script --context SystemDbContext

  Multi-Schema Support:

  public class BusinessDbContext : DbContext
  {
      private readonly string _schemaName;
      
      public BusinessDbContext(string schemaName)
      {
          _schemaName = schemaName;
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          modelBuilder.HasDefaultSchema(_schemaName);
          // Configure business-specific entities
      }
  }

  Performance Optimization with Dapper:

  // Use Dapper for complex queries or bulk operations
  public class BusinessRepository
  {
      private readonly IDbConnection _connection;
      
      public async Task<IEnumerable<Business>> GetBusinessesWithStatsAsync(Guid principalId)
      {
          const string sql = @"
              SELECT b.*, 
                     COUNT(DISTINCT bu.id) as user_count,
                     MAX(al.created_at) as last_activity
              FROM businesses b
              LEFT JOIN business_users bu ON bu.business_id = b.id
              LEFT JOIN audit_logs al ON al.business_id = b.id
              WHERE b.principal_user_id = @principalId
              GROUP BY b.id";
              
          return await _connection.QueryAsync<Business>(sql, new { principalId });
      }
  }

  6.4 Docker Development Environment

  docker-compose.yml:

  version: '3.8'
  
  services:
    postgres:
      image: postgres:15-alpine
      container_name: multitenant_postgres
      environment:
        POSTGRES_USER: postgres
        POSTGRES_PASSWORD: postgres
        POSTGRES_DB: multitenant
      ports:
        - "5432:5432"
      volumes:
        - postgres_data:/var/lib/postgresql/data
        - ./scripts/init.sql:/docker-entrypoint-initdb.d/init.sql
      networks:
        - multitenant_network
    
    pgadmin:
      image: dpage/pgadmin4:latest
      container_name: multitenant_pgadmin
      environment:
        PGADMIN_DEFAULT_EMAIL: admin@example.com
        PGADMIN_DEFAULT_PASSWORD: admin
        PGADMIN_CONFIG_SERVER_MODE: 'False'
        PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED: 'False'
      ports:
        - "5050:80"
      volumes:
        - pgadmin_data:/var/lib/pgadmin
      depends_on:
        - postgres
      networks:
        - multitenant_network
    
    # Alternative: Lightweight database admin (1.5MB!)
    adminer:
      image: adminer:latest
      container_name: multitenant_adminer
      ports:
        - "8080:8080"
      environment:
        ADMINER_DEFAULT_SERVER: postgres
        ADMINER_DESIGN: pepa-linha  # Theme: pepa-linha, nette, etc.
      depends_on:
        - postgres
      networks:
        - multitenant_network
    
    redis:
      image: redis:7-alpine
      container_name: multitenant_redis
      ports:
        - "6379:6379"
      volumes:
        - redis_data:/data
      networks:
        - multitenant_network
    
    # Optional: Add the .NET app to docker-compose for full stack
    # app:
    #   build:
    #     context: .
    #     dockerfile: Dockerfile
    #   container_name: multitenant_app
    #   environment:
    #     - ASPNETCORE_ENVIRONMENT=Development
    #     - ConnectionStrings__Default=Host=postgres;Database=multitenant;Username=postgres;Password=postgres
    #     - Redis__ConnectionString=redis:6379
    #   ports:
    #     - "5000:5000"
    #   depends_on:
    #     - postgres
    #     - redis
    #   networks:
    #     - multitenant_network
  
  volumes:
    postgres_data:
    pgadmin_data:
    redis_data:
  
  networks:
    multitenant_network:
      driver: bridge

  Development URLs:
    - PostgreSQL: localhost:5432
    - pgAdmin: http://localhost:5050
    - Adminer: http://localhost:8080
    - Redis: localhost:6379
    - GraphQL API: http://localhost:5000/graphql
    - GraphQL Playground: http://localhost:5000/graphql (Banana Cake Pop)

  pgAdmin Connection Settings:
    - Host: postgres (or host.docker.internal from pgAdmin)
    - Port: 5432
    - Username: postgres
    - Password: postgres
    - Database: multitenant
  
  Adminer Connection Settings:
    - System: PostgreSQL
    - Server: postgres
    - Username: postgres
    - Password: postgres
    - Database: multitenant

  6.5 Database Design

  Database Structure:
    System Database:
      Schema: public
      Tables: principal_users, businesses, business_users, audit_logs

    Business Databases:
      Schema Pattern: business_{uuid}
      Isolation: Complete schema isolation
      Migration: Flyway or EF Core migrations per schema

    Connection Strategy:
      Pool Size: 20 connections per schema
      Idle Timeout: 5 minutes
      Max Schemas per DB: 100 (then shard)

  7. API Examples

  7.1 Principal User Flow

  # Step 1: Principal login (gets management token)
  mutation {
    principalLogin(input: {
      email: "john@example.com"
      password: "secure_password"
    }) {
      token                    # Management token (no business context)
      user {
        id
        email
      }
      businesses {             # List of owned businesses to choose from
        id
        name
        displayName
        isActive
        lastAccessed
      }
    }
  }

  # Step 2: Select a business to manage (upgrade token)
  mutation {
    selectBusiness(businessId: "uuid-of-acme-corp") {
      token                    # New token with business context
      business {
        id
        name
        displayName
      }
      schemaName              # "business_uuid" - schema to use
      permissions             # ["all"] for principal
    }
  }
  # Use this new token for all subsequent requests
  # Headers: { "Authorization": "Bearer {business-context-token}" }

  # Step 3: Now can access business-specific data
  query {
    # This query now knows which business schema to use
    customers {
      id
      name
      email
    }
    
    products {
      id
      name
      price
    }
  }

  # Step 4: Switch to different business (get new token)
  mutation {
    selectBusiness(businessId: "uuid-of-another-business") {
      token                    # New token for different business
      business {
        id
        name
      }
      schemaName
    }
  }

  # Step 5: Create new business (using management or business-context token)
  mutation {
    createBusiness(input: {
      name: "new-venture"
      displayName: "New Venture Inc"
      industry: "Technology"
      timezone: "America/New_York"
    }) {
      business {
        id
        name
        schemaName
      }
      apiKey
      apiSecret
    }
  }

  7.2 Business User Flow

  # 1. Business login with API credentials
  mutation {
    businessLogin(input: {
      apiKey: "biz_k_1234567890"
      apiSecret: "biz_s_abcdefghijklmnop"
    }) {
      token
      business {
        id
        displayName
      }
      permissions
    }
  }

  # 2. Access business-specific data
  query {
    # Automatically scoped to authenticated business schema
    customers {
      id
      name
      email
      totalOrders
    }

    products {
      id
      name
      price
      stockQuantity
    }
  }

  8. Migration & Deployment

  8.1 Schema Creation Process

  1. Validate business name uniqueness
  2. Generate schema name (business_uuid)
  3. Create database schema
  4. Run schema migrations
  5. Generate API credentials
  6. Register in system table
  7. Create audit log entry
  8. Return credentials to principal

  8.2 Rollback Strategy

  - Transaction-wrapped schema creation
  - Automatic cleanup on failure
  - Audit log for all attempts
  - Manual intervention tools for admins

  9. Success Metrics

  - Business Creation Time: <5 seconds
  - API Response Time: p95 <200ms
  - System Uptime: 99.9%
  - Concurrent Businesses: 10,000+
  - Data Breach Incidents: 0
  - Schema Migration Success Rate: 99.9%
  - API Authentication Success Rate: 99.5%

  10. Future Enhancements

  - Phase 2: Business-to-business data sharing
  - Phase 3: White-label customization per business
  - Phase 4: Mobile SDK for business users
  - Phase 5: Advanced analytics and reporting
  - Phase 6: Marketplace for business integrations
  - Phase 7: AI-powered business insights