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
}