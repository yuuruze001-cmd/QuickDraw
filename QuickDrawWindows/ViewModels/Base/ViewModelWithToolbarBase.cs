using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;

namespace QuickDraw.ViewModels.Base;

public partial class ViewModelWithToolbarBase(ITitlebarService titlebarService) : ObservableObject
{
    protected ITitlebarService TitlebarService = titlebarService;

    public GridLength TitlebarLeftInset => TitlebarService.LeftInset;
    public  GridLength TitlebarRightInset => TitlebarService.RightInset;
}
