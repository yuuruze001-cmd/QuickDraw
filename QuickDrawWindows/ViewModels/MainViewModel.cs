using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickDraw.Contracts.Services;
using QuickDraw.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace QuickDraw.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial double TimerSiderValue { get; set; } = TimerEnum.T2m.ToSliderValue();

    [ObservableProperty]
    public partial ObservableCollection<ImageFolderViewModel> ImageFolderCollection { get; set; }

    public ICommand StartSlideShowCommand { get; }

    private INavigationService _navigationService;
    private ISettingsService _settingsService;

    public MainViewModel(INavigationService navigationService, ISettingsService settingsService)
    {
        _navigationService = navigationService;
        _settingsService = settingsService;

        StartSlideShowCommand = new RelayCommand(StartSlideShow);

        ImageFolderCollection = new ObservableCollection<ImageFolderViewModel>(_settingsService.Settings!.ImageFolderList.ImageFolders.Select(f => new ImageFolderViewModel(f)));
    }

    private void StartSlideShow()
    {
        // TODO: move actual logic here to start this
        _navigationService.NavigateTo(typeof(SlideViewModel).FullName!);
    }
}