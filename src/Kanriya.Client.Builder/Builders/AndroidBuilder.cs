using System.Diagnostics;
using Spectre.Console;

namespace Kanriya.Client.Builder.Builders;

public class AndroidBuilder : BasePlatformBuilder
{
    public override string PlatformName => "Android APK";
    public override string[] Aliases => new[] { "android", "apk" };
    
    public override async Task<bool> CanBuildAsync()
    {
        return await CheckAndroidWorkload();
    }
    
    public override async Task BuildAsync(bool launch, bool skipZip, ProgressTask? task = null)
    {
        AnsiConsole.MarkupLine("🔨 [bold]Building Android APK...[/]");
        task?.Increment(10);
        
        if (!await CheckAndroidWorkload())
        {
            AnsiConsole.MarkupLine("[red]❌ Android workload not installed.[/]");
            AnsiConsole.MarkupLine("[yellow]Run: dotnet workload install maui-android[/]");
            return;
        }
        
        task?.Increment(10);
        
        var outputDir = Path.Combine(PublishDir, "android");
        
        await RunDotnetCommand($"publish \"{Path.Combine(ClientProject, "Kanriya.Client.Avalonia.Android", "Kanriya.Client.Avalonia.Android.csproj")}\" " +
            $"-c Release -o \"{outputDir}\"");
        
        task?.Increment(60);
        
        var apkPath = FindApk();
        if (apkPath != null)
        {
            var destinationApk = Path.Combine(PublishDir, "Kanriya.apk");
            File.Copy(apkPath, destinationApk, true);
            
            AnsiConsole.MarkupLine($"✅ [green]Android APK ready: {destinationApk}[/]");
            task?.Increment(100 - (task?.Value ?? 0));
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ APK built but signed version not found[/]");
            task?.Increment(100 - (task?.Value ?? 0));
        }
    }
    
    public override async Task LaunchAsync(bool publish = false)
    {
        var destinationApk = Path.Combine(PublishDir, "Kanriya.apk");
        
        if (!File.Exists(destinationApk))
        {
            AnsiConsole.MarkupLine("[red]❌ Android APK not found. Build Android platform first.[/]");
            return;
        }
        
        if (AnsiConsole.Confirm("📱 [bold]Install and launch on device/emulator?[/]", true))
        {
            await LaunchAndroidApp(destinationApk);
        }
    }
    
    private static async Task<bool> CheckAndroidWorkload()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "workload list",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            return output.Contains("maui-android") || output.Contains("android");
        }
        catch
        {
            return false;
        }
    }
    
    private static string? FindApk()
    {
        try
        {
            var searchDirs = new[]
            {
                Path.Combine(ClientProject, "Kanriya.Client.Avalonia.Android", "bin"),
                Path.Combine(PublishDir, "android")
            };
            
            foreach (var dir in searchDirs)
            {
                if (Directory.Exists(dir))
                {
                    var apkFiles = Directory.GetFiles(dir, "*.apk", SearchOption.AllDirectories)
                        .Where(f => !f.Contains("unsigned"))
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .ToList();
                    
                    if (apkFiles.Any())
                    {
                        return apkFiles.First();
                    }
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    private static async Task LaunchAndroidApp(string apkPath)
    {
        try
        {
            // First, check for connected devices/emulators
            var connectedDevices = await GetConnectedAndroidDevices();
            string selectedDevice;
            
            if (connectedDevices.Any())
            {
                // Use already connected device/emulator
                if (connectedDevices.Count == 1)
                {
                    selectedDevice = connectedDevices.First().Key;
                    AnsiConsole.MarkupLine($"🤖 [blue]Using connected device: {connectedDevices.First().Value}[/]");
                }
                else
                {
                    var choices = connectedDevices.Select(d => $"{d.Value} ({d.Key})").ToList();
                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("🤖 [bold]Select Android device:[/]")
                            .AddChoices(choices));
                    
                    selectedDevice = connectedDevices.First(d => choice.Contains(d.Key)).Key;
                }
            }
            else
            {
                // No connected devices, get all available AVDs
                var availableAvds = await GetAvailableAndroidAvds();
                
                if (!availableAvds.Any())
                {
                    AnsiConsole.MarkupLine("[red]❌ No Android devices or AVDs found[/]");
                    AnsiConsole.MarkupLine("[yellow]Create an AVD using Android Studio first[/]");
                    return;
                }
                
                AnsiConsole.MarkupLine("[yellow]⚠️ No Android devices are currently connected[/]");
                
                // Let user choose which AVD to boot
                if (availableAvds.Count == 1)
                {
                    var avdName = availableAvds.First();
                    AnsiConsole.MarkupLine($"🤖 [blue]Starting emulator: {avdName}[/]");
                    selectedDevice = await StartAndroidEmulator(avdName);
                }
                else
                {
                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("🤖 [bold]Select Android Virtual Device to boot:[/]")
                            .AddChoices(availableAvds));
                    
                    selectedDevice = await StartAndroidEmulator(choice);
                }
                
                if (string.IsNullOrEmpty(selectedDevice))
                {
                    AnsiConsole.MarkupLine("[red]❌ Failed to start Android emulator[/]");
                    return;
                }
            }
            
            // Install APK on selected device
            AnsiConsole.MarkupLine("📦 [blue]Installing APK...[/]");
            var installProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {selectedDevice} install -r \"{apkPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            installProcess.Start();
            await installProcess.WaitForExitAsync();
            
            if (installProcess.ExitCode == 0)
            {
                AnsiConsole.MarkupLine("✅ [green]APK installed![/]");
                
                // Launch app on selected device
                var launchProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = $"-s {selectedDevice} shell am start -n com.kanriya.client/.MainActivity",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                launchProcess.Start();
                await launchProcess.WaitForExitAsync();
                
                AnsiConsole.MarkupLine("🚀 [green]App launched![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]❌ Failed to install APK[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not launch Android app: {ex.Message}[/]");
        }
    }
    
    private static async Task<Dictionary<string, string>> GetConnectedAndroidDevices()
    {
        var devices = new Dictionary<string, string>();
        
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "devices -l",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            var lines = output.Split('\n')
                .Where(line => line.Contains('\t') && !line.Contains("List of devices") && line.Contains("device"))
                .ToList();
            
            foreach (var line in lines)
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var deviceId = parts[0].Trim();
                    var deviceInfo = parts.Length > 2 ? parts[2].Trim() : "Unknown Device";
                    
                    // Extract device name from model info
                    if (deviceInfo.Contains("model:"))
                    {
                        var modelStart = deviceInfo.IndexOf("model:") + 6;
                        var modelEnd = deviceInfo.IndexOf(" ", modelStart);
                        if (modelEnd == -1) modelEnd = deviceInfo.Length;
                        var model = deviceInfo.Substring(modelStart, modelEnd - modelStart);
                        devices[deviceId] = model.Replace("_", " ");
                    }
                    else
                    {
                        devices[deviceId] = deviceId.StartsWith("emulator-") ? "Android Emulator" : "Android Device";
                    }
                }
            }
        }
        catch
        {
            // Return empty dictionary on error
        }
        
        return devices;
    }
    
    private static async Task<List<string>> GetAvailableAndroidAvds()
    {
        var avds = new List<string>();
        
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "emulator",
                    Arguments = "-list-avds",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            avds = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        catch
        {
            // Return empty list on error
        }
        
        return avds;
    }
    
    private static async Task<string> StartAndroidEmulator(string avdName)
    {
        try
        {
            // Start emulator in background
            var emulatorProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "emulator",
                    Arguments = $"-avd {avdName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            emulatorProcess.Start();
            AnsiConsole.MarkupLine($"🚀 [blue]Starting {avdName}... (this may take a moment)[/]");
            
            // Wait for emulator to appear in adb devices
            for (int i = 0; i < 60; i++) // Wait up to 60 seconds
            {
                await Task.Delay(1000);
                
                var devices = await GetConnectedAndroidDevices();
                var emulatorDevice = devices.FirstOrDefault(d => d.Key.StartsWith("emulator-"));
                
                if (emulatorDevice.Key != null)
                {
                    // Wait a bit more for the emulator to fully boot
                    AnsiConsole.MarkupLine("[dim]Waiting for emulator to finish booting...[/]");
                    await Task.Delay(3000);
                    
                    // Check if boot is completed
                    var bootCheck = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "adb",
                            Arguments = $"-s {emulatorDevice.Key} shell getprop sys.boot_completed",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    
                    try
                    {
                        bootCheck.Start();
                        var bootOutput = await bootCheck.StandardOutput.ReadToEndAsync();
                        await bootCheck.WaitForExitAsync();
                        
                        if (bootOutput.Trim() == "1")
                        {
                            AnsiConsole.MarkupLine("✅ [green]Emulator is ready![/]");
                            return emulatorDevice.Key;
                        }
                    }
                    catch
                    {
                        // Continue waiting
                    }
                }
            }
            
            AnsiConsole.MarkupLine("[yellow]⚠️ Emulator may still be starting up...[/]");
            
            // Return the first emulator device found, even if not fully booted
            var currentDevices = await GetConnectedAndroidDevices();
            var emulator = currentDevices.FirstOrDefault(d => d.Key.StartsWith("emulator-"));
            return emulator.Key ?? "";
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not start emulator: {ex.Message}[/]");
            return "";
        }
    }
}