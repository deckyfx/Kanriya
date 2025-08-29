# Kanriya Client Splash Screen Implementation Plan

## Overview
Implement comprehensive cross-platform splash screen system for Kanriya Client with consistent branding, loading states, and smooth transitions across all platforms.

## Design Philosophy
**HYBRID APPROACH**: Combine platform-native splash screens (where available) with custom Avalonia splash screens for consistent branding and advanced loading states across all platforms.

## Splash Screen Strategy
- **Desktop**: Custom Avalonia splash window with animations
- **Android**: Native splash screen → Custom Avalonia splash screen
- **iOS**: Launch screen → Custom Avalonia splash screen  
- **Browser**: HTML loading screen → Custom Avalonia splash screen
- **Consistent Branding**: Same Kanriya logo, colors, and animations everywhere
- **Loading States**: Progress indicators for app initialization

## Implementation Plan

### 1. Cross-Platform Splash Architecture

#### Project Structure
```
src/Kanriya.Client.Avalonia/Kanriya.Client.Avalonia/
├── Views/
│   ├── SplashScreenWindow.axaml        # Main splash screen window
│   ├── SplashScreenWindow.axaml.cs
│   └── Controls/
│       ├── SplashScreenContent.axaml   # Reusable splash content
│       ├── LoadingIndicator.axaml      # Custom loading animation
│       └── BrandLogo.axaml             # Kanriya logo component
├── ViewModels/
│   ├── SplashScreenViewModel.cs        # Splash screen logic
│   └── LoadingStateViewModel.cs        # Loading progress management
├── Services/
│   ├── SplashScreenService.cs          # Splash screen orchestration
│   ├── AppInitializationService.cs     # App startup logic
│   └── PlatformSplashService.cs        # Platform-specific splash handling
├── Assets/
│   ├── Splash/
│   │   ├── kanriya-logo.svg            # Main logo (SVG for scaling)
│   │   ├── kanriya-symbol.svg          # Icon version
│   │   ├── kanriya-wordmark.svg        # Text logo
│   │   └── splash-background.png       # Optional background
│   └── Animations/
│       ├── logo-fade-in.json           # Lottie animation (optional)
│       └── loading-spinner.svg         # Custom loading spinner
└── Styles/
    └── SplashStyles.axaml              # Splash screen styles
```

### 2. Core Splash Screen Service

```csharp
public class SplashScreenService : ISplashScreenService
{
    private readonly IAppInitializationService _appInitialization;
    private readonly IPlatformSplashService _platformSplash;
    private SplashScreenWindow? _splashWindow;
    
    // Splash Screen Management
    public async Task ShowSplashScreenAsync();
    public async Task HideSplashScreenAsync();
    public void UpdateProgress(double progress, string message);
    public void UpdateLoadingState(LoadingState state);
    
    // Platform Integration
    public bool HasNativeSplash { get; }
    public async Task WaitForNativeSplashCompleteAsync();
    
    // Timing Control
    public TimeSpan MinimumDisplayTime { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaximumDisplayTime { get; set; } = TimeSpan.FromSeconds(10);
    
    // Events
    public event EventHandler<SplashProgressEventArgs>? ProgressChanged;
    public event EventHandler? SplashCompleted;
    
    // Initialization Flow
    public async Task InitializeApplicationAsync()
    {
        await ShowSplashScreenAsync();
        
        try
        {
            // Step 1: Initialize core services
            UpdateProgress(0.1, "Initializing core services...");
            await _appInitialization.InitializeCoreServicesAsync();
            
            // Step 2: Load configuration
            UpdateProgress(0.3, "Loading configuration...");
            await _appInitialization.LoadConfigurationAsync();
            
            // Step 3: Initialize UI services
            UpdateProgress(0.5, "Preparing user interface...");
            await _appInitialization.InitializeUIServicesAsync();
            
            // Step 4: Load resources
            UpdateProgress(0.7, "Loading resources...");
            await _appInitialization.LoadResourcesAsync();
            
            // Step 5: Final preparations
            UpdateProgress(0.9, "Finalizing startup...");
            await _appInitialization.FinalizeStartupAsync();
            
            UpdateProgress(1.0, "Ready!");
            await Task.Delay(500); // Brief pause to show completion
        }
        finally
        {
            await HideSplashScreenAsync();
        }
    }
}
```

### 3. Splash Screen Window Implementation

#### SplashScreenWindow.axaml
```xml
<Window x:Class="Kanriya.Client.Avalonia.Views.SplashScreenWindow"
        xmlns="https://github.com/avaloniaui"
        Title="Kanriya"
        Width="600"
        Height="400"
        WindowStartupLocation="CenterScreen"
        CanResize="False"
        ShowInTaskbar="False"
        SystemDecorations="None"
        Background="Transparent"
        TransparencyLevelHint="Transparent">
    
    <!-- Splash Screen Content -->
    <Border Background="#FFFFFF" 
            CornerRadius="12"
            BoxShadow="0 8 32 rgba(0,0,0,0.2)">
        
        <Grid RowDefinitions="*,Auto,Auto,*" 
              Margin="48">
            
            <!-- Top Spacer -->
            <Grid Grid.Row="0" />
            
            <!-- Logo Section -->
            <StackPanel Grid.Row="1" 
                        HorizontalAlignment="Center"
                        Spacing="16">
                
                <!-- Main Logo -->
                <Image Source="/Assets/Splash/kanriya-logo.svg"
                       Width="120"
                       Height="120"
                       Name="MainLogo">
                    <Image.RenderTransform>
                        <ScaleTransform ScaleX="0.8" ScaleY="0.8" />
                    </Image.RenderTransform>
                </Image>
                
                <!-- App Name -->
                <TextBlock Text="Kanriya"
                           FontSize="32"
                           FontWeight="Bold"
                           HorizontalAlignment="Center"
                           Foreground="#2D3748"
                           Name="AppNameText"
                           Opacity="0" />
                
                <!-- Tagline -->
                <TextBlock Text="Business Management Platform"
                           FontSize="16"
                           HorizontalAlignment="Center"
                           Foreground="#718096"
                           Name="TaglineText"
                           Opacity="0" />
            </StackPanel>
            
            <!-- Loading Section -->
            <StackPanel Grid.Row="2"
                        Margin="0,32,0,0"
                        Spacing="12">
                
                <!-- Loading Indicator -->
                <controls:LoadingIndicator Name="LoadingSpinner"
                                         Width="32"
                                         Height="32"
                                         HorizontalAlignment="Center" />
                
                <!-- Progress Bar -->
                <ProgressBar Name="ProgressBar"
                             Height="4"
                             Background="#E2E8F0"
                             Foreground="#3182CE"
                             Value="{Binding Progress}"
                             Minimum="0"
                             Maximum="1" />
                
                <!-- Status Text -->
                <TextBlock Text="{Binding StatusMessage}"
                           FontSize="14"
                           HorizontalAlignment="Center"
                           Foreground="#4A5568"
                           Name="StatusText" />
            </StackPanel>
            
            <!-- Bottom Spacer -->
            <Grid Grid.Row="3" />
        </Grid>
    </Border>
</Window>
```

#### SplashScreenWindow.axaml.cs
```csharp
public partial class SplashScreenWindow : Window
{
    private readonly SplashScreenViewModel _viewModel;
    
    public SplashScreenWindow()
    {
        InitializeComponent();
        _viewModel = new SplashScreenViewModel();
        DataContext = _viewModel;
        
        // Start entrance animations
        Loaded += OnLoaded;
    }
    
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        await PlayEntranceAnimationsAsync();
    }
    
    private async Task PlayEntranceAnimationsAsync()
    {
        // Animate logo scale
        var logoAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(800),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0),
                               new Setter(ScaleTransform.ScaleYProperty, 1.0) }
                }
            }
        };
        
        // Animate text fade in
        var textAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(600),
            Delay = TimeSpan.FromMilliseconds(400),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(OpacityProperty, 1.0) }
                }
            }
        };
        
        // Run animations
        await Task.WhenAll(
            logoAnimation.RunAsync(MainLogo),
            textAnimation.RunAsync(AppNameText),
            textAnimation.RunAsync(TaglineText)
        );
    }
    
    public async Task PlayExitAnimationAsync()
    {
        var exitAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(400),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(OpacityProperty, 0.0) }
                }
            }
        };
        
        await exitAnimation.RunAsync(this);
    }
}
```

### 4. Loading Indicator Control

#### LoadingIndicator.axaml
```xml
<UserControl x:Class="Kanriya.Client.Avalonia.Views.Controls.LoadingIndicator">
    <!-- Animated loading spinner -->
    <Canvas Width="32" Height="32">
        <Ellipse Width="32" Height="32"
                 Stroke="#3182CE"
                 StrokeThickness="3"
                 StrokeDashArray="8 4"
                 Name="Spinner">
            
            <!-- Rotation animation -->
            <Ellipse.RenderTransform>
                <RotateTransform Angle="0" CenterX="16" CenterY="16" />
            </Ellipse.RenderTransform>
        </Ellipse>
    </Canvas>
</UserControl>
```

#### LoadingIndicator.axaml.cs
```csharp
public partial class LoadingIndicator : UserControl
{
    private bool _isAnimating;
    
    public LoadingIndicator()
    {
        InitializeComponent();
        StartAnimation();
    }
    
    private async void StartAnimation()
    {
        _isAnimating = true;
        
        var rotateAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(2),
            IterationCount = IterationCount.Infinite,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(RotateTransform.AngleProperty, 360.0) }
                }
            }
        };
        
        while (_isAnimating)
        {
            await rotateAnimation.RunAsync(Spinner.RenderTransform);
        }
    }
    
    public void StopAnimation()
    {
        _isAnimating = false;
    }
}
```

### 5. Platform-Specific Implementations

#### Android Splash Screen
```xml
<!-- In Android project: Resources/drawable/splash_screen.xml -->
<layer-list xmlns:android="http://schemas.android.com/apk/res/android">
    <!-- Background -->
    <item android:drawable="@color/splash_background" />
    
    <!-- Logo -->
    <item android:gravity="center">
        <bitmap android:src="@drawable/kanriya_logo"
                android:gravity="center" />
    </item>
</layer-list>
```

```xml
<!-- In styles.xml -->
<style name="SplashTheme" parent="Theme.AppCompat.Light.NoActionBar">
    <item name="android:windowBackground">@drawable/splash_screen</item>
    <item name="android:windowFullscreen">true</item>
</style>
```

#### iOS Launch Screen
```xml
<!-- LaunchScreen.xib -->
<document type="com.apple.InterfaceBuilder3.CocoaTouch.XIB" version="3.0">
    <objects>
        <view contentMode="scaleToFill" id="iN0-l3-epB">
            <!-- Background -->
            <color key="backgroundColor" red="1" green="1" blue="1" alpha="1" colorSpace="calibratedRGB"/>
            
            <!-- Logo ImageView -->
            <imageView userInteractionEnabled="NO" contentMode="scaleAspectFit" 
                      translatesAutoresizingMaskIntoConstraints="NO" 
                      id="kanriya-logo"
                      image="kanriya-logo.png">
                <!-- Constraints for centering -->
            </imageView>
        </view>
    </objects>
</document>
```

#### Browser Loading Screen
```html
<!-- In wwwroot/index.html -->
<div id="splash-screen" style="
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: #ffffff;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    z-index: 9999;
">
    <img src="assets/kanriya-logo.svg" 
         alt="Kanriya" 
         style="width: 120px; height: 120px; margin-bottom: 24px;">
    
    <h1 style="
        font-family: Inter, sans-serif;
        font-size: 32px;
        font-weight: bold;
        color: #2D3748;
        margin: 0 0 8px 0;
    ">Kanriya</h1>
    
    <p style="
        font-family: Inter, sans-serif;
        font-size: 16px;
        color: #718096;
        margin: 0 0 32px 0;
    ">Business Management Platform</p>
    
    <div class="loading-spinner"></div>
    <p id="loading-text" style="
        font-family: Inter, sans-serif;
        font-size: 14px;
        color: #4A5568;
        margin-top: 16px;
    ">Loading application...</p>
</div>

<style>
.loading-spinner {
    width: 32px;
    height: 32px;
    border: 3px solid #E2E8F0;
    border-top: 3px solid #3182CE;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}
</style>

<script>
// Hide splash screen when Avalonia app is ready
window.hideSplashScreen = function() {
    const splash = document.getElementById('splash-screen');
    splash.style.opacity = '0';
    splash.style.transition = 'opacity 0.4s ease';
    setTimeout(() => splash.remove(), 400);
};
</script>
```

### 6. Application Startup Integration

#### Program.cs (Desktop)
```csharp
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
        
        // Show splash screen first
        var splashService = new SplashScreenService();
        
        builder.AfterSetup(async _ =>
        {
            // Initialize application with splash screen
            await splashService.InitializeApplicationAsync();
        });
        
        builder.StartWithClassicDesktopLifetime(args);
    }
}
```

#### App.axaml.cs
```csharp
public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Don't show main window immediately
            // Let splash screen handle the initialization
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // Mobile platforms - splash screen integration
            var splashService = ServiceLocator.GetService<ISplashScreenService>();
            splashService.InitializeApplicationAsync();
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}
```

### 7. Responsive Splash Screen

#### Adaptive Sizing
```csharp
public class SplashScreenViewModel : ViewModelBase
{
    private readonly IResponsiveService _responsive;
    
    public double LogoSize => _responsive.CurrentDisplaySize switch
    {
        DisplaySize.Small => 80,   // Smaller logo on mobile
        DisplaySize.Medium => 100, // Medium on tablet
        DisplaySize.Large => 120,  // Full size on desktop
        _ => 100
    };
    
    public double WindowWidth => _responsive.CurrentDisplaySize switch
    {
        DisplaySize.Small => Math.Min(_responsive.CurrentWidth * 0.9, 350),
        DisplaySize.Medium => 500,
        DisplaySize.Large => 600,
        _ => 500
    };
    
    public double WindowHeight => _responsive.CurrentDisplaySize switch
    {
        DisplaySize.Small => Math.Min(_responsive.CurrentHeight * 0.6, 300),
        DisplaySize.Medium => 350,
        DisplaySize.Large => 400,
        _ => 350
    };
}
```

### 8. Integration with ClientEnvironmentConfig

```csharp
public static class ClientEnvironmentConfig
{
    // ... existing code ...
    
    /// <summary>
    /// Splash screen and startup configuration
    /// </summary>
    public static class Startup
    {
        public static ISplashScreenService SplashScreen => GetSplashScreenService();
        public static IAppInitializationService Initialization => GetInitializationService();
        
        // Splash screen control
        public static Task ShowSplashScreenAsync() => SplashScreen.ShowSplashScreenAsync();
        public static Task HideSplashScreenAsync() => SplashScreen.HideSplashScreenAsync();
        public static void UpdateProgress(double progress, string message) 
            => SplashScreen.UpdateProgress(progress, message);
        
        // Initialization control
        public static Task InitializeApplicationAsync() => SplashScreen.InitializeApplicationAsync();
        
        // Configuration
        public static TimeSpan MinimumSplashTime { get; set; } = TimeSpan.FromSeconds(2);
        public static bool ShowProgressBar { get; set; } = true;
        public static bool EnableSplashAnimations { get; set; } = true;
    }
}
```

### 9. Loading States and Progress

#### LoadingState Enumeration
```csharp
public enum LoadingState
{
    Initializing,
    LoadingConfiguration,
    InitializingServices,
    LoadingResources,
    PreparingUI,
    ConnectingToServer,
    LoadingUserData,
    Finalizing,
    Ready,
    Error
}

public class LoadingStateManager
{
    private readonly Dictionary<LoadingState, (double Progress, string Message)> _stateInfo = new()
    {
        { LoadingState.Initializing, (0.1, "Starting application...") },
        { LoadingState.LoadingConfiguration, (0.2, "Loading configuration...") },
        { LoadingState.InitializingServices, (0.3, "Initializing services...") },
        { LoadingState.LoadingResources, (0.5, "Loading resources...") },
        { LoadingState.PreparingUI, (0.7, "Preparing interface...") },
        { LoadingState.ConnectingToServer, (0.8, "Connecting to server...") },
        { LoadingState.LoadingUserData, (0.9, "Loading user data...") },
        { LoadingState.Finalizing, (0.95, "Finalizing...") },
        { LoadingState.Ready, (1.0, "Ready!") }
    };
    
    public (double Progress, string Message) GetStateInfo(LoadingState state)
        => _stateInfo.GetValueOrDefault(state, (0.0, "Loading..."));
}
```

### 10. Error Handling and Fallbacks

#### Error Splash Screen
```csharp
public class ErrorSplashScreenViewModel : ViewModelBase
{
    public string ErrorTitle { get; set; } = "Startup Error";
    public string ErrorMessage { get; set; } = "";
    public string ErrorDetails { get; set; } = "";
    
    public ICommand RetryCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand SendReportCommand { get; }
    
    public async Task ShowError(Exception exception)
    {
        ErrorMessage = exception.Message;
        ErrorDetails = exception.ToString();
        
        // Log error
        // Show error splash screen
        // Provide recovery options
    }
}
```

## Benefits

- **Professional Appearance**: Polished startup experience across all platforms
- **Consistent Branding**: Same Kanriya logo and design everywhere
- **Loading Feedback**: Users see progress and status updates
- **Smooth Transitions**: Animated entrance and exit
- **Platform Optimization**: Native splash screens where supported
- **Responsive Design**: Adapts to different screen sizes
- **Error Handling**: Graceful error recovery with user options
- **Offline Support**: All assets embedded, no external dependencies

## Platform-Specific Features

### Desktop
- Custom window with rounded corners and shadow
- Smooth animations and transitions
- System integration (no taskbar icon during splash)

### Android
- Native Android splash screen (instant display)
- Seamless transition to Avalonia splash screen
- Material Design compliance

### iOS
- Native iOS launch screen (instant display)
- Human Interface Guidelines compliance
- Seamless transition to Avalonia splash screen

### Browser
- HTML/CSS loading screen (instant display)
- Progressive web app integration
- Smooth fade transition to Avalonia app

## Future Enhancements

- **Advanced Animations**: Lottie animations for more dynamic splash screens
- **Dynamic Content**: Server-driven splash screen content
- **Personalization**: User-specific splash screen content
- **Performance Metrics**: Startup time tracking and optimization
- **A/B Testing**: Different splash screen designs for user testing
- **Accessibility**: Screen reader support and high contrast modes