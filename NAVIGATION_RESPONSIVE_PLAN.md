# Kanriya Client Navigation & Responsive Design Plan

## Overview
Implement Single Page Application (SPA) navigation with modal system and adaptive responsive design for Kanriya Client across all platforms.

## Navigation Architecture
- **SPA Pattern**: Single main window with content area switching
- **Modal System**: Overlays that adapt to screen size (full-screen on mobile)
- **Deep Linking**: URL-style routing for browser platform
- **Back Stack**: Navigation history management
- **Parameter Passing**: Type-safe data flow between views

## Responsive Design Strategy
**Adaptive Design**: Different layouts optimized for each screen size category

### Screen Breakpoints
```csharp
public enum DisplaySize
{
    Small,   // Phone < 480px
    Medium,  // Tablet/Large Phone 480px - 1200px  
    Large    // Desktop > 1200px
}

public static class ScreenBreakpoints
{
    public const int SmallMaxWidth = 480;
    public const int MediumMaxWidth = 1200;
    
    // Small: < 480px (Phone)
    // Medium: 480px - 1200px (Tablet, Large Phone)
    // Large: > 1200px (Desktop)
}
```

## Implementation Plan

### 1. Project Structure
```
src/Kanriya.Client.Avalonia/Kanriya.Client.Avalonia/
├── Services/
│   ├── NavigationService.cs
│   ├── ModalService.cs
│   └── ResponsiveService.cs
├── ViewModels/
│   ├── Base/
│   │   ├── NavigatableViewModel.cs
│   │   ├── ModalViewModel.cs
│   │   └── ResponsiveViewModel.cs
│   ├── Pages/
│   │   ├── HomeViewModel.cs
│   │   ├── ProfileViewModel.cs
│   │   └── SettingsViewModel.cs
│   └── Modals/
│       ├── LoginModalViewModel.cs
│       └── ConfirmDialogViewModel.cs
├── Views/
│   ├── MainWindow.axaml              # SPA Container
│   ├── Layouts/
│   │   ├── SmallLayout.axaml         # < 480px
│   │   ├── MediumLayout.axaml        # 480-1200px
│   │   └── LargeLayout.axaml         # > 1200px
│   ├── Pages/
│   │   ├── HomeView.axaml
│   │   ├── ProfileView.axaml
│   │   └── SettingsView.axaml
│   ├── Modals/
│   │   ├── LoginModal.axaml
│   │   └── ConfirmDialog.axaml
│   └── Controls/
│       ├── ResponsiveContainer.axaml
│       └── AdaptiveModal.axaml
└── Models/
    ├── NavigationModels.cs
    └── ResponsiveModels.cs
```

### 2. Navigation Service Architecture

#### Core Navigation Service
```csharp
public class NavigationService : INavigationService
{
    // Page Navigation
    public async Task NavigateToAsync<TViewModel>() where TViewModel : NavigatableViewModel;
    public async Task NavigateToAsync<TViewModel>(object parameters) where TViewModel : NavigatableViewModel;
    public async Task NavigateToAsync(Type viewModelType, object? parameters = null);
    
    // Navigation Stack
    public async Task GoBackAsync();
    public async Task GoForwardAsync();
    public void ClearHistory();
    
    // Properties
    public bool CanGoBack { get; }
    public bool CanGoForward { get; }
    public NavigatableViewModel? CurrentViewModel { get; }
    public IReadOnlyList<NavigationEntry> NavigationStack { get; }
    
    // Events
    public event EventHandler<NavigationEventArgs>? Navigated;
    public event EventHandler<NavigationEventArgs>? Navigating;
    
    // Deep Linking (Browser support)
    public void RegisterRoute<TViewModel>(string route) where TViewModel : NavigatableViewModel;
    public async Task NavigateToRouteAsync(string route, object? parameters = null);
}
```

#### Modal Service
```csharp
public class ModalService : IModalService
{
    // Modal Management
    public async Task<TResult?> ShowModalAsync<TViewModel, TResult>(object? parameters = null) 
        where TViewModel : ModalViewModel<TResult>;
    
    public async Task ShowModalAsync<TViewModel>(object? parameters = null) 
        where TViewModel : ModalViewModel;
    
    // Modal Stack
    public async Task CloseModalAsync<TResult>(TResult result);
    public async Task CloseAllModalsAsync();
    
    // Adaptive Behavior
    public bool ShouldShowFullScreen { get; } // True for small screens
    public ModalDisplayMode GetDisplayMode(DisplaySize screenSize);
    
    // Properties
    public bool HasActiveModals { get; }
    public IReadOnlyList<ModalViewModel> ModalStack { get; }
    
    // Events
    public event EventHandler<ModalEventArgs>? ModalOpened;
    public event EventHandler<ModalEventArgs>? ModalClosed;
}
```

### 3. Responsive Service

```csharp
public class ResponsiveService : IResponsiveService
{
    // Screen Detection
    public DisplaySize CurrentDisplaySize { get; }
    public double CurrentWidth { get; }
    public double CurrentHeight { get; }
    
    // Responsive Queries
    public bool IsSmallScreen => CurrentDisplaySize == DisplaySize.Small;
    public bool IsMediumScreen => CurrentDisplaySize == DisplaySize.Medium;
    public bool IsLargeScreen => CurrentDisplaySize == DisplaySize.Large;
    
    // Layout Helpers
    public Thickness GetAdaptivePadding();
    public double GetAdaptiveFontSize(double baseSize);
    public int GetAdaptiveColumns(int baseColumns);
    
    // Events
    public event EventHandler<DisplaySizeChangedEventArgs>? DisplaySizeChanged;
    
    // Platform Detection
    public bool IsMobile => IsSmallScreen && (IsAndroid || IsIOS);
    public bool IsTablet => IsMediumScreen;
    public bool IsDesktop => IsLargeScreen;
}
```

### 4. Base ViewModels

#### NavigatableViewModel
```csharp
public abstract class NavigatableViewModel : ViewModelBase, INavigatableViewModel
{
    protected INavigationService Navigation { get; }
    protected IModalService Modal { get; }
    protected IResponsiveService Responsive { get; }
    
    // Navigation Lifecycle
    public virtual Task OnNavigatedToAsync(object? parameters) => Task.CompletedTask;
    public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;
    public virtual bool CanNavigateFrom() => true;
    
    // Navigation Commands
    public ICommand GoBackCommand { get; }
    public ICommand NavigateToCommand { get; }
    
    // Responsive Properties
    public DisplaySize CurrentDisplaySize => Responsive.CurrentDisplaySize;
    public bool IsSmallScreen => Responsive.IsSmallScreen;
    public bool IsMediumScreen => Responsive.IsMediumScreen;
    public bool IsLargeScreen => Responsive.IsLargeScreen;
}
```

#### ModalViewModel
```csharp
public abstract class ModalViewModel : ViewModelBase, IModalViewModel
{
    // Modal Control
    public ICommand CloseCommand { get; }
    public virtual bool CanClose() => true;
    
    // Modal Lifecycle
    public virtual Task OnModalOpenedAsync(object? parameters) => Task.CompletedTask;
    public virtual Task OnModalClosingAsync() => Task.CompletedTask;
    
    // Adaptive Properties
    public bool ShouldShowFullScreen { get; }
    public ModalDisplayMode DisplayMode { get; }
}

public abstract class ModalViewModel<TResult> : ModalViewModel, IModalViewModel<TResult>
{
    public TResult? Result { get; protected set; }
    
    protected void CloseWithResult(TResult result)
    {
        Result = result;
        CloseCommand.Execute(null);
    }
}
```

### 5. Main Window Structure (SPA Container)

```xml
<!-- MainWindow.axaml -->
<Window x:Class="Kanriya.Client.Avalonia.Views.MainWindow"
        Title="{Binding WindowTitle}"
        Width="{Binding WindowWidth}"
        Height="{Binding WindowHeight}">
    
    <!-- Responsive Layout Container -->
    <Grid>
        <!-- Main Content Area -->
        <ContentPresenter Name="MainContentPresenter" 
                          Content="{Binding CurrentView}"
                          ContentTemplate="{StaticResource ViewDataTemplate}" />
        
        <!-- Modal Overlay -->
        <Grid Name="ModalOverlay" 
              Background="Black" 
              Opacity="0.5"
              IsVisible="{Binding HasActiveModals}" />
        
        <!-- Modal Container -->
        <ContentPresenter Name="ModalPresenter"
                          Content="{Binding CurrentModal}"
                          ContentTemplate="{StaticResource ModalDataTemplate}"
                          IsVisible="{Binding HasActiveModals}" />
    </Grid>
</Window>
```

### 6. Adaptive Layouts

#### Small Screen Layout (< 480px)
```xml
<!-- SmallLayout.axaml - Mobile Optimized -->
<UserControl>
    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Header (Compact) -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Height="56">
            <Button Name="MenuButton" Width="48" Height="48">☰</Button>
            <TextBlock Text="{Binding Title}" VerticalAlignment="Center" />
        </StackPanel>
        
        <!-- Content (Full Width) -->
        <ScrollViewer Grid.Row="1">
            <StackPanel Margin="16" Spacing="12">
                <ContentPresenter Content="{Binding Content}" />
            </StackPanel>
        </ScrollViewer>
        
        <!-- Bottom Navigation -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Height="56">
            <!-- Mobile tab bar -->
        </StackPanel>
    </Grid>
</UserControl>
```

#### Medium Screen Layout (480px - 1200px)
```xml
<!-- MediumLayout.axaml - Tablet Optimized -->
<UserControl>
    <Grid ColumnDefinitions="200,*">
        <!-- Sidebar Navigation -->
        <StackPanel Grid.Column="0" Background="LightGray">
            <!-- Navigation menu -->
        </StackPanel>
        
        <!-- Main Content -->
        <Grid Grid.Column="1" RowDefinitions="Auto,*">
            <!-- Header -->
            <StackPanel Grid.Row="0" Height="64" Margin="24,12">
                <TextBlock Text="{Binding Title}" FontSize="24" />
            </StackPanel>
            
            <!-- Content -->
            <ScrollViewer Grid.Row="1" Margin="24">
                <ContentPresenter Content="{Binding Content}" />
            </ScrollViewer>
        </Grid>
    </Grid>
</UserControl>
```

#### Large Screen Layout (> 1200px)
```xml
<!-- LargeLayout.axaml - Desktop Optimized -->
<UserControl>
    <Grid ColumnDefinitions="250,*,300">
        <!-- Left Sidebar -->
        <StackPanel Grid.Column="0" Background="LightGray">
            <!-- Primary navigation -->
        </StackPanel>
        
        <!-- Main Content -->
        <Grid Grid.Column="1" RowDefinitions="Auto,*">
            <!-- Header with breadcrumbs -->
            <StackPanel Grid.Row="0" Height="80" Margin="32,16">
                <TextBlock Text="{Binding Breadcrumbs}" FontSize="14" Opacity="0.7" />
                <TextBlock Text="{Binding Title}" FontSize="28" FontWeight="Bold" />
            </StackPanel>
            
            <!-- Content -->
            <ScrollViewer Grid.Row="1" Margin="32">
                <ContentPresenter Content="{Binding Content}" />
            </ScrollViewer>
        </Grid>
        
        <!-- Right Panel (Optional) -->
        <StackPanel Grid.Column="2" Background="WhiteSmoke">
            <!-- Secondary content, widgets, etc. -->
        </StackPanel>
    </Grid>
</UserControl>
```

### 7. Adaptive Modal System

#### Modal Display Modes
```csharp
public enum ModalDisplayMode
{
    Dialog,      // Centered dialog (medium/large screens)
    BottomSheet, // Slide up from bottom (mobile)
    FullScreen,  // Full screen overlay (small screens)
    Sidebar      // Side panel (large screens only)
}
```

#### Adaptive Modal Template
```xml
<!-- AdaptiveModal.axaml -->
<UserControl>
    <Grid>
        <!-- Dialog Mode (Medium/Large screens) -->
        <Border Name="DialogContainer"
                Background="White"
                CornerRadius="8"
                BoxShadow="0 8 32 rgba(0,0,0,0.3)"
                MaxWidth="600"
                MaxHeight="800"
                HorizontalAlignment="Center"
                VerticalAlignment="Center"
                IsVisible="{Binding DisplayMode, Converter={StaticResource EqualToConverter}, ConverterParameter=Dialog}">
            
            <ContentPresenter Content="{Binding ModalContent}" />
        </Border>
        
        <!-- Full Screen Mode (Small screens) -->
        <Grid Name="FullScreenContainer"
              Background="White"
              IsVisible="{Binding DisplayMode, Converter={StaticResource EqualToConverter}, ConverterParameter=FullScreen}">
            
            <Grid RowDefinitions="Auto,*">
                <!-- Header with close button -->
                <StackPanel Grid.Row="0" Orientation="Horizontal" Height="56" Margin="16,0">
                    <Button Command="{Binding CloseCommand}">✕</Button>
                    <TextBlock Text="{Binding Title}" VerticalAlignment="Center" Margin="16,0" />
                </StackPanel>
                
                <!-- Content -->
                <ScrollViewer Grid.Row="1">
                    <ContentPresenter Content="{Binding ModalContent}" Margin="16" />
                </ScrollViewer>
            </Grid>
        </Grid>
        
        <!-- Bottom Sheet Mode (Mobile alternative) -->
        <Border Name="BottomSheetContainer"
                Background="White"
                CornerRadius="12,12,0,0"
                VerticalAlignment="Bottom"
                MinHeight="200"
                MaxHeight="600"
                IsVisible="{Binding DisplayMode, Converter={StaticResource EqualToConverter}, ConverterParameter=BottomSheet}">
            
            <ContentPresenter Content="{Binding ModalContent}" Margin="16" />
        </Border>
    </Grid>
</UserControl>
```

### 8. Integration with ClientEnvironmentConfig

```csharp
public static class ClientEnvironmentConfig
{
    // ... existing code ...
    
    /// <summary>
    /// Navigation and UI configuration
    /// </summary>
    public static class Navigation
    {
        public static INavigationService Service => GetNavigationService();
        public static IModalService Modal => GetModalService();
        public static IResponsiveService Responsive => GetResponsiveService();
        
        // Screen information
        public static DisplaySize CurrentDisplaySize => Responsive.CurrentDisplaySize;
        public static bool IsSmallScreen => Responsive.IsSmallScreen;
        public static bool IsMediumScreen => Responsive.IsMediumScreen;
        public static bool IsLargeScreen => Responsive.IsLargeScreen;
        
        // Navigation helpers
        public static Task NavigateToAsync<T>() where T : NavigatableViewModel 
            => Service.NavigateToAsync<T>();
        public static Task ShowModalAsync<T>() where T : ModalViewModel 
            => Modal.ShowModalAsync<T>();
    }
}
```

### 9. Usage Examples

#### Page Navigation
```csharp
public class HomeViewModel : NavigatableViewModel
{
    public ICommand NavigateToProfileCommand { get; }
    
    private async Task NavigateToProfile()
    {
        await Navigation.NavigateToAsync<ProfileViewModel>(new { UserId = 123 });
    }
    
    public override async Task OnNavigatedToAsync(object? parameters)
    {
        // Load data based on parameters
        if (parameters is Dictionary<string, object> p && p.ContainsKey("refresh"))
        {
            await RefreshDataAsync();
        }
    }
}
```

#### Modal Usage
```csharp
public class LoginModalViewModel : ModalViewModel<bool>
{
    public ICommand LoginCommand { get; }
    
    private async Task Login()
    {
        var success = await AuthService.LoginAsync(Username, Password);
        if (success)
        {
            CloseWithResult(true);
        }
    }
}

// Usage
var loginSuccess = await Modal.ShowModalAsync<LoginModalViewModel, bool>();
if (loginSuccess == true)
{
    await Navigation.NavigateToAsync<DashboardViewModel>();
}
```

#### Responsive Design
```csharp
public class ProductListViewModel : NavigatableViewModel
{
    public int ColumnsCount => CurrentDisplaySize switch
    {
        DisplaySize.Small => 1,   // Single column on mobile
        DisplaySize.Medium => 2,  // Two columns on tablet
        DisplaySize.Large => 3,   // Three columns on desktop
        _ => 2
    };
    
    public GridLength SidebarWidth => IsLargeScreen 
        ? new GridLength(250) 
        : new GridLength(0);
}
```

### 10. Platform-Specific Adaptations

#### Desktop
- Multi-window support (optional)
- Keyboard shortcuts
- Menu bar integration
- System tray integration

#### Mobile (Android/iOS)
- Hardware back button handling
- Status bar integration
- Safe area handling (notches, home indicators)
- Native navigation animations

#### Browser
- URL routing and history
- Browser back/forward button integration
- Responsive breakpoint detection
- Touch vs. mouse interaction

## Benefits

- **Unified Experience**: Consistent navigation across all platforms
- **Adaptive Design**: Optimized layouts for each screen size
- **Modal Flexibility**: Context-sensitive modal display modes
- **Type Safety**: Strongly-typed navigation with parameters
- **Deep Linking**: URL-based navigation for web platform
- **Performance**: Efficient view recycling and lazy loading
- **Accessibility**: Screen reader and keyboard navigation support

## Future Enhancements

- **Animation System**: Page transitions and modal animations
- **Gesture Support**: Swipe navigation and touch interactions
- **State Persistence**: Navigation state restoration
- **Lazy Loading**: On-demand view model and view loading
- **Caching**: View instance reuse and smart caching
- **Testing**: Navigation unit testing framework