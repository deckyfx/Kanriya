using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kanriya.Client.Avalonia.ViewModels;

public partial class MainMenuViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;
    
    [ObservableProperty]
    private bool _isPaneOpen = true;
    
    public MainMenuViewModel()
    {
        // Start with the main menu view
        _currentPage = new HomeViewModel();
    }
    
    [RelayCommand]
    private void NavigateToBuildInfo()
    {
        CurrentPage = new BuildInfoViewModel();
    }
    
    [RelayCommand]
    private void NavigateToTypography()
    {
        CurrentPage = new TypographyViewModel();
    }
    
    [RelayCommand]
    private void NavigateToDisplayImage()
    {
        CurrentPage = new DisplayImageViewModel();
    }
    
    [RelayCommand]
    private void NavigateToHome()
    {
        CurrentPage = new HomeViewModel();
    }
    
    [RelayCommand]
    private void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }
}