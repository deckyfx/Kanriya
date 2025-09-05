using Microsoft.EntityFrameworkCore;
using Kanriya.Server.Data;
using Kanriya.Server.Data.BrandSchema;
using Kanriya.Server.Program;
using Kanriya.Shared;
using Kanriya.Shared.Utils;
using Npgsql;
using System.Text;
using System.Text.Json;

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
    Task CleanupBrandPostgreSQLResourcesAsync(Brand brand);
    Task<(bool Success, string Message, Brand? Brand)> UpdateBrandNameAsync(
        string userId, 
        string brandId, 
        string newName, 
        CancellationToken cancellationToken = default);
    Task<bool> UserOwnsBrandAsync(string userId, string brandId, CancellationToken cancellationToken = default);
    Task<List<(string Key, string Value, DateTime CreatedAt, DateTime UpdatedAt)>> GetBrandInfoAsync(
        string brandId, 
        CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateBrandInfoAsync(
        string brandId, 
        string key, 
        string value, 
        CancellationToken cancellationToken = default);
    Task<(bool success, string? token, BrandUser? brandUser, Brand? brand)> AuthenticateBrandAsync(
        string apiKey, 
        string apiPassword,
        CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, string? ApiKey, string? ApiPassword)> ResetBrandCredentialsAsync(
        string? userId,
        string brandId,
        string? brandUserId,
        CancellationToken cancellationToken = default);
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
        // Validate brand name
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Brand name cannot be empty");
        }
        
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Check if brand name already exists (case-insensitive)
        var normalizedName = name.Trim().ToLowerInvariant();
        var existingBrand = await context.Brands
            .Where(b => b.Name.ToLower() == normalizedName)
            .FirstOrDefaultAsync();
            
        if (existingBrand != null)
        {
            _logger.LogWarning("Brand name '{Name}' already exists (ID: {ExistingId})", name, existingBrand.Id);
            throw new InvalidOperationException($"Brand name '{name}' is already taken");
        }
        
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
            var schemaName = StringUtils.CreateSafeString(brandGuid, "brand", true);
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
            
            // 3. Create brand schema tables (users, user_roles, and infoes)
            await CreateBrandSchemaTablesAsync(brand);
            
            // 4. Create brand owner user with API credentials
            var (apiSecret, apiPassword) = await CreateBrandOwnerAsync(brand, ownerUserId);
            
            // 5. Log created tables and their initial data for verification
            await LogBrandSchemaTablesAsync(brand);
            
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
            // Note: We can't clean up here as we don't know what was created
            // The transaction rollback should handle the brand record
            // PostgreSQL resources would need manual cleanup if they were created
            
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
        
        // Create infoes table (stores both info and config as key-value pairs)
        using var createInfoesTable = new NpgsqlCommand(@$"
            CREATE TABLE IF NOT EXISTS {brand.SchemaName}.infoes (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                key VARCHAR(100) NOT NULL,
                value TEXT NOT NULL,
                created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
            );
            
            CREATE UNIQUE INDEX IF NOT EXISTS ix_{brand.SchemaName}_infoes_key 
                ON {brand.SchemaName}.infoes(key);
        ", connection);
        await createInfoesTable.ExecuteNonQueryAsync();
        
        // Create outlets table
        using var createOutletsTable = new NpgsqlCommand(@$"
            CREATE TABLE IF NOT EXISTS {brand.SchemaName}.outlets (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                code VARCHAR(50) NOT NULL,
                name VARCHAR(255) NOT NULL,
                address TEXT,
                is_active BOOLEAN DEFAULT true,
                created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(code)
            );
            
            CREATE INDEX IF NOT EXISTS ix_{brand.SchemaName}_outlets_code 
                ON {brand.SchemaName}.outlets(code);
            CREATE INDEX IF NOT EXISTS ix_{brand.SchemaName}_outlets_is_active 
                ON {brand.SchemaName}.outlets(is_active);
        ", connection);
        await createOutletsTable.ExecuteNonQueryAsync();
        
        // Create user_outlets table (user-outlet permissions)
        using var createUserOutletsTable = new NpgsqlCommand(@$"
            CREATE TABLE IF NOT EXISTS {brand.SchemaName}.user_outlets (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                user_id UUID NOT NULL REFERENCES {brand.SchemaName}.users(id) ON DELETE CASCADE,
                outlet_id UUID NOT NULL REFERENCES {brand.SchemaName}.outlets(id) ON DELETE CASCADE,
                created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(user_id, outlet_id)
            );
            
            CREATE INDEX IF NOT EXISTS ix_{brand.SchemaName}_user_outlets_user_id 
                ON {brand.SchemaName}.user_outlets(user_id);
            CREATE INDEX IF NOT EXISTS ix_{brand.SchemaName}_user_outlets_outlet_id 
                ON {brand.SchemaName}.user_outlets(outlet_id);
        ", connection);
        await createUserOutletsTable.ExecuteNonQueryAsync();
        
        // Insert initial brand name info
        using var insertBrandName = new NpgsqlCommand(@$"
            INSERT INTO {brand.SchemaName}.infoes (key, value)
            VALUES ('Brand Name', @brandName)
            ON CONFLICT (key) DO NOTHING;
        ", connection);
        insertBrandName.Parameters.AddWithValue("brandName", brand.Name);
        await insertBrandName.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Created brand schema tables (users, user_roles, infoes, outlets, user_outlets) for brand {BrandId} with name '{BrandName}'", 
            brand.Id, brand.Name);
    }
    
    /// <summary>
    /// Log brand schema tables and their data for verification
    /// </summary>
    private async Task LogBrandSchemaTablesAsync(Brand brand)
    {
        var connectionString = _brandService.BuildConnectionString(brand);
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // List all tables in the brand schema
        using var listTables = new NpgsqlCommand(@$"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = '{brand.SchemaName}'
            ORDER BY table_name;
        ", connection);
        
        var tables = new List<string>();
        using (var reader = await listTables.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }
        
        _logger.LogInformation("Brand {BrandId} schema '{SchemaName}' contains tables: {Tables}", 
            brand.Id, brand.SchemaName, string.Join(", ", tables));
        
        // Log users table data
        using var getUserCount = new NpgsqlCommand(@$"
            SELECT COUNT(*), 
                   STRING_AGG(display_name || ' (' || api_secret || ')', ', ') 
            FROM {brand.SchemaName}.users;
        ", connection);
        
        using (var reader = await getUserCount.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                var count = reader.GetInt64(0);
                var users = reader.IsDBNull(1) ? "" : reader.GetString(1);
                _logger.LogInformation("  - users table: {Count} record(s) - {Users}", count, users);
            }
        }
        
        // Log user_roles table data
        using var getRoleCount = new NpgsqlCommand(@$"
            SELECT COUNT(*), STRING_AGG(role, ', ') 
            FROM {brand.SchemaName}.user_roles;
        ", connection);
        
        using (var reader = await getRoleCount.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                var count = reader.GetInt64(0);
                var roles = reader.IsDBNull(1) ? "" : reader.GetString(1);
                _logger.LogInformation("  - user_roles table: {Count} record(s) - Roles: {Roles}", count, roles);
            }
        }
        
        // Log infoes table data
        using var getInfoCount = new NpgsqlCommand(@$"
            SELECT COUNT(*), 
                   STRING_AGG(key || '=' || value, ', ' ORDER BY key) 
            FROM {brand.SchemaName}.infoes;
        ", connection);
        
        using (var reader = await getInfoCount.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                var count = reader.GetInt64(0);
                var infoes = reader.IsDBNull(1) ? "" : reader.GetString(1);
                _logger.LogInformation("  - infoes table: {Count} record(s) - {Infoes}", count, infoes);
            }
        }
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
    /// </summary>
    public async Task<List<Brand>> GetUserBrandsAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Brands
            .Where(b => b.OwnerId == userId && b.IsActive)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();
    }
    
    /// <summary>
    /// Clean up PostgreSQL resources for a brand (schema and user) without deleting the brand record
    /// Used when the brand record will be deleted by cascade
    /// </summary>
    public async Task CleanupBrandPostgreSQLResourcesAsync(Brand brand)
    {
        try
        {
            // Drop PostgreSQL schema
            await _pgService.DropSchemaAsync(brand.SchemaName);
            
            // Drop PostgreSQL user
            await _pgService.DropUserAsync(brand.DatabaseUser);
            
            // Clear cache
            _brandService.ClearCache(brand.Id);
            
            _logger.LogInformation("Cleaned up PostgreSQL resources for brand {BrandId}", brand.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup PostgreSQL resources for brand {BrandId}, continuing anyway", brand.Id);
            // Don't throw - we want to continue with user deletion even if PostgreSQL cleanup fails
        }
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
    
    /// <summary>
    /// Update brand name (principal token required)
    /// </summary>
    public async Task<(bool Success, string Message, Brand? Brand)> UpdateBrandNameAsync(
        string userId, 
        string brandId, 
        string newName, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return (false, "Brand name is required", null);
        }
        
        if (newName.Length < 3 || newName.Length > 100)
        {
            return (false, "Brand name must be between 3 and 100 characters", null);
        }
        
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var brand = await context.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId, cancellationToken);
            
        if (brand == null)
        {
            return (false, "Brand not found", null);
        }
        
        if (brand.OwnerId != userId)
        {
            return (false, "You don't have permission to update this brand", null);
        }
        
        brand.Name = newName;
        brand.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated brand {BrandId} name to {NewName}", brandId, newName);
        
        return (true, "Brand name updated successfully", brand);
    }
    
    /// <summary>
    /// Check if user owns a brand
    /// </summary>
    public async Task<bool> UserOwnsBrandAsync(string userId, string brandId, CancellationToken cancellationToken = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        return await context.Brands
            .AnyAsync(b => b.Id == brandId && b.OwnerId == userId && b.IsActive, cancellationToken);
    }
    
    /// <summary>
    /// Get all info from brand's infoes table
    /// </summary>
    public async Task<List<(string Key, string Value, DateTime CreatedAt, DateTime UpdatedAt)>> GetBrandInfoAsync(
        string brandId, 
        CancellationToken cancellationToken = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        // Get the brand to get its schema name
        var brand = await context.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId && b.IsActive, cancellationToken);
            
        if (brand == null)
        {
            throw new InvalidOperationException($"Brand {brandId} not found");
        }
        
        var connectionString = _brandService.BuildConnectionString(brand);
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Query all key-value pairs from the infoes table
        using var command = new NpgsqlCommand(@$"
            SELECT key, value, created_at, updated_at
            FROM {brand.SchemaName}.infoes
            ORDER BY key;
        ", connection);
        
        var result = new List<(string Key, string Value, DateTime CreatedAt, DateTime UpdatedAt)>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add((
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDateTime(2),
                reader.GetDateTime(3)
            ));
        }
        
        _logger.LogInformation("Retrieved {Count} info items from brand {BrandId}", result.Count, brandId);
        return result;
    }
    
    /// <summary>
    /// Update or insert a key-value pair in brand's infoes table
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateBrandInfoAsync(
        string brandId, 
        string key, 
        string value, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return (false, "Key is required");
        }
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, "Value is required");
        }
        
        // No reserved keys - brand users can update any key including "Brand Name" in their infoes table
        // The registry brand name remains immutable
        
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        // Get the brand to get its schema name
        var brand = await context.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId && b.IsActive, cancellationToken);
            
        if (brand == null)
        {
            return (false, $"Brand {brandId} not found");
        }
        
        var connectionString = _brandService.BuildConnectionString(brand);
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Upsert the key-value pair
        using var command = new NpgsqlCommand(@$"
            INSERT INTO {brand.SchemaName}.infoes (key, value, updated_at)
            VALUES (@key, @value, CURRENT_TIMESTAMP)
            ON CONFLICT (key) DO UPDATE
            SET value = @value, updated_at = CURRENT_TIMESTAMP;
        ", connection);
        
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("value", value);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
        
        _logger.LogInformation("Updated brand {BrandId} info: {Key} = {Value}", brandId, key, value);
        return (true, "Brand info updated successfully");
    }
    
    /// <summary>
    /// Authenticate using brand API credentials
    /// </summary>
    public async Task<(bool success, string? token, BrandUser? brandUser, Brand? brand)> AuthenticateBrandAsync(
        string apiKey, 
        string apiPassword,
        CancellationToken cancellationToken = default)
    {
        // This is a simplified implementation
        // In production, you would need to properly verify API credentials against the brand schema
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            // For now, we'll return a simple authentication failure
            // You'll need to implement proper brand authentication logic
            _logger.LogWarning("Brand authentication not fully implemented");
            return (false, null, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during brand authentication");
            return (false, null, null, null);
        }
    }
    
    private string GenerateBrandJwtToken(Brand brand, BrandUser? brandUser)
    {
        // This is a simplified token generation
        // In production, you should use proper JWT library with claims
        var tokenData = new
        {
            token_type = "BRAND",
            brand_id = brand.Id.ToString(),
            brand_schema = brand.SchemaName,
            brand_name = brand.Name,
            user_id = brandUser?.Id.ToString(),
            exp = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
        };
        
        // For now, return a simple base64 encoded JSON
        // In production, use proper JWT signing
        var json = JsonSerializer.Serialize(tokenData);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
    
    /// <summary>
    /// Reset brand API credentials - brand owner or brand-context user can reset
    /// </summary>
    public async Task<(bool Success, string Message, string? ApiKey, string? ApiPassword)> ResetBrandCredentialsAsync(
        string? userId,
        string brandId,
        string? brandUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            // Get the brand
            var brand = await context.Brands
                .FirstOrDefaultAsync(b => b.Id == brandId, cancellationToken);
                
            if (brand == null)
            {
                return (false, "Brand not found", null, null);
            }
            
            // Check authorization: either principal user (brand owner) or brand-context user
            bool isAuthorized = false;
            string authType = "";
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Principal context - check if user is brand owner
                if (brand.OwnerId == userId)
                {
                    isAuthorized = true;
                    authType = "principal owner";
                }
                else
                {
                    _logger.LogWarning("Principal user {UserId} attempted to reset credentials for brand {BrandId} owned by {OwnerId}", 
                        userId, brandId, brand.OwnerId);
                }
            }
            else if (!string.IsNullOrEmpty(brandUserId))
            {
                // Brand context - user can reset their own brand's credentials
                // TODO: Add role check here when permissions are implemented (e.g., BrandOwner role)
                isAuthorized = true;
                authType = "brand context";
            }
            
            if (!isAuthorized)
            {
                return (false, "Unauthorized: Only the brand owner or brand users can reset credentials", null, null);
            }
            
            // Generate new API credentials
            var apiKey = _apiCredentialService.GenerateApiSecret();
            var apiPassword = _apiCredentialService.GenerateApiPassword();
            
            // Get brand-specific connection
            var connectionString = _brandService.BuildConnectionString(brand);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Get the first brand user (owner)
            string? targetBrandUserId = null;
            using (var getUser = new NpgsqlCommand(@$"
                SELECT u.id 
                FROM {brand.SchemaName}.users u
                INNER JOIN {brand.SchemaName}.user_roles ur ON u.id = ur.user_id
                WHERE ur.role = 'BrandOwner'
                ORDER BY u.created_at
                LIMIT 1;
            ", connection))
            {
                var result = await getUser.ExecuteScalarAsync(cancellationToken);
                if (result != null)
                {
                    targetBrandUserId = result.ToString();
                }
            }
            
            if (string.IsNullOrEmpty(targetBrandUserId))
            {
                _logger.LogError("No brand owner found in brand schema {SchemaName}", brand.SchemaName);
                return (false, "Brand owner not found in brand schema", null, null);
            }
            
            // Update the API credentials
            var hashedPassword = _apiCredentialService.HashApiPassword(apiPassword);
            
            using (var updateCredentials = new NpgsqlCommand(@$"
                UPDATE {brand.SchemaName}.users 
                SET api_secret = @apiKey, 
                    api_password_hash = @apiPassword,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @userId::uuid;
            ", connection))
            {
                updateCredentials.Parameters.AddWithValue("apiKey", apiKey);
                updateCredentials.Parameters.AddWithValue("apiPassword", hashedPassword);
                updateCredentials.Parameters.AddWithValue("userId", targetBrandUserId);
                
                var rowsAffected = await updateCredentials.ExecuteNonQueryAsync(cancellationToken);
                
                if (rowsAffected == 0)
                {
                    return (false, "Failed to update credentials", null, null);
                }
            }
            
            _logger.LogInformation("Reset API credentials for brand {BrandId} by {AuthType} (UserId: {UserId}, BrandUserId: {BrandUserId})", 
                brandId, authType, userId ?? "N/A", brandUserId ?? "N/A");
            
            return (true, "Credentials reset successfully. Save these new credentials - they won't be shown again.", apiKey, apiPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting brand credentials for brand {BrandId}", brandId);
            return (false, $"Error resetting credentials: {ex.Message}", null, null);
        }
    }
}