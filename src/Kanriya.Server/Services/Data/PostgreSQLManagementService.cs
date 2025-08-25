using System.Security.Cryptography;
using Npgsql;
using Kanriya.Server.Program;

namespace Kanriya.Server.Services.Data;

/// <summary>
/// Interface for PostgreSQL database management operations
/// </summary>
public interface IPostgreSQLManagementService
{
    Task<(string username, string password)> CreateDatabaseUserAsync(string businessId);
    Task CreateSchemaAsync(string schemaName, string owner);
    Task GrantSchemaAccessAsync(string schemaName, string username);
    Task RevokeAllAccessAsync(string username, string[] excludeSchemas);
    Task DropSchemaAsync(string schemaName);
    Task DropUserAsync(string username);
    Task<bool> UserExistsAsync(string username);
    Task<bool> SchemaExistsAsync(string schemaName);
}

/// <summary>
/// Service for managing PostgreSQL database operations at the system level
/// </summary>
public class PostgreSQLManagementService : IPostgreSQLManagementService
{
    private readonly ILogger<PostgreSQLManagementService> _logger;
    private readonly string _masterConnectionString;
    
    public PostgreSQLManagementService(ILogger<PostgreSQLManagementService> logger)
    {
        _logger = logger;
        // Use master connection with superuser privileges
        _masterConnectionString = BuildMasterConnectionString();
    }
    
    private string BuildMasterConnectionString()
    {
        return $"Host={EnvironmentConfig.Database.Host};" +
               $"Port={EnvironmentConfig.Database.Port};" +
               $"Database={EnvironmentConfig.Database.DatabaseName};" +
               $"Username={EnvironmentConfig.Database.Username};" +
               $"Password={EnvironmentConfig.Database.Password};" +
               "Include Error Detail=true";
    }
    
    /// <summary>
    /// Create a new database user for a brand
    /// </summary>
    public async Task<(string username, string password)> CreateDatabaseUserAsync(string brandId)
    {
        var username = $"user_{brandId.ToLower()}";
        var password = GenerateSecurePassword();
        
        try
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            
            // Check if user already exists
            if (await UserExistsAsync(username))
            {
                _logger.LogWarning("User {Username} already exists", username);
                throw new InvalidOperationException($"Database user {username} already exists");
            }
            
            // Create user with password
            // Note: We have to escape the password properly for SQL
            var escapedPassword = password.Replace("'", "''");
            
            using var command = new NpgsqlCommand($@"
                CREATE USER {username} WITH 
                    PASSWORD '{escapedPassword}'
                    NOSUPERUSER
                    NOCREATEDB
                    NOCREATEROLE
                    NOINHERIT
                    NOLOGIN
                    NOREPLICATION
                    NOBYPASSRLS;
                    
                -- Allow login after setup is complete
                ALTER USER {username} WITH LOGIN;",
                connection);
                
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Created database user {Username} for brand {BrandId}", 
                username, brandId);
                
            return (username, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database user for brand {BrandId}", brandId);
            throw;
        }
    }
    
    /// <summary>
    /// Create a new schema for a brand
    /// </summary>
    public async Task CreateSchemaAsync(string schemaName, string owner)
    {
        try
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            
            // Check if schema already exists
            if (await SchemaExistsAsync(schemaName))
            {
                _logger.LogWarning("Schema {SchemaName} already exists", schemaName);
                throw new InvalidOperationException($"Schema {schemaName} already exists");
            }
            
            // Create schema owned by the brand user
            using var command = new NpgsqlCommand($@"
                CREATE SCHEMA {schemaName} AUTHORIZATION {owner};
                
                -- Grant usage on schema to owner
                GRANT ALL ON SCHEMA {schemaName} TO {owner};
                
                -- Set default privileges for tables created in this schema
                ALTER DEFAULT PRIVILEGES IN SCHEMA {schemaName} 
                    GRANT ALL ON TABLES TO {owner};
                    
                ALTER DEFAULT PRIVILEGES IN SCHEMA {schemaName} 
                    GRANT ALL ON SEQUENCES TO {owner};
                    
                ALTER DEFAULT PRIVILEGES IN SCHEMA {schemaName} 
                    GRANT ALL ON FUNCTIONS TO {owner};",
                connection);
                
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Created schema {SchemaName} with owner {Owner}", 
                schemaName, owner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create schema {SchemaName}", schemaName);
            throw;
        }
    }
    
    /// <summary>
    /// Grant full access to a schema for a user
    /// </summary>
    public async Task GrantSchemaAccessAsync(string schemaName, string username)
    {
        try
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand($@"
                -- Grant usage on schema
                GRANT USAGE ON SCHEMA {schemaName} TO {username};
                
                -- Grant all privileges on all tables
                GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA {schemaName} TO {username};
                
                -- Grant all privileges on all sequences
                GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA {schemaName} TO {username};
                
                -- Grant execute on all functions
                GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA {schemaName} TO {username};
                
                -- Set search path for the user
                ALTER USER {username} SET search_path TO {schemaName}, public;",
                connection);
                
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Granted schema {SchemaName} access to user {Username}", 
                schemaName, username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant schema access for {Username} to {SchemaName}", 
                username, schemaName);
            throw;
        }
    }
    
    /// <summary>
    /// Revoke all access from a user except specified schemas
    /// </summary>
    public async Task RevokeAllAccessAsync(string username, string[] excludeSchemas)
    {
        try
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            
            // Get all schemas
            var schemasQuery = @"
                SELECT schema_name 
                FROM information_schema.schemata 
                WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                    AND schema_name != ALL(@excludeSchemas)";
                
            using var schemasCommand = new NpgsqlCommand(schemasQuery, connection);
            schemasCommand.Parameters.AddWithValue("excludeSchemas", excludeSchemas);
            
            var schemas = new List<string>();
            using (var reader = await schemasCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    schemas.Add(reader.GetString(0));
                }
            }
            
            // Revoke access from each schema
            foreach (var schema in schemas)
            {
                using var revokeCommand = new NpgsqlCommand($@"
                    REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA {schema} FROM {username};
                    REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA {schema} FROM {username};
                    REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA {schema} FROM {username};
                    REVOKE USAGE ON SCHEMA {schema} FROM {username};",
                    connection);
                    
                await revokeCommand.ExecuteNonQueryAsync();
            }
            
            _logger.LogInformation("Revoked all access from user {Username} except schemas: {Schemas}", 
                username, string.Join(", ", excludeSchemas));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke access for user {Username}", username);
            throw;
        }
    }
    
    /// <summary>
    /// Drop a schema (DANGER: permanent deletion)
    /// </summary>
    public async Task DropSchemaAsync(string schemaName)
    {
        try
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schemaName} CASCADE;", connection);
            await command.ExecuteNonQueryAsync();
            
            _logger.LogWarning("Dropped schema {SchemaName} and all its contents", schemaName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to drop schema {SchemaName}", schemaName);
            throw;
        }
    }
    
    /// <summary>
    /// Drop a database user (DANGER: permanent deletion)
    /// </summary>
    public async Task DropUserAsync(string username)
    {
        try
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            
            // First, reassign owned objects to postgres user
            using var reassignCommand = new NpgsqlCommand(
                $"REASSIGN OWNED BY {username} TO postgres;", connection);
            await reassignCommand.ExecuteNonQueryAsync();
            
            // Drop owned objects
            using var dropOwnedCommand = new NpgsqlCommand(
                $"DROP OWNED BY {username};", connection);
            await dropOwnedCommand.ExecuteNonQueryAsync();
            
            // Finally, drop the user
            using var dropUserCommand = new NpgsqlCommand(
                $"DROP USER IF EXISTS {username};", connection);
            await dropUserCommand.ExecuteNonQueryAsync();
            
            _logger.LogWarning("Dropped database user {Username}", username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to drop user {Username}", username);
            throw;
        }
    }
    
    /// <summary>
    /// Check if a database user exists
    /// </summary>
    public async Task<bool> UserExistsAsync(string username)
    {
        try
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(
                "SELECT 1 FROM pg_user WHERE usename = @username", connection);
            command.Parameters.AddWithValue("username", username);
            
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user {Username} exists", username);
            throw;
        }
    }
    
    /// <summary>
    /// Check if a schema exists
    /// </summary>
    public async Task<bool> SchemaExistsAsync(string schemaName)
    {
        try
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(
                "SELECT 1 FROM information_schema.schemata WHERE schema_name = @schemaName", 
                connection);
            command.Parameters.AddWithValue("schemaName", schemaName);
            
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if schema {SchemaName} exists", schemaName);
            throw;
        }
    }
    
    /// <summary>
    /// Generate a secure random password
    /// </summary>
    private static string GenerateSecurePassword(int length = 32)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=[]{}|;:,.<>?";
        var password = new char[length];
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length * 4];
        rng.GetBytes(bytes);
        
        for (int i = 0; i < length; i++)
        {
            var value = BitConverter.ToUInt32(bytes, i * 4);
            password[i] = validChars[(int)(value % (uint)validChars.Length)];
        }
        
        return new string(password);
    }
}