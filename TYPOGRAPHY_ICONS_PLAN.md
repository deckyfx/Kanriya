# Kanriya Client Typography & Icons System Plan

## Overview
Implement **offline-first** typography and icon system for Kanriya Client with full embedded resources, cross-platform consistency, and zero external dependencies.

## Design Philosophy
**OFFLINE-FIRST**: All fonts, icons, and design assets must be embedded in the application. No external CDN dependencies, no internet requirements for UI rendering.

## Typography Strategy
- **Primary Font**: Inter (Google Font - downloaded and embedded)
- **Monospace Font**: Roboto Mono (Google Font - downloaded and embedded)  
- **Icon Font**: Material Icons (Google Icons - downloaded and embedded)
- **Fallback Strategy**: System fonts as emergency fallbacks only
- **Distribution**: All fonts embedded as Avalonia resources
- **Offline Support**: 100% functional without internet connection

## Icon Strategy
- **Primary Icons**: Material Design Icons (SVG files embedded)
- **Icon Font**: Material Icons font (embedded for fallback)
- **Brand Icons**: Custom Kanriya brand iconography (embedded SVG)
- **Format**: SVG + Font Icons for maximum compatibility
- **Storage**: All icons embedded as assembly resources
- **Loading**: Instant loading, no network requests

## Offline Setup Process

### 1. Download Google Fonts (One-time setup)
```bash
# Download fonts from Google Fonts
# Visit https://fonts.google.com/specimen/Inter
# Visit https://fonts.google.com/specimen/Roboto+Mono
# Download all required weights: 400, 500, 600, 700

# Organize files:
mkdir -p Assets/Fonts/GoogleFonts/Inter
mkdir -p Assets/Fonts/GoogleFonts/RobotoMono
mkdir -p Assets/Fonts/GoogleFonts/MaterialIcons

# Download Material Icons font
# Visit https://fonts.google.com/icons
# Download Material Icons font files
```

### 2. Download Google Icons (One-time setup)
```bash
# Download SVG icons from Google Icons
# Visit https://fonts.google.com/icons
# Download individual SVG files for needed icons
# Organize by category in Assets/Icons/MaterialDesign/

# Alternative: Use Google's Material Design Icons repository
# git clone https://github.com/google/material-design-icons.git
# Copy needed SVG files to project
```

### 3. MSBuild Configuration for Embedded Resources
```xml
<!-- In Kanriya.Client.Avalonia.csproj -->
<ItemGroup>
    <!-- Embed all fonts -->
    <AvaloniaResource Include="Assets\Fonts\**\*.ttf" />
    <AvaloniaResource Include="Assets\Fonts\**\*.otf" />
    
    <!-- Embed all icons -->
    <EmbeddedResource Include="Assets\Icons\**\*.svg" />
    
    <!-- Embed licenses -->
    <EmbeddedResource Include="Assets\Fonts\LICENSES\*.txt" />
    <EmbeddedResource Include="Assets\Icons\LICENSES\*.txt" />
</ItemGroup>
```

## Implementation Plan

### 1. Typography System

#### Embedded Font Structure
```
src/Kanriya.Client.Avalonia/Kanriya.Client.Avalonia/
├── Assets/
│   └── Fonts/
│       ├── GoogleFonts/               # Downloaded from Google Fonts
│       │   ├── Inter/
│       │   │   ├── Inter-Regular.ttf           # 400 weight
│       │   │   ├── Inter-Medium.ttf            # 500 weight
│       │   │   ├── Inter-SemiBold.ttf          # 600 weight
│       │   │   └── Inter-Bold.ttf              # 700 weight
│       │   ├── RobotoMono/
│       │   │   ├── RobotoMono-Regular.ttf      # 400 weight
│       │   │   ├── RobotoMono-Medium.ttf       # 500 weight
│       │   │   └── RobotoMono-Bold.ttf         # 700 weight
│       │   └── MaterialIcons/
│       │       ├── MaterialIcons-Regular.ttf   # Icon font
│       │       └── MaterialSymbols-Filled.ttf  # New Material Symbols
│       └── LICENSES/                   # Font license files
│           ├── Inter-LICENSE.txt
│           ├── RobotoMono-LICENSE.txt
│           └── MaterialIcons-LICENSE.txt
└── Styles/
    ├── Typography.axaml
    └── IconStyles.axaml
```

#### Typography Service
```csharp
public class TypographyService : ITypographyService
{
    // Font Management
    public FontFamily GetPrimaryFont();
    public FontFamily GetMonospaceFont();
    public FontFamily GetSystemFont();
    
    // Responsive Typography
    public double GetScaledFontSize(TypographyScale scale, DisplaySize screenSize);
    public double GetLineHeight(double fontSize, TypographyScale scale);
    public FontWeight GetFontWeight(TypographyWeight weight);
    
    // Platform Optimization
    public FontFamily GetOptimalFont(DisplaySize screenSize, bool isMonospace = false);
    public double GetOptimalFontSize(TypographyScale scale, DisplaySize screenSize);
    
    // Theming Support
    public SolidColorBrush GetTextColor(TypographyLevel level, bool isDarkTheme = false);
    public double GetOpacity(TypographyLevel level);
    
    // Properties
    public bool AreCustomFontsLoaded { get; }
    public IReadOnlyList<string> AvailableFonts { get; }
}
```

#### Typography Scale System
```csharp
public enum TypographyScale
{
    // Display sizes (large headings)
    Display1,    // 96sp - Hero text
    Display2,    // 60sp - Large headers
    Display3,    // 48sp - Section headers
    
    // Headlines
    Headline1,   // 36sp - Page titles
    Headline2,   // 24sp - Card titles
    Headline3,   // 20sp - List headers
    Headline4,   // 18sp - Small headers
    
    // Body text
    Body1,       // 16sp - Primary body text
    Body2,       // 14sp - Secondary body text
    Body3,       // 12sp - Small body text
    
    // Captions and labels
    Caption,     // 11sp - Image captions, meta info
    Label,       // 10sp - Input labels, tags
    Overline     // 9sp - Category labels, breadcrumbs
}

public enum TypographyWeight
{
    Light,       // 300
    Regular,     // 400
    Medium,      // 500
    SemiBold,    // 600
    Bold         // 700
}

public enum TypographyLevel
{
    Primary,     // Full opacity, main content
    Secondary,   // 87% opacity, supporting content
    Disabled     // 54% opacity, inactive content
}
```

#### Responsive Font Scaling
```csharp
public static class TypographyScaling
{
    // Base font sizes (for Large screens)
    private static readonly Dictionary<TypographyScale, double> BaseSizes = new()
    {
        { TypographyScale.Display1, 96 },
        { TypographyScale.Display2, 60 },
        { TypographyScale.Display3, 48 },
        { TypographyScale.Headline1, 36 },
        { TypographyScale.Headline2, 24 },
        { TypographyScale.Headline3, 20 },
        { TypographyScale.Headline4, 18 },
        { TypographyScale.Body1, 16 },
        { TypographyScale.Body2, 14 },
        { TypographyScale.Body3, 12 },
        { TypographyScale.Caption, 11 },
        { TypographyScale.Label, 10 },
        { TypographyScale.Overline, 9 }
    };
    
    // Scaling factors by screen size
    public static double GetScalingFactor(DisplaySize screenSize) => screenSize switch
    {
        DisplaySize.Small => 0.85,   // 15% smaller on mobile
        DisplaySize.Medium => 0.95,  // 5% smaller on tablet
        DisplaySize.Large => 1.0,    // Base size on desktop
        _ => 1.0
    };
    
    public static double GetFontSize(TypographyScale scale, DisplaySize screenSize)
    {
        var baseSize = BaseSizes[scale];
        var scalingFactor = GetScalingFactor(screenSize);
        return Math.Round(baseSize * scalingFactor);
    }
}
```

### 2. Icon System

#### Embedded Icon Structure  
```
src/Kanriya.Client.Avalonia/Kanriya.Client.Avalonia/
├── Assets/
│   └── Icons/
│       ├── MaterialDesign/            # Google Material Design Icons (SVG)
│       │   ├── ui/                    # UI controls
│       │   │   ├── menu.svg               # Downloaded from Google Icons
│       │   │   ├── close.svg              # fonts.google.com/icons
│       │   │   ├── search.svg
│       │   │   └── settings.svg
│       │   ├── navigation/            # Navigation
│       │   │   ├── home.svg
│       │   │   ├── arrow_back.svg
│       │   │   ├── arrow_forward.svg
│       │   │   └── arrow_upward.svg
│       │   ├── actions/               # User actions
│       │   │   ├── add.svg
│       │   │   ├── edit.svg
│       │   │   ├── delete.svg
│       │   │   └── share.svg
│       │   ├── communication/         # Social/communication
│       │   │   ├── mail.svg
│       │   │   ├── phone.svg
│       │   │   ├── message.svg
│       │   │   └── notifications.svg
│       │   └── business/              # Business logic
│       │       ├── person.svg
│       │       ├── groups.svg
│       │       ├── business.svg
│       │       └── analytics.svg
│       ├── Brand/                     # Kanriya brand icons (Custom SVG)
│       │   ├── kanriya-logo.svg
│       │   ├── kanriya-icon.svg
│       │   ├── kanriya-wordmark.svg
│       │   └── kanriya-symbol.svg
│       └── LICENSES/                  # Icon license files
│           ├── MaterialDesign-LICENSE.txt
│           └── Brand-LICENSE.txt
└── Services/
    ├── EmbeddedIconService.cs
    ├── MaterialIconService.cs
    └── SvgIconExtension.cs
```

#### Icon Service
```csharp
public class IconService : IIconService
{
    // Icon Loading
    public Task<DrawingImage?> LoadIconAsync(string iconName, IconSet iconSet = IconSet.Lucide);
    public Task<SvgImage?> LoadSvgIconAsync(string iconName, IconSet iconSet = IconSet.Lucide);
    public DrawingImage? GetCachedIcon(string iconName, IconSet iconSet = IconSet.Lucide);
    
    // Icon Customization
    public DrawingImage? ColorizeIcon(DrawingImage icon, SolidColorBrush color);
    public DrawingImage? ResizeIcon(DrawingImage icon, double size);
    public DrawingImage? CreateThemedIcon(string iconName, bool isDarkTheme, double size = 24);
    
    // Icon Discovery
    public IReadOnlyList<string> GetAvailableIcons(IconSet iconSet = IconSet.Lucide);
    public bool IconExists(string iconName, IconSet iconSet = IconSet.Lucide);
    
    // Preloading
    public Task PreloadIconSetAsync(IconSet iconSet);
    public Task PreloadCommonIconsAsync();
    
    // Platform Integration
    public DrawingImage? GetPlatformIcon(PlatformIconType iconType);
    public string GetIconPath(string iconName, IconSet iconSet = IconSet.Lucide);
}

public enum IconSet
{
    Lucide,      // Primary icon set
    Brand,       // Kanriya brand icons
    Platform     // Platform-specific icons
}

public enum PlatformIconType
{
    AppIcon,
    NotificationIcon,
    MenuIcon,
    BackIcon
}
```

#### XAML Icon Extensions
```xml
<!-- Custom markup extension for easy icon usage -->
<Button>
    <Button.Content>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <icons:SvgIcon Icon="user" Size="16" />
            <TextBlock Text="Profile" />
        </StackPanel>
    </Button.Content>
</Button>

<!-- With theming support -->
<icons:SvgIcon Icon="settings" 
               Size="24" 
               Color="{DynamicResource PrimaryTextBrush}"
               IsDarkTheme="{Binding IsDarkTheme}" />
```

#### Icon Extensions Implementation
```csharp
public class SvgIconExtension : MarkupExtension
{
    public string Icon { get; set; } = "";
    public double Size { get; set; } = 24;
    public IconSet IconSet { get; set; } = IconSet.Lucide;
    public SolidColorBrush? Color { get; set; }
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var iconService = ServiceLocator.GetService<IIconService>();
        var svgIcon = iconService.LoadSvgIconAsync(Icon, IconSet).Result;
        
        if (svgIcon != null && Color != null)
        {
            // Apply color theming
            svgIcon = iconService.ColorizeIcon(svgIcon, Color);
        }
        
        return new Image 
        { 
            Source = svgIcon, 
            Width = Size, 
            Height = Size 
        };
    }
}
```

### 3. Avalonia Styles Integration

#### Typography Styles (Typography.axaml)
```xml
<Styles xmlns="https://github.com/avaloniaui">
    <!-- Font Families -->
    <Style.Resources>
        <FontFamily x:Key="PrimaryFont">avares://Kanriya.Client.Avalonia/Assets/Fonts/Inter#Inter</FontFamily>
        <FontFamily x:Key="MonospaceFont">avares://Kanriya.Client.Avalonia/Assets/Fonts/JetBrainsMono#JetBrains Mono</FontFamily>
    </Style.Resources>
    
    <!-- Display Styles -->
    <Style Selector="TextBlock.Display1">
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}" />
        <Setter Property="FontSize" Value="{Binding Source={x:Static local:TypographyHelper.Display1FontSize}}" />
        <Setter Property="FontWeight" Value="Light" />
        <Setter Property="LineHeight" Value="1.12" />
    </Style>
    
    <Style Selector="TextBlock.Display2">
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}" />
        <Setter Property="FontSize" Value="{Binding Source={x:Static local:TypographyHelper.Display2FontSize}}" />
        <Setter Property="FontWeight" Value="Light" />
        <Setter Property="LineHeight" Value="1.16" />
    </Style>
    
    <!-- Headline Styles -->
    <Style Selector="TextBlock.Headline1">
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}" />
        <Setter Property="FontSize" Value="{Binding Source={x:Static local:TypographyHelper.Headline1FontSize}}" />
        <Setter Property="FontWeight" Value="Regular" />
        <Setter Property="LineHeight" Value="1.25" />
    </Style>
    
    <!-- Body Text Styles -->
    <Style Selector="TextBlock.Body1">
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}" />
        <Setter Property="FontSize" Value="{Binding Source={x:Static local:TypographyHelper.Body1FontSize}}" />
        <Setter Property="FontWeight" Value="Regular" />
        <Setter Property="LineHeight" Value="1.5" />
    </Style>
    
    <!-- Responsive Styles -->
    <Style Selector="TextBlock.Body1.Small">
        <Setter Property="FontSize" Value="{Binding Source={x:Static local:TypographyHelper.Body1SmallFontSize}}" />
    </Style>
    
    <!-- Monospace Styles -->
    <Style Selector="TextBlock.Monospace">
        <Setter Property="FontFamily" Value="{StaticResource MonospaceFont}" />
        <Setter Property="FontSize" Value="14" />
    </Style>
</Styles>
```

#### Icon Styles
```xml
<Styles xmlns="https://github.com/avaloniaui">
    <!-- Icon Button Styles -->
    <Style Selector="Button.IconButton">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="Padding" Value="8" />
        <Setter Property="CornerRadius" Value="4" />
    </Style>
    
    <Style Selector="Button.IconButton:pointerover">
        <Setter Property="Background" Value="{DynamicResource SystemControlHighlightListLowBrush}" />
    </Style>
    
    <!-- Icon Sizes -->
    <Style Selector="Image.IconSmall">
        <Setter Property="Width" Value="16" />
        <Setter Property="Height" Value="16" />
    </Style>
    
    <Style Selector="Image.IconMedium">
        <Setter Property="Width" Value="24" />
        <Setter Property="Height" Value="24" />
    </Style>
    
    <Style Selector="Image.IconLarge">
        <Setter Property="Width" Value="32" />
        <Setter Property="Height" Value="32" />
    </Style>
</Styles>
```

### 4. Responsive Typography Helper

```csharp
public static class TypographyHelper
{
    private static IResponsiveService? _responsiveService;
    
    // Dynamic font size properties for XAML binding
    public static double Display1FontSize => GetScaledFontSize(TypographyScale.Display1);
    public static double Display2FontSize => GetScaledFontSize(TypographyScale.Display2);
    public static double Headline1FontSize => GetScaledFontSize(TypographyScale.Headline1);
    public static double Body1FontSize => GetScaledFontSize(TypographyScale.Body1);
    
    // Screen size specific variants
    public static double Body1SmallFontSize => TypographyScaling.GetFontSize(TypographyScale.Body1, DisplaySize.Small);
    public static double Body1MediumFontSize => TypographyScaling.GetFontSize(TypographyScale.Body1, DisplaySize.Medium);
    public static double Body1LargeFontSize => TypographyScaling.GetFontSize(TypographyScale.Body1, DisplaySize.Large);
    
    private static double GetScaledFontSize(TypographyScale scale)
    {
        _responsiveService ??= ServiceLocator.GetService<IResponsiveService>();
        return TypographyScaling.GetFontSize(scale, _responsiveService.CurrentDisplaySize);
    }
}
```

### 5. Integration with ClientEnvironmentConfig

```csharp
public static class ClientEnvironmentConfig
{
    // ... existing code ...
    
    /// <summary>
    /// Typography and iconography configuration
    /// </summary>
    public static class Design
    {
        public static ITypographyService Typography => GetTypographyService();
        public static IIconService Icons => GetIconService();
        
        // Typography helpers
        public static double GetFontSize(TypographyScale scale) 
            => Typography.GetScaledFontSize(scale, Navigation.CurrentDisplaySize);
        
        public static FontFamily PrimaryFont => Typography.GetPrimaryFont();
        public static FontFamily MonospaceFont => Typography.GetMonospaceFont();
        
        // Icon helpers  
        public static Task<DrawingImage?> LoadIconAsync(string iconName)
            => Icons.LoadIconAsync(iconName);
        
        public static DrawingImage? GetThemedIcon(string iconName, bool isDarkTheme, double size = 24)
            => Icons.CreateThemedIcon(iconName, isDarkTheme, size);
    }
}
```

### 6. Usage Examples

#### Typography in ViewModels
```csharp
public class MainViewModel : NavigatableViewModel
{
    public double TitleFontSize => ClientEnvironmentConfig.Design.GetFontSize(TypographyScale.Headline1);
    public double BodyFontSize => ClientEnvironmentConfig.Design.GetFontSize(TypographyScale.Body1);
    
    public FontFamily PrimaryFont => ClientEnvironmentConfig.Design.PrimaryFont;
}
```

#### Typography in XAML
```xml
<!-- Using predefined styles -->
<TextBlock Text="Page Title" Classes="Headline1" />
<TextBlock Text="Body content here" Classes="Body1" />
<TextBlock Text="Code example" Classes="Monospace" />

<!-- Using bound properties -->
<TextBlock Text="{Binding Title}" 
           FontFamily="{Binding PrimaryFont}"
           FontSize="{Binding TitleFontSize}" />
```

#### Icons in ViewModels
```csharp
public class ToolbarViewModel : ViewModelBase
{
    [ObservableProperty]
    private DrawingImage? _menuIcon;
    
    [ObservableProperty]
    private DrawingImage? _searchIcon;
    
    public async Task LoadIconsAsync()
    {
        MenuIcon = await ClientEnvironmentConfig.Design.LoadIconAsync("menu");
        SearchIcon = await ClientEnvironmentConfig.Design.LoadIconAsync("search");
    }
}
```

#### Icons in XAML
```xml
<!-- Using custom extension -->
<Button Command="{Binding MenuCommand}">
    <icons:SvgIcon Icon="menu" Size="24" />
</Button>

<!-- Using bound properties -->
<Image Source="{Binding MenuIcon}" Width="24" Height="24" />

<!-- Icon with text -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <icons:SvgIcon Icon="user" Size="16" />
    <TextBlock Text="Profile" Classes="Body2" />
</StackPanel>
```

### 7. Font Loading Strategy

#### MSBuild Configuration
```xml
<!-- In Kanriya.Client.Avalonia.csproj -->
<ItemGroup>
    <AvaloniaResource Include="Assets\Fonts\**" />
    <EmbeddedResource Include="Assets\Icons\**" />
</ItemGroup>
```

#### Font Preloading
```csharp
public class FontLoader
{
    public static async Task PreloadFontsAsync()
    {
        // Preload custom fonts on application start
        var primaryFont = new FontFamily("avares://Kanriya.Client.Avalonia/Assets/Fonts/Inter#Inter");
        var monospaceFont = new FontFamily("avares://Kanriya.Client.Avalonia/Assets/Fonts/JetBrainsMono#JetBrains Mono");
        
        // Trigger font loading
        var testText = new TextBlock { FontFamily = primaryFont, Text = "Test" };
        var testMono = new TextBlock { FontFamily = monospaceFont, Text = "Test" };
    }
}
```

### 8. Platform-Specific Considerations

#### Android
- Use `android:fontFamily` attributes when needed
- Respect system font scaling settings
- Handle high DPI displays

#### iOS
- Integrate with Dynamic Type system
- Respect accessibility font sizing
- Use SF Symbols when appropriate

#### Browser
- Font loading optimization
- Web font fallbacks
- CSS font-display strategies

## Benefits

- **Consistent Typography**: Unified text system across all platforms
- **Scalable Icons**: SVG icons that work at any size and theme
- **Responsive Design**: Adaptive typography based on screen size
- **Performance**: Embedded fonts and cached icons
- **Accessibility**: Proper contrast ratios and font scaling
- **Maintainability**: Centralized design system
- **Flexibility**: Easy to update fonts and icons globally

## Recommended Font & Icon Sets

### Typography
- **Primary**: Inter (excellent readability, modern design)
- **Monospace**: JetBrains Mono (developer-friendly, great for data)
- **Fallbacks**: System fonts (Segoe UI, Roboto, SF Pro)

### Icons
- **Primary**: Lucide Icons (consistent, modern, large set)
- **Alternative**: Heroicons, Feather Icons
- **Brand**: Custom Kanriya iconography

## Future Enhancements

- **Icon Animation**: Animated SVG icons for interactions
- **Variable Fonts**: Advanced typography with OpenType features
- **Custom Icons**: Icon generation and custom iconography tools
- **Accessibility**: Enhanced screen reader and high contrast support
- **Theming**: Advanced color and style theming system