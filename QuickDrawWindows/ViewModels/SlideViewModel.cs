using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;
using QuickDraw.Services;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace QuickDraw.ViewModels;

public partial class SlideViewModel(ITitlebarService titlebarService, INavigationService navigationService) : Base.ViewModelWithToolbarBase(titlebarService), INavigationAware
{
    public event EventHandler? InvalidateCanvas;

    [ObservableProperty]
    public partial bool Grayscale { get; set; }

    [ObservableProperty]
    public partial Visibility PauseVisibility { get; set; }

    [RelayCommand]
    private void ToggleGrayscale() => Grayscale = !Grayscale;

    [RelayCommand]
    public void GoBack() => navigationService.GoBack();

    partial void OnGrayscaleChanged(bool oldValue, bool newValue)
    {
        if (oldValue != newValue)
        {
            InvalidateCanvas?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void OnNavigatedTo(object parameter) 
    {
        // TODO: toggle pause visibility based on if there is a timer or not
        // eg. if SlideTimerDuration == Models.TimerEnum.NoLimit
        // Might be a better place, eg if we have the view bind one of their initilization events to the VM

        var delay = TimeSpan.Parse((string)Application.Current.Resources["ControlFastAnimationDuration"]);

        await Task.Delay(delay);

        TitlebarService?.TitleBar?.PreferredHeightOption = TitleBarHeightOption.Tall;
        TitlebarService?.TitleBar?.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
    }

    public void OnNavigatedFrom() {}
}
