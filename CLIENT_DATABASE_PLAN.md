# 🗄️ Client Database Architecture Plan

**Kanriya Client Local Database Implementation using SQLite**

---

## Overview

SQLite is the optimal choice for Kanriya client applications due to:
- **Zero Configuration**: No server setup required
- **Cross-Platform**: Works on Desktop, Android, iOS, and Browser (via WASM)
- **ACID Compliance**: Full transaction support
- **Lightweight**: Minimal resource usage
- **Entity Framework Core Support**: Full .NET integration

---

## Architecture Strategy

### Database Location Strategy
```
Platform-Specific Database Locations:
├── Windows: %LOCALAPPDATA%/Kanriya/kanriya.db
├── macOS: ~/Library/Application Support/Kanriya/kanriya.db
├── Linux: ~/.local/share/Kanriya/kanriya.db
├── Android: /data/data/com.deckyfx.kanriya.android/files/kanriya.db
├── iOS: Documents/kanriya.db
└── Browser: IndexedDB (via SQL.js or similar)
```

### Multi-Brand Database Strategy
```
Option 1: Single Database with Brand Isolation
kanriya.db
├── Brands table (local brand registry)
├── CurrentSession table (active brand context)
└── Brand-specific tables with brand_id foreign keys

Option 2: Separate Database Per Brand
├── kanriya-system.db (brand registry, app settings)
├── brand-{brand-id}.db (brand-specific data)
└── cached-{brand-id}.db (server sync cache)
```

---

## Entity Framework Core Integration

### Package Requirements
```xml
<!-- In Kanriya.Client.Avalonia.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
```

### Client Database Context
```csharp
// src/Kanriya.Client.Avalonia/Data/ClientDbContext.cs
public class ClientDbContext : DbContext
{
    public DbSet<LocalBrand> Brands { get; set; }
    public DbSet<UserSession> Sessions { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
    public DbSet<CacheEntry> Cache { get; set; }
    
    // Brand-specific cached data
    public DbSet<CachedOutlet> Outlets { get; set; }
    public DbSet<CachedMenu> MenuItems { get; set; }
    public DbSet<CachedOrder> Orders { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = DatabasePathResolver.GetDatabasePath();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities with proper indexing
        modelBuilder.Entity<LocalBrand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BrandId).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });
        
        modelBuilder.Entity<CachedOutlet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BrandId, e.ServerId });
            entity.HasIndex(e => e.LastSyncAt);
        });
    }
}
```

### Platform-Specific Database Path Resolution
```csharp
// src/Kanriya.Client.Avalonia/Services/DatabasePathResolver.cs
public static class DatabasePathResolver
{
    public static string GetDatabasePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Kanriya", "kanriya.db");
        }
        else if (OperatingSystem.IsMacOS())
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userHome, "Library", "Application Support", "Kanriya", "kanriya.db");
        }
        else if (OperatingSystem.IsLinux())
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userHome, ".local", "share", "Kanriya", "kanriya.db");
        }
        else if (OperatingSystem.IsAndroid())
        {
            // Android-specific path resolution
            return Path.Combine(FileSystem.AppDataDirectory, "kanriya.db");
        }
        else if (OperatingSystem.IsIOS())
        {
            // iOS Documents directory
            return Path.Combine(FileSystem.AppDataDirectory, "kanriya.db");
        }
        
        // Fallback for other platforms
        return Path.Combine(Directory.GetCurrentDirectory(), "kanriya.db");
    }
    
    public static void EnsureDirectoryExists()
    {
        var dbPath = GetDatabasePath();
        var directory = Path.GetDirectoryName(dbPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
```

---

## Local Entity Models

### Core Local Entities
```csharp
// src/Kanriya.Client.Avalonia/Data/Entities/LocalBrand.cs
public class LocalBrand
{
    public int Id { get; set; }
    public string BrandId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string ServerUrl { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// src/Kanriya.Client.Avalonia/Data/Entities/UserSession.cs
public class UserSession
{
    public int Id { get; set; }
    public string BrandId { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public string UserName { get; set; } = null!;
    public string UserRole { get; set; } = null!;
    public bool IsActive { get; set; }
}

// src/Kanriya.Client.Avalonia/Data/Entities/AppSettings.cs
public class AppSettings
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string Category { get; set; } = null!;
}

// src/Kanriya.Client.Avalonia/Data/Entities/CacheEntry.cs
public class CacheEntry
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string Data { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public string BrandId { get; set; } = null!;
    public DateTime LastSyncAt { get; set; }
}
```

### Cached Business Entities
```csharp
// src/Kanriya.Client.Avalonia/Data/Entities/CachedOutlet.cs
public class CachedOutlet
{
    public int Id { get; set; }
    public string ServerId { get; set; } = null!; // Server-side outlet ID
    public string BrandId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime LastSyncAt { get; set; }
    public bool HasPendingChanges { get; set; }
}

// Similar patterns for CachedMenu, CachedOrder, etc.
```

---

## Database Service Layer

### Client Database Service
```csharp
// src/Kanriya.Client.Avalonia/Services/Data/ClientDatabaseService.cs
public class ClientDatabaseService
{
    private readonly ClientDbContext _context;
    
    public ClientDatabaseService(ClientDbContext context)
    {
        _context = context;
    }
    
    public async Task InitializeAsync()
    {
        DatabasePathResolver.EnsureDirectoryExists();
        await _context.Database.EnsureCreatedAsync();
        await SeedInitialDataAsync();
    }
    
    public async Task<LocalBrand?> GetActiveBrandAsync()
    {
        return await _context.Brands
            .Where(b => b.IsActive)
            .FirstOrDefaultAsync();
    }
    
    public async Task SetActiveBrandAsync(string brandId)
    {
        // Deactivate all brands
        await _context.Brands
            .Where(b => b.IsActive)
            .ExecuteUpdateAsync(b => b.SetProperty(x => x.IsActive, false));
        
        // Activate selected brand
        await _context.Brands
            .Where(b => b.BrandId == brandId)
            .ExecuteUpdateAsync(b => b.SetProperty(x => x.IsActive, true));
        
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedInitialDataAsync()
    {
        if (!await _context.Settings.AnyAsync())
        {
            _context.Settings.AddRange(new[]
            {
                new AppSettings { Key = "Theme", Value = "Dark", Category = "UI" },
                new AppSettings { Key = "Language", Value = "en", Category = "Localization" },
                new AppSettings { Key = "AutoSync", Value = "true", Category = "Sync" }
            });
            await _context.SaveChangesAsync();
        }
    }
}
```

### Cache Management Service
```csharp
// src/Kanriya.Client.Avalonia/Services/Data/CacheService.cs
public class CacheService
{
    private readonly ClientDbContext _context;
    
    public async Task<T?> GetAsync<T>(string key, string brandId) where T : class
    {
        var entry = await _context.Cache
            .Where(c => c.Key == key && c.BrandId == brandId && c.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
        
        if (entry == null) return null;
        
        return JsonSerializer.Deserialize<T>(entry.Data);
    }
    
    public async Task SetAsync<T>(string key, T data, string brandId, TimeSpan? expiry = null)
    {
        var expiresAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(24));
        var json = JsonSerializer.Serialize(data);
        
        var existingEntry = await _context.Cache
            .Where(c => c.Key == key && c.BrandId == brandId)
            .FirstOrDefaultAsync();
        
        if (existingEntry != null)
        {
            existingEntry.Data = json;
            existingEntry.ExpiresAt = expiresAt;
            existingEntry.LastSyncAt = DateTime.UtcNow;
        }
        else
        {
            _context.Cache.Add(new CacheEntry
            {
                Key = key,
                Data = json,
                BrandId = brandId,
                ExpiresAt = expiresAt,
                LastSyncAt = DateTime.UtcNow
            });
        }
        
        await _context.SaveChangesAsync();
    }
}
```

---

## Offline Synchronization Strategy

### Sync Service Architecture
```csharp
// src/Kanriya.Client.Avalonia/Services/Data/SyncService.cs
public class SyncService
{
    private readonly GraphQLClient _graphqlClient;
    private readonly ClientDbContext _context;
    private readonly CacheService _cache;
    
    public async Task<SyncResult> SyncBrandDataAsync(string brandId, bool forceFullSync = false)
    {
        var result = new SyncResult { BrandId = brandId, StartedAt = DateTime.UtcNow };
        
        try
        {
            // 1. Sync outlets
            await SyncOutletsAsync(brandId, forceFullSync);
            result.OutletsSynced = true;
            
            // 2. Sync menu items
            await SyncMenuItemsAsync(brandId, forceFullSync);
            result.MenuItemsSynced = true;
            
            // 3. Push local changes
            await PushPendingChangesAsync(brandId);
            result.ChangesPushed = true;
            
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        result.CompletedAt = DateTime.UtcNow;
        return result;
    }
    
    private async Task SyncOutletsAsync(string brandId, bool forceFullSync)
    {
        var lastSync = forceFullSync ? DateTime.MinValue : await GetLastSyncTimeAsync(brandId, "outlets");
        
        var query = new GetOutletsQuery { BrandId = brandId, Since = lastSync };
        var response = await _graphqlClient.SendQueryAsync(query);
        
        foreach (var outlet in response.Data.Outlets)
        {
            await UpsertCachedOutletAsync(outlet, brandId);
        }
        
        await SetLastSyncTimeAsync(brandId, "outlets", DateTime.UtcNow);
    }
}

public class SyncResult
{
    public string BrandId { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool OutletsSynced { get; set; }
    public bool MenuItemsSynced { get; set; }
    public bool ChangesPushed { get; set; }
}
```

### Conflict Resolution Strategy
```csharp
public enum ConflictResolutionStrategy
{
    ServerWins,      // Server data overwrites local
    LocalWins,       // Local data overwrites server
    MergeTimestamp,  // Most recent timestamp wins
    UserPrompt       // Ask user to resolve
}

public class ConflictResolver
{
    public async Task<T> ResolveConflictAsync<T>(T localEntity, T serverEntity, ConflictResolutionStrategy strategy)
        where T : class, ITimestampedEntity
    {
        return strategy switch
        {
            ConflictResolutionStrategy.ServerWins => serverEntity,
            ConflictResolutionStrategy.LocalWins => localEntity,
            ConflictResolutionStrategy.MergeTimestamp => 
                localEntity.LastModifiedAt > serverEntity.LastModifiedAt ? localEntity : serverEntity,
            ConflictResolutionStrategy.UserPrompt => await PromptUserForResolutionAsync(localEntity, serverEntity),
            _ => serverEntity
        };
    }
}
```

---

## DI Configuration

### Service Registration
```csharp
// In Program.cs or App.xaml.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientDatabase(this IServiceCollection services)
    {
        services.AddDbContext<ClientDbContext>();
        services.AddScoped<ClientDatabaseService>();
        services.AddScoped<CacheService>();
        services.AddScoped<SyncService>();
        services.AddScoped<ConflictResolver>();
        
        return services;
    }
}

// Usage in Program.cs
builder.Services.AddClientDatabase();
```

### Application Initialization
```csharp
// In App.xaml.cs or similar
public override async void OnFrameworkInitializationCompleted()
{
    var services = ServiceLocator.Current;
    var dbService = services.GetRequiredService<ClientDatabaseService>();
    
    // Initialize database on app start
    await dbService.InitializeAsync();
    
    base.OnFrameworkInitializationCompleted();
}
```

---

## Migration Strategy

### EF Core Migrations for Client
```bash
# Create initial migration
dotnet ef migrations add InitialClientDatabase --context ClientDbContext --output-dir Data/Migrations

# Update database
dotnet ef database update --context ClientDbContext
```

### Version Management
```csharp
public class DatabaseVersionManager
{
    public async Task CheckAndMigrateAsync()
    {
        var currentVersion = await GetCurrentVersionAsync();
        var appVersion = GetApplicationVersion();
        
        if (currentVersion < appVersion)
        {
            await MigrateToVersionAsync(appVersion);
        }
    }
    
    private async Task MigrateToVersionAsync(Version targetVersion)
    {
        // Custom migration logic for client-specific changes
        // that might not be covered by EF migrations
    }
}
```

---

## Performance Considerations

### Database Optimization
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Proper indexing for common queries
    modelBuilder.Entity<CachedOutlet>()
        .HasIndex(e => new { e.BrandId, e.IsActive })
        .HasDatabaseName("IX_CachedOutlets_Brand_Active");
    
    modelBuilder.Entity<CacheEntry>()
        .HasIndex(e => new { e.Key, e.BrandId })
        .HasDatabaseName("IX_Cache_Key_Brand");
    
    // Configure SQLite-specific optimizations
    modelBuilder.Entity<CacheEntry>()
        .Property(e => e.Data)
        .HasColumnType("TEXT"); // Ensure proper SQLite type
}
```

### Connection Pooling
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    var dbPath = DatabasePathResolver.GetDatabasePath();
    
    optionsBuilder.UseSqlite($"Data Source={dbPath}", options =>
    {
        options.CommandTimeout(30); // 30 second timeout
    });
    
    // Enable connection pooling for better performance
    optionsBuilder.EnableServiceProviderCaching();
    optionsBuilder.EnableSensitiveDataLogging(false); // Disable in production
}
```

---

## Testing Strategy

### Integration Tests
```csharp
[TestClass]
public class ClientDatabaseTests
{
    private ClientDbContext _context;
    private string _testDbPath;
    
    [TestInitialize]
    public void Setup()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<ClientDbContext>()
            .UseSqlite($"Data Source={_testDbPath}")
            .Options;
        
        _context = new ClientDbContext(options);
        _context.Database.EnsureCreated();
    }
    
    [TestMethod]
    public async Task Should_Store_And_Retrieve_LocalBrand()
    {
        // Arrange
        var brand = new LocalBrand
        {
            BrandId = "test-brand-123",
            Name = "Test Brand",
            ApiKey = "test-api-key",
            ServerUrl = "https://test.kanriya.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();
        
        var retrieved = await _context.Brands
            .FirstOrDefaultAsync(b => b.BrandId == "test-brand-123");
        
        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Test Brand", retrieved.Name);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _context?.Dispose();
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }
}
```

---

## Browser Platform Considerations

### SQLite in Browser (WASM)
For browser platform, SQLite requires special handling:

```csharp
#if BROWSER
// Use SQL.js or similar WASM SQLite implementation
public class BrowserSqliteContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Browser-specific SQLite configuration
        optionsBuilder.UseSqlite("Data Source=:memory:"); // In-memory for WASM
        // Or use IndexedDB adapter
    }
}
#else
// Regular SQLite for other platforms
public class ClientDbContext : DbContext
{
    // Standard SQLite configuration
}
#endif
```

---

## Summary

This comprehensive SQLite implementation provides:

✅ **Cross-Platform Compatibility** - Works on all Avalonia target platforms  
✅ **Offline-First Architecture** - Full functionality without server connection  
✅ **Multi-Brand Support** - Isolated brand data with proper context switching  
✅ **Intelligent Caching** - Smart cache management with expiration  
✅ **Conflict Resolution** - Handles sync conflicts gracefully  
✅ **Performance Optimized** - Proper indexing and query optimization  
✅ **Testing Ready** - Comprehensive test coverage strategy  

The next implementation phase would focus on the GraphQL client integration and real-time synchronization mechanisms.