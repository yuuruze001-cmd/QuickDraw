using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuickDraw.Views.Base;

namespace QuickDraw.Views;

public sealed partial class MainTitlebarControl : Base.TitlebarBaseControl
{
    public bool Disabled
    {
        get { return (bool)GetValue(DisabledProperty); }
        set { SetValue(DisabledProperty, value); }
    }
    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled),
        typeof(bool),
        typeof(SlideTitlebarControl),
        new PropertyMetadata(false)
    );

    public MainTitlebarControl()
    {
        this.Resources["WindowCaptionForegroundColor"] = (Application.Current.Resources["WindowCaptionForeground"] as SolidColorBrush)?.Color;
        this.Resources["WindowCaptionForegroundDisabledColor"] = (Application.Current.Resources["WindowCaptionForegroundDisabled"] as SolidColorBrush)?.Color;

        InitializeComponent();
    }
}
