# Kanriya App Icons

This directory contains the complete set of application icons for the Kanriya brand, generated using IconKitchen and optimized for all supported platforms.

## Icon Design

The Kanriya icon features:
- **Modern Design**: Clean, professional appearance suitable for business applications
- **Platform Adaptive**: Follows design guidelines for each platform
- **Scalable**: Works at all sizes from 16x16 to 1024x1024
- **Brand Consistent**: Maintains Kanriya brand identity across platforms

## Directory Structure

```
icons/
├── android/                 # Android Icons
│   ├── play_store_512.png   # Google Play Store icon
│   └── res/                 # Android resource directories
│       ├── mipmap-anydpi-v26/
│       │   └── ic_launcher.xml        # Adaptive icon manifest
│       ├── mipmap-mdpi/               # 48x48px (160dpi)
│       │   ├── ic_launcher.png
│       │   ├── ic_launcher_background.png
│       │   ├── ic_launcher_foreground.png
│       │   └── ic_launcher_monochrome.png
│       ├── mipmap-hdpi/               # 72x72px (240dpi)
│       ├── mipmap-xhdpi/              # 96x96px (320dpi)
│       ├── mipmap-xxhdpi/             # 144x144px (480dpi)
│       └── mipmap-xxxhdpi/            # 192x192px (640dpi)
├── ios/                     # iOS Icons
│   ├── AppIcon-20@2x.png             # 40x40px (Settings)
│   ├── AppIcon-20@3x.png             # 60x60px (Settings)
│   ├── AppIcon-29@2x.png             # 58x58px (Settings)
│   ├── AppIcon-29@3x.png             # 87x87px (Settings)
│   ├── AppIcon-40@2x.png             # 80x80px (Spotlight)
│   ├── AppIcon-40@3x.png             # 120x120px (Spotlight)
│   ├── AppIcon@2x.png                # 120x120px (App)
│   ├── AppIcon@3x.png                # 180x180px (App)
│   ├── AppIcon~ios-marketing.png     # 1024x1024px (App Store)
│   └── Contents.json                 # iOS asset catalog
└── web/                     # Web Icons
    ├── favicon.ico                   # 16x16, 32x32, 48x48
    ├── icon-192.png                  # PWA icon
    ├── icon-192-maskable.png         # PWA maskable icon
    ├── icon-512.png                  # PWA icon
    ├── icon-512-maskable.png         # PWA maskable icon
    ├── apple-touch-icon.png          # 180x180px (iOS Safari)
    └── README.txt                    # Web icon usage notes
```

## Platform Integration

### Android
The Android icons use the adaptive icon system (API 26+) with separate background and foreground layers:
- **Background**: Solid color or subtle pattern
- **Foreground**: Main Kanriya logo/symbol
- **Monochrome**: Single-color version for themed icons

### iOS
Complete iOS icon set covering all required sizes:
- **App Icons**: Main application icons in multiple sizes
- **Settings**: Small icons for Settings app
- **Spotlight**: Medium icons for Spotlight search
- **App Store**: High-resolution icon for App Store listing

### Web/PWA
Web icons optimized for Progressive Web Apps:
- **Favicon**: Traditional browser favicon (ICO format)
- **PWA Icons**: Standard and maskable versions
- **Apple Touch**: iOS Safari home screen icon

## Usage in Avalonia Projects

Icons are automatically copied to the appropriate locations in each platform project:

### Desktop (Windows/macOS/Linux)
```xml
<!-- In .csproj -->
<ApplicationIcon>..\Kanriya.Client.Avalonia\Assets\kanriya-icon.ico</ApplicationIcon>
```

### Android
```xml
<!-- Resources automatically included -->
<AndroidResource Include="Resources\mipmap-*\ic_launcher.png" />
```

### iOS
```xml
<!-- Resources automatically included in bundle -->
<BundleResource Include="Resources\AppIcon*.png" />
```

### Browser
```html
<!-- In index.html -->
<link rel="icon" type="image/x-icon" href="/favicon.ico">
<link rel="icon" type="image/png" sizes="192x192" href="/icon-192.png">
<link rel="apple-touch-icon" href="/apple-touch-icon.png">
```

## Icon Specifications

| Platform | Format | Sizes | Usage |
|----------|--------|-------|--------|
| Android | PNG | 48-192px | Launcher, notifications, settings |
| iOS | PNG | 20-1024px | App icon, settings, App Store |
| Windows | ICO | 16-256px | Taskbar, window chrome, file explorer |
| macOS | ICNS/PNG | 16-1024px | Dock, Finder, app bundle |
| Linux | PNG | 16-512px | Application menu, desktop |
| Web | ICO/PNG | 16-512px | Browser tab, PWA, bookmarks |

## Branding Guidelines

The Kanriya icon should:
- ✅ Be used at appropriate sizes for each platform
- ✅ Maintain aspect ratio (square format)
- ✅ Use original colors without modification
- ✅ Have sufficient padding/margins as designed

The Kanriya icon should NOT:
- ❌ Be stretched or distorted
- ❌ Have colors changed or filters applied
- ❌ Be used with additional text or graphics
- ❌ Be placed on backgrounds that reduce visibility

## License

The Kanriya brand and associated icons are part of the Kanriya project and are licensed under the MIT License for learning and educational purposes.