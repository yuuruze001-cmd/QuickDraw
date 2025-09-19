using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;
using QuickDraw.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuickDraw.ViewModels;

public partial class MainViewModel : Base.ViewModelWithToolbarBase, INavigationAware
{
    [ObservableProperty]
    public partial double TimerSiderValue { get; set; } = TimerEnum.T2m.ToSliderValue();

    [ObservableProperty]
    public partial ObservableCollection<ImageFolderViewModel> ImageFolderCollection { get; set; }

    private INavigationService _navigationService;
    private ISettingsService _settingsService;

    public MainViewModel(INavigationService navigationService, ISettingsService settingsService, ITitlebarService titlebarService) : base(titlebarService)
    {
        _navigationService = navigationService;
        _settingsService = settingsService;

        ImageFolderCollection = new ObservableCollection<ImageFolderViewModel>(_settingsService.Settings!.ImageFolderList.ImageFolders.Select(f => new ImageFolderViewModel(f)));
    }

    [RelayCommand]
    private void StartSlideShow()
    {
        // TODO: move actual logic here to start this
        _navigationService.NavigateTo(typeof(SlideViewModel).FullName!, null, false, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    public async void OnNavigatedTo(object parameter)
    {
        var delay = TimeSpan.Parse((string)Application.Current.Resources["ControlFastAnimationDuration"]);

        await Task.Delay(delay);

        TitlebarService.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        TitlebarService.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
    }

    public void OnNavigatedFrom() { }
}