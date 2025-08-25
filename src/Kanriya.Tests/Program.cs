using Kanriya.Tests.Helpers;
using Kanriya.Tests.Suites;
using Kanriya.Tests.Tests;
using Spectre.Console;

namespace Kanriya.Tests;

class Program
{
    private static GraphQLClient graphQLClient = null!;
    private static UserTestHelper userHelper = null!;
    private static DatabaseHelper dbHelper = null!;
    private static string baseUrl = null!;

    static async Task Main(string[] args)
    {
        // Load environment variables
        LoadEnvironment();
        
        // Setup
        var port = Environment.GetEnvironmentVariable("SERVER_LISTEN_PORT") ?? "10000";
        baseUrl = $"http://localhost:{port}";
        var serverUrl = $"{baseUrl}/graphql";
        
        var dbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "10005";
        var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "kanriya";
        var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "kanriya";
        var dbPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "kanriya";
        var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
        
        // Initialize helpers
        graphQLClient = new GraphQLClient(serverUrl);
        userHelper = new UserTestHelper(graphQLClient, baseUrl);
        dbHelper = new DatabaseHelper(connectionString);

        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Kanriya Tests")
            .Centered()
            .Color(Color.Blue));
        AnsiConsole.WriteLine();

        // Run test suites based on argument
        var suite = args.Length > 0 ? args[0].ToLower() : "all";
        
        int totalPassed = 0;
        int totalFailed = 0;
        
        switch (suite)
        {
            case "auth":
            case "auth-full":
                // Run comprehensive auth suite with improved visual reporting
                var (authFullPassed, authFullFailed) = await AuthTestSuite.RunAsync(userHelper, dbHelper, graphQLClient);
                totalPassed += authFullPassed;
                totalFailed += authFullFailed;
                break;
                
            case "auth-simple":
                // Run simple auth test (old version for quick testing)
                var (authPassed, authFailed) = await UserAuthorizationTest.RunAsync(userHelper, dbHelper, graphQLClient);
                totalPassed += authPassed;
                totalFailed += authFailed;
                break;
                
            case "brand":
                // Run brand management tests
                var (brandPassed, brandFailed) = await BrandManagementTest.RunAsync(userHelper, dbHelper, graphQLClient);
                totalPassed += brandPassed;
                totalFailed += brandFailed;
                break;
                
            default:
                // Run all test suites
                AnsiConsole.MarkupLine("[yellow]Running all test suites...[/]");
                AnsiConsole.WriteLine();
                
                // Auth suite with improved visual reporting
                var (authP, authF) = await AuthTestSuite.RunAsync(userHelper, dbHelper, graphQLClient);
                totalPassed += authP;
                totalFailed += authF;
                
                // Brand suite (uses actions from auth for setup)
                var (brandP, brandF) = await BrandManagementTest.RunAsync(userHelper, dbHelper, graphQLClient);
                totalPassed += brandP;
                totalFailed += brandF;
                break;
        }

        // Show summary
        ShowTestSummary(totalPassed, totalFailed);
        
        Environment.Exit(totalFailed > 0 ? 1 : 0);
    }

    static void ShowTestSummary(int passed, int failed)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Test Summary[/]"));
        
        var total = passed + failed;
        var successRate = total > 0 ? (passed * 100.0 / total) : 0;
        
        // Create summary panel
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        
        grid.AddRow("[blue]Total Tests:[/]", $"[blue]{total}[/]");
        grid.AddRow("[green]Passed:[/]", $"[green]{passed}[/]");
        grid.AddRow("[red]Failed:[/]", $"[red]{failed}[/]");
        grid.AddRow("[yellow]Success Rate:[/]", $"[yellow]{successRate:F1}%[/]");
        
        var panel = new Panel(grid)
            .Header("[bold]Results[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(successRate >= 80 ? Color.Green : Color.Red);
        
        AnsiConsole.Write(panel);
        
        AnsiConsole.WriteLine();
        
        // Final message
        if (failed == 0)
        {
            AnsiConsole.Write(new Panel("[green bold]🎉 All tests passed![/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Green));
        }
        else if (successRate >= 80)
        {
            AnsiConsole.Write(new Panel($"[yellow bold]⚠️ {failed} test(s) failed but {successRate:F1}% passed[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Yellow));
        }
        else
        {
            AnsiConsole.Write(new Panel($"[red bold]❌ {failed} test(s) failed ({successRate:F1}% success rate)[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Red));
        }
    }

    static void LoadEnvironment()
    {
        var envPath = ".env";
        for (int i = 0; i <= 3 && !File.Exists(envPath); i++)
        {
            envPath = Path.Combine(string.Join("/", Enumerable.Repeat("..", i)), ".env");
        }

        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim().Trim('"');
                    if (key != "UID" && key != "GID")
                        Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }
}