using Spectre.Console;

namespace Kanriya.Tests.Helpers;

/// <summary>
/// Test result reporter with clear visual indicators
/// </summary>
public static class TestReporter
{
    // Test result symbols
    private const string PASS = "✅";           // Test passed as expected
    private const string FAIL = "❌";           // Test failed unexpectedly
    private const string REJECT = "🚫";         // Correctly rejected invalid input (negative test pass)
    private const string ACCEPT = "⚠️";         // Incorrectly accepted invalid input (negative test fail)
    private const string INFO = "ℹ️";           // Information/setup step
    private const string TODO = "📝";           // Not implemented yet
    private const string CLEANUP = "🧹";        // Cleanup action
    
    /// <summary>
    /// Report a positive test that should succeed
    /// </summary>
    public static void ReportPositiveTest(string action, bool success, string? details = null)
    {
        if (success)
        {
            // Green text + Green checkmark (test passed as expected)
            AnsiConsole.MarkupLine($"  {PASS} [green]{action}[/]");
            if (details != null)
                AnsiConsole.MarkupLine($"      [green dim]{details}[/]");
        }
        else
        {
            // Green text + Red X (test failed but text stays green)
            AnsiConsole.MarkupLine($"  {FAIL} [green]{action} - FAILED[/]");
            if (details != null)
                AnsiConsole.MarkupLine($"      [red]{details}[/]");
        }
    }
    
    /// <summary>
    /// Report a negative test that should fail/reject
    /// </summary>
    public static void ReportNegativeTest(string action, bool correctlyRejected, string? reason = null)
    {
        if (correctlyRejected)
        {
            // Red text (showing rejection) + Green checkmark (test passed)
            AnsiConsole.MarkupLine($"  {PASS} [red]{action} → Rejected[/]");
            if (reason != null)
                AnsiConsole.MarkupLine($"      [red dim]{reason}[/]");
        }
        else
        {
            // Green text (showing acceptance) + Red X (test failed)  
            AnsiConsole.MarkupLine($"  {FAIL} [green bold]{action} → Accepted (SHOULD REJECT!)[/]");
            if (reason != null)
                AnsiConsole.MarkupLine($"      [red]System incorrectly accepted this input[/]");
        }
    }
    
    /// <summary>
    /// Report an informational/setup step
    /// </summary>
    public static void ReportInfo(string message)
    {
        AnsiConsole.MarkupLine($"  {INFO} [dim]{message}[/]");
    }
    
    /// <summary>
    /// Report a cleanup action
    /// </summary>
    public static void ReportCleanup(string message)
    {
        AnsiConsole.MarkupLine($"  {CLEANUP} [dim]{message}[/]");
    }
    
    /// <summary>
    /// Report a TODO/not implemented feature
    /// </summary>
    public static void ReportTodo(string feature)
    {
        AnsiConsole.MarkupLine($"  {TODO} [yellow dim]TODO: {feature}[/]");
    }
    
    /// <summary>
    /// Start a scenario with clear formatting
    /// </summary>
    public static void StartScenario(string number, string title, string? description = null)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold blue]Scenario {number}:[/] [yellow]{title}[/]");
        if (description != null)
            AnsiConsole.MarkupLine($"[dim]  {description}[/]");
    }
    
    /// <summary>
    /// Start a stage
    /// </summary>
    public static void StartStage(string stageName)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule($"[bold yellow]{stageName}[/]")
            .LeftJustified()
            .RuleStyle(Style.Parse("dim"));
        AnsiConsole.Write(rule);
    }
    
    /// <summary>
    /// Create a summary table with clear categories
    /// </summary>
    public static void ShowSummary(int positivePass, int positiveFail, int negativePass, int negativeFail)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Test Results Summary[/]"));
        
        var table = new Table();
        table.AddColumn("Category");
        table.AddColumn("Result");
        table.AddColumn("Count");
        
        // Positive tests (should succeed)
        table.AddRow(
            "[blue]Positive Tests[/]",
            positivePass > 0 ? $"{PASS} Passed" : "",
            $"[green]{positivePass}[/]"
        );
        
        if (positiveFail > 0)
        {
            table.AddRow(
                "",
                $"{FAIL} Failed",
                $"[red bold]{positiveFail}[/]"
            );
        }
        
        table.AddEmptyRow();
        
        // Negative tests (should reject/fail)
        table.AddRow(
            "[blue]Negative Tests[/]",
            negativePass > 0 ? $"{REJECT} Correctly Rejected" : "",
            $"[green]{negativePass}[/]"
        );
        
        if (negativeFail > 0)
        {
            table.AddRow(
                "",
                $"{ACCEPT} Incorrectly Accepted",
                $"[red bold]{negativeFail}[/]"
            );
        }
        
        table.AddEmptyRow();
        
        // Totals
        var totalTests = positivePass + positiveFail + negativePass + negativeFail;
        var totalPassed = positivePass + negativePass;
        var totalFailed = positiveFail + negativeFail;
        var successRate = totalTests > 0 ? (totalPassed * 100.0 / totalTests) : 0;
        
        table.AddRow(
            "[bold]Total[/]",
            "",
            $"[bold]{totalTests}[/]"
        );
        
        table.AddRow(
            "[bold]Success Rate[/]",
            totalFailed == 0 ? "[green bold]ALL PASSED[/]" : $"[yellow]{successRate:F1}%[/]",
            totalFailed > 0 ? $"[red]({totalFailed} issues)[/]" : "[green](Perfect!)[/]"
        );
        
        AnsiConsole.Write(table);
        
        // Final verdict
        AnsiConsole.WriteLine();
        if (totalFailed == 0)
        {
            AnsiConsole.Write(new Panel($"{PASS} [green bold]All {totalTests} tests passed![/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Green));
        }
        else if (positiveFail == 0 && negativeFail > 0)
        {
            AnsiConsole.Write(new Panel($"{ACCEPT} [yellow bold]System is too permissive: {negativeFail} validation(s) missing[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Yellow));
        }
        else if (positiveFail > 0 && negativeFail == 0)
        {
            AnsiConsole.Write(new Panel($"{FAIL} [red bold]System has {positiveFail} functional failure(s)[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Red));
        }
        else
        {
            AnsiConsole.Write(new Panel($"{FAIL} [red bold]{positiveFail} functional failures, {negativeFail} validation issues[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Red));
        }
    }
}