# Kanriya Client Media Assets Implementation Plan

## Overview
Implement comprehensive media asset handling system in Kanriya Client with support for embedded and remote assets across all platforms (Desktop, Android, iOS, Browser).

## Supported Media Types
- **Images**: PNG, JPG, WebP, SVG
- **Audio**: MP3, WAV, OGG
- **Video**: MP4, WebM, MOV

## Architecture Goals
- **Single Source of Truth**: Shared client library manages all media operations
- **Platform Agnostic**: Consistent API across Desktop, Android, iOS, Browser
- **Embedded & Remote**: Support both local assets and remote URLs
- **Caching Strategy**: Intelligent caching for performance
- **Async Loading**: Non-blocking asset loading with progress indicators

## Implementation Plan

### 1. Package Dependencies

#### Core Packages
```xml
<!-- Avalonia Client csproj -->
<PackageReference Include="Avalonia.Skia" />                    <!-- WebP, advanced image formats -->
<PackageReference Include="LibVLCSharp.Avalonia" />            <!-- Video playback -->
<PackageReference Include="Avalonia.Svg" />                    <!-- SVG support (if not built-in) -->
```

#### Platform-Specific Audio Packages
```xml
<!-- Desktop Windows -->
<PackageReference Include="NAudio" Condition="'$(TargetFramework)' == 'net9.0-windows'" />

<!-- Cross-platform alternative -->
<PackageReference Include="PortAudio" />                       <!-- Cross-platform audio -->
<PackageReference Include="OpenTK.Audio" />                    <!-- OpenAL audio -->
```

### 2. Folder Structure
```
src/Kanriya.Client.Avalonia/Kanriya.Client.Avalonia/
├── Assets/
│   ├── Images/
│   │   ├── logo.png
│   │   ├── icons/
│   │   │   ├── app-icon.svg
│   │   │   └── platform-icons/
│   │   └── backgrounds/
│   ├── Audio/
│   │   ├── sounds/
│   │   │   ├── notification.mp3
│   │   │   └── success.wav
│   │   └── music/
│   └── Videos/
│       ├── intro.mp4
│       └── tutorials/
├── Services/
│   ├── MediaService.cs
│   ├── ImageService.cs
│   ├── AudioService.cs
│   └── VideoService.cs
└── Models/
    └── MediaModels.cs
```

### 3. Core Media Service Architecture

#### MediaService (Main Service)
```csharp
public class MediaService
{
    private readonly ImageService _imageService;
    private readonly AudioService _audioService;
    private readonly VideoService _videoService;
    private readonly IMemoryCache _cache;
    
    // Unified media loading
    public async Task<IMediaAsset?> LoadMediaAsync(string source, MediaType type);
    public async Task<T?> LoadMediaAsync<T>(string source) where T : class, IMediaAsset;
    
    // Caching
    public async Task PreloadAsync(string[] sources);
    public void ClearCache();
    public void ClearCache(MediaType type);
    
    // Progress tracking
    public event EventHandler<MediaLoadProgressEventArgs>? LoadProgress;
}
```

#### ImageService
```csharp
public class ImageService
{
    // Image loading
    public async Task<Bitmap?> LoadBitmapAsync(string source);
    public async Task<SvgImage?> LoadSvgAsync(string source);
    public async Task<IImage?> LoadImageAsync(string source);
    
    // Format detection
    public ImageFormat DetectFormat(string source);
    public bool IsSupported(string source);
    
    // Embedded assets
    public async Task<Bitmap?> LoadEmbeddedBitmapAsync(string resourcePath);
    
    // Remote assets
    public async Task<Bitmap?> LoadRemoteBitmapAsync(string url);
    
    // Caching with size variants
    public async Task<Bitmap?> LoadWithCacheAsync(string source, Size? targetSize = null);
}
```

#### AudioService
```csharp
public class AudioService
{
    // Playback control
    public async Task PlayAsync(string source);
    public async Task PlayAsync(string source, AudioOptions options);
    public void Pause();
    public void Stop();
    public void Resume();
    
    // Properties
    public bool IsPlaying { get; }
    public bool IsPaused { get; }
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; }
    public float Volume { get; set; }
    
    // Events
    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackPaused;
    public event EventHandler? PlaybackStopped;
    public event EventHandler? PlaybackCompleted;
    
    // Multiple audio support
    public async Task<IAudioPlayer> CreatePlayerAsync(string source);
}
```

#### VideoService
```csharp
public class VideoService
{
    // Video playback (using LibVLC)
    public async Task<IVideoPlayer> CreatePlayerAsync(string source);
    public async Task PlayAsync(string source, VideoOptions options);
    
    // Controls
    public void Play();
    public void Pause();
    public void Stop();
    public void Seek(TimeSpan position);
    
    // Properties
    public bool IsPlaying { get; }
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; }
    public Size VideoSize { get; }
    public float Volume { get; set; }
    
    // Events
    public event EventHandler<VideoEventArgs>? VideoLoaded;
    public event EventHandler<VideoEventArgs>? PlaybackStateChanged;
}
```

### 4. Media Asset Models

```csharp
public interface IMediaAsset
{
    string Source { get; }
    MediaType Type { get; }
    bool IsLoaded { get; }
    DateTime LoadedAt { get; }
}

public enum MediaType
{
    Image,
    Audio,
    Video
}

public enum ImageFormat
{
    PNG,
    JPG,
    JPEG,
    WebP,
    SVG,
    GIF,
    BMP
}

public class MediaLoadProgressEventArgs : EventArgs
{
    public string Source { get; init; }
    public double Progress { get; init; } // 0.0 to 1.0
    public long BytesLoaded { get; init; }
    public long? TotalBytes { get; init; }
}

public class AudioOptions
{
    public float Volume { get; set; } = 1.0f;
    public bool Loop { get; set; } = false;
    public TimeSpan StartPosition { get; set; } = TimeSpan.Zero;
}

public class VideoOptions
{
    public float Volume { get; set; } = 1.0f;
    public bool Loop { get; set; } = false;
    public bool AutoPlay { get; set; } = true;
    public bool ShowControls { get; set; } = true;
}
```

### 5. XAML Integration

#### Image Display
```xml
<!-- Static embedded -->
<Image Source="avares://Kanriya.Client.Avalonia/Assets/Images/logo.png" />

<!-- SVG -->
<Svg Source="/Assets/Images/icons/app-icon.svg" />

<!-- Dynamic binding -->
<Image Source="{Binding ProfileImage}" />

<!-- Remote with loading -->
<Panel>
    <Image Source="{Binding RemoteImage}" IsVisible="{Binding ImageLoaded}" />
    <ProgressBar IsVisible="{Binding !ImageLoaded}" Value="{Binding LoadProgress}" />
</Panel>
```

#### Video Display (LibVLC)
```xml
<Grid>
    <!-- Video player -->
    <vlc:VideoView MediaPlayer="{Binding VideoPlayer}" />
    
    <!-- Controls overlay -->
    <StackPanel Orientation="Horizontal" VerticalAlignment="Bottom">
        <Button Command="{Binding PlayCommand}">Play</Button>
        <Button Command="{Binding PauseCommand}">Pause</Button>
        <Button Command="{Binding StopCommand}">Stop</Button>
        <Slider Value="{Binding VideoPosition}" Maximum="{Binding VideoDuration}" />
    </StackPanel>
</Grid>
```

### 6. Platform-Specific Implementation

#### Desktop (Windows/macOS/Linux)
```csharp
#if DESKTOP
public class DesktopAudioService : IAudioService
{
    // NAudio for Windows, native APIs for macOS/Linux
    // Full codec support
    // Hardware acceleration
}
#endif
```

#### Android
```csharp
#if ANDROID
public class AndroidAudioService : IAudioService
{
    // MediaPlayer API
    // Native Android audio codecs
    // Integration with Android audio focus
}
#endif
```

#### iOS
```csharp
#if IOS
public class IOSAudioService : IAudioService
{
    // AVAudioPlayer
    // Native iOS audio frameworks
    // Integration with iOS audio session
}
#endif
```

#### Browser (WebAssembly)
```csharp
#if BROWSER
public class BrowserAudioService : IAudioService
{
    // HTML5 Audio API
    // Web Audio Context
    // Limited codec support
}
#endif
```

### 7. Asset URI Schemes

#### Embedded Assets
```
avares://Kanriya.Client.Avalonia/Assets/Images/logo.png
avares://Kanriya.Client.Avalonia/Assets/Audio/notification.mp3
```

#### Remote Assets
```
https://cdn.kanriya.com/images/profile.jpg
https://media.kanriya.com/videos/tutorial.mp4
```

#### Local Files (Desktop only)
```
file:///Users/user/Pictures/avatar.png
file://C:/Users/user/Documents/video.mp4
```

### 8. Caching Strategy

#### Memory Cache
- Small images (< 1MB)
- Recently accessed media
- Thumbnails and previews

#### Disk Cache
- Large media files
- Offline availability
- User-generated content

#### Cache Management
```csharp
public class MediaCacheManager
{
    // Size limits
    public long MaxMemoryCacheSize { get; set; } = 50 * 1024 * 1024; // 50MB
    public long MaxDiskCacheSize { get; set; } = 500 * 1024 * 1024;   // 500MB
    
    // Cleanup policies
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(7);
    public void CleanupExpired();
    public void CleanupLeastUsed();
}
```

### 9. Integration with ClientEnvironmentConfig

```csharp
public static class ClientEnvironmentConfig
{
    // ... existing code ...
    
    /// <summary>
    /// Media configuration and services
    /// </summary>
    public static class Media
    {
        public static MediaService Service => GetMediaService();
        public static string CacheDirectory => GetMediaCacheDirectory();
        public static void ClearCache() => Service.ClearCache();
        
        // Asset helpers
        public static string GetAssetPath(string relativePath);
        public static Uri GetEmbeddedAssetUri(string relativePath);
    }
}
```

### 10. Usage Examples

#### In ViewModels
```csharp
public class MainViewModel : ViewModelBase
{
    private readonly MediaService _media;
    
    [ObservableProperty]
    private Bitmap? _logoImage;
    
    [ObservableProperty]
    private bool _isLoadingVideo;
    
    public async Task LoadAssetsAsync()
    {
        // Load embedded logo
        LogoImage = await _media.LoadMediaAsync<Bitmap>("avares://Kanriya.Client.Avalonia/Assets/Images/logo.png");
        
        // Load remote video
        IsLoadingVideo = true;
        await _media.VideoService.PlayAsync("https://media.kanriya.com/intro.mp4");
        IsLoadingVideo = false;
        
        // Play notification sound
        await _media.AudioService.PlayAsync("/Assets/Audio/notification.mp3");
    }
}
```

### 11. Error Handling

```csharp
public enum MediaError
{
    NetworkError,
    UnsupportedFormat,
    FileNotFound,
    InsufficientMemory,
    DecodingError,
    PermissionDenied
}

public class MediaException : Exception
{
    public MediaError ErrorType { get; }
    public string Source { get; }
}
```

### 12. Performance Optimizations

#### Lazy Loading
- Load media only when needed
- Progressive image loading (low-res → high-res)
- Thumbnail generation

#### Background Processing
- Preload critical assets
- Background cache warming
- Async decode operations

#### Memory Management
- Automatic disposal of unused media
- Weak references for cached items
- Platform-specific memory pressure handling

## Benefits

- **Unified API**: Same interface across all platforms
- **Performance**: Intelligent caching and async loading  
- **Flexibility**: Support for embedded and remote assets
- **Extensibility**: Easy to add new media types and formats
- **Integration**: Seamless integration with existing Kanriya architecture

## Future Enhancements

- **Streaming Support**: Live audio/video streams
- **Image Processing**: Resize, crop, filters on-the-fly
- **3D Assets**: Support for 3D models and textures
- **AR/VR Media**: Spatial audio and 360° video
- **CDN Integration**: Smart content delivery network usage
- **Offline Sync**: Intelligent media synchronization for offline use