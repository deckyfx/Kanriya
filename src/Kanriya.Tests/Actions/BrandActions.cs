using Kanriya.Tests.Helpers;
using Kanriya.Shared.Utils;
using Spectre.Console;

namespace Kanriya.Tests.Actions;

/// <summary>
/// Reusable brand management actions for testing
/// Each action is atomic and can be used by any test suite
/// </summary>
public static class BrandActions
{
    /// <summary>
    /// Action: Create brand with valid name
    /// </summary>
    public static async Task<(bool success, string? brandId, string? apiKey, string? apiPassword)> CreateValidBrand(
        BrandTestHelper brandHelper,
        string? brandName = null)
    {
        brandName ??= $"TestBrand_{StringUtils.GenerateRandomAlphaNumeric(10)}";
        var (success, brandId, apiKey, apiPassword, message) = await brandHelper.CreateBrandAsync(brandName);
        
        if (success)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Brand created: {brandName} (ID: {brandId})[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Brand creation failed: {message}[/]");
        }
        
        return (success, brandId, apiKey, apiPassword);
    }

    /// <summary>
    /// Action: Create brand with invalid name (empty/null)
    /// </summary>
    public static async Task<bool> CreateBrandInvalidName(BrandTestHelper brandHelper, string invalidName)
    {
        var (success, _, _, _, _) = await brandHelper.CreateBrandAsync(invalidName);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Create brand without authentication
    /// </summary>
    public static async Task<bool> CreateBrandUnauthorized(BrandTestHelper brandHelper, GraphQLClient client)
    {
        var originalToken = client.GetAuthToken();
        client.SetAuthToken(null);
        
        var (success, _, _, _, _) = await brandHelper.CreateBrandAsync("Unauthorized Brand");
        
        client.SetAuthToken(originalToken);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Delete brand with valid authorization
    /// </summary>
    public static async Task<bool> DeleteBrandValid(BrandTestHelper brandHelper, string brandId)
    {
        var (success, message) = await brandHelper.DeleteBrandAsync(brandId);
        
        if (success)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Brand deleted: {brandId}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Brand deletion failed: {message}[/]");
        }
        
        return success;
    }

    /// <summary>
    /// Action: Delete brand without authentication
    /// </summary>
    public static async Task<bool> DeleteBrandUnauthorized(BrandTestHelper brandHelper, GraphQLClient client, string brandId)
    {
        var originalToken = client.GetAuthToken();
        client.SetAuthToken(null);
        
        var (success, _) = await brandHelper.DeleteBrandAsync(brandId);
        
        client.SetAuthToken(originalToken);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Sign in to brand with valid API credentials
    /// </summary>
    public static async Task<(bool success, string? token)> SignInBrandValid(
        BrandTestHelper brandHelper,
        string brandId,
        string apiKey,
        string apiPassword)
    {
        var (success, token, message) = await brandHelper.SignInBrandAsync(brandId, apiKey, apiPassword);
        
        if (success)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Brand sign-in successful[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Brand sign-in failed: {message}[/]");
        }
        
        return (success, token);
    }

    /// <summary>
    /// Action: Sign in to brand with invalid API key
    /// </summary>
    public static async Task<bool> SignInBrandInvalidKey(
        BrandTestHelper brandHelper,
        string brandId,
        string invalidKey,
        string apiPassword)
    {
        var (success, _, _) = await brandHelper.SignInBrandAsync(brandId, invalidKey, apiPassword);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Sign in to brand with wrong password
    /// </summary>
    public static async Task<bool> SignInBrandWrongPassword(
        BrandTestHelper brandHelper,
        string brandId,
        string apiKey,
        string wrongPassword)
    {
        var (success, _, _) = await brandHelper.SignInBrandAsync(brandId, apiKey, wrongPassword);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Sign in to brand with wrong brand ID
    /// </summary>
    public static async Task<bool> SignInBrandWrongId(
        BrandTestHelper brandHelper,
        string wrongBrandId,
        string apiKey,
        string apiPassword)
    {
        var (success, _, _) = await brandHelper.SignInBrandAsync(wrongBrandId, apiKey, apiPassword);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Update brand name with valid authorization
    /// </summary>
    public static async Task<bool> UpdateBrandNameValid(
        BrandTestHelper brandHelper,
        string brandId,
        string newName)
    {
        var (success, message) = await brandHelper.UpdateBrandNameAsync(brandId, newName);
        
        if (success)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Brand name updated to: {newName}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Brand name update failed: {message}[/]");
        }
        
        return success;
    }

    /// <summary>
    /// Action: Update brand name with invalid name
    /// </summary>
    public static async Task<bool> UpdateBrandNameInvalid(
        BrandTestHelper brandHelper,
        string brandId,
        string invalidName)
    {
        var (success, _) = await brandHelper.UpdateBrandNameAsync(brandId, invalidName);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Update brand name without authentication
    /// </summary>
    public static async Task<bool> UpdateBrandNameUnauthorized(
        BrandTestHelper brandHelper,
        GraphQLClient client,
        string brandId,
        string newName)
    {
        var originalToken = client.GetAuthToken();
        client.SetAuthToken(null);
        
        var (success, _) = await brandHelper.UpdateBrandNameAsync(brandId, newName);
        
        client.SetAuthToken(originalToken);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Update brand info with brand-context token
    /// </summary>
    public static async Task<bool> UpdateBrandInfoValid(
        BrandTestHelper brandHelper,
        string key,
        string value)
    {
        var (success, message) = await brandHelper.UpdateBrandInfoAsync(key, value);
        
        if (success)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Brand info updated: {key} = {value}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Brand info update failed: {message}[/]");
        }
        
        return success;
    }

    /// <summary>
    /// Action: Update brand info with principal token (should fail)
    /// </summary>
    public static async Task<bool> UpdateBrandInfoWithPrincipalToken(
        BrandTestHelper brandHelper,
        string key,
        string value)
    {
        var (success, _) = await brandHelper.UpdateBrandInfoAsync(key, value);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Get user's brands list
    /// </summary>
    public static async Task<(bool success, List<string>? brandIds, List<string>? brandNames)> GetUserBrands(
        BrandTestHelper brandHelper)
    {
        var (success, brands, message) = await brandHelper.GetMyBrandsAsync();
        
        if (success && brands.HasValue)
        {
            var brandIds = new List<string>();
            var brandNames = new List<string>();
            
            foreach (var brand in brands.Value.EnumerateArray())
            {
                var id = brand.GetProperty("id").GetString();
                var name = brand.GetProperty("name").GetString();
                if (id != null) brandIds.Add(id);
                if (name != null) brandNames.Add(name);
            }
            
            AnsiConsole.MarkupLine($"[green dim]  ✓ Retrieved {brandIds.Count} brands[/]");
            return (true, brandIds, brandNames);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Failed to get brands: {message}[/]");
            return (false, null, null);
        }
    }

    /// <summary>
    /// Action: Create a fully functional brand with API credentials
    /// Returns brand with all necessary credentials for testing
    /// </summary>
    public static async Task<(string? brandId, string? apiKey, string? apiPassword)> CreateTestBrand(
        BrandTestHelper brandHelper,
        string? brandName = null)
    {
        var (success, brandId, apiKey, apiPassword) = await CreateValidBrand(brandHelper, brandName);
        
        if (!success || string.IsNullOrEmpty(brandId))
        {
            AnsiConsole.MarkupLine("[red]Failed to create test brand[/]");
            return (null, null, null);
        }
        
        return (brandId, apiKey, apiPassword);
    }

    /// <summary>
    /// Action: Create brand and sign in to get brand-context token
    /// </summary>
    public static async Task<(string? brandId, string? brandToken)> CreateAndSignInBrand(
        BrandTestHelper brandHelper,
        string? brandName = null)
    {
        var (brandId, apiKey, apiPassword) = await CreateTestBrand(brandHelper, brandName);
        
        if (string.IsNullOrEmpty(brandId) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiPassword))
        {
            return (null, null);
        }
        
        var (success, token) = await SignInBrandValid(brandHelper, brandId, apiKey, apiPassword);
        
        if (!success || string.IsNullOrEmpty(token))
        {
            AnsiConsole.MarkupLine("[red]Failed to sign in to brand[/]");
            return (brandId, null);
        }
        
        return (brandId, token);
    }

    /// <summary>
    /// Action: Cleanup brand (delete if exists)
    /// Used for cleaning up test brands at the end of test stages
    /// </summary>
    public static async Task CleanupBrand(BrandTestHelper brandHelper, string? brandId)
    {
        if (string.IsNullOrEmpty(brandId))
            return;
            
        try
        {
            await brandHelper.DeleteBrandAsync(brandId);
            AnsiConsole.MarkupLine($"[dim]  Cleaned up brand: {brandId}[/]");
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Action: Cleanup all user's brands
    /// Used for comprehensive cleanup at the end of tests
    /// </summary>
    public static async Task CleanupAllUserBrands(BrandTestHelper brandHelper)
    {
        try
        {
            var (success, brandIds, _) = await GetUserBrands(brandHelper);
            
            if (success && brandIds != null)
            {
                foreach (var brandId in brandIds)
                {
                    await CleanupBrand(brandHelper, brandId);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Action: Verify brand exists in database
    /// </summary>
    public static async Task<bool> VerifyBrandExists(DatabaseHelper dbHelper, string brandId)
    {
        var exists = await dbHelper.BrandExistsAsync(brandId);
        
        if (exists)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Brand exists in database: {brandId}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Brand not found in database: {brandId}[/]");
        }
        
        return exists;
    }

    /// <summary>
    /// Action: Verify brand schema exists
    /// </summary>
    public static async Task<bool> VerifySchemaExists(DatabaseHelper dbHelper, string schemaName)
    {
        var exists = await dbHelper.SchemaExistsAsync(schemaName);
        
        if (exists)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Schema exists: {schemaName}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Schema not found: {schemaName}[/]");
        }
        
        return exists;
    }

    /// <summary>
    /// Action: Verify brand ownership
    /// </summary>
    public static async Task<bool> VerifyBrandOwnership(DatabaseHelper dbHelper, string brandId, string expectedOwnerId)
    {
        var brand = await dbHelper.GetBrandFromDatabaseAsync(brandId);
        var isOwner = brand?.OwnerId == expectedOwnerId;
        
        if (isOwner)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Brand ownership verified[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Brand ownership mismatch[/]");
        }
        
        return isOwner;
    }
}