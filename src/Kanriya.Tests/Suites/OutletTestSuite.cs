using Kanriya.Tests.Helpers;
using Kanriya.Tests.Actions;
using Kanriya.Shared.Utils;
using Spectre.Console;
using System.Text.Json;

namespace Kanriya.Tests.Suites;

/// <summary>
/// Outlet Management Test Suite
/// Tests outlet CRUD operations, permissions, and access control within brands
/// </summary>
public static class OutletTestSuite
{
    private static int _positivePass = 0;
    private static int _positiveFail = 0;
    private static int _negativePass = 0;
    private static int _negativeFail = 0;
    
    // Principal user for brand creation
    private static TestUser? _principalUser;
    private static string? _principalUserId;
    private static string? _principalToken;
    
    // Brand context
    private static string? _brandId;
    private static string? _brandName;
    private static string? _brandApiKey;
    private static string? _brandApiPassword;
    private static string? _brandToken;
    
    
    // Outlet tracking
    private static string? _primaryOutletId;
    private static string? _secondaryOutletId;
    
    // Helpers
    private static BrandTestHelper? _brandHelper;
    private static OutletTestHelper? _outletHelper;
    
    public static async Task<(int passed, int failed)> RunAsync(
        UserTestHelper userHelper,
        DatabaseHelper dbHelper, 
        GraphQLClient client)
    {
        // Reset state
        _positivePass = 0;
        _positiveFail = 0;
        _negativePass = 0;
        _negativeFail = 0;
        
        AnsiConsole.Write(new Panel(new FigletText("Outlet Suite")
            .Color(Color.Blue))
            .Header("[bold blue]Outlet Management Test Suite[/]")
            .Border(BoxBorder.Double));
        AnsiConsole.WriteLine();
        
        // Setup test infrastructure
        bool setupSuccess = await SetupPrincipalUserAndBrand(userHelper, dbHelper, client);
        if (!setupSuccess)
        {
            AnsiConsole.MarkupLine("[red]⚠️ CRITICAL: Setup failed. Exiting test suite.[/]");
            return (_positivePass + _negativePass, _positiveFail + _negativeFail);
        }
        
        // Create helpers
        _brandHelper = new BrandTestHelper(client);
        _outletHelper = new OutletTestHelper(client);
        
        // Run test stages
        await RunOutletCrudStage();
        await RunOutletPermissionsStage();
        await RunNegativeTestsStage();
        
        // Cleanup
        await CleanupTestData(dbHelper);
        
        // Show summary
        TestReporter.ShowSummary(_positivePass, _positiveFail, _negativePass, _negativeFail);
        
        return (_positivePass + _negativePass, _positiveFail + _negativeFail);
    }
    
    private static async Task<bool> SetupPrincipalUserAndBrand(
        UserTestHelper userHelper,
        DatabaseHelper dbHelper,
        GraphQLClient client)
    {
        AnsiConsole.Write(new Rule("[bold blue]Stage 1: Setup Principal User and Brand[/]").LeftJustified());
        AnsiConsole.WriteLine();
        
        try
        {
            // Create and sign in principal user
            _principalUser = await userHelper.CreateAndSignInUserAsync("outlet_test");
            if (_principalUser == null)
            {
                AnsiConsole.MarkupLine("[red]Failed to create principal user[/]");
                return false;
            }
            
            _principalUserId = _principalUser.Id;
            _principalToken = _principalUser.Token;
            client.SetAuthToken(_principalToken!);
            
            // Create a brand using helper
            _brandName = $"Outlet Test Brand {DateTime.UtcNow.Ticks}";
            var brandHelper = new BrandTestHelper(client);
            var (success, brandId, apiKey, apiPassword, message) = await brandHelper.CreateBrandAsync(_brandName);
            
            if (!success)
            {
                AnsiConsole.MarkupLine($"[red]Failed to create brand: {message}[/]");
                return false;
            }
            
            _brandId = brandId;
            _brandApiKey = apiKey;
            _brandApiPassword = apiPassword;
            
            AnsiConsole.MarkupLine($"[green]✓[/] Created brand: {_brandName}");
            AnsiConsole.MarkupLine($"[dim]  Brand ID: {_brandId}[/]");
            AnsiConsole.MarkupLine($"[dim]  API Key: {_brandApiKey}[/]");
            
            // Sign in with brand credentials
            var brandSignInResult = await SignInWithBrandCredentials(client, _brandApiKey!, _brandApiPassword!, _brandId!);
            if (!brandSignInResult.success)
            {
                AnsiConsole.MarkupLine($"[red]Failed to sign in with brand credentials: {brandSignInResult.message}[/]");
                return false;
            }
            
            _brandToken = brandSignInResult.token;
            client.SetAuthToken(_brandToken!);
            
            AnsiConsole.MarkupLine($"[green]✓[/] Signed in with brand credentials (BrandOwner)");
            _positivePass++;
            
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Setup failed: {ex.Message}[/]");
            _positiveFail++;
            return false;
        }
    }
    
    private static async Task<(bool success, string? token, string? message)> SignInWithBrandCredentials(
        GraphQLClient client,
        string apiKey,
        string apiPassword,
        string brandId)
    {
        var mutation = @"
            mutation SignIn($input: SignInInput!) {
                signIn(input: $input) {
                    success
                    message
                    token
                }
            }";
        
        var variables = new
        {
            input = new
            {
                email = apiKey,
                password = apiPassword,
                brandId = brandId
            }
        };
        
        var response = await client.ExecuteAsync(mutation, variables);
        
        if (response == null)
        {
            return (false, null, "No response from server");
        }
        
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, null, "No data in response");
        }
        
        var signIn = data.GetProperty("signIn");
        var success = signIn.GetProperty("success").GetBoolean();
        var message = signIn.GetProperty("message").GetString();
        var token = success ? signIn.GetProperty("token").GetString() : null;
        
        return (success, token, message);
    }
    
    private static async Task RunOutletCrudStage()
    {
        AnsiConsole.Write(new Rule("[bold blue]Stage 2: Outlet CRUD Operations[/]").LeftJustified());
        AnsiConsole.WriteLine();
        
        // Ensure we're using brand token
        _outletHelper!._client.SetAuthToken(_brandToken!);
        
        // Test 1: Create primary outlet
        AnsiConsole.MarkupLine("[bold]Test 1: Create Primary Outlet[/]");
        var (createSuccess, outletId, createMessage) = await _outletHelper.CreateOutletAsync(
            "OUT001",
            "Main Outlet - Downtown",
            "123 Main Street, Downtown"
        );
        
        if (createSuccess)
        {
            _primaryOutletId = outletId;
            _positivePass++;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Failed to create outlet: {createMessage}");
            _positiveFail++;
        }
        
        // Test 2: Create secondary outlet
        AnsiConsole.MarkupLine("[bold]Test 2: Create Secondary Outlet[/]");
        var (createSuccess2, outletId2, createMessage2) = await _outletHelper.CreateOutletAsync(
            "OUT002",
            "Branch Outlet - Mall",
            "456 Shopping Mall, Level 2"
        );
        
        if (createSuccess2)
        {
            _secondaryOutletId = outletId2;
            _positivePass++;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Failed to create secondary outlet: {createMessage2}");
            _positiveFail++;
        }
        
        // Test 3: List outlets
        AnsiConsole.MarkupLine("[bold]Test 3: List All Outlets[/]");
        var (listSuccess, count, outletIds, listMessage) = await _outletHelper.ListOutletsAsync();
        
        if (listSuccess && count >= 2)
        {
            _positivePass++;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Failed to list outlets: {listMessage}");
            _positiveFail++;
        }
        
        // Test 4: Update outlet
        AnsiConsole.MarkupLine("[bold]Test 4: Update Outlet[/]");
        var (updateSuccess, updateMessage) = await _outletHelper.UpdateOutletAsync(
            _primaryOutletId!,
            name: "Updated Main Outlet",
            address: "789 New Address"
        );
        
        if (updateSuccess)
        {
            _positivePass++;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Failed to update outlet: {updateMessage}");
            _positiveFail++;
        }
        
        // Test 5: Get single outlet
        AnsiConsole.MarkupLine("[bold]Test 5: Get Single Outlet[/]");
        var (getSuccess, outletName, getMessage) = await _outletHelper.GetOutletAsync(_primaryOutletId!);
        
        if (getSuccess)
        {
            _positivePass++;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Failed to get outlet: {getMessage}");
            _positiveFail++;
        }
        
        AnsiConsole.WriteLine();
    }
    
    private static async Task RunOutletPermissionsStage()
    {
        AnsiConsole.Write(new Rule("[bold blue]Stage 3: Outlet Permission Tests[/]").LeftJustified());
        AnsiConsole.WriteLine();
        
        // Test 1: BrandOwner can access all outlets (already using brand owner token)
        AnsiConsole.MarkupLine("[bold]Test 1: BrandOwner Access to All Outlets[/]");
        var (listSuccess, count, outletIds, listMessage) = await _outletHelper!.ListMyOutletsAsync();
        
        if (listSuccess && count >= 2)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] BrandOwner has access to all {count} outlets");
            _positivePass++;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] BrandOwner outlet access failed");
            _positiveFail++;
        }
        
        // Note: Testing BrandOperator permissions would require creating a second brand user
        // and granting them specific outlet access. This would involve:
        // 1. Creating a new brand user with BrandOperator role
        // 2. Granting them access to specific outlets
        // 3. Testing their limited access
        
        AnsiConsole.MarkupLine("[yellow]Note: BrandOperator permission tests would require additional user creation[/]");
        AnsiConsole.WriteLine();
    }
    
    private static async Task RunNegativeTestsStage()
    {
        AnsiConsole.Write(new Rule("[bold blue]Stage 4: Negative Tests[/]").LeftJustified());
        AnsiConsole.WriteLine();
        
        // Test 1: Duplicate outlet code
        AnsiConsole.MarkupLine("[bold]Test 1: Reject Duplicate Outlet Code[/]");
        var (dupSuccess, dupId, dupMessage) = await _outletHelper!.CreateOutletAsync(
            "OUT001",  // Duplicate code
            "Duplicate Outlet",
            "999 Test Street"
        );
        
        if (!dupSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] [[Negative]] Correctly rejected duplicate outlet code: {dupMessage}");
            _negativePass++;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] [[Negative]] Should have rejected duplicate outlet code");
            _negativeFail++;
        }
        
        // Test 2: Access outlet without brand context (using principal token)
        AnsiConsole.MarkupLine("[bold]Test 2: Reject Access Without Brand Context[/]");
        _outletHelper._client.SetAuthToken(_principalToken!);
        
        var (accessSuccess, accessCount, accessIds, accessMessage) = await _outletHelper.ListOutletsAsync();
        
        if (!accessSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] [[Negative]] Correctly denied outlet access without brand context");
            _negativePass++;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] [[Negative]] Should have denied outlet access without brand context");
            _negativeFail++;
        }
        
        // Restore brand token for cleanup
        _outletHelper._client.SetAuthToken(_brandToken!);
        
        AnsiConsole.WriteLine();
    }
    
    private static async Task CleanupTestData(DatabaseHelper dbHelper)
    {
        AnsiConsole.Write(new Rule("[bold blue]Cleanup[/]").LeftJustified());
        AnsiConsole.WriteLine();
        
        try
        {
            // Clean up test brands and users (cascade will handle outlets)
            var deletedCount = await dbHelper.DeleteAllBrandsAsync();
            if (deletedCount > 0)
            {
                AnsiConsole.MarkupLine($"[dim]  Cleaned up {deletedCount} test brands and outlets[/]");
            }
            
            // Note: User cleanup is handled by cascade deletion when brand is deleted
            AnsiConsole.MarkupLine("[green]✓[/] Cleanup completed");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Cleanup error - {ex.Message}[/]");
        }
        
        AnsiConsole.WriteLine();
    }
}