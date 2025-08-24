# Kanriya.Server Dependency Map and Initialization Order

## 🔄 Initialization Order (Program.cs)

```
┌─────────────────────────────────────────┐
│  1. WebApplicationBuilder Created       │
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  2. EnvironmentConfig.LoadEnvironment() │ ← Must be FIRST!
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  3. LogService.Initialize()             │ ← Needs environment vars
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  4. DatabaseConfig.ConfigureDatabase()  │ ← Uses EnvironmentConfig.Database
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  5. JwtConfig.ConfigureJwtAuth()        │ ← Uses EnvironmentConfig.Jwt
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  6. AuthorizationConfig.Configure()     │ ← Depends on JWT
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  7. GraphQLConfig.ConfigureGraphQL()    │ ← Registers UserService
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  8. HangfireConfig.ConfigureServices()  │ ← Must be BEFORE Mail!
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  9. MailServiceConfig.ConfigureMail()   │ ← Needs IBackgroundJobClient
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  10. HttpEndpointsConfig.Configure()    │ ← Controllers need services
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  11. builder.Build() → Application      │
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  12. DatabaseConfig.InitializeAsync()   │ ← Migrations & seed data
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  13. Configure Middleware Pipeline      │
└────────────────┬────────────────────────┘
                 ▼
┌─────────────────────────────────────────┐
│  14. app.Run() → Server Starts          │
└─────────────────────────────────────────┘
```

## 📦 Service Registration Order & Dependencies

### Step 1-3: Core Infrastructure
```
1. EnvironmentConfig.LoadEnvironment()
   ├── Loads .env file
   ├── Sets SERVER_BIND_IP, SERVER_LISTEN_PORT
   └── Must run BEFORE everything else

2. LogService.Initialize()
   ├── Depends on: EnvironmentConfig (for SEQ_LOG_HOST, SEQ_LOG_PORT, SEQ_LOG_SECURE)
   └── Provides: Logging for all subsequent operations

3. Serilog Configuration
   └── Depends on: LogService
```

### Step 4: Database Layer
```
DatabaseConfig.ConfigureDatabase()
├── Registers: AppDbContext
├── Depends on: EnvironmentConfig.Database.GetConnectionString()
└── Used by: MailerService, UserService, all repositories
```

### Step 5-6: Authentication & Authorization
```
JwtConfig.ConfigureJwtAuthentication()
├── Registers: Authentication schemes
├── Depends on: EnvironmentConfig.Jwt
└── Used by: UserService.GenerateJwtToken()

AuthorizationConfig.ConfigureAuthorization()
├── Registers: Authorization policies
└── Depends on: Authentication (must come after JwtConfig)
```

### Step 7: GraphQL
```
GraphQLConfig.ConfigureGraphQL()
├── Registers: 
│   ├── IUserService (UserService)
│   ├── IGreetLogService (GreetLogService)
│   └── GraphQL schema
├── Depends on: Database, Authentication
└── UserService depends on: IMailerService (circular if not careful!)
```

### Step 8: Hangfire (Background Jobs)
```
HangfireConfig.ConfigureHangfireServices()
├── Registers: IBackgroundJobClient (automatically)
├── Depends on: Database connection
└── Used by: MailerService
```

### Step 9: Mail Services
```
MailServiceConfig.ConfigureMailServices()
├── Registers:
│   ├── SystemSmtpMailDriver → IMailDriver
│   ├── MailerService → IMailerService
│   └── MailProcessor → IMailProcessor
└── ⚠️ REMOVED: IBackgroundJobClient registration (was circular!)
```

### Step 10: HTTP Endpoints
```
HttpEndpointsConfig.ConfigureHttpServices()
├── Registers: Controllers, Swagger
└── Controllers depend on: IUserService
```

## 🔗 Service Dependency Tree

### UserService Dependencies
```
UserService
├── IServiceProvider (for scope creation)
├── ILogger<UserService>
├── IConfiguration
└── IMailerService ⚠️ (creates dependency on mail system)
```

### MailerService Dependencies
```
MailerService
├── AppDbContext (database)
├── ILogger<MailerService>
├── IMailDriver (SystemSmtpMailDriver)
└── IBackgroundJobClient (from Hangfire)
```

### SystemSmtpMailDriver Dependencies
```
SystemSmtpMailDriver
├── IConfiguration
├── ILogger<SystemSmtpMailDriver>
└── EnvironmentConfig (static access)
```

## ⚠️ Potential Issues & Solutions

### 1. **Circular Dependency Risk**
**Problem**: UserService → IMailerService → IBackgroundJobClient → Hangfire
**Solution**: Ensure Hangfire is configured BEFORE mail services

### 2. **Static Dependency on EnvironmentConfig**
**Problem**: Multiple services access EnvironmentConfig statically
**Risk**: If environment isn't loaded first, services fail
**Solution**: Always call EnvironmentConfig.LoadEnvironment() first

### 3. **Database Initialization Timing**
**Problem**: Services need database, but migrations run after build
**Risk**: Services might try to access DB before it's ready
**Solution**: Use lazy initialization or check DB connectivity

### 4. **Service Registration Order**
**Critical Order**:
1. Environment → Logging → Database
2. Authentication → Authorization
3. Hangfire → Mail Services
4. GraphQL (depends on UserService which needs MailerService)
5. HTTP/Controllers last

## 🚨 Fixed Issues

### Circular Dependency in MailServiceConfig
```csharp
// ❌ WRONG - This was causing circular dependency
services.AddScoped<IBackgroundJobClient>(provider => 
    provider.GetRequiredService<IBackgroundJobClient>());

// ✅ FIXED - Hangfire registers this automatically
// Removed the manual registration
```

## 📋 Recommended Refactoring

### 1. **Reduce UserService Dependencies**
Instead of injecting IMailerService directly:
```csharp
// Option 1: Use IServiceProvider to resolve on-demand
private IMailerService GetMailerService() => 
    _serviceProvider.GetRequiredService<IMailerService>();

// Option 2: Use a factory pattern
services.AddScoped<Func<IMailerService>>(provider => 
    () => provider.GetRequiredService<IMailerService>());
```

### 2. **Create Service Registration Groups**
```csharp
public static class ServiceRegistration
{
    public static void AddInfrastructure(this IServiceCollection services) 
    {
        // Database, Logging, Configuration
    }
    
    public static void AddAuthentication(this IServiceCollection services) 
    {
        // JWT, Authorization
    }
    
    public static void AddBusinessServices(this IServiceCollection services) 
    {
        // UserService, GreetLogService, etc.
    }
    
    public static void AddBackgroundServices(this IServiceCollection services) 
    {
        // Hangfire, Mail Services
    }
}
```

### 3. **Use Options Pattern for Configuration**
Instead of static EnvironmentConfig:
```csharp
services.Configure<JwtOptions>(options =>
{
    options.Secret = Environment.GetEnvironmentVariable("AUTH_JWT_SECRET");
    // etc.
});
```

## 🔍 Debugging Startup Issues

When encountering startup hangs or dependency issues:

1. **Check initialization order** - Ensure dependencies are registered before consumers
2. **Look for circular references** - A → B → C → A
3. **Verify environment loading** - EnvironmentConfig must run first
4. **Check database connectivity** - Many services depend on AppDbContext
5. **Review service lifetimes** - Scoped vs Singleton vs Transient mismatches
6. **Enable detailed logging** - Set log level to Debug during startup

## 📝 Quick Reference

**Must Initialize First**:
- EnvironmentConfig (provides all config)
- LogService (needed for debugging)

**Database Dependent**:
- AppDbContext
- All repositories
- MailerService
- Hangfire

**Order Sensitive**:
- Authentication before Authorization
- Hangfire before MailServices
- All services before GraphQL
- GraphQL before middleware configuration