using System;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kanriya.Client.Avalonia.ViewModels;

public partial class DisplayImageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _pageTitle = "Image Display Test";
    
    [ObservableProperty]
    private string _currentImageUrl = "";
    
    [ObservableProperty]
    private Bitmap? _currentBitmap;
    
    public ObservableCollection<ImageTestItem> ImageTests { get; }
    
    public DisplayImageViewModel()
    {
        ImageTests = new ObservableCollection<ImageTestItem>
        {
            // Embedded PNG images
            new ImageTestItem 
            { 
                Name = "Embedded PNG - Kanriya Logo",
                Type = "PNG",
                Source = "Embedded",
                Path = "avares://Kanriya.Client.Avalonia/Assets/Icons/kanriya-logo.png"
            },
            new ImageTestItem 
            { 
                Name = "Embedded PNG - Gradient Test",
                Type = "PNG",
                Source = "Embedded",
                Path = "avares://Kanriya.Client.Avalonia/Assets/Icons/test-gradient.png"
            },
            new ImageTestItem 
            { 
                Name = "Embedded PNG - Circles Test",
                Type = "PNG",
                Source = "Embedded",
                Path = "avares://Kanriya.Client.Avalonia/Assets/Icons/test-circles.png"
            },
            new ImageTestItem 
            { 
                Name = "Embedded PNG - Hexagon (Transparent)",
                Type = "PNG",
                Source = "Embedded",
                Path = "avares://Kanriya.Client.Avalonia/Assets/Icons/test-hexagon.png"
            },
            
            // Embedded SVG images
            new ImageTestItem 
            { 
                Name = "Embedded SVG - Wave Design",
                Type = "SVG",
                Source = "Embedded",
                Path = "avares://Kanriya.Client.Avalonia/Assets/Icons/test-wave.svg"
            },
            new ImageTestItem 
            { 
                Name = "Embedded SVG - Logo Design",
                Type = "SVG",
                Source = "Embedded",
                Path = "avares://Kanriya.Client.Avalonia/Assets/Icons/test-logo.svg"
            },
            new ImageTestItem 
            { 
                Name = "Embedded SVG - Animated",
                Type = "SVG",
                Source = "Embedded",
                Path = "avares://Kanriya.Client.Avalonia/Assets/Icons/test-animated.svg"
            },
            
            // Remote images
            new ImageTestItem 
            { 
                Name = "Remote PNG - Placeholder",
                Type = "PNG",
                Source = "Remote",
                Path = "https://via.placeholder.com/300x200/667eea/ffffff?text=Kanriya+PNG"
            },
            new ImageTestItem 
            { 
                Name = "Remote SVG - Placeholder",
                Type = "SVG",
                Source = "Remote",
                Path = "https://via.placeholder.com/300x200/764ba2/ffffff?text=Kanriya+SVG"
            },
            new ImageTestItem 
            { 
                Name = "Remote Image - Avalonia Logo",
                Type = "SVG",
                Source = "Remote",
                Path = "https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Dialogs/Assets/avalonia-logo.ico"
            },
            new ImageTestItem 
            { 
                Name = "Remote PNG - Sample Photo",
                Type = "PNG",
                Source = "Remote",
                Path = "https://picsum.photos/400/300"
            }
        };
    }
    
    [RelayCommand]
    private void LoadImage(ImageTestItem? item)
    {
        if (item == null) return;
        
        CurrentImageUrl = item.Path;
        
        // For embedded resources, we can load them directly
        if (item.Source == "Embedded")
        {
            try
            {
                var uri = new Uri(item.Path);
                // The Image control in the view will handle loading from the URI
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load embedded image: {ex.Message}");
            }
        }
    }
}

public class ImageTestItem
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Source { get; set; } = "";
    public string Path { get; set; } = "";
    
    public string Description => $"{Type} ({Source})";
}