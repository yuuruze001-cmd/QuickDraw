using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;
using QuickDraw.Services;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuickDraw.ViewModels;

public partial class SlideViewModel(ITitlebarService titlebarService) : Base.ViewModelWithToolbarBase(titlebarService), INavigationAware
{
    public event EventHandler? InvalidateCanvas;

    [ObservableProperty]
    public partial bool Grayscale { get; set; }

    [RelayCommand]
    private void ToggleGrayscale() => Grayscale = !Grayscale;

    partial void OnGrayscaleChanged(bool oldValue, bool newValue)
    {
        if (oldValue != newValue)
        {
            InvalidateCanvas?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void OnNavigatedTo(object parameter) 
    {
        var delay = TimeSpan.Parse((string)Application.Current.Resources["ControlFastAnimationDuration"]);

        await Task.Delay(delay);

        TitlebarService.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        TitlebarService.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
    }

    public void OnNavigatedFrom() {}
}
