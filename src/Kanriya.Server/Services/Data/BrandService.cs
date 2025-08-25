using Microsoft.EntityFrameworkCore;
using Kanriya.Server.Data;
using Kanriya.Server.Data.BrandSchema;
using Kanriya.Server.Program;
using Npgsql;

namespace Kanriya.Server.Services.Data;

/// <summary>
/// Interface for brand management operations
/// </summary>
public interface IBrandService
{
    Task<(Brand brand, string apiSecret, string apiPassword)> CreateBrandAsync(string name, string ownerUserId);
    Task<Brand?> GetBrandAsync(string brandId);
    Task<List<Brand>> GetAllBrandsAsync();
    Task<List<Brand>> GetUserBrandsAsync(string userId);
    Task<bool> DeleteBrandAsync(string brandId);
}

/// <summary>
/// Service for managing brands and their schemas
/// </summary>
public class BrandService : IBrandService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IPostgreSQLManagementService _pgService;
    private readonly IBrandConnectionService _brandService;
    private readonly IApiCredentialService _apiCredentialService;
    private readonly ILogger<BrandService> _logger;
    
    public BrandService(
        IDbContextFactory<AppDbContext> contextFactory,
        IPostgreSQLManagementService pgService,
        IBrandConnectionService brandService,
        IApiCredentialService apiCredentialService,
        ILogger<BrandService> logger)
    {
        _contextFactory = contextFactory;
        _pgService = pgService;
        _brandService = brandService;
        _apiCredentialService = apiCredentialService;
        _logger = logger;
    }
    
    /// <summary>
    /// Create a new brand with PostgreSQL schema and initial user
    /// </summary>
    public async Task<(Brand brand, string apiSecret, string apiPassword)> CreateBrandAsync(
        string name, 
        string ownerUserId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Check if owner exists
        var owner = await context.Users
            .FirstOrDefaultAsync(u => u.Id == ownerUserId);
            
        if (owner == null)
        {
            throw new InvalidOperationException($"User {ownerUserId} not found");
        }
        
        using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            // 1. Generate unique brand ID and schema name
            var brandGuid = Guid.NewGuid().ToString();
            var schemaName = $"brand_{brandGuid.Replace("-", "_")}";
            var (dbUser, dbPassword) = await _pgService.CreateDatabaseUserAsync(brandGuid);
            await _pgService.CreateSchemaAsync(schemaName, dbUser);
            await _pgService.GrantSchemaAccessAsync(schemaName, dbUser);
            
            // 2. Create brand record in master schema
            var brand = new Brand
            {
                Id = brandGuid,
                Name = name,
                OwnerId = ownerUserId,
                SchemaName = schemaName,
                DatabaseUser = dbUser,
                EncryptedPassword = _brandService.EncryptPassword(dbPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            context.Brands.Add(brand);
            await context.SaveChangesAsync();
            
            // 3. Create brand schema tables (users and user_roles)
            await CreateBrandSchemaTablesAsync(brand);
            
            // 4. Create brand owner user with API credentials
            var (apiSecret, apiPassword) = await CreateBrandOwnerAsync(brand, ownerUserId);
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("Created brand {BrandId} with schema {SchemaName}", 
                brandGuid, schemaName);
                
            return (brand, apiSecret, apiPassword);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create brand {BrandName}", name);
            
            // Try to clean up PostgreSQL resources
            try
            {
                var brandGuid = Guid.NewGuid().ToString();
                await _pgService.DropSchemaAsync($"brand_{brandGuid.Replace("-", "_")}");
                await _pgService.DropUserAsync($"user_{brandGuid.ToLower()}");
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            throw;
        }
    }
    
    /// <summary>
    /// Create brand schema tables
    /// </summary>
    private async Task CreateBrandSchemaTablesAsync(Brand brand)
    {
        var connectionString = _brandService.BuildConnectionString(brand);
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Create users table
        using var createUsersTable = new NpgsqlCommand(@$"
            CREATE TABLE IF NOT EXISTS {brand.SchemaName}.users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                api_secret VARCHAR(16) NOT NULL UNIQUE,
                api_password_hash TEXT NOT NULL,
                brand_schema VARCHAR(100) NOT NULL,
                display_name VARCHAR(200),
                is_active BOOLEAN DEFAULT true,
                created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                last_login_at TIMESTAMPTZ
            );
            
            CREATE INDEX IF NOT EXISTS ix_{brand.SchemaName}_users_api_secret 
                ON {brand.SchemaName}.users(api_secret);
            CREATE INDEX IF NOT EXISTS ix_{brand.SchemaName}_users_is_active 
                ON {brand.SchemaName}.users(is_active);
        ", connection);
        await createUsersTable.ExecuteNonQueryAsync();
        
        // Create user_roles table
        using var createRolesTable = new NpgsqlCommand(@$"
            CREATE TABLE IF NOT EXISTS {brand.SchemaName}.user_roles (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                user_id UUID NOT NULL REFERENCES {brand.SchemaName}.users(id) ON DELETE CASCADE,
                role VARCHAR(50) NOT NULL,
                is_active BOOLEAN DEFAULT true,
                created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
            );
            
            CREATE UNIQUE INDEX IF NOT EXISTS ix_{brand.SchemaName}_user_roles_user_role 
                ON {brand.SchemaName}.user_roles(user_id, role);
        ", connection);
        await createRolesTable.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Created brand schema tables for brand {BrandId}", brand.Id);
    }
    
    /// <summary>
    /// Create the brand owner user with API credentials
    /// </summary>
    private async Task<(string apiSecret, string apiPassword)> CreateBrandOwnerAsync(Brand brand, string principalUserId)
    {
        var connectionString = _brandService.BuildConnectionString(brand);
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Generate API credentials
        var apiSecret = _apiCredentialService.GenerateApiSecret();
        var apiPassword = _apiCredentialService.GenerateApiPassword();
        var apiPasswordHash = _apiCredentialService.HashApiPassword(apiPassword);
        
        // Create user
        var userId = Guid.NewGuid();
        using var createUser = new NpgsqlCommand(@$"
            INSERT INTO {brand.SchemaName}.users 
                (id, api_secret, api_password_hash, brand_schema, display_name, is_active)
            VALUES 
                (@id, @apiSecret, @apiPasswordHash, @brandSchema, @displayName, true)
        ", connection);
        
        createUser.Parameters.AddWithValue("id", userId);
        createUser.Parameters.AddWithValue("apiSecret", apiSecret);
        createUser.Parameters.AddWithValue("apiPasswordHash", apiPasswordHash);
        createUser.Parameters.AddWithValue("brandSchema", brand.SchemaName);
        createUser.Parameters.AddWithValue("displayName", $"Owner (Principal: {principalUserId})");
        await createUser.ExecuteNonQueryAsync();
        
        // Assign BrandOwner role
        using var assignRole = new NpgsqlCommand(@$"
            INSERT INTO {brand.SchemaName}.user_roles 
                (user_id, role, is_active)
            VALUES 
                (@userId, @role, true)
        ", connection);
        
        assignRole.Parameters.AddWithValue("userId", userId);
        assignRole.Parameters.AddWithValue("role", BrandRoles.BrandOwner);
        await assignRole.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Created brand owner user for brand {BrandId}", brand.Id);
        
        return (apiSecret, apiPassword);
    }
    
    /// <summary>
    /// Get a brand by ID
    /// </summary>
    public async Task<Brand?> GetBrandAsync(string brandId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId);
    }
    
    /// <summary>
    /// Get all brands
    /// </summary>
    public async Task<List<Brand>> GetAllBrandsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Brands
            .Where(b => b.IsActive)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();
    }
    
    /// <summary>
    /// Get brands for a specific user
    /// For now, this returns all brands as we don't have user-brand mapping
    /// In the future, this should filter based on user's access
    /// </summary>
    public async Task<List<Brand>> GetUserBrandsAsync(string userId)
    {
        // TODO: Implement proper user-brand relationship
        // For now, return all brands
        return await GetAllBrandsAsync();
    }
    
    /// <summary>
    /// Delete a brand and all its data
    /// </summary>
    public async Task<bool> DeleteBrandAsync(string brandId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            var brand = await context.Brands.FindAsync(brandId);
            if (brand == null)
            {
                return false;
            }
            
            // 1. Drop PostgreSQL schema
            await _pgService.DropSchemaAsync(brand.SchemaName);
            
            // 2. Drop PostgreSQL user
            await _pgService.DropUserAsync(brand.DatabaseUser);
            
            // 3. Delete brand record
            context.Brands.Remove(brand);
            await context.SaveChangesAsync();
            
            // 4. Clear cache
            _brandService.ClearCache(brandId);
            
            await transaction.CommitAsync();
            
            _logger.LogWarning("Deleted brand {BrandId} and all its data", brandId);
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to delete brand {BrandId}", brandId);
            throw;
        }
    }
}