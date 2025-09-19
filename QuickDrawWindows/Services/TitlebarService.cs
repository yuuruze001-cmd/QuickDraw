using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using QuickDraw.Contracts.Services;
using QuickDraw.Utilities;
namespace QuickDraw.Services;

public class TitlebarService : ITitlebarService
{
    private readonly MainWindow _window;
    private readonly AppWindowTitleBar _titlebar;
    private readonly double _scale;

    private readonly GridLength _leftInset;
    private readonly GridLength _rightInset;

    public TitlebarService()
    {
        _window = App.Window;
        var appWindow = _window.AppWindow;
        _titlebar = appWindow.TitleBar;

        _scale = MonitorInfo.GetInvertedScaleAdjustment(_window);

        _leftInset = new(_titlebar.LeftInset * _scale, GridUnitType.Pixel);
        _rightInset = new(_titlebar.RightInset * _scale, GridUnitType.Pixel);
    }

    public AppWindowTitleBar TitleBar => _titlebar;

    public GridLength LeftInset => _leftInset;
    public GridLength RightInset => _rightInset;
}
