using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuickDraw.Contracts.Services;
using QuickDraw.Utilities;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.Graphics;

namespace QuickDraw.Views;

[DependencyProperty<bool>("Paused", DefaultValue = false)]
[DependencyProperty<Visibility>("PauseVisibility", DefaultValue = Visibility.Visible)]
[DependencyProperty<double>("Progress", DefaultValue = 25)]
[DependencyProperty<ICommand>("NextButtonCommand")]
[DependencyProperty<ICommand>("PreviousButtonCommand")]
[DependencyProperty<ICommand>("GrayscaleButtonCommand")]
[DependencyProperty<ICommand>("PauseButtonCommand")]
[DependencyProperty<ICommand>("BackButtonCommand")]
public sealed partial class SlideTitlebarControl : Base.TitlebarBaseControl
{
    public SlideTitlebarControl()
    {
        InitializeComponent();
    }

    protected override RectInt32[] CalculateDragRegions()
    {
        var scale = TitlebarService?.Scale ?? 1.0;
        var backWidth = BackColumn.ActualWidth;
        var centerLeftWidth = CenterLeftColumn.ActualWidth;
        var centerRightWidth = CenterRightColumn.ActualWidth;

        RectInt32 dragRectL = new(
            (int)(LeftInset.Value + backWidth * scale),
            0,
            (int)((centerLeftWidth - backWidth) * scale - LeftInset.Value),
            (int)(ActualHeight * scale)
        );

        RectInt32 dragRectR = new(
            (int)((ActualWidth - centerRightWidth) * scale),
            0,
            (int)(centerRightWidth * scale - RightInset.Value),
            (int)(ActualHeight * scale)
        );

        return [dragRectL, dragRectR];
    }
}
