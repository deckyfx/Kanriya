using Npgsql;
using Spectre.Console;

namespace Kanriya.Tests.Helpers;

/// <summary>
/// Helper for database verification operations
/// </summary>
public class DatabaseHelper
{
    private readonly string _connectionString;
    
    public DatabaseHelper(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    /// <summary>
    /// Check if a record exists in the database
    /// </summary>
    public async Task<bool> RecordExistsAsync(string table, string column, string value)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            
            using var cmd = new NpgsqlCommand(
                $"SELECT COUNT(*) FROM {table} WHERE {column} = @value", conn);
            cmd.Parameters.AddWithValue("value", value);
            
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red dim]Database check failed: {ex.Message}[/]");
            return false;
        }
    }
    
    /// <summary>
    /// Get count of records matching a condition
    /// </summary>
    public async Task<int> GetCountAsync(string query)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(query, conn);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red dim]Database query failed: {ex.Message}[/]");
            return -1;
        }
    }
    
    /// <summary>
    /// Check if user is in pending_users table
    /// </summary>
    public async Task<bool> UserIsPendingAsync(string email)
    {
        var exists = await RecordExistsAsync("pending_users", "email", email);
        if (exists)
        {
            LogSuccess($"User found in pending_users table: {email}");
        }
        else
        {
            LogError($"User not found in pending_users table: {email}");
        }
        return exists;
    }
    
    /// <summary>
    /// Check if user is in users table (active)
    /// </summary>
    public async Task<bool> UserIsActiveAsync(string email)
    {
        var exists = await RecordExistsAsync("users", "email", email);
        if (exists)
        {
            LogSuccess($"User found in users table: {email}");
        }
        else
        {
            LogError($"User not found in users table: {email}");
        }
        return exists;
    }
    
    /// <summary>
    /// Check if brand exists
    /// </summary>
    public async Task<bool> BrandExistsAsync(string brandId)
    {
        var exists = await RecordExistsAsync("brands", "id", brandId);
        if (exists)
        {
            LogSuccess($"Brand found in database: {brandId}");
        }
        else
        {
            LogError($"Brand not found in database: {brandId}");
        }
        return exists;
    }
    
    /// <summary>
    /// Check if schema exists
    /// </summary>
    public async Task<bool> SchemaExistsAsync(string schemaName)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM information_schema.schemata WHERE schema_name = @name)", 
                conn);
            cmd.Parameters.AddWithValue("name", schemaName);
            
            var exists = (bool)(await cmd.ExecuteScalarAsync() ?? false);
            
            if (exists)
            {
                LogSuccess($"Schema exists: {schemaName}");
            }
            else
            {
                LogError($"Schema not found: {schemaName}");
            }
            
            return exists;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red dim]Schema check failed: {ex.Message}[/]");
            return false;
        }
    }
    
    private void LogSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green dim]  ✓ {message}[/]");
    }
    
    private void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[red dim]  ✗ {message}[/]");
    }
    
    /// <summary>
    /// Check if PostgreSQL database user exists
    /// </summary>
    public async Task<bool> DatabaseUserExistsAsync(string username)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM pg_user WHERE usename = @name)", 
                conn);
            cmd.Parameters.AddWithValue("name", username);
            
            var exists = (bool)(await cmd.ExecuteScalarAsync() ?? false);
            
            if (exists)
            {
                LogSuccess($"Database user exists: {username}");
            }
            else
            {
                LogError($"Database user not found: {username}");
            }
            
            return exists;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red dim]Database user check failed: {ex.Message}[/]");
            return false;
        }
    }
    
    /// <summary>
    /// Delete all brands from database
    /// </summary>
    public async Task<int> DeleteAllBrandsAsync()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // First get all brands to delete their schemas
            using var selectCommand = new NpgsqlCommand(@"
                SELECT schema_name, database_user 
                FROM brands", connection);
            
            var schemasToDelete = new List<(string schema, string dbUser)>();
            using (var reader = await selectCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    schemasToDelete.Add((reader.GetString(0), reader.GetString(1)));
                }
            }
            
            // Drop schemas and users
            foreach (var (schema, dbUser) in schemasToDelete)
            {
                try
                {
                    using var dropSchemaCmd = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE", connection);
                    await dropSchemaCmd.ExecuteNonQueryAsync();
                    
                    using var dropUserCmd = new NpgsqlCommand($"DROP USER IF EXISTS {dbUser}", connection);
                    await dropUserCmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Ignore errors when dropping schemas/users
                }
            }
            
            // Delete all brands
            using var deleteCommand = new NpgsqlCommand("DELETE FROM brands", connection);
            var deleted = await deleteCommand.ExecuteNonQueryAsync();
            
            LogSuccess($"Deleted {deleted} brands from database");
            return deleted;
        }
        catch (Exception ex)
        {
            LogError($"Error deleting brands: {ex.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// Get brand from database by ID
    /// </summary>
    public async Task<dynamic?> GetBrandFromDatabaseAsync(string brandId)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM brands WHERE id = @id", 
                conn);
            cmd.Parameters.AddWithValue("id", brandId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new
                {
                    Id = reader["id"].ToString(),
                    Name = reader["name"].ToString(),
                    OwnerId = reader["owner_id"].ToString(),
                    SchemaName = reader["schema_name"].ToString(),
                    DatabaseUser = reader["database_user"].ToString(),
                    IsActive = (bool)reader["is_active"],
                    CreatedAt = (DateTime)reader["created_at"],
                    UpdatedAt = (DateTime)reader["updated_at"]
                };
            }
            
            return null;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red dim]Failed to get brand: {ex.Message}[/]");
            return null;
        }
    }
    
    /// <summary>
    /// Check if user has any brands
    /// </summary>
    public async Task<bool> UserHasBrandsAsync(string userId)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM brands WHERE owner_id = @userId AND is_active = true)", 
                conn);
            cmd.Parameters.AddWithValue("userId", userId);
            
            var exists = (bool)(await cmd.ExecuteScalarAsync() ?? false);
            
            if (exists)
            {
                LogSuccess($"User has brands: {userId}");
            }
            else
            {
                LogError($"User has no brands: {userId}");
            }
            
            return exists;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red dim]User brands check failed: {ex.Message}[/]");
            return false;
        }
    }
    
    /// <summary>
    /// Check if any schemas exist for user's brands
    /// </summary>
    public async Task<bool> SchemasExistForUserAsync(string userId)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            
            // First get all brand schemas for this user
            using var getSchemas = new NpgsqlCommand(
                "SELECT schema_name FROM brands WHERE owner_id = @userId", 
                conn);
            getSchemas.Parameters.AddWithValue("userId", userId);
            
            var schemas = new List<string>();
            using (var reader = await getSchemas.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    schemas.Add(reader["schema_name"].ToString() ?? "");
                }
            }
            
            // Check if any of these schemas still exist
            foreach (var schema in schemas)
            {
                if (await SchemaExistsAsync(schema))
                {
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red dim]Schema check for user failed: {ex.Message}[/]");
            return false;
        }
    }
    
    /// <summary>
    /// Get count of user's brands
    /// </summary>
    public async Task<int> GetUserBrandCountAsync(string userId)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            
            using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM brands WHERE owner_id = @userId AND is_active = true", 
                conn);
            cmd.Parameters.AddWithValue("userId", userId);
            
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            
            LogSuccess($"User has {count} brand(s)");
            
            return count;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red dim]Failed to count user brands: {ex.Message}[/]");
            return 0;
        }
    }
}