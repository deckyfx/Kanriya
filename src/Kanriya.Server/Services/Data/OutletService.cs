using Microsoft.EntityFrameworkCore;
using Kanriya.Server.Data;
using Kanriya.Server.Data.BrandSchema;
using Kanriya.Server.Types.Outputs;
using HotChocolate.Subscriptions;
using Npgsql;

namespace Kanriya.Server.Services.Data;

/// <summary>
/// Interface for outlet management operations
/// </summary>
public interface IOutletService
{
    // Outlet CRUD operations
    Task<Outlet> CreateOutletAsync(string brandId, string brandSchema, string code, string name, string? address = null);
    Task<Outlet?> GetOutletAsync(string brandId, string brandSchema, string outletId);
    Task<List<Outlet>> GetBrandOutletsAsync(string brandId, string brandSchema);
    Task<List<Outlet>> GetUserOutletsAsync(string brandId, string brandSchema, string userId);
    Task<Outlet?> UpdateOutletAsync(string brandId, string brandSchema, string outletId, string? code = null, string? name = null, string? address = null, bool? isActive = null);
    Task<bool> DeleteOutletAsync(string brandId, string brandSchema, string outletId);
    
    // User-Outlet permission management
    Task<bool> GrantUserOutletAccessAsync(string brandId, string brandSchema, string userId, string outletId);
    Task<bool> RevokeUserOutletAccessAsync(string brandId, string brandSchema, string userId, string outletId);
    Task<bool> UpdateUserOutletsAsync(string brandId, string brandSchema, string userId, List<string> outletIds);
    Task<bool> UserHasOutletAccessAsync(string brandId, string brandSchema, string userId, string outletId);
    Task<List<BrandUser>> GetOutletUsersAsync(string brandId, string brandSchema, string outletId);
}

/// <summary>
/// Service for managing outlets and user-outlet permissions
/// </summary>
public class OutletService : IOutletService
{
    private readonly IBrandConnectionService _brandConnectionService;
    private readonly ITopicEventSender _eventSender;
    private readonly ILogger<OutletService> _logger;
    
    public OutletService(
        IBrandConnectionService brandConnectionService,
        ITopicEventSender eventSender,
        ILogger<OutletService> logger)
    {
        _brandConnectionService = brandConnectionService;
        _eventSender = eventSender;
        _logger = logger;
    }
    
    /// <summary>
    /// Create a new outlet for a brand
    /// </summary>
    public async Task<Outlet> CreateOutletAsync(string brandId, string brandSchema, string code, string name, string? address = null)
    {
        try
        {
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            var outletId = Guid.NewGuid().ToString();
            
            using var command = new NpgsqlCommand(@$"
                INSERT INTO {brandSchema}.outlets (id, code, name, address, is_active, created_at, updated_at)
                VALUES (@id, @code, @name, @address, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                RETURNING *;
            ", connection);
            
            command.Parameters.AddWithValue("id", Guid.Parse(outletId));
            command.Parameters.AddWithValue("code", code);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("address", (object?)address ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var outlet = new Outlet
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
                    Code = reader.GetString(reader.GetOrdinal("code")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
                
                // Publish outlet created event
                var outletEvent = new SubscriptionEvent<Outlet>
                {
                    Event = EventType.Created,
                    Document = outlet,
                    Time = DateTime.UtcNow,
                    Previous = null
                };
                await _eventSender.SendAsync("OutletChanged", outletEvent, CancellationToken.None);
                
                _logger.LogInformation("Created outlet {OutletId} with code {Code} for brand {BrandId}", outlet.Id, code, brandId);
                return outlet;
            }
            
            throw new Exception("Failed to create outlet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create outlet with code {Code} for brand {BrandId}", code, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Get a specific outlet by ID
    /// </summary>
    public async Task<Outlet?> GetOutletAsync(string brandId, string brandSchema, string outletId)
    {
        try
        {
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(@$"
                SELECT * FROM {brandSchema}.outlets WHERE id = @id;
            ", connection);
            
            command.Parameters.AddWithValue("id", Guid.Parse(outletId));
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Outlet
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
                    Code = reader.GetString(reader.GetOrdinal("code")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get outlet {OutletId} for brand {BrandId}", outletId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Get all outlets for a brand
    /// </summary>
    public async Task<List<Outlet>> GetBrandOutletsAsync(string brandId, string brandSchema)
    {
        try
        {
            var outlets = new List<Outlet>();
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(@$"
                SELECT * FROM {brandSchema}.outlets ORDER BY created_at DESC;
            ", connection);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                outlets.Add(new Outlet
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
                    Code = reader.GetString(reader.GetOrdinal("code")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }
            
            _logger.LogInformation("Retrieved {Count} outlets for brand {BrandId}", outlets.Count, brandId);
            return outlets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get outlets for brand {BrandId}", brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Get outlets accessible to a specific user
    /// </summary>
    public async Task<List<Outlet>> GetUserOutletsAsync(string brandId, string brandSchema, string userId)
    {
        try
        {
            // First check if user has BrandOwner role (access to all outlets)
            var isBrandOwner = await IsUserBrandOwnerAsync(brandId, brandSchema, userId);
            
            if (isBrandOwner)
            {
                // Brand owners have access to all outlets
                return await GetBrandOutletsAsync(brandId, brandSchema);
            }
            
            // Otherwise, get outlets from user_outlets table
            var outlets = new List<Outlet>();
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(@$"
                SELECT o.* FROM {brandSchema}.outlets o
                INNER JOIN {brandSchema}.user_outlets uo ON o.id = uo.outlet_id
                WHERE uo.user_id = @userId
                ORDER BY o.created_at DESC;
            ", connection);
            
            command.Parameters.AddWithValue("userId", Guid.Parse(userId));
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                outlets.Add(new Outlet
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
                    Code = reader.GetString(reader.GetOrdinal("code")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }
            
            _logger.LogInformation("Retrieved {Count} outlets for user {UserId} in brand {BrandId}", outlets.Count, userId, brandId);
            return outlets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get outlets for user {UserId} in brand {BrandId}", userId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Update an outlet's information
    /// </summary>
    public async Task<Outlet?> UpdateOutletAsync(string brandId, string brandSchema, string outletId, string? code = null, string? name = null, string? address = null, bool? isActive = null)
    {
        try
        {
            // Get the current outlet first for the "previous" state
            var previousOutlet = await GetOutletAsync(brandId, brandSchema, outletId);
            if (previousOutlet == null)
            {
                return null;
            }
            
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            var updates = new List<string>();
            var parameters = new List<NpgsqlParameter>();
            
            if (code != null)
            {
                updates.Add("code = @code");
                parameters.Add(new NpgsqlParameter("code", code));
            }
            
            if (name != null)
            {
                updates.Add("name = @name");
                parameters.Add(new NpgsqlParameter("name", name));
            }
            
            if (address != null)
            {
                updates.Add("address = @address");
                parameters.Add(new NpgsqlParameter("address", address));
            }
            
            if (isActive.HasValue)
            {
                updates.Add("is_active = @isActive");
                parameters.Add(new NpgsqlParameter("isActive", isActive.Value));
            }
            
            if (updates.Count == 0)
            {
                return await GetOutletAsync(brandId, brandSchema, outletId);
            }
            
            updates.Add("updated_at = CURRENT_TIMESTAMP");
            
            using var command = new NpgsqlCommand(@$"
                UPDATE {brandSchema}.outlets 
                SET {string.Join(", ", updates)}
                WHERE id = @id
                RETURNING *;
            ", connection);
            
            command.Parameters.AddWithValue("id", Guid.Parse(outletId));
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var outlet = new Outlet
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
                    Code = reader.GetString(reader.GetOrdinal("code")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
                
                // Publish outlet updated event
                var outletEvent = new SubscriptionEvent<Outlet>
                {
                    Event = EventType.Updated,
                    Document = outlet,
                    Time = DateTime.UtcNow,
                    Previous = previousOutlet
                };
                await _eventSender.SendAsync("OutletChanged", outletEvent, CancellationToken.None);
                
                _logger.LogInformation("Updated outlet {OutletId} for brand {BrandId}", outletId, brandId);
                return outlet;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update outlet {OutletId} for brand {BrandId}", outletId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Delete an outlet (hard delete in development)
    /// </summary>
    public async Task<bool> DeleteOutletAsync(string brandId, string brandSchema, string outletId)
    {
        try
        {
            // Get the outlet first for the "previous" state
            var previousOutlet = await GetOutletAsync(brandId, brandSchema, outletId);
            if (previousOutlet == null)
            {
                return false;
            }
            
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(@$"
                DELETE FROM {brandSchema}.outlets WHERE id = @id;
            ", connection);
            
            command.Parameters.AddWithValue("id", Guid.Parse(outletId));
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                // Publish outlet deleted event
                var outletEvent = new SubscriptionEvent<Outlet>
                {
                    Event = EventType.Deleted,
                    Document = null, // Document is null for delete events
                    Time = DateTime.UtcNow,
                    Previous = previousOutlet
                };
                await _eventSender.SendAsync("OutletChanged", outletEvent, CancellationToken.None);
                
                _logger.LogInformation("Deleted outlet {OutletId} for brand {BrandId}", outletId, brandId);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete outlet {OutletId} for brand {BrandId}", outletId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Grant a user access to an outlet
    /// </summary>
    public async Task<bool> GrantUserOutletAccessAsync(string brandId, string brandSchema, string userId, string outletId)
    {
        try
        {
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(@$"
                INSERT INTO {brandSchema}.user_outlets (user_id, outlet_id, created_at)
                VALUES (@userId, @outletId, CURRENT_TIMESTAMP)
                ON CONFLICT (user_id, outlet_id) DO NOTHING;
            ", connection);
            
            command.Parameters.AddWithValue("userId", Guid.Parse(userId));
            command.Parameters.AddWithValue("outletId", Guid.Parse(outletId));
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Granted user {UserId} access to outlet {OutletId} in brand {BrandId}", userId, outletId, brandId);
            }
            
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant user {UserId} access to outlet {OutletId} in brand {BrandId}", userId, outletId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Revoke a user's access to an outlet
    /// </summary>
    public async Task<bool> RevokeUserOutletAccessAsync(string brandId, string brandSchema, string userId, string outletId)
    {
        try
        {
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(@$"
                DELETE FROM {brandSchema}.user_outlets 
                WHERE user_id = @userId AND outlet_id = @outletId;
            ", connection);
            
            command.Parameters.AddWithValue("userId", Guid.Parse(userId));
            command.Parameters.AddWithValue("outletId", Guid.Parse(outletId));
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Revoked user {UserId} access to outlet {OutletId} in brand {BrandId}", userId, outletId, brandId);
            }
            
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke user {UserId} access to outlet {OutletId} in brand {BrandId}", userId, outletId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Update a user's outlet access list (replaces existing permissions)
    /// </summary>
    public async Task<bool> UpdateUserOutletsAsync(string brandId, string brandSchema, string userId, List<string> outletIds)
    {
        try
        {
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                // Delete existing permissions
                using var deleteCommand = new NpgsqlCommand(@$"
                    DELETE FROM {brandSchema}.user_outlets WHERE user_id = @userId;
                ", connection, transaction);
                deleteCommand.Parameters.AddWithValue("userId", Guid.Parse(userId));
                await deleteCommand.ExecuteNonQueryAsync();
                
                // Insert new permissions
                foreach (var outletId in outletIds)
                {
                    using var insertCommand = new NpgsqlCommand(@$"
                        INSERT INTO {brandSchema}.user_outlets (user_id, outlet_id, created_at)
                        VALUES (@userId, @outletId, CURRENT_TIMESTAMP);
                    ", connection, transaction);
                    insertCommand.Parameters.AddWithValue("userId", Guid.Parse(userId));
                    insertCommand.Parameters.AddWithValue("outletId", Guid.Parse(outletId));
                    await insertCommand.ExecuteNonQueryAsync();
                }
                
                await transaction.CommitAsync();
                
                _logger.LogInformation("Updated user {UserId} outlet access list to {Count} outlets in brand {BrandId}", userId, outletIds.Count, brandId);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId} outlet access in brand {BrandId}", userId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Check if a user has access to a specific outlet
    /// </summary>
    public async Task<bool> UserHasOutletAccessAsync(string brandId, string brandSchema, string userId, string outletId)
    {
        try
        {
            // First check if user has BrandOwner role
            var isBrandOwner = await IsUserBrandOwnerAsync(brandId, brandSchema, userId);
            
            if (isBrandOwner)
            {
                // Brand owners have access to all outlets
                return true;
            }
            
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(@$"
                SELECT 1 FROM {brandSchema}.user_outlets 
                WHERE user_id = @userId AND outlet_id = @outletId;
            ", connection);
            
            command.Parameters.AddWithValue("userId", Guid.Parse(userId));
            command.Parameters.AddWithValue("outletId", Guid.Parse(outletId));
            
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check user {UserId} access to outlet {OutletId} in brand {BrandId}", userId, outletId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Get all users who have access to a specific outlet
    /// </summary>
    public async Task<List<BrandUser>> GetOutletUsersAsync(string brandId, string brandSchema, string outletId)
    {
        try
        {
            var users = new List<BrandUser>();
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Get users with explicit outlet access
            using var command = new NpgsqlCommand(@$"
                SELECT u.* FROM {brandSchema}.users u
                INNER JOIN {brandSchema}.user_outlets uo ON u.id = uo.user_id
                WHERE uo.outlet_id = @outletId
                
                UNION
                
                -- Include all BrandOwner users (they have access to all outlets)
                SELECT u.* FROM {brandSchema}.users u
                INNER JOIN {brandSchema}.user_roles ur ON u.id = ur.user_id
                WHERE ur.role = 'BrandOwner' AND ur.is_active = true
                
                ORDER BY created_at DESC;
            ", connection);
            
            command.Parameters.AddWithValue("outletId", Guid.Parse(outletId));
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new BrandUser
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
                    ApiSecret = reader.GetString(reader.GetOrdinal("api_secret")),
                    ApiPasswordHash = reader.GetString(reader.GetOrdinal("api_password_hash")),
                    BrandSchema = reader.GetString(reader.GetOrdinal("brand_schema")),
                    DisplayName = reader.IsDBNull(reader.GetOrdinal("display_name")) ? null : reader.GetString(reader.GetOrdinal("display_name")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login_at"))
                });
            }
            
            _logger.LogInformation("Retrieved {Count} users with access to outlet {OutletId} in brand {BrandId}", users.Count, outletId, brandId);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users for outlet {OutletId} in brand {BrandId}", outletId, brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Check if a user has BrandOwner role
    /// </summary>
    private async Task<bool> IsUserBrandOwnerAsync(string brandId, string brandSchema, string userId)
    {
        try
        {
            var connectionString = await _brandConnectionService.GetConnectionStringAsync(brandId);
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(@$"
                SELECT 1 FROM {brandSchema}.user_roles 
                WHERE user_id = @userId AND role = 'BrandOwner' AND is_active = true;
            ", connection);
            
            command.Parameters.AddWithValue("userId", Guid.Parse(userId));
            
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} is BrandOwner in schema {BrandSchema}", userId, brandSchema);
            return false;
        }
    }
}