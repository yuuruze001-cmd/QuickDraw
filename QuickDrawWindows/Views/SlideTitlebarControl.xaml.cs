using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using DependencyPropertyGenerator;

namespace QuickDraw.Views;

[DependencyProperty<bool>("Paused", DefaultValue = false)]
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
}
