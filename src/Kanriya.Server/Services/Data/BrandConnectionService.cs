using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Kanriya.Server.Data;
using Kanriya.Server.Data.BrandSchema;
using Kanriya.Server.Program;

namespace Kanriya.Server.Services.Data;

/// <summary>
/// Interface for managing brand database connections
/// </summary>
public interface IBrandConnectionService
{
    string BuildConnectionString(string brandId);
    string BuildConnectionString(Brand brand);
    Task<string> GetConnectionStringAsync(string brandId);
    DbContextOptions<BrandDbContext> GetBrandOptions(string brandId);
    Task<DbContextOptions<BrandDbContext>> GetBrandOptionsAsync(string brandId);
    Task<bool> ValidateConnectionAsync(string brandId);
    void ClearCache(string brandId);
    string EncryptPassword(string password);
    string DecryptPassword(string encryptedPassword);
}

/// <summary>
/// Service for managing brand-specific database connections
/// </summary>
public class BrandConnectionService : IBrandConnectionService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<BrandConnectionService> _logger;
    private readonly Dictionary<string, string> _connectionStringCache = new();
    private readonly Dictionary<string, DbContextOptions<BrandDbContext>> _optionsCache = new();
    private readonly string _encryptionKey;
    
    public BrandConnectionService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<BrandConnectionService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        
        // Get or generate encryption key for passwords
        _encryptionKey = Environment.GetEnvironmentVariable("TENANT_PASSWORD_KEY") ?? 
                         GenerateDefaultKey();
    }
    
    /// <summary>
    /// Build a connection string for a brand (requires brand info to be loaded)
    /// </summary>
    public string BuildConnectionString(string brandId)
    {
        // This method assumes you already have the brand info
        // Use GetConnectionStringAsync for cached/database lookup
        throw new NotImplementedException(
            "Use GetConnectionStringAsync for database-backed connection strings");
    }
    
    /// <summary>
    /// Build connection string from a Brand object
    /// </summary>
    public string BuildConnectionString(Brand brand)
    {
        // Decrypt the password
        var password = DecryptPassword(brand.EncryptedPassword);
        
        // Escape password for connection string - wrap in single quotes if it contains special chars
        var escapedPassword = password.Contains(';') || password.Contains(' ') || password.Contains('=') 
            ? $"'{password.Replace("'", "''")}'" 
            : password;
        
        // Build connection string with brand-specific user and schema
        var connectionString = $"Host={EnvironmentConfig.Database.Host};" +
                              $"Port={EnvironmentConfig.Database.Port};" +
                              $"Database={EnvironmentConfig.Database.DatabaseName};" +
                              $"Username={brand.DatabaseUser};" +
                              $"Password={escapedPassword};" +
                              $"Search Path={brand.SchemaName},public;" +
                              "Include Error Detail=true";
        
        // Cache it for future use
        _connectionStringCache[brand.Id] = connectionString;
        
        return connectionString;
    }
    
    /// <summary>
    /// Get connection string for a brand from database
    /// </summary>
    public async Task<string> GetConnectionStringAsync(string brandId)
    {
        // Check cache first
        if (_connectionStringCache.TryGetValue(brandId, out var cached))
        {
            return cached;
        }
        
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var brand = await context.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == brandId && b.IsActive);
            
        if (brand == null)
        {
            throw new InvalidOperationException($"Brand {brandId} not found or inactive");
        }
        
        // Decrypt the password
        var password = DecryptPassword(brand.EncryptedPassword);
        
        // Escape password for connection string - wrap in single quotes if it contains special chars
        var escapedPassword = password.Contains(';') || password.Contains(' ') || password.Contains('=') 
            ? $"'{password.Replace("'", "''")}'" 
            : password;
        
        // Build connection string with brand-specific user and schema
        var connectionString = $"Host={EnvironmentConfig.Database.Host};" +
                              $"Port={EnvironmentConfig.Database.Port};" +
                              $"Database={EnvironmentConfig.Database.DatabaseName};" +
                              $"Username={brand.DatabaseUser};" +
                              $"Password={escapedPassword};" +
                              $"Search Path={brand.SchemaName},public;" +
                              "Include Error Detail=true";
        
        // Cache the connection string
        _connectionStringCache[brandId] = connectionString;
        
        return connectionString;
    }
    
    /// <summary>
    /// Get DbContext options for a brand (synchronous, requires cache)
    /// </summary>
    public DbContextOptions<BrandDbContext> GetBrandOptions(string brandId)
    {
        if (_optionsCache.TryGetValue(brandId, out var cached))
        {
            return cached;
        }
        
        throw new InvalidOperationException(
            $"Brand options for {brandId} not in cache. Use GetBrandOptionsAsync first.");
    }
    
    /// <summary>
    /// Get DbContext options for a brand from database
    /// </summary>
    public async Task<DbContextOptions<BrandDbContext>> GetBrandOptionsAsync(string brandId)
    {
        // Check cache first
        if (_optionsCache.TryGetValue(brandId, out var cached))
        {
            return cached;
        }
        
        var connectionString = await GetConnectionStringAsync(brandId);
        
        var optionsBuilder = new DbContextOptionsBuilder<BrandDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        })
        .UseSnakeCaseNamingConvention();
        
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif
        
        var options = optionsBuilder.Options;
        
        // Cache the options
        _optionsCache[brandId] = options;
        
        return options;
    }
    
    /// <summary>
    /// Validate that we can connect to a brand's database
    /// </summary>
    public async Task<bool> ValidateConnectionAsync(string brandId)
    {
        try
        {
            // Get brand to retrieve schema name
            using var appContext = await _contextFactory.CreateDbContextAsync();
            var brand = await appContext.Brands
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == brandId && b.IsActive);
            
            if (brand == null)
            {
                _logger.LogWarning("Brand {BrandId} not found for validation", brandId);
                return false;
            }
            
            var options = await GetBrandOptionsAsync(brandId);
            using var context = new BrandDbContext(options, brand.SchemaName);
            
            // Try to connect
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                _logger.LogInformation("Successfully validated connection for brand {BrandId}", 
                    brandId);
            }
            else
            {
                _logger.LogWarning("Failed to validate connection for brand {BrandId}", 
                    brandId);
            }
            
            return canConnect;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating connection for brand {BrandId}", brandId);
            return false;
        }
    }
    
    /// <summary>
    /// Clear cached connection information for a brand
    /// </summary>
    public void ClearCache(string brandId)
    {
        _connectionStringCache.Remove(brandId);
        _optionsCache.Remove(brandId);
        _logger.LogInformation("Cleared connection cache for brand {BrandId}", brandId);
    }
    
    /// <summary>
    /// Encrypt a database password
    /// </summary>
    public string EncryptPassword(string password)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
        
        // Combine IV and encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
        
        return Convert.ToBase64String(result);
    }
    
    /// <summary>
    /// Decrypt a database password
    /// </summary>
    public string DecryptPassword(string encryptedPassword)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedPassword);
        
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
        
        // Extract IV from the beginning
        var iv = new byte[aes.IV.Length];
        Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;
        
        // Extract encrypted data
        var cipherBytes = new byte[encryptedBytes.Length - iv.Length];
        Buffer.BlockCopy(encryptedBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);
        
        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    
    private static string GenerateDefaultKey()
    {
        // Generate a consistent key based on machine name and app name
        // In production, this should come from secure configuration
        var source = $"{Environment.MachineName}-Kanriya-BrandEncryption";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(source));
        return Convert.ToBase64String(hash);
    }
}