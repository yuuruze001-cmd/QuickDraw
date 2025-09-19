using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;
using QuickDraw.Views.Base;
using System.Diagnostics;

namespace QuickDraw.ViewModels.Base;

public partial class ViewModelWithToolbarBase : ObservableObject
{
    public ITitlebarService TitlebarService;

    public GridLength TitlebarLeftInset => TitlebarService.LeftInset;
    public  GridLength TitlebarRightInset => TitlebarService.RightInset;

    [ObservableProperty]
    public partial bool TitlebarInactive { get; set; }

    public ViewModelWithToolbarBase(ITitlebarService titlebarService)
    {
        TitlebarService = titlebarService;
        App.Window.Activated += Window_Activated;
    }

    public void HandleDragRegionsChanged(object sender, DragRegionsChangedEventArgs args)
    {
        TitlebarService?.TitleBar?.SetDragRectangles(args.DragRegions);
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            TitlebarInactive = true;
        }
        else
        {
            TitlebarInactive = false;
        }
    }
}
