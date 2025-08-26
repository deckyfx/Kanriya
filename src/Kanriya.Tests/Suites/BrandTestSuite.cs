using Kanriya.Tests.Helpers;
using Kanriya.Tests.Actions;
using Kanriya.Shared.Utils;
using Spectre.Console;
using System.Text.Json;

namespace Kanriya.Tests.Suites;

/// <summary>
/// Brand Management Test Suite with enhanced structure and reporting
/// Uses single user across all stages with strict chaining
/// </summary>
public static class BrandTestSuite
{
    private static int _positivePass = 0;
    private static int _positiveFail = 0;
    private static int _negativePass = 0;
    private static int _negativeFail = 0;
    
    // Primary user for all stages
    private static TestUser? _testUser;
    private static string? _userId;
    
    // Second user for cross-user access tests
    private static TestUser? _testUser2;
    private static string? _userId2;
    private static string? _user2Token;
    
    // Primary brand from Stage 1 to reuse in subsequent stages
    private static string? _primaryBrandId;
    private static string? _primaryBrandName;
    private static string? _primaryApiKey;
    private static string? _primaryApiPassword;
    
    // Second brand for primary user (cross-brand tests)
    private static string? _secondaryBrandId;
    private static string? _secondaryBrandName;
    private static string? _secondaryApiKey;
    private static string? _secondaryApiPassword;
    
    // Track test brands for cleanup
    private static readonly List<string> _createdBrandIds = new();
    
    // Flag for strict chaining
    private static bool _shouldContinue = true;

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
        _testUser = null;
        _userId = null;
        _createdBrandIds.Clear();
        _shouldContinue = true;

        AnsiConsole.Write(new Panel(new FigletText("Brand Suite")
            .Color(Color.Green))
            .Header("[bold green]Brand Management Test Suite - Enhanced[/]")
            .Border(BoxBorder.Double));
        AnsiConsole.WriteLine();

        // Setup single test user for all stages
        if (_shouldContinue)
            _shouldContinue = await SetupTestUser(userHelper, dbHelper, client);
        
        if (!_shouldContinue)
        {
            AnsiConsole.MarkupLine("[red]⚠️ CRITICAL: Failed to setup test user. Exiting test suite.[/]");
            TestReporter.ShowSummary(_positivePass, _positiveFail, _negativePass, _negativeFail);
            return (_positivePass + _negativePass, _positiveFail + _negativeFail);
        }

        // Clean up any existing brands to ensure clean slate
        var deletedCount = await dbHelper.DeleteAllBrandsAsync();
        if (deletedCount > 0)
        {
            AnsiConsole.MarkupLine($"[dim]  Cleaned up {deletedCount} existing brands before starting[/]");
        }
        
        // Create brand helper with authenticated client
        var brandHelper = new BrandTestHelper(client);

        // Run all stages with strict chaining
        if (_shouldContinue)
            _shouldContinue = await RunCreateBrandStage(brandHelper, dbHelper);
        
        if (_shouldContinue)
            _shouldContinue = await RunBrandAuthenticationStage(brandHelper, client);
        
        if (_shouldContinue)
            _shouldContinue = await RunBrandInfoOperationsStage(brandHelper, client);
        
        if (_shouldContinue)
            _shouldContinue = await RunBrandDeletionStage(brandHelper, dbHelper);
        
        // Always cleanup even if tests failed
        await CleanupTestData(brandHelper, userHelper, dbHelper, client);

        // Show summary
        TestReporter.ShowSummary(_positivePass, _positiveFail, _negativePass, _negativeFail);

        var totalPassed = _positivePass + _negativePass;
        var totalFailed = _positiveFail + _negativeFail;
        return (totalPassed, totalFailed);
    }

    private static async Task<bool> SetupTestUser(
        UserTestHelper userHelper,
        DatabaseHelper dbHelper,
        GraphQLClient client)
    {
        TestReporter.StartStage("Stage 0: User Setup");
        
        // Scenario 0.1: Create and verify primary test user
        TestReporter.StartScenario("0.1", "Create and verify primary test user",
            "Creating primary user for brand operations");

        try
        {
            // Create primary user
            _testUser = TestUser.Generate("brand_suite");
            var (signupSuccess, verificationToken) = await userHelper.SignUpAsync(_testUser);
            if (!signupSuccess || string.IsNullOrEmpty(verificationToken))
            {
                AnsiConsole.MarkupLine($"[red]  ✗ Failed to create primary test user[/]");
                _positiveFail++;
                return false;
            }
            AnsiConsole.MarkupLine($"[green dim]  ✓ User created: {_testUser.Email}[/]");
            _positivePass++;

            // Verify email
            var verifySuccess = await userHelper.VerifyEmailAsync(verificationToken);
            if (!verifySuccess)
            {
                AnsiConsole.MarkupLine($"[red]  ✗ Failed to verify email[/]");
                _positiveFail++;
                return false;
            }
            AnsiConsole.MarkupLine($"[green dim]  ✓ Email verified[/]");
            _positivePass++;

            // Sign in
            var (signinSuccess, token) = await userHelper.SignInAsync(_testUser);
            if (!signinSuccess || string.IsNullOrEmpty(token))
            {
                AnsiConsole.MarkupLine($"[red]  ✗ Failed to sign in[/]");
                _positiveFail++;
                return false;
            }
            AnsiConsole.MarkupLine($"[green dim]  ✓ User signed in[/]");
            _positivePass++;
            
            _testUser.Token = token;
            client.SetAuthToken(token);

            // Store user ID from token (we'll use email as identifier for now)
            _userId = _testUser.Email; // Using email as user identifier
            AnsiConsole.MarkupLine($"[green dim]  ✓ Primary user setup complete: {_userId}[/]");
            _positivePass++;

            AnsiConsole.MarkupLine($"[bold green]  ✓ Primary test user ready[/]");
            
            // Scenario 0.2: Create and verify second test user
            TestReporter.StartScenario("0.2", "Create and verify second test user",
                "Creating second user for cross-user access tests");
            
            // Create second user
            _testUser2 = TestUser.Generate("brand_suite_user2");
            var (signup2Success, verify2Token) = await userHelper.SignUpAsync(_testUser2);
            if (!signup2Success || string.IsNullOrEmpty(verify2Token))
            {
                AnsiConsole.MarkupLine($"[red]  ✗ Failed to create second test user[/]");
                _positiveFail++;
                return false;
            }
            AnsiConsole.MarkupLine($"[green dim]  ✓ User 2 created: {_testUser2.Email}[/]");
            _positivePass++;

            // Verify second user's email
            var verify2Success = await userHelper.VerifyEmailAsync(verify2Token);
            if (!verify2Success)
            {
                AnsiConsole.MarkupLine($"[red]  ✗ Failed to verify second user's email[/]");
                _positiveFail++;
                return false;
            }
            AnsiConsole.MarkupLine($"[green dim]  ✓ User 2 email verified[/]");
            _positivePass++;

            // Sign in as second user to get token
            var (signin2Success, token2) = await userHelper.SignInAsync(_testUser2);
            if (!signin2Success || string.IsNullOrEmpty(token2))
            {
                AnsiConsole.MarkupLine($"[red]  ✗ Failed to sign in as second user[/]");
                _positiveFail++;
                return false;
            }
            AnsiConsole.MarkupLine($"[green dim]  ✓ User 2 signed in[/]");
            _positivePass++;
            
            _testUser2.Token = token2;
            _user2Token = token2;
            _userId2 = _testUser2.Email;
            AnsiConsole.MarkupLine($"[green dim]  ✓ Second user setup complete: {_userId2}[/]");
            _positivePass++;

            // Restore primary user token for subsequent tests
            client.SetAuthToken(token);
            AnsiConsole.MarkupLine($"[green dim]  ✓ Restored primary user token[/]");
            
            AnsiConsole.MarkupLine($"[bold green]  ✓ Both test users ready[/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]  ✗ Setup failed: {ex.Message}[/]");
            _positiveFail++;
            return false;
        }
    }

    private static async Task<bool> RunCreateBrandStage(BrandTestHelper brandHelper, DatabaseHelper dbHelper)
    {
        TestReporter.StartStage("Stage 1: Create Brand");
        bool stageSuccess = true;

        // Scenario 1.1: Create brand without authentication (negative test)
        TestReporter.StartScenario("1.1", "Unauthorized creation",
            "Expecting: Reject brand creation without authentication");
        
        var unauthorizedResult = await BrandActions.CreateBrandUnauthorized(brandHelper, brandHelper._client);
        TestReporter.ReportNegativeTest("Create brand without auth", unauthorizedResult);
        if (unauthorizedResult) _negativePass++; else { _negativeFail++; stageSuccess = false; }

        // Scenario 1.2: Create brand with invalid name (negative test)
        if (stageSuccess)
        {
            TestReporter.StartScenario("1.2", "Invalid brand name",
                "Expecting: Reject empty/null brand names");
            
            var invalidNameResult = await BrandActions.CreateBrandInvalidName(brandHelper, "");
            TestReporter.ReportNegativeTest("Create brand with empty name", invalidNameResult);
            if (invalidNameResult) _negativePass++; else { _negativeFail++; stageSuccess = false; }
        }

        // Scenario 1.3: Create valid brand (positive test)
        if (stageSuccess)
        {
            TestReporter.StartScenario("1.3", "Valid brand creation",
                "Expecting: Create brand with schema and API credentials");
            
            var brandName1 = $"TestBrand_{StringUtils.GenerateRandomAlphaNumeric(8)}";
            var (success, brandId, apiKey, apiPassword) = await BrandActions.CreateValidBrand(brandHelper, brandName1);
            
            if (!success || string.IsNullOrEmpty(brandId))
            {
                TestReporter.ReportPositiveTest("Create valid brand", false);
                _positiveFail++;
                stageSuccess = false;
            }
            else
            {
                _createdBrandIds.Add(brandId);
                TestReporter.ReportPositiveTest("Create valid brand", true, $"ID: {brandId}");
                _positivePass++;
                
                // Store primary brand credentials for reuse in subsequent stages
                _primaryBrandId = brandId;
                _primaryBrandName = brandName1;
                _primaryApiKey = apiKey;
                _primaryApiPassword = apiPassword;

                // Verify brand in database
                var brandExists = await BrandActions.VerifyBrandExists(dbHelper, brandId);
                if (brandExists) _positivePass++; else { _positiveFail++; stageSuccess = false; }

                // Verify schema exists
                if (stageSuccess)
                {
                    var brand = await dbHelper.GetBrandFromDatabaseAsync(brandId);
                    if (brand != null)
                    {
                        var schemaExists = await BrandActions.VerifySchemaExists(dbHelper, brand.SchemaName);
                        if (schemaExists) _positivePass++; else { _positiveFail++; stageSuccess = false; }
                    }
                    else
                    {
                        _positiveFail++;
                        stageSuccess = false;
                    }
                }
            }
        }

        // Scenario 1.4: Reject duplicate brand names (negative test)
        if (stageSuccess)
        {
            TestReporter.StartScenario("1.4", "Duplicate brand name rejection",
                "Expecting: Reject creating brand with duplicate name");
            
            // Try to create another brand with the same name as primary brand
            var (duplicateSuccess, _, _, _) = await BrandActions.CreateValidBrand(brandHelper, _primaryBrandName);
            TestReporter.ReportNegativeTest("Reject duplicate brand name", !duplicateSuccess);
            if (!duplicateSuccess) _negativePass++; else { _negativeFail++; stageSuccess = false; }
        }

        // Scenario 1.5: Create second brand for primary user (positive test)
        if (stageSuccess)
        {
            TestReporter.StartScenario("1.5", "Create second brand for same user",
                "Expecting: Allow user to own multiple brands");
            
            var secondBrandName = $"SecondBrand_{StringUtils.GenerateRandomAlphaNumeric(8)}";
            var (success2, brandId2, apiKey2, apiPassword2) = await BrandActions.CreateValidBrand(brandHelper, secondBrandName);
            
            if (success2 && !string.IsNullOrEmpty(brandId2))
            {
                _createdBrandIds.Add(brandId2);
                TestReporter.ReportPositiveTest("Create second brand for user", true, $"ID: {brandId2}");
                _positivePass++;
                
                // Store secondary brand credentials for later use
                _secondaryBrandId = brandId2;
                _secondaryBrandName = secondBrandName;
                _secondaryApiKey = apiKey2;
                _secondaryApiPassword = apiPassword2;
                
                // Verify second brand in database
                var brand2Exists = await BrandActions.VerifyBrandExists(dbHelper, brandId2);
                if (brand2Exists) _positivePass++; else { _positiveFail++; stageSuccess = false; }
            }
            else
            {
                _positiveFail++;
                stageSuccess = false;
            }
        }

        if (!stageSuccess)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Stage 1 failed - stopping test suite[/]");
        }
        return stageSuccess;
    }

    private static async Task<bool> RunBrandAuthenticationStage(BrandTestHelper brandHelper, GraphQLClient client)
    {
        TestReporter.StartStage("Stage 2: Brand Authentication");
        bool stageSuccess = true;

        // Reuse the brand created in Stage 1
        if (string.IsNullOrEmpty(_primaryBrandId) || string.IsNullOrEmpty(_primaryApiKey) || string.IsNullOrEmpty(_primaryApiPassword))
        {
            AnsiConsole.MarkupLine($"[red]  ✗ No brand available from Stage 1 for authentication tests[/]");
            _positiveFail++;
            return false;
        }
        
        AnsiConsole.MarkupLine($"[green dim]  ✓ Using brand from Stage 1: {_primaryBrandName} (ID: {_primaryBrandId})[/]");

        // Scenario 2.1: Sign in with wrong API key (negative test)
        TestReporter.StartScenario("2.1", "Invalid API key",
            "Expecting: Reject authentication with wrong key");
        
        var wrongKeyResult = await BrandActions.SignInBrandInvalidKey(
            brandHelper, _primaryBrandId, "wrong_key_1234567890", _primaryApiPassword!);
        TestReporter.ReportNegativeTest("Sign in with wrong API key", wrongKeyResult);
        if (wrongKeyResult) _negativePass++; else { _negativeFail++; stageSuccess = false; }

        if (stageSuccess)
        {
            // Scenario 2.2: Sign in with wrong password (negative test)
            TestReporter.StartScenario("2.2", "Wrong password",
                "Expecting: Reject authentication with wrong password");
            
            var wrongPasswordResult = await BrandActions.SignInBrandWrongPassword(
                brandHelper, _primaryBrandId, _primaryApiKey!, "wrong_password");
            TestReporter.ReportNegativeTest("Sign in with wrong password", wrongPasswordResult);
            if (wrongPasswordResult) _negativePass++; else { _negativeFail++; stageSuccess = false; }
        }

        if (stageSuccess)
        {
            // Scenario 2.3: Sign in with wrong brand ID (negative test)
            TestReporter.StartScenario("2.3", "Wrong brand ID",
                "Expecting: Reject authentication with non-existent brand");
            
            var wrongIdResult = await BrandActions.SignInBrandWrongId(
                brandHelper, "non-existent-brand-id", _primaryApiKey!, _primaryApiPassword!);
            TestReporter.ReportNegativeTest("Sign in with wrong brand ID", wrongIdResult);
            if (wrongIdResult) _negativePass++; else { _negativeFail++; stageSuccess = false; }
        }

        // Scenario 2.4: Sign in with valid credentials for primary brand (positive test)
        if (stageSuccess)
        {
            TestReporter.StartScenario("2.4", "Valid primary brand sign-in",
                "Expecting: Generate brand-context JWT token for primary brand");
            
            var (signinSuccess, brandToken) = await BrandActions.SignInBrandValid(
                brandHelper, _primaryBrandId, _primaryApiKey!, _primaryApiPassword!);
            
            if (signinSuccess && !string.IsNullOrEmpty(brandToken))
            {
                TestReporter.ReportPositiveTest("Sign in with primary brand API credentials", true);
                _positivePass++;
            }
            else
            {
                _positiveFail++;
                stageSuccess = false;
            }
        }

        // Scenario 2.5: Sign in with valid credentials for secondary brand (positive test)
        if (stageSuccess && !string.IsNullOrEmpty(_secondaryBrandId))
        {
            TestReporter.StartScenario("2.5", "Valid secondary brand sign-in",
                "Expecting: Generate brand-context JWT token for secondary brand");
            
            var (signin2Success, brand2Token) = await BrandActions.SignInBrandValid(
                brandHelper, _secondaryBrandId, _secondaryApiKey!, _secondaryApiPassword!);
            
            if (signin2Success && !string.IsNullOrEmpty(brand2Token))
            {
                TestReporter.ReportPositiveTest("Sign in with secondary brand API credentials", true);
                _positivePass++;
            }
            else
            {
                _positiveFail++;
                stageSuccess = false;
            }
        }

        if (!stageSuccess)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Stage 2 failed - stopping test suite[/]");
        }
        return stageSuccess;
    }

    private static async Task<bool> RunBrandInfoOperationsStage(BrandTestHelper brandHelper, GraphQLClient client)
    {
        TestReporter.StartStage("Stage 3: Brand Info Operations (Brand Context)");
        bool stageSuccess = true;

        // Verify both brands are available
        if (string.IsNullOrEmpty(_primaryBrandId) || string.IsNullOrEmpty(_primaryApiKey) || string.IsNullOrEmpty(_primaryApiPassword))
        {
            AnsiConsole.MarkupLine($"[red]  ✗ Primary brand not available from Stage 1[/]");
            _positiveFail++;
            return false;
        }
        
        if (string.IsNullOrEmpty(_secondaryBrandId) || string.IsNullOrEmpty(_secondaryApiKey) || string.IsNullOrEmpty(_secondaryApiPassword))
        {
            AnsiConsole.MarkupLine($"[yellow]  ⚠ Secondary brand not available - skipping cross-brand tests[/]");
        }
        
        AnsiConsole.MarkupLine($"[green dim]  ✓ Using primary brand: {_primaryBrandName} (ID: {_primaryBrandId})[/]");
        if (!string.IsNullOrEmpty(_secondaryBrandId))
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Using secondary brand: {_secondaryBrandName} (ID: {_secondaryBrandId})[/]");
        }

        // Sign in to get brand tokens
        var (signinSuccess, primaryBrandToken) = await BrandActions.SignInBrandValid(
            brandHelper, _primaryBrandId, _primaryApiKey!, _primaryApiPassword!);
        
        if (!signinSuccess || string.IsNullOrEmpty(primaryBrandToken))
        {
            AnsiConsole.MarkupLine($"[red]  ✗ Failed to sign in to primary brand[/]");
            _positiveFail++;
            return false;
        }

        // Save principal token for later
        var principalToken = client.GetAuthToken();

        // Scenario 3.1: Get brand registry info with principal token (positive test)
        TestReporter.StartScenario("3.1", "Get brand from registry with principal",
            "Expecting: Allow getting brand from registry with principal token");
        
        client.SetAuthToken(principalToken);
        var (getSuccess, brandData, _) = await brandHelper.GetBrandAsync(_primaryBrandId);
        TestReporter.ReportPositiveTest("Get brand from registry with principal token", getSuccess);
        if (getSuccess) _positivePass++; else { _positiveFail++; stageSuccess = false; }

        // Scenario 3.2: Get brand info from infoes table with principal token (negative test) 
        if (stageSuccess)
        {
            TestReporter.StartScenario("3.2", "Get brand info with principal token",
                "Expecting: Reject getting brand infoes with principal token - requires brand context");
            
            client.SetAuthToken(principalToken);
            var (getInfoSuccess, infoList) = await brandHelper.GetBrandInfoAsync();
            TestReporter.ReportNegativeTest("Reject brand info query with principal token", !getInfoSuccess);
            if (!getInfoSuccess) _negativePass++; else { _negativeFail++; stageSuccess = false; }
        }

        // Scenario 3.3: Get brand info from infoes table with brand-context token (positive test)
        if (stageSuccess)
        {
            TestReporter.StartScenario("3.3", "Get brand info with brand-context token",
                "Expecting: Successfully get brand infoes with brand-context token");
            
            client.SetAuthToken(primaryBrandToken);
            var (getInfoSuccess, infoList) = await brandHelper.GetBrandInfoAsync();
            TestReporter.ReportPositiveTest("Get brand info with brand-context token", getInfoSuccess);
            if (getInfoSuccess && infoList != null && infoList.Count > 0)
            {
                _positivePass++;
                
                // Verify "Brand Name" key exists
                var brandNameInfo = infoList.FirstOrDefault(i => i.Key == "Brand Name");
                TestReporter.ReportPositiveTest("Brand Name exists in infoes", brandNameInfo != null);
                if (brandNameInfo != null) 
                {
                    _positivePass++;
                    AnsiConsole.MarkupLine($"[green dim]    Brand Name value: {brandNameInfo.Value}[/]");
                }
                else 
                {
                    _positiveFail++;
                }
            }
            else
            {
                _positiveFail++;
                stageSuccess = false;
            }
        }

        // Scenario 3.4: Update brand info with principal token (negative test)
        if (stageSuccess)
        {
            TestReporter.StartScenario("3.4", "Update brand info with principal token",
                "Expecting: Reject updating brand infoes with principal token - requires brand context");
            
            client.SetAuthToken(principalToken);
            var (updateSuccess, _) = await brandHelper.UpdateBrandInfoAsync("TestKey", "TestValue");
            TestReporter.ReportNegativeTest("Reject update brand info with principal token", !updateSuccess);
            if (!updateSuccess) _negativePass++; else { _negativeFail++; stageSuccess = false; }
        }

        // Scenario 3.5: Update brand info with brand-context token (positive test)
        if (stageSuccess)
        {
            TestReporter.StartScenario("3.5", "Update brand info with brand-context token",
                "Expecting: Successfully update brand infoes with brand-context token");
            
            client.SetAuthToken(primaryBrandToken);
            var testKey = "TestConfig";
            var testValue = $"Value_{StringUtils.GenerateRandomAlphaNumeric(8)}";
            var (updateSuccess, message) = await brandHelper.UpdateBrandInfoAsync(testKey, testValue);
            TestReporter.ReportPositiveTest("Update brand info with brand-context token", updateSuccess);
            if (updateSuccess)
            {
                _positivePass++;
                
                // Verify the value was updated
                var (verifySuccess, infoList) = await brandHelper.GetBrandInfoAsync();
                if (verifySuccess && infoList != null)
                {
                    var updatedInfo = infoList.FirstOrDefault(i => i.Key == testKey);
                    TestReporter.ReportPositiveTest("Verify updated value", updatedInfo?.Value == testValue);
                    if (updatedInfo?.Value == testValue) _positivePass++; else _positiveFail++;
                }
            }
            else
            {
                _positiveFail++;
                stageSuccess = false;
            }
        }

        // Scenario 3.6: Update Brand Name in infoes (positive test) 
        if (stageSuccess)
        {
            TestReporter.StartScenario("3.6", "Update Brand Name in infoes table",
                "Expecting: Successfully update Brand Name in infoes (display name change)");
            
            client.SetAuthToken(primaryBrandToken);
            var newDisplayName = $"NewDisplay_{StringUtils.GenerateRandomAlphaNumeric(8)}";
            var (updateNameSuccess, _) = await brandHelper.UpdateBrandInfoAsync("Brand Name", newDisplayName);
            TestReporter.ReportPositiveTest("Update Brand Name in infoes", updateNameSuccess);
            if (updateNameSuccess) _positivePass++; else { _positiveFail++; stageSuccess = false; }
        }

        // Scenario 3.7: Cross-brand info access - Try to access secondary brand info with primary brand token (negative)
        if (stageSuccess && !string.IsNullOrEmpty(_secondaryBrandId))
        {
            TestReporter.StartScenario("3.7", "Cross-brand info access rejection",
                "Expecting: Cannot access other brand's info with different brand token");
            
            // Sign in to secondary brand
            var (signin2Success, secondaryBrandToken) = await BrandActions.SignInBrandValid(
                brandHelper, _secondaryBrandId, _secondaryApiKey!, _secondaryApiPassword!);
            
            if (signin2Success && !string.IsNullOrEmpty(secondaryBrandToken))
            {
                // Use secondary brand token
                client.SetAuthToken(secondaryBrandToken);
                
                // Add a key to secondary brand
                var (addSuccess, _) = await brandHelper.UpdateBrandInfoAsync("SecondaryKey", "SecondaryValue");
                if (addSuccess) _positivePass++;
                
                // Now try to use primary brand token to access secondary brand's info
                // This should fail because brand tokens are scoped to their specific brand
                client.SetAuthToken(primaryBrandToken);
                var (getOtherBrandInfo, otherInfoList) = await brandHelper.GetBrandInfoAsync();
                
                // This should succeed but return primary brand's info, not secondary's
                if (getOtherBrandInfo && otherInfoList != null)
                {
                    // Check that we don't see SecondaryKey (it belongs to secondary brand)
                    var hasSecondaryKey = otherInfoList.Any(i => i.Key == "SecondaryKey");
                    TestReporter.ReportNegativeTest("Primary token cannot see secondary brand info", !hasSecondaryKey);
                    if (!hasSecondaryKey) _negativePass++; else { _negativeFail++; stageSuccess = false; }
                }
            }
        }

        // Scenario 3.8: Delete brand with principal token (positive test)
        // Note: Brand names in registry are immutable, deletion is allowed for owners
        if (stageSuccess)
        {
            TestReporter.StartScenario("3.8", "Verify brand registry immutability",
                "Expecting: Brand names in registry cannot be updated (immutable)");
            
            // Since updateBrandName mutation was removed, we just verify the concept
            client.SetAuthToken(principalToken);
            
            // Get the brand to verify its name hasn't changed in registry
            var (getRegistrySuccess, registryBrand, _) = await brandHelper.GetBrandAsync(_primaryBrandId);
            if (getRegistrySuccess && registryBrand.HasValue)
            {
                // The name in registry should still be the original
                var brandName = registryBrand.Value.GetProperty("name").GetString();
                TestReporter.ReportPositiveTest("Brand name in registry is immutable", 
                    brandName == _primaryBrandName);
                if (brandName == _primaryBrandName) _positivePass++; else _positiveFail++;
            }
        }

        // Always restore principal token for next tests
        client.SetAuthToken(principalToken);
        
        if (!stageSuccess)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Stage 3 failed - stopping test suite[/]");
        }
        return stageSuccess;
    }


    private static async Task<bool> RunBrandDeletionStage(BrandTestHelper brandHelper, DatabaseHelper dbHelper)
    {
        TestReporter.StartStage("Stage 4: Delete Brand");
        bool stageSuccess = true;

        // Use the primary brand from Stage 1 for deletion test
        if (string.IsNullOrEmpty(_primaryBrandId))
        {
            AnsiConsole.MarkupLine($"[red]  ✗ No brand available from Stage 1 for deletion test[/]");
            _positiveFail++;
            return false;
        }
        
        AnsiConsole.MarkupLine($"[green dim]  ✓ Using brand from Stage 1: {_primaryBrandName} (ID: {_primaryBrandId})[/]");

        // Get schema name before deletion
        var brand = await dbHelper.GetBrandFromDatabaseAsync(_primaryBrandId);
        var schemaName = brand?.SchemaName;

        // Scenario 4.1: Delete non-existent brand (negative test)
        TestReporter.StartScenario("4.1", "Delete non-existent brand",
            "Expecting: Handle deletion of non-existent brand gracefully");
        
        var (nonExistentDelete, _) = await brandHelper.DeleteBrandAsync("non-existent-brand-id");
        TestReporter.ReportNegativeTest("Delete non-existent brand", !nonExistentDelete);
        if (!nonExistentDelete) _negativePass++; else { _negativeFail++; stageSuccess = false; }

        if (stageSuccess)
        {
            // Scenario 4.2: Delete brand without authentication (negative test)
            TestReporter.StartScenario("4.2", "Delete without auth",
                "Expecting: Reject deletion without authentication");
            
            var unauthorizedDelete = await BrandActions.DeleteBrandUnauthorized(
                brandHelper, brandHelper._client, _primaryBrandId);
            TestReporter.ReportNegativeTest("Delete brand without auth", unauthorizedDelete);
            if (unauthorizedDelete) _negativePass++; else { _negativeFail++; stageSuccess = false; }
        }

        if (stageSuccess)
        {
            // Scenario 4.3: Delete brand with valid authorization (positive test)
            TestReporter.StartScenario("4.3", "Valid brand deletion",
                "Expecting: Delete brand and all related data");
            
            var deleteSuccess = await BrandActions.DeleteBrandValid(brandHelper, _primaryBrandId);
            TestReporter.ReportPositiveTest("Delete brand", deleteSuccess);
            if (deleteSuccess) _positivePass++; else { _positiveFail++; stageSuccess = false; }

            if (deleteSuccess)
            {
                // Verify brand no longer exists
                var brandDeleted = !(await dbHelper.BrandExistsAsync(_primaryBrandId));
                TestReporter.ReportPositiveTest("Brand removed from database", brandDeleted);
                if (brandDeleted) _positivePass++; else { _positiveFail++; stageSuccess = false; }

                // Verify schema no longer exists
                if (!string.IsNullOrEmpty(schemaName) && stageSuccess)
                {
                    var schemaDeleted = !(await dbHelper.SchemaExistsAsync(schemaName));
                    TestReporter.ReportPositiveTest("Schema removed", schemaDeleted);
                    if (schemaDeleted) _positivePass++; else { _positiveFail++; stageSuccess = false; }
                }
                
                // Remove from created brands list since it's deleted
                _createdBrandIds.Remove(_primaryBrandId);
            }
        }

        if (!stageSuccess)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Stage 4 failed[/]");
        }
        return stageSuccess;
    }

    private static async Task CleanupTestData(
        BrandTestHelper brandHelper,
        UserTestHelper userHelper,
        DatabaseHelper dbHelper,
        GraphQLClient client)
    {
        TestReporter.StartStage("Stage 5: Cleanup & Cascade Deletion Test");
        
        // Scenario 5.1: Test cascade deletion when user is deleted
        TestReporter.StartScenario("5.1", "Cascade deletion test",
            "Expecting: When user is deleted, all their brands are automatically deleted");
        
        // Verify secondary brand still exists before user deletion
        if (!string.IsNullOrEmpty(_secondaryBrandId))
        {
            var brandExistsBefore = await dbHelper.BrandExistsAsync(_secondaryBrandId);
            TestReporter.ReportPositiveTest("Secondary brand exists before user deletion", brandExistsBefore);
            if (brandExistsBefore) _positivePass++; else _positiveFail++;
            
            // Get schema name for later verification
            var brand = await dbHelper.GetBrandFromDatabaseAsync(_secondaryBrandId);
            var schemaName = brand?.SchemaName;
            
            // Delete the primary test user
            if (_testUser != null && !string.IsNullOrEmpty(_testUser.Email))
            {
                AnsiConsole.MarkupLine($"[dim]  Deleting primary user: {_testUser.Email}[/]");
                
                // Sign in as user to delete account
                var (signinSuccess, token) = await userHelper.SignInAsync(_testUser);
                if (signinSuccess && !string.IsNullOrEmpty(token))
                {
                    client.SetAuthToken(token);
                    
                    var deleteSuccess = await userHelper.DeleteAccountAsync(_testUser.Password);
                    TestReporter.ReportPositiveTest("Primary user deleted", deleteSuccess);
                    if (deleteSuccess) _positivePass++; else _positiveFail++;
                    
                    if (deleteSuccess)
                    {
                        // Verify all user's brands are deleted (cascade deletion)
                        var brandExistsAfter = await dbHelper.BrandExistsAsync(_secondaryBrandId);
                        TestReporter.ReportPositiveTest("Secondary brand deleted by cascade", !brandExistsAfter);
                        if (!brandExistsAfter) _positivePass++; else _positiveFail++;
                        
                        // Verify schema is also deleted
                        if (!string.IsNullOrEmpty(schemaName))
                        {
                            var schemaExists = await dbHelper.SchemaExistsAsync(schemaName);
                            TestReporter.ReportPositiveTest("Brand schema deleted by cascade", !schemaExists);
                            if (!schemaExists) _positivePass++; else _positiveFail++;
                        }
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]  ⚠ Could not sign in to delete primary user[/]");
                    _positiveFail++;
                }
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]  ⚠ No secondary brand available for cascade deletion test[/]");
        }
        
        // Scenario 5.2: Clean up second test user
        TestReporter.StartScenario("5.2", "Clean up second user",
            "Expecting: Successfully delete second test user");
        
        if (_testUser2 != null && !string.IsNullOrEmpty(_testUser2.Email))
        {
            try
            {
                AnsiConsole.MarkupLine($"[dim]  Cleaning up second test user: {_testUser2.Email}[/]");
                
                // Sign in as second user to delete account
                var (signin2Success, token2) = await userHelper.SignInAsync(_testUser2);
                if (signin2Success && !string.IsNullOrEmpty(token2))
                {
                    client.SetAuthToken(token2);
                    
                    var delete2Success = await userHelper.DeleteAccountAsync(_testUser2.Password);
                    TestReporter.ReportPositiveTest("Second user deleted", delete2Success);
                    if (delete2Success) _positivePass++; else _positiveFail++;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]  ⚠ Could not sign in to delete second user[/]");
                    _positiveFail++;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]  ⚠ Error cleaning up second user: {ex.Message}[/]");
                _positiveFail++;
            }
        }
        
        // Clean up any remaining brands (shouldn't be any if cascade works properly)
        if (_createdBrandIds.Count > 0)
        {
            AnsiConsole.MarkupLine($"[dim]  Cleaning up {_createdBrandIds.Count} remaining brands...[/]");
            foreach (var brandId in _createdBrandIds)
            {
                try
                {
                    await BrandActions.CleanupBrand(brandHelper, brandId);
                    AnsiConsole.MarkupLine($"[dim]  Cleaned up brand: {brandId}[/]");
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        AnsiConsole.MarkupLine($"[dim]  ✓ Cleanup complete[/]");
    }
}