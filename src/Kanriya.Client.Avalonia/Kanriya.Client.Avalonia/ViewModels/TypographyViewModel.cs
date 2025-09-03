using CommunityToolkit.Mvvm.ComponentModel;

namespace Kanriya.Client.Avalonia.ViewModels;

public partial class TypographyViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _pageTitle = "Typography Showcase";
    
    public TypographyViewModel()
    {
        // Typography showcase doesn't need much logic, it's mostly in the view
    }
}