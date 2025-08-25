using Kanriya.Tests.Helpers;
using Spectre.Console;

namespace Kanriya.Tests.Actions;

/// <summary>
/// Reusable database verification actions
/// </summary>
public static class DatabaseActions
{
    /// <summary>
    /// Action: Verify user is in pending_users table
    /// </summary>
    public static async Task<bool> VerifyUserIsPending(DatabaseHelper dbHelper, string email)
    {
        var exists = await dbHelper.UserIsPendingAsync(email);
        return exists;
    }

    /// <summary>
    /// Action: Verify user is in active users table
    /// </summary>
    public static async Task<bool> VerifyUserIsActive(DatabaseHelper dbHelper, string email)
    {
        var exists = await dbHelper.UserIsActiveAsync(email);
        return exists;
    }

    /// <summary>
    /// Action: Verify user does not exist in any table
    /// </summary>
    public static async Task<bool> VerifyUserNotExists(DatabaseHelper dbHelper, string email)
    {
        var notInPending = !await dbHelper.UserIsPendingAsync(email);
        var notInActive = !await dbHelper.UserIsActiveAsync(email);
        
        if (notInPending && notInActive)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ User not in database: {email}[/]");
            return true;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ User still exists in database: {email}[/]");
            return false;
        }
    }

    /// <summary>
    /// Action: Verify brand exists in database
    /// </summary>
    public static async Task<bool> VerifyBrandExists(DatabaseHelper dbHelper, string brandId)
    {
        return await dbHelper.BrandExistsAsync(brandId);
    }

    /// <summary>
    /// Action: Verify schema exists
    /// </summary>
    public static async Task<bool> VerifySchemaExists(DatabaseHelper dbHelper, string schemaName)
    {
        return await dbHelper.SchemaExistsAsync(schemaName);
    }
}