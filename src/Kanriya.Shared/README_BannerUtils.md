# BannerUtils Usage Examples

The `BannerUtils` class provides consistent banner and metadata display across all subprojects with fancy ASCII art powered by Spectre.Console.

## Available Methods

### 1. `DisplayAppBanner(Assembly, string?)`
Displays a complete application banner with all metadata:

```csharp
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayAppBanner(assembly, "GraphQL API Server with Authentication");
```

Output:
```
==================================================
KANRIYA SERVER
==================================================
Version: 1.0.0 (Build 1.0.0.0)
App ID: com.deckyfx.kanriya.server
Codename: Development
Build Date: 2025-08-29

GraphQL API Server with Authentication
==================================================
```

### 2. `DisplaySimpleBanner(Assembly)`
Displays a minimal banner with just name and version:

```csharp
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplaySimpleBanner(assembly);
```

Output:
```
Kanriya Server v1.0.0
```

### 3. `DisplayAppInfo(Assembly)`
Displays metadata in a structured format:

```csharp
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayAppInfo(assembly);
```

Output:
```
Application Information:
  Name: Kanriya Server
  ID: com.deckyfx.kanriya.server
  Version: 1.0.0 (Build 1.0.0.0)
  Codename: Development
  Build Date: 2025-08-29
```

### 4. `DisplayMetadataOnly(Assembly)`
Displays metadata after custom banner (useful with Spectre.Console):

```csharp
// Your custom banner code here (e.g., Figlet text)
AnsiConsole.Write(new FigletText("My App").Centered());

// Then display metadata using BannerUtils
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayMetadataOnly(assembly);
```

### 5. `GetAppMetadata(Assembly)`
Returns metadata as dictionary for custom display:

```csharp
var assembly = Assembly.GetExecutingAssembly();
var metadata = BannerUtils.GetAppMetadata(assembly);

foreach (var kvp in metadata)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

### 6. `CreateBannerString(Assembly, string?)`
Creates banner string without displaying it:

```csharp
var assembly = Assembly.GetExecutingAssembly();
var bannerString = BannerUtils.CreateBannerString(assembly, "Additional info");
// Use bannerString with logging framework or other output mechanism
```

### 7. `DisplayStartupInfo(Assembly, string?)`
Complete startup information with environment details:

```csharp
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayStartupInfo(assembly, "Production Environment");
```

### 8. `DisplayFancyBanner(Assembly, string?, Color?)` ⭐ NEW!
Display fancy ASCII art banner using Spectre.Console with Figlet text:

```csharp
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayFancyBanner(assembly, "GraphQL API Server", Color.Cyan1);
```

Output: Beautiful ASCII art with colored panels showing all metadata!

### 9. `DisplayCompactFancyBanner(Assembly, Color?)`
Display compact fancy banner with smaller styling:

```csharp
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayCompactFancyBanner(assembly, Color.Green);
```

### 10. `DisplayFancyMetadata(Assembly, string?, Color?)`
Display fancy metadata panel using Spectre.Console:

```csharp
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayFancyMetadata(assembly, "App Info", Color.Cyan1);
```

## Integration Examples

### Server Project (with Fancy ASCII Art!)
```csharp
// In Program.cs
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayFancyBanner(assembly, "GraphQL API Server with Authentication and Real-time Subscriptions", Color.Cyan1);
```

### Tests Project (with Fancy ASCII Art!)
```csharp
// In Program.cs - replaces old Spectre.Console code
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplayFancyBanner(assembly, "Comprehensive Test Suite for Kanriya Server", Color.Blue);
```

### Console Application
```csharp
// Simple console app
var assembly = Assembly.GetExecutingAssembly();
BannerUtils.DisplaySimpleBanner(assembly);
```

## Benefits

- **Consistent Formatting**: All subprojects use the same banner style
- **No Code Duplication**: Single source of truth for banner logic
- **Flexible**: Multiple methods for different use cases
- **Framework Agnostic**: Works with plain Console, Serilog, Spectre.Console, etc.
- **Metadata Access**: Centralized access to build metadata across all projects