using System.Text.Json;
using Kanriya.Tests.Helpers;
using Spectre.Console;

namespace Kanriya.Tests.Tests;

/// <summary>
/// Brand management test scenario
/// </summary>
public static class BrandManagementTest
{
    public static async Task<(int passed, int failed)> RunAsync(
        UserTestHelper userHelper, 
        DatabaseHelper dbHelper,
        GraphQLClient client)
    {
        int passed = 0;
        int failed = 0;
        
        AnsiConsole.Write(new Rule("[cyan]Brand Management Tests[/]"));
        AnsiConsole.WriteLine();
        
        // Setup: Create and sign in a user
        AnsiConsole.MarkupLine("[dim]Setting up test user...[/]");
        var user = await userHelper.CreateAndSignInUserAsync("brand_owner");
        if (user == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to create test user for brand tests[/]");
            return (0, 1);
        }
        passed++; // User setup successful
        
        string? brandId = null;
        string? schemaName = null;
        
        // Test 1: Create Brand
        AnsiConsole.MarkupLine("[yellow]🏢 Step 1: Create Brand...[/]");
        var brandName = $"TestBrand{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        
        var createMutation = @"
            mutation CreateBrand($input: CreateBrandInput!) {
                createBrand(input: $input) {
                    success
                    message
                    brand {
                        id
                        name
                        schemaName
                    }
                    apiPassword
                }
            }";
            
        var createResult = await client.ExecuteAsync(createMutation, new
        {
            input = new { name = brandName }
        });
        
        if (GraphQLClient.GetSuccess(createResult, "data", "createBrand"))
        {
            brandId = GraphQLClient.GetString(createResult, "data", "createBrand", "brand", "id");
            schemaName = GraphQLClient.GetString(createResult, "data", "createBrand", "brand", "schemaName");
            var apiPassword = GraphQLClient.GetString(createResult, "data", "createBrand", "apiPassword");
            
            AnsiConsole.MarkupLine($"[green]  ✅ Brand created: {brandName}[/]");
            AnsiConsole.MarkupLine($"[green dim]  Schema: {schemaName}[/]");
            AnsiConsole.MarkupLine($"[green dim]  API Password: {apiPassword?.Substring(0, Math.Min(4, apiPassword?.Length ?? 0))}****[/]");
            passed++;
            
            // Verify brand in database
            if (!string.IsNullOrEmpty(brandId) && await dbHelper.BrandExistsAsync(brandId))
                passed++;
            else
                failed++;
            
            // Verify schema was created
            if (!string.IsNullOrEmpty(schemaName) && await dbHelper.SchemaExistsAsync(schemaName))
                passed++;
            else
                failed++;
        }
        else
        {
            var message = GraphQLClient.GetMessage(createResult, "data", "createBrand");
            AnsiConsole.MarkupLine($"[red]  ❌ Brand creation failed: {message}[/]");
            failed++;
        }
        
        // Test 2: List User's Brands
        AnsiConsole.MarkupLine("[yellow]📋 Step 2: List My Brands...[/]");
        var listQuery = @"
            query GetMyBrands {
                myBrands {
                    id
                    name
                    createdAt
                }
            }";
            
        var listResult = await client.ExecuteAsync(listQuery);
        
        if (listResult != null &&
            listResult.RootElement.TryGetProperty("data", out var listData) &&
            listData.TryGetProperty("myBrands", out var brands) &&
            brands.ValueKind == JsonValueKind.Array)
        {
            var brandCount = brands.GetArrayLength();
            AnsiConsole.MarkupLine($"[green]  ✅ Found {brandCount} brand(s)[/]");
            
            foreach (var brand in brands.EnumerateArray())
            {
                if (brand.TryGetProperty("name", out var name) &&
                    brand.TryGetProperty("id", out var id))
                {
                    AnsiConsole.MarkupLine($"[dim]    - {name.GetString()} (ID: {id.GetString()})[/]");
                }
            }
            
            if (brandCount > 0)
                passed++;
            else
                failed++;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]  ❌ Failed to list brands[/]");
            failed++;
        }
        
        // Test 3: Get Specific Brand
        if (!string.IsNullOrEmpty(brandId))
        {
            AnsiConsole.MarkupLine("[yellow]🔍 Step 3: Get Specific Brand...[/]");
            var getQuery = @"
                query GetBrand($brandId: String!) {
                    brand(brandId: $brandId) {
                        id
                        name
                        schemaName
                        isActive
                    }
                }";
                
            var getResult = await client.ExecuteAsync(getQuery, new { brandId });
            
            if (getResult != null &&
                getResult.RootElement.TryGetProperty("data", out var getData) &&
                getData.TryGetProperty("brand", out var brand) &&
                brand.ValueKind != JsonValueKind.Null)
            {
                var name = GraphQLClient.GetString(getResult, "data", "brand", "name");
                AnsiConsole.MarkupLine($"[green]  ✅ Retrieved brand: {name}[/]");
                passed++;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]  ❌ Failed to get specific brand[/]");
                failed++;
            }
        }
        
        // Test 4: Delete Brand (cleanup)
        if (!string.IsNullOrEmpty(brandId))
        {
            AnsiConsole.MarkupLine("[yellow]🗑️ Step 4: Delete Brand...[/]");
            var deleteMutation = @"
                mutation DeleteBrand($id: String!) {
                    deleteBrand(id: $id) {
                        success
                        message
                    }
                }";
                
            var deleteResult = await client.ExecuteAsync(deleteMutation, new { id = brandId });
            
            if (GraphQLClient.GetSuccess(deleteResult, "data", "deleteBrand"))
            {
                AnsiConsole.MarkupLine("[green]  ✅ Brand deleted successfully[/]");
                passed++;
                
                // Verify brand removed from database
                if (!await dbHelper.BrandExistsAsync(brandId))
                {
                    AnsiConsole.MarkupLine("[green dim]  ✓ Brand removed from database[/]");
                    passed++;
                }
                else
                {
                    failed++;
                }
                
                // Verify schema removed
                if (!string.IsNullOrEmpty(schemaName) && !await dbHelper.SchemaExistsAsync(schemaName))
                {
                    AnsiConsole.MarkupLine("[green dim]  ✓ Schema removed[/]");
                    passed++;
                }
                else
                {
                    failed++;
                }
            }
            else
            {
                var message = GraphQLClient.GetMessage(deleteResult, "data", "deleteBrand");
                AnsiConsole.MarkupLine($"[red]  ❌ Brand deletion failed: {message}[/]");
                failed++;
            }
        }
        
        // Cleanup: Delete test user
        AnsiConsole.MarkupLine("[dim]Cleaning up test user...[/]");
        await userHelper.DeleteAccountAsync(user.Password);
        
        client.SetAuthToken(null);
        AnsiConsole.WriteLine();
        
        return (passed, failed);
    }
}