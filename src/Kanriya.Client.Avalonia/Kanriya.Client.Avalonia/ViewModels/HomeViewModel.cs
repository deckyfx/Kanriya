using CommunityToolkit.Mvvm.ComponentModel;
using Kanriya.Shared.Services;

namespace Kanriya.Client.Avalonia.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly LocalizationService _localization;
    
    [ObservableProperty]
    private string _welcomeMessage;
    
    [ObservableProperty]
    private string _description;
    
    public HomeViewModel()
    {
        _localization = LocalizationService.Instance;
        _welcomeMessage = _localization.t("app.welcome");
        _description = "Select an option from the menu to explore different features of the Kanriya Client.";
    }
}